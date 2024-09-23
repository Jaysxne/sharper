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
        
        //Configs //////////////////////////////
        public static bool isServer = false;
        public static string server_key = "defaultkey";
        public static string host = "localhost";
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


        public async static void CreateClient()
        {
            client = new Nakama.Client(scheme, host, port, server_key);
            client.ReceivedSessionUpdated += OnClientSessionUpdated;

            session = await client.AuthenticateDeviceAsync(OS.GetUniqueId(),create:true,username:"Jaysinxe");

            Socket = Nakama.Socket.From(client);
            Socket.Connected += OnSocketConnected;
            Socket.Closed += OnSocketClosed;
            Socket.ReceivedError += OnSocketRecievedError;
            await Socket.ConnectAsync(session);
            bridge = new NakamaMultiplayerBridge(Socket as Nakama.Socket);
            _node.GetTree().GetMultiplayer().MultiplayerPeer = bridge.MultiplayerPeer;
            bridge.JoinMatch("6c76dab6-72c6-534b-88ac-b14d253760b9.");
            
            GD.Print("Done Client");
            
            
            

            
 
        }

        public async static void CreateServer()
        {
            client = new Nakama.Client(scheme, host, port, server_key);
            client.ReceivedSessionUpdated += OnClientSessionUpdated;

            session = await client.AuthenticateDeviceAsync(OS.GetUniqueId(), create: true, username: "Server1");

            Socket = Nakama.Socket.From(client);
            Socket.Connected += OnSocketConnected;
            Socket.Closed += OnSocketClosed;
            Socket.ReceivedError += OnSocketRecievedError;
            await Socket.ConnectAsync(session);
            bridge = new NakamaMultiplayerBridge(Socket as Nakama.Socket);
            _node.GetTree().GetMultiplayer().MultiplayerPeer = bridge.MultiplayerPeer;
            bridge.CreateMatch("server1");
            GD.Print("Done");






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
            GD.PushError(exception.Message + isServer);
            
        }

        private static void OnSocketConnected()
        {
            GD.Print("Socket Connected!");
        }
    }
}
