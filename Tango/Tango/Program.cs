using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using Vsync;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using Helpers;

namespace Tango
{
	//Will be responsible for storing rooms 
	public class Room
	{
		public string room_name;
		public Dictionary<string, string> playerLocs;
		public List<string> players;
		//Put anything else in here

		public Room (string name)
		{
			room_name = name;
			playerLocs = new Dictionary<string, string> ();
			players = new List<string> ();
		}
	}




	class MainClass
	{
		//GROUP PARAMS
		public const int UPDATE = 0;
		public const int LOOKUP = 1;
		public const int REFRESH = 2;
		public const int LEAVE_ROOM = 3;
		public const int JOIN_ROOM = 4;

		//MAX IN ROOM ALLOWANCE
		public const int MAX_IN_ROOM = 5;

		public static Room currentRoom;
		private static NetworkStream stream;
		public static TcpClient client;
		public static Vsync.Group roomGroup;
		public static String ID;

		//Whether the client has made contact recently
		public static Boolean still_alive = false;
		public const int CLIENT_TIMEOUT = 5000;
		public const int UPDATE_RATE = 50;

		//System Timers
		public static System.Timers.Timer send_timer;
		public static System.Timers.Timer is_alive_timer;


		//checks to see if the message wants you to change rooms
		public static bool isChangeRoomMsg(String msg) {
			if (msg.StartsWith("changeRoom:")) {
				return true;
			}
			return false;
		}

		public static bool isUpdateCoordinates(String msg) {
			return false;
		}

		public static void send_info(Object source, System.Timers.ElapsedEventArgs e) {
			try{
				string info;
				lock (currentRoom) {
					info = Extensions.FromDictionaryToJson(currentRoom.playerLocs);
				}
				Byte[] r = Encoding.UTF8.GetBytes(info);
				Byte[] resp = Helpers.Connections.encode(r);
				try {
					stream.Write(resp, 0, resp.Length);
				}
				catch (Exception exception) {
					Console.WriteLine("Caught an exception in send_info, pretty much just ignoring. It was:");
					Console.WriteLine(exception.ToString());
				}
			}
			catch(Exception err){
				//Fail Loudly
				Console.WriteLine(err);
			}
		}

		/*public static void is_alive(Object source, System.Timers.ElapsedEventArgs e) {
			Console.WriteLine ("IN IS_ALIVE METHOD");
			if (still_alive) {
				still_alive = false;
			} else {
				Console.WriteLine ("CLOSING CONNECTION BECAUSE NO RESPONSE");
				roomGroup.OrderedSend (LEAVE_ROOM, ID);
				send_timer.Stop ();
				is_alive_timer.Stop ();
				client.Close ();
				roomGroup.Leave ();
			}
			
		}*/

		public static void Main (string[] args)
		{
			String roomname = "no roomname given";
			Console.WriteLine ("THE SIZE OF ARGS IS " + args.Length);
			if (args.Length < 2) {
				Console.WriteLine ("There are less than 2 args given.  This is fine for now, but in the future we need to give the room name.");
			} else {
				roomname = args [1];
				int max_port = 65000;
				int min_port = 15000;
				int port_diff = max_port - min_port;
				int room_hash = roomname.GetHashCode ();
				room_hash = room_hash % port_diff;
				room_hash = room_hash + port_diff;
				room_hash = room_hash % port_diff;
				Console.WriteLine ("Mod is " + room_hash);
				room_hash += min_port;
				Vsync.Vsync.VSYNC_GROUPPORT = room_hash;
				Console.WriteLine ("VSYNC PORT NO IS " + room_hash);
			}
				
			currentRoom = new Room (roomname);
			ID = Path.GetRandomFileName ().Replace (".", "");
			Console.WriteLine ("User ID IS " + ID);

			//Set up TCP Listener
			Console.WriteLine (args [0]);
			int port = Int32.Parse (args [0]);
			TcpListener server = new TcpListener (port);
			server.Start ();
			Console.WriteLine ("Server has started on 127.0.0.1:{0}.\nWaiting for a connection...", port);
			client = server.AcceptTcpClient ();
			stream = client.GetStream ();
			Console.WriteLine ("Matched with a client! Now Starting VSYNC");

			VsyncSystem.Start ();
			Console.WriteLine ("VSYNC STARTED");
			String groupName = "TEST ROOM";

			roomGroup = new Vsync.Group (groupName);
			roomGroup.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine ("New View: " + v);
				Console.Title = "View " + v.viewid + ", my rank=" + v.GetMyRank ();
			};
			roomGroup.Handlers [UPDATE] += (Action<string,string>)delegate(string username, string val) {
				lock (currentRoom) {
					currentRoom.playerLocs [username] = val;
				}
			};
			roomGroup.Handlers [LOOKUP] += (Action<string>)delegate(string s) {
				//VsyncSystem.WriteLine("IN LOOKUP");
				lock (currentRoom) {
					roomGroup.Reply (currentRoom.playerLocs [s]);
				}
			};
			roomGroup.Handlers [REFRESH] += (Action)delegate() {
				lock (currentRoom) {
					string reply = Extensions.FromDictionaryToJson (currentRoom.playerLocs);
					roomGroup.Reply (reply);
				}
			};
			roomGroup.Handlers [JOIN_ROOM] += (Action<string>)delegate(string username) {
				lock (currentRoom) {
					if (currentRoom.players.Count >= MAX_IN_ROOM) {
						roomGroup.Reply ("FULL");
					} else {
						currentRoom.players.Add (username);
						roomGroup.Reply ("JOINED");
					}
				}
			};
			roomGroup.Handlers [LEAVE_ROOM] += (Action<string>)delegate(string s) {
				Console.WriteLine("IN LEAVE ROOM");
				lock (currentRoom) {
					currentRoom.playerLocs.Remove (s);
					currentRoom.players.Remove(s);
				}
			};
			roomGroup.Join ();
			Console.WriteLine ("Room Group Joined");
			Console.WriteLine (groupName);

			List<String> results = new List<String> ();
			roomGroup.OrderedQuery (Vsync.Group.ALL, JOIN_ROOM, ID, new Vsync.EOLMarker (), results); 
			Boolean full = false;
			for (int j = 0; j < results.Count; j++) {
				if (results [j].Equals ("FULL")) {
					Console.WriteLine ("FULL");
					full = true;
				}
			}
			if (full) {
				roomGroup.OrderedSend (LEAVE_ROOM, ID);
				Byte[] r = Encoding.UTF8.GetBytes ("ROOM FULL");
				Byte[] resp = Helpers.Connections.encode (r);
				stream.Write (resp, 0, resp.Length);
				client.Close ();
				Console.WriteLine ("LEAVING ROOM");
				roomGroup.Leave ();
				return;
			}
								
			send_timer = new System.Timers.Timer ();
			send_timer.Elapsed += send_info;
			send_timer.Interval = UPDATE_RATE;
			send_timer.Start ();

			/*is_alive_timer = new System.Timers.Timer ();
			is_alive_timer.Elapsed += is_alive;
			is_alive_timer.Interval = CLIENT_TIMEOUT;
			is_alive_timer.Start ();*/

			Byte[] bytes = new Byte[4096];
			String data = null;

			int i = Helpers.Connections.checkRead (stream, bytes);
			//Continuously read from the buffer while the connection is open 
			while (i != 0) {
				data = System.Text.Encoding.ASCII.GetString (bytes, 0, i);
				//First request -> Respond with HTTP Change Protocol Handshake 
				if (new Regex ("^GET").IsMatch (data)) {
					Byte[] response = Encoding.UTF8.GetBytes ("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
					                  + "Connection: Upgrade" + Environment.NewLine
					                  + "Upgrade: websocket" + Environment.NewLine
					                  + "Sec-WebSocket-Accept: " + Convert.ToBase64String (
						                  SHA1.Create ().ComputeHash (
							                  Encoding.UTF8.GetBytes (
								                  new Regex ("Sec-WebSocket-Key: (.*)").Match (data).Groups [1].Value.Trim () + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
							                  )
						                  )
					                  ) + Environment.NewLine
					                  + Environment.NewLine);

					stream.Write (response, 0, response.Length);

					//Generate and send user ID
					Byte[] q = Encoding.UTF8.GetBytes (ID);
					//Byte[] resp = Helpers.Connections.encode(q);
					Byte[] resp = Helpers.Connections.encode (q);
					stream.Write (resp, 0, resp.Length);

					//Start the timer and send dictionary
					i = Helpers.Connections.checkRead (stream, bytes);

				} else {
					//decode input and put into dictionary if valid location data
					Byte[] decoded = Helpers.Connections.decode (bytes);
					//string s = System.Text.Encoding.UTF8.GetString(decoded, 0, decoded.Length);
					//Console.WriteLine ("RECEIVED MESSAGE " + s);
					String response = Helpers.Connections.parseAndPut (decoded);
					//Console.WriteLine ("Received these coordinates" + response);
					if (response == null) {
						Console.WriteLine ("NULL STUFF RECEIVED");
					} else {
						still_alive = true;
						roomGroup.OrderedSend (UPDATE, ID, response); 
					}
					i = Helpers.Connections.checkRead (stream, bytes);
					//Console.WriteLine ("Decoded: {0}", System.Text.Encoding.UTF8.GetString (decoded, 0, decoded.Length));
				}
			}

			Console.WriteLine ("Reached end of input");
			Console.WriteLine ("NEW");
			try {
				roomGroup.OrderedSend (LEAVE_ROOM, ID);
			}
			catch (Exception) {
				Console.WriteLine ("WE HAVE AN ERROR BUT WE ARE IGNORING IT NOW");
			}
			send_timer.Stop ();
			//is_alive_timer.Stop ();
			Console.WriteLine ("ROOM GROUP LEAVE"); 
			//roomGroup.Leave();
			client.Close ();
			//Console.WriteLine ("HERE 3");
		}
	}
}