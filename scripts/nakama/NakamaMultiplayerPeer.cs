using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Godot.MultiplayerPeer;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;
using Godot.NativeInterop;
using System.Reflection.Metadata.Ecma335;

namespace Sharper
{
	public partial class NakamaMultiplayerPeer : MultiplayerPeerExtension
	{
		const int MAX_PACKET_SIZE = 1 << 24;

		int _self_id = 0;
		public ConnectionStatus _connection_status = ConnectionStatus.Disconnected;
		bool _refusing_new_connections = false;
		int _target_id = 0;

		partial class Packet : RefCounted
		{
			public Packet(byte[] p_data, int p_from)
			{
				data = p_data;
				from = p_from;
			}
			public byte[] data;
			public int from;

		}
		



		Queue<Packet> _incoming_packets = [];

		[Signal]
		public delegate void packet_generatedEventHandler(int peer_id, byte[] buffer);

		
        public byte[] _get_packet_script()
		{
			if (_incoming_packets.Count == 0)
				return new byte[] { };

			return _incoming_packets.Dequeue().data;

		}


        public int _get_packet_mode() => (int)MultiplayerPeer.TransferModeEnum.Reliable;


        public int _get_packet_channel() => 0;



        public Godot.Error _put_packet_script(byte[] p_bufffer)
		{
			EmitSignal(SignalName.packet_generated, p_bufffer);
			return Godot.Error.Ok;
		}


        public int _get_available_packet_count() => _incoming_packets.Count;


        public int _get_max_packet_size() => MAX_PACKET_SIZE;

        public void _set_transfer_channel(int p_channel)
		{
			return;
		}


        public int _get_transfer_channel() => 0;

        public void _set_transfer_mode(MultiplayerPeer.TransferModeEnum p_mode)
		{
			return;
		}


        public int _get_transfer_mode() => (int)MultiplayerPeer.TransferModeEnum.Reliable;

        public void _set_target_peer(int p_peer_id)
		{
			_target_id = p_peer_id;

		}


		public int _get_packet_peer()
		{
			if (_connection_status != ConnectionStatus.Connected)
			{
				return 1;

			}


			if (_incoming_packets.Count == 0) { return 1; }


			return _incoming_packets.Peek().from;

		}


		public bool _is_server() => _self_id == 1;


		void _poll()
		{
			return;
		}


		public int _get_unique_id() => _self_id;


		public void _set_refuse_new_connections(bool p_enable) => _refusing_new_connections = p_enable;



		public bool _is_refusing_new_connections() => _refusing_new_connections;

		public ConnectionStatus _get_connection_status() => _connection_status;

		public void initialize(int p_self_id)
		{
			if (_connection_status != ConnectionStatus.Connecting) { return; }

			_self_id = p_self_id;

			if (_self_id == 1) { _connection_status = ConnectionStatus.Disconnected; }


		}


		public void set_connection_status(int p_connection_status) => _connection_status = (ConnectionStatus)p_connection_status;



		public void deliver_packet(byte[] p_data , int p_from_peer_id)
		{
            Packet packet = new Packet(p_data, p_from_peer_id);
			_incoming_packets.Enqueue(packet);

        }
		

	}
}
