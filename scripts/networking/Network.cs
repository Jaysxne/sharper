using Godot;
using Nakama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharper
{
    public partial class Network : Node
    {
        public override void _EnterTree()
        {
            base._EnterTree();
            _node = this;
           
            
            
        }


        public static Network _node; // THIS IS AN AUTOLAOD SCRIPT AND MOST MEMBERS ARE STATIC SO EXPOSE PROXY NODE FOR INSTANCE METHODS
        public static Network Get() { return _node; }
        [Export]
        public int PeerID { get { return _node.Multiplayer.GetUniqueId(); } private set { return; } }
        
        // Configs //////////////////////////////////////////
        public static bool isServer = false;
        public static string server_key = "defaultkey";
        
        public static string host = "45.61.166.39";
        public static int port = 7350;
        public static string scheme = "http";
        ////////////////////////////////////////////////////////////
       

        /// NETWORK MEMBERS ////////////////////////////////////////
        public static Nakama.Client client;
        public static ISocket Socket { get; private set; }
        public static ISession session;
       
        public static NakamaMultiplayerPeer NakamaMultiplayerPeer = new NakamaMultiplayerPeer();
        public static NakamaMultiplayerBridge bridge { get; private set; }

        //////////////////////////////////////////////////////////////////////////////////////////


        /////////////////////////------Client----------- ///////////////////////////////////////
        public static async Task<(ISession,string errorMsg )> CreateClient(string email,string password, string username = "")
        {
            try
            {
                client = new Nakama.Client(scheme, host, port, server_key);
                client.ReceivedSessionUpdated += OnClientSessionUpdated;
                session = await client.AuthenticateEmailAsync(email, password, username: username, (username != ""));



                Socket = Nakama.Socket.From(client);
                Socket.Connected += OnSocketConnected;
                Socket.Closed += OnSocketClosed;
                Socket.ReceivedError += OnSocketRecievedError;
                await Socket.ConnectAsync(session);

                bridge = new NakamaMultiplayerBridge(Socket as Nakama.Socket);
                _node.GetTree().GetMultiplayer().MultiplayerPeer = bridge.MultiplayerPeer;
                GD.Print("Done Client");

                return (session,null);

            }
            catch (ApiResponseException e)
            {

                GD.PushError(e.ToString());
                return (null,e.Message);
            }
           
 
        }

        /////////////////////////------SERVER----------- ///////////////////////////////////////
        public async static void CreateServer()
        {
            GD.Print("Creating Server");
            client = new Nakama.Client(scheme, host, port, server_key);
            client.ReceivedSessionUpdated += OnClientSessionUpdated;

            try
            {

                session = await client.AuthenticateDeviceAsync(OS.GetUniqueId(), create: true, username: "Server1");
                GD.Print("session created");

                Socket = Nakama.Socket.From(client);
                GD.Print("creating socket");
                Socket.Connected += OnSocketConnected;
                Socket.Closed += OnSocketClosed;
                Socket.ReceivedError += OnSocketRecievedError;
                await Socket.ConnectAsync(session);

                bridge = new NakamaMultiplayerBridge(Socket as Nakama.Socket);
                _node.GetTree().GetMultiplayer().MultiplayerPeer = bridge.MultiplayerPeer;
                bridge.CreateMatch("server1");
                GD.Print("Done");

            }
            catch (Nakama.ApiResponseException e)
            {

                GD.PushWarning(e.ToString());
            }

        }

        static private void OnClientSessionUpdated(ISession session)
        {
            GD.Print("Connected to Session!");
        }


        private static void OnSocketClosed()
        {
            GD.Print("Socket Closed");
        }

        private static void OnSocketRecievedError(Exception exception)
        {
            Nakama.ApiResponseException e = exception as Nakama.ApiResponseException;  
            GD.PushError(e.ToString());
            
        }

        private static void OnSocketConnected()
        {
            GD.Print("Socket Connected!");
        }
    }
}
