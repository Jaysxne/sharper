using Godot;
using Nakama;
using Nakama.TinyJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sharper
{
     public partial class NakamaMultiplayerBridge : RefCounted
    {
        // ENUMS
       public enum EMatchState
        {
            DISCONNECTED, JOINING, CONNECTED, SOCKET_CLOSED
        }
       public enum EMetaMessageType { CLAIM_HOST, ASSIGN_PEER_ID}

        /////////////////////////////////////////////////
        
        /// READONLYS
        public Nakama.Socket NakamaSocket { get; private set; }

        public EMatchState MatchState { get; private set; } = EMatchState.DISCONNECTED;
        public String MatchID { get; private set; } = "";

        public NakamaMultiplayerPeer MultiplayerPeer { get; private set; }

        ///////////////////// 

        // Custom CONFIGS??
        int metaOpCode = 9001;
        int rpcOPCode = 9002;


        public partial class User : RefCounted
        {
            public Nakama.IUserPresence presence;
            public int peerID = 0;

           public User(IUserPresence p_presence)
            {
                presence = p_presence;
            }

        }

        /// Internals

        private string mySessionID;
        private int myPeerID = 0;
        private Dictionary<int, string> idSessionMap = new Dictionary<int, string>();
        private Dictionary<string,User> sessionUserMap = new Dictionary<string, User>();
        private string matchmakerTicket = "";

        [Signal]
        public delegate void MatchJoinedErrorEventHandler(string e);


        [Signal]
        public delegate void MatchLostEventHandler();

        [Signal]
        public delegate void MatchJoinedEventHandler();


       public NakamaMultiplayerBridge (Nakama.Socket pNakamaSocet)
        {
            GD.Print("Bridge Created!");
            NakamaSocket = pNakamaSocet;
            MultiplayerPeer =  new NakamaMultiplayerPeer();
            NakamaSocket.ReceivedMatchPresence += this.OnNakamaSocketRecievedMatchPresence;
            NakamaSocket.ReceivedMatchmakerMatched += this.OnNakamaSocketRecievedMatchmakerMatch;
            NakamaSocket.ReceivedMatchState += this.OnNakamaSocketRecievedMatchState;
            NakamaSocket.Closed += this.OnNakamaSocketClosed;

            MultiplayerPeer.packet_generated += this.OnMultiplayerPeerPacketGenerated;
            MultiplayerPeer.set_connection_status((int)Godot.MultiplayerPeer.ConnectionStatus.Connecting);
        }

        public async void CreateMatch(string serverName)
        {
            if (MatchState != EMatchState.DISCONNECTED)
            {
                GD.PushError($"Cannot create match when state is {MatchState.ToString()}");
                return;
            }
            MatchState = EMatchState.JOINING;
            MultiplayerPeer.set_connection_status((int)Godot.MultiplayerPeer.ConnectionStatus.Connecting);

            try
            {
                
                IMatch match = await NakamaSocket.CreateMatchAsync(serverName);
                SetupMatch(match);
                SetupHost();
                GD.Print($"MATCH CREATED! : {match.Id}");
            }
            catch (Nakama.ApiResponseException e)
            {

                EmitSignal(SignalName.MatchJoinedError, e.ToString());
                Leave();
            }
           
        }
        
        public async void JoinMatch(string pMatchID)
        {
            if (MatchState != EMatchState.DISCONNECTED)
            {
                GD.PushError($"Cannot join match when state is {MatchState.ToString()}");
                return;
            }

            MatchState= EMatchState.JOINING;
            MultiplayerPeer.set_connection_status((int)Godot.MultiplayerPeer.ConnectionStatus.Connecting);

            try
            {
                IMatch joinedMatch = await NakamaSocket.JoinMatchAsync(pMatchID);
                SetupMatch(joinedMatch);
                

            }
            catch (Nakama.ApiResponseException e)
            {

                EmitSignal(SignalName.MatchJoinedError,e.ToString());
                Leave();
                return;
            }
        }

        public void SetupHost()
        {
            myPeerID = 1;
            MapIDToSession(1,mySessionID);
            MatchState = EMatchState.CONNECTED;
            MultiplayerPeer.initialize(myPeerID); // Bridges Bridge Peer ID TO PEER ID
        }

        public async void HostAddPeer(IUserPresence presence)
        {
            int peerID = GenerateID(presence.SessionId);
            MapIDToSession(peerID,presence.SessionId);

            Dictionary<string, dynamic> message = new Dictionary<string, dynamic>();
            message.Add("type", (int)EMetaMessageType.CLAIM_HOST);
            message.Add("session_id", mySessionID);


            try
            {
                await NakamaSocket.SendMatchStateAsync(MatchID, metaOpCode, JsonSerializer.Serialize(message), new IUserPresence[] { presence });

                foreach (int otherPeerId in idSessionMap.Keys)
                {
                    string otherSessionId = idSessionMap[otherPeerId];
                    if (otherSessionId == presence.SessionId || otherSessionId == mySessionID)
                    {
                        continue;
                    }
                    Dictionary<string,dynamic> otherPeerMessage = new Dictionary<string,dynamic>();
                    otherPeerMessage.Add("type", (int)EMetaMessageType.ASSIGN_PEER_ID);
                    otherPeerMessage.Add("session_id", otherSessionId);
                    otherPeerMessage.Add("peer_id", otherPeerId);
                    await NakamaSocket.SendMatchStateAsync(MatchID, metaOpCode, JsonSerializer.Serialize(otherPeerMessage), new IUserPresence[] { presence });
                }

                Dictionary<string, dynamic> announceMessage = new Dictionary<string,dynamic>();
                announceMessage.Add("type", (int)EMetaMessageType.ASSIGN_PEER_ID);
                announceMessage.Add("session_id",presence.SessionId);
                announceMessage.Add("peer_id",peerID);
                await NakamaSocket.SendMatchStateAsync(MatchID, metaOpCode, JsonSerializer.Serialize(announceMessage));
                MultiplayerPeer.EmitSignal(NakamaMultiplayerPeer.SignalName.PeerConnected, peerID);
            }
            catch (Nakama.ApiResponseException e)
            {

                GD.PushError(e.Message);
            }
            

        }


        public void MapIDToSession(int peerID, string sessionID)
        {
            idSessionMap[peerID] = sessionID;
            sessionUserMap[sessionID].peerID = peerID;
         
        }

        public void SetupMatch(IMatch match)
        {
            MatchID = match.Id;
            mySessionID = match.Self.SessionId;
            sessionUserMap[mySessionID] = new User(match.Self);

            foreach (IUserPresence presence in match.Presences)
            {
                if (!sessionUserMap.ContainsKey(presence.SessionId))
                {
                    sessionUserMap[presence.SessionId] = new User(presence); 
                }
            }
        }


        public IUserPresence GetUserPresenceForPeer(int peerID)
        {
            string sessionID = null;
            idSessionMap.TryGetValue(peerID, out sessionID);

            if (sessionID == null) { return null; }

            User user = sessionUserMap[sessionID];
            if (user == null) { return null; }

            return user.presence;
        }

        public async void Leave()
        {
            if (MatchState == EMatchState.DISCONNECTED) { return; }

            MatchState = EMatchState.DISCONNECTED;

            if (MatchID != String.Empty)
            {
                try
                {
                    await NakamaSocket.LeaveMatchAsync(MatchID);
                }
                catch (Exception e)
                {

                   GD.PushError($"Error: {e.Message}");
                }
            }

            if (matchmakerTicket != null)
            {
                try
                {
                    await NakamaSocket.RemoveMatchmakerAsync(matchmakerTicket);
                }
                catch (Exception e)
                {

                    GD.PushError($"Error: {e.Message}");
                }
            }

            Cleanup();
            

        }

        public int GenerateID(string pSessionID)
        {
            int peerID = pSessionID.GetHashCode() & 0x7FFFFFFF;
            while (peerID <= 1 || idSessionMap.ContainsKey(peerID))
            {
                GD.PushWarning("BRIDGE: generating new PeerID");
                peerID++;
                if (peerID > 0x7FFFFFFF || peerID <= 0) { peerID =  (int)GD.Randi() & 0x7FFFFFFF; }
            }
            return peerID;
        }

        public void Cleanup()
        {
            foreach (int peerID in idSessionMap.Keys)
            {
               MultiplayerPeer.EmitSignal(NakamaMultiplayerPeer.SignalName.PeerDisconnected, peerID);
            }

            MatchID = string.Empty;
            matchmakerTicket = string.Empty;
            mySessionID = string.Empty;
            myPeerID = 0;
            idSessionMap.Clear();
            sessionUserMap.Clear();

            MultiplayerPeer.set_connection_status((int)Godot.MultiplayerPeer.ConnectionStatus.Disconnected);
        }

        public void OnMultiplayerPeerPacketGenerated(int peer_id, byte[] buffer)
        {
           
            throw new NotImplementedException();   

        }

        public void OnNakamaSocketClosed()
        {
            MatchState = EMatchState.SOCKET_CLOSED;
            Cleanup();
        }

        public void OnNakamaSocketRecievedMatchState(IMatchState state)
        {
            if (MatchState == EMatchState.DISCONNECTED || state.MatchId != MatchID) { return; }

            if (state.OpCode == (int)metaOpCode)
            {
                var stateAsString = Encoding.UTF8.GetString(state.State);
                Dictionary<string, dynamic> stateData = parseStateData(stateAsString);
               
                JsonElement jelement = stateData["type"];
                EMetaMessageType type = (EMetaMessageType)jelement.Deserialize<int>();
               

             
                

                if (type == EMetaMessageType.CLAIM_HOST)
                {
                  
                    if (idSessionMap.ContainsKey(1) ) { GD.PushError($"User {state.UserPresence.Username} is claiming to hose when user {idSessionMap[1]} has already claimed it!"); }
                    else { MapIDToSession(1, state.UserPresence.SessionId); }
                    return;
                }

                if (state.UserPresence.SessionId != idSessionMap[1])
                {
                    GD.PushError($"Recieved meta message from user who isn't the host: {sessionUserMap[state.UserPresence.SessionId]}");
                    return;
                }

                if (type == EMetaMessageType.ASSIGN_PEER_ID)
                {
                    JsonElement sessionIDJElement = stateData["session_id"];
                    JsonElement peerIDJElement = stateData["peer_id"];
                    string sessionID = sessionIDJElement.Deserialize<string>();
                    int peerId = peerIDJElement.Deserialize<int>();

                    if (sessionUserMap.ContainsKey(sessionID) && sessionUserMap[sessionID].peerID != 0)
                    {
                        GD.PushError($"Attempting to assign peer id {peerId} to {sessionID} which already has peerID: {sessionUserMap[sessionID].peerID}");
                        return;
                    }

                    MapIDToSession(peerId, sessionID);
                    if (mySessionID == sessionID)
                    {
                        MatchState = EMatchState.CONNECTED;
                        MultiplayerPeer.initialize(peerId);
                        MultiplayerPeer.set_connection_status((int)Godot.MultiplayerPeer.ConnectionStatus.Connected);
                        EmitSignal(SignalName.MatchJoined);
                        MultiplayerPeer.EmitSignal(NakamaMultiplayerPeer.SignalName.PeerConnected, 1);
                    }
                    else { MultiplayerPeer.EmitSignal(NakamaMultiplayerPeer.SignalName.PeerConnected, peerId); }
                }
                else { NakamaSocket.Logger.ErrorFormat($"Recieved meta message with unknown type: {type.ToString()}"); }

            }
                
            

            


        }
        
        public Dictionary<string,dynamic> parseStateData(string stringStateData)
        {
            Dictionary<string,dynamic> content = JsonSerializer.Deserialize<Dictionary<string,dynamic>>(stringStateData);
            if (content == null)
            {
                GD.PushError("BRIDGE: Recieved null state data! Thrown from BRIDGE PARSER");
                return null; 
            }
           
            return content;
        }

        public void OnNakamaSocketRecievedMatchmakerMatch(IMatchmakerMatched matched)
        {
            throw new NotImplementedException();
        }

        public void OnNakamaSocketRecievedMatchPresence(IMatchPresenceEvent @event)
        {
            if (MatchState == EMatchState.DISCONNECTED || @event.MatchId != MatchID) { return; }

            foreach (IUserPresence presence in @event.Joins)
            {
                if (!sessionUserMap.ContainsKey(presence.SessionId)) { sessionUserMap[presence.SessionId] = new User(presence); }

                if (myPeerID == 1 && sessionUserMap[presence.SessionId].peerID == 0) { HostAddPeer(presence); }

            }

            foreach (IUserPresence presence in @event.Leaves)
            {
                if (!sessionUserMap.ContainsKey(presence.SessionId)) { continue; }

                int peerID = sessionUserMap[presence.SessionId].peerID;

                MultiplayerPeer.EmitSignal(NakamaMultiplayerPeer.SignalName.PeerDisconnected, peerID);
                sessionUserMap.Remove(presence.SessionId);
                idSessionMap.Remove(peerID);
            }

        }
    }
}
