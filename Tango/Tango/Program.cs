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
		private static ArrayList roomList;
		public static Room currentRoom;


		public static Vsync.Group createRoomGroup (string name) {
			Vsync.Group roomGroup = new Vsync.Group (name);
			roomGroup.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine("New View: " + v);
				Console.Title = "View " + v.viewid + ", my rank=" + v.GetMyRank();
			};
			roomGroup.Handlers[UPDATE] += (Action<string,string>) delegate(string username, string val) {
				VsyncSystem.WriteLine("IN UPDATE");
				currentRoom.playerLocs[username] = val;
			};
			roomGroup.Handlers[LOOKUP] += (Action<string>)delegate(string s) {
				VsyncSystem.WriteLine("IN LOOKUP");
				roomGroup.Reply(currentRoom.playerLocs[s]);
			};
			roomGroup.Handlers [REFRESH] += (Action)delegate() {
				string reply = Extensions.FromDictionaryToJson(currentRoom.playerLocs);
				roomGroup.Reply(reply);
			};
			roomGroup.Handlers [REMOVE] += (Action<string>)delegate(string s) {
				VsyncSystem.WriteLine("DELETING USER " + s);
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

		public static void Main (string[] args)
		{
			roomList = new ArrayList ();
			currentRoom = new Room ("NO ROOM");


			//Set up TCP Listener
			TcpListener server = new TcpListener (7569);
			server.Start ();
			Console.WriteLine("Server has started on 127.0.0.1:7569.{0}Waiting for a connection...", Environment.NewLine);
			TcpClient client = server.AcceptTcpClient ();
			NetworkStream stream = client.GetStream();

			Console.WriteLine ("Matched with a client!");

			Byte[] bytes = new Byte[4096];
			String data = null;
			String ID = Path.GetRandomFileName ().Replace (".", "");
			Console.WriteLine ("ID IS " + ID);

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
					string s = System.Text.Encoding.UTF8.GetString(decoded, 0, decoded.Length);
					Console.WriteLine ("RECEIVED MESSAGE " + s);
					String response = Helpers.Connections.parseAndPut(decoded);
					Console.WriteLine ("Received these coordinates" + response);
					i = Helpers.Connections.checkRead(stream, bytes);
					Console.WriteLine ("Decoded: {0}", System.Text.Encoding.UTF8.GetString (decoded, 0, decoded.Length));
				}
			}


			/*//Gets VSYNC SET UP
			//Set up the main group that everyone joins
			VsyncSystem.Start();
			Console.WriteLine ("VSYNC STARTED");
			Vsync.Group mainGroup = new Vsync.Group ("Main");
			mainGroup.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine("New View: " + v);
				Console.Title = "Main Group View " + v.viewid + ", my rank=" + v.GetMyRank();
			};
			mainGroup.Join ();
			Console.WriteLine ("JOINED MAIN GROUP");

			String groupName = "newRoom3";

			Vsync.Group roomGroup = createRoomGroup (groupName);
			roomGroup.Join ();
			Console.WriteLine ("Room Group Joined");
			Console.WriteLine (groupName);

			//Set timer and always send messages when the timer goes off
			System.Timers.Timer t;

			//Keep checking for more input from the client
			while (true) {
				int i = 5;
				i = i + 1;
				i = i - 1;
			}*/

			//Quick visual check to make sure initialization goes smoothely
			//UNNECESSARY -> TAKE OUT LATER
			/*List<string> valuesDic = new List<string> ();
			g.Query (1, REFRESH, new EOLMarker() , valuesDic);
			Console.WriteLine (valuesDic [0]);*/
		}
	}
}