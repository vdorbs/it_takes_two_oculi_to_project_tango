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
		public const int UPDATE = 0;
		public const int LOOKUP = 1;
		public const int REFRESH = 2;
		public const int REMOVE = 3;
		public static Room currentRoom;
		private static NetworkStream stream;


		public static Vsync.Group createRoomGroup (string name) {
			Vsync.Group roomGroup = new Vsync.Group (name);
			roomGroup.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine("New View: " + v);
				Console.Title = "View " + v.viewid + ", my rank=" + v.GetMyRank();
			};
			roomGroup.Handlers[UPDATE] += (Action<string,string>) delegate(string username, string val) {
				//VsyncSystem.WriteLine("IN UPDATE");
				currentRoom.playerLocs[username] = val;
			};
			roomGroup.Handlers[LOOKUP] += (Action<string>)delegate(string s) {
				//VsyncSystem.WriteLine("IN LOOKUP");
				roomGroup.Reply(currentRoom.playerLocs[s]);
			};
			roomGroup.Handlers [REFRESH] += (Action)delegate() {
				string reply = Extensions.FromDictionaryToJson(currentRoom.playerLocs);
				roomGroup.Reply(reply);
			};
			roomGroup.Handlers [REMOVE] += (Action<string>)delegate(string s) {
				//VsyncSystem.WriteLine("DELETING USER " + s);
				currentRoom.playerLocs.Remove(s);
			};
			/*g.MakeChkpt += (Vsync.ChkptMaker)delegate(View nv) {
				g.SendChkpt(valueStore);
				g.EndOfChkpt();
			};
			g.LoadChkpt += (loadVSchkpt)delegate(Dictionary<string, position> vs) {
			valueStore = vs;*/
			return roomGroup;
		}

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
				string info = Extensions.FromDictionaryToJson(currentRoom.playerLocs);
				Byte[] r = Encoding.UTF8.GetBytes(info);
				Byte[] resp = Helpers.Connections.encode(r);
				stream.Write(resp, 0, resp.Length);
			}
			catch(Exception err){
				//Fail Loudly
				Console.WriteLine(err);
			}
		}

		public static void Main (string[] args)
		{
			if (args.Length < 2) {
				Console.WriteLine ("There are less than 2 args given.  This is fine for now, but in the future we need to give the room name.");
			} 
			else {
				String roomname = args[1];
				int max_port = 65000;
				int min_port = 15000;
				int port_diff = max_port - min_port;
				int room_hash = roomname.GetHashCode();
				room_hash = room_hash % port_diff;
				room_hash = room_hash + port_diff;
				room_hash = room_hash % port_diff;
				Console.WriteLine ("Mod is " + room_hash);
				room_hash += min_port;
				Vsync.Vsync.VSYNC_GROUPPORT = room_hash;
				Console.WriteLine ("VSYNC PORT NO IS " + room_hash);
			}
				
			currentRoom = new Room ("NO ROOM");
			String ID = Path.GetRandomFileName ().Replace (".", "");
			Console.WriteLine ("ID IS " + ID);

			//Set up TCP Listener
			Console.WriteLine(args[0]);
			int port = Int32.Parse (args [0]);
			TcpListener server = new TcpListener (port);
			server.Start ();
			Console.WriteLine("Server has started on 127.0.0.1:{0}.\nWaiting for a connection...", port);
			TcpClient client = server.AcceptTcpClient ();
			stream = client.GetStream();
			Console.WriteLine ("Matched with a client! Now Starting VSYNC");

			/*VsyncSystem.Start ();
			Console.WriteLine ("VSYNC STARTED");
			String groupName = "TEST ROOM";

			Vsync.Group roomGroup = createRoomGroup (groupName);
			roomGroup.Join ();
			Console.WriteLine ("Room Group Joined");
			Console.WriteLine (groupName);*/

			System.Timers.Timer t = new System.Timers.Timer ();
			t.Elapsed += send_info;
			t.Interval = 100;
			t.Start ();

			Byte[] bytes = new Byte[4096];
			String data = null;

			int i = Helpers.Connections.checkRead(stream, bytes);
			//Continuously read from the buffer while the connection is open 
			while (i != 0) {
				data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
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
					Byte[] q = Encoding.UTF8.GetBytes(ID);
					//Byte[] resp = Helpers.Connections.encode(q);
					Byte[] resp = Helpers.Connections.encode(q);
					stream.Write(resp, 0, resp.Length);

					//Start the timer and send dictionary
					i = Helpers.Connections.checkRead(stream, bytes);

				}  else {
					//decode input and put into dictionary if valid location data
					Byte[] decoded = Helpers.Connections.decode (bytes);
					//string s = System.Text.Encoding.UTF8.GetString(decoded, 0, decoded.Length);
					//Console.WriteLine ("RECEIVED MESSAGE " + s);
					String response = Helpers.Connections.parseAndPut(decoded);
					//Console.WriteLine ("Received these coordinates" + response);
					roomGroup.OrderedSend (0, ID, response); 
					i = Helpers.Connections.checkRead(stream, bytes);
					//Console.WriteLine ("Decoded: {0}", System.Text.Encoding.UTF8.GetString (decoded, 0, decoded.Length));
				}
			}

			Console.WriteLine ("Reached end of input");
			t.Stop ();
			client.Close ();

			//Quick visual check to make sure initialization goes smoothely
			//UNNECESSARY -> TAKE OUT LATER
			/*List<string> valuesDic = new List<string> ();
			g.Query (1, REFRESH, new EOLMarker() , valuesDic);
			Console.WriteLine (valuesDic [0]);*/
		}
	}
}