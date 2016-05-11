using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using Helpers;
using Vsync;

namespace Server
{
	public class Server
	{
		private class Room{
			public List<int> ports = new List<int> ();
		}

		//Empty constructor
		//public Server ()
		//{

 //		}
		public const int UPDATE = 0;
		public const int LOOKUP = 1;
		delegate void loaddckpt(string rs);

		public static void Main(String[] Args){
			Dictionary<string, List<int>> rooms = new Dictionary<string, List<int>> ();
			VsyncSystem.Start ();
			Console.WriteLine ("vsync started");
			Vsync.Group lGroup = new Vsync.Group ("Load Balancers");
			lGroup.Handlers [UPDATE] += (Action<String, List<int>> ) delegate(string id, List<int> r) {
				foreach(KeyValuePair<string, List<int>> entry in rooms){
					if (Int32.Parse(id) == Int32.Parse(entry.Key)){
						rooms[entry.Key] = r;
						return;
					}
				}
				rooms[id] = r;
				Console.WriteLine("Adding {0} to dict", id);
			};
			lGroup.Handlers [LOOKUP] += (Action<String>) delegate(string id) {
				//Console.WriteLine("Looking in dict for {0}:end", id);
				//bool contains = false;
				foreach(KeyValuePair<string, List<int>> entry in rooms){
					//Console.WriteLine("s:{0}:f", entry.Key);
					if (Int32.Parse(id) == Int32.Parse(entry.Key)){
						lGroup.Reply(entry.Value);
						return;
					}
				}
				/*Console.WriteLine(rooms.ContainsKey(id));
				if (contains){
					lGroup.Reply (rooms [id]);
					Console.WriteLine("Found room in dictionary!");
				}
				else{ */
				lGroup.Reply(new List<int>());
				Console.WriteLine("Creating room in dictionary!");

			}; 
			lGroup.MakeChkpt += (Vsync.ChkptMaker)delegate(View nv){
				lGroup.SendChkpt(Extensions.FromDictionaryToJson(rooms));
				lGroup.EndOfChkpt();
			};
			lGroup.LoadChkpt += (loaddckpt)delegate (string rs) {
				rooms = Extensions.FromJsonToDictionary(rs);
			};

			//loadGroup.DHTEnable (1, 1, 1, 86400000);
			lGroup.Join ();
			Console.WriteLine ("Server Group Joined");
			int port = Int32.Parse (Args [0]);
			TcpListener server = new TcpListener (port);
			server.Start ();
			Console.WriteLine("Server has started on 127.0.0.1:7000.{0}Waiting for a connection...", Environment.NewLine);
			int counter = 7568;
			String room = null;
			while (true) {
				TcpClient client = server.AcceptTcpClient ();
				counter++;
				NetworkStream stream = client.GetStream();
				bool connect = true;
				//String room = null;
				while (connect) {
					Byte[] bytes = new Byte[4096];
					String data = null;
					int i = Helpers.Connections.checkRead (stream, bytes);
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
					} else {
						Byte[] response = Helpers.Connections.decode (bytes);
						string r = System.Text.Encoding.ASCII.GetString (response);
						//r = r.Substring(0, 10);
						room = r;
						bool isValid = true;
						//Console.WriteLine ("{0}", r);
						//Console.WriteLine ("Length: {1}, Last Char: {0}!", room.Substring ((r.Length - 1)), room.Length);
						if (room.Contains("j")){
							Console.WriteLine ("here");
							try
							{
								List<List<int>> rez = new List<List<int>>();
								room = room.Remove(room.IndexOf("j"), 1);
								Console.WriteLine(room);
								lGroup.OrderedQuery(1, LOOKUP, room, new Vsync.EOLMarker (), rez);
								List<int> ports = rez[0];
								if (ports.Count == 0){
									isValid = false;
									Console.WriteLine("BAD ID");
									byte[] f = Encoding.UTF8.GetBytes("BAD ID");
									f = Helpers.Connections.encode(f);
									stream.Write(f, 0, f.Length);
								}
								Console.WriteLine(room);
								//r = room;
							}
							catch(Exception e){
								Console.WriteLine ("Error: {0}", e);
							}
						}

						if (isValid) {
							Console.WriteLine ("The room name is " + room);
							break;
						}
					}
				}


				List<List<int>> result = new List<List<int>> ();
				try
				{
					lGroup.OrderedQuery (1, LOOKUP, room, new Vsync.EOLMarker (), result);
					List<int> ports = result[0];
					ports.Add (counter);
					lGroup.OrderedSend (UPDATE, room, ports);
				}
				catch(Exception e){
					Console.WriteLine ("DICTIONARY ADD Error: {0}", e);
				}



				/*Console.WriteLine ("good here...");
				List<int> portlst = loadGroup.DHTGet<String, List<int>>((string) room);
				Console.WriteLine ("good after this ish");
				if (portlst == null) {
					portlst = new List<int> ();
				}
				portlst.Add (counter);
				Console.WriteLine ("putting");
				loadGroup.DHTPut (room, portlst);
				Console.WriteLine ("put"); */

				Byte[] q = Encoding.UTF8.GetBytes (counter.ToString ());
				Byte[] resp = Helpers.Connections.encode (q); 
				Console.WriteLine ("HEY " + "../launchServer.sh " + counter.ToString () + " " + room);
				Process proc = new Process {
					StartInfo = new ProcessStartInfo {
						FileName = "/bin/bash",
						Arguments = "../launchServer.sh " + counter.ToString() + " " + room,
						UseShellExecute = true,
						RedirectStandardOutput = false
					}
				};

				proc.Start();
				Console.WriteLine ("A Client Connected!Handler server started.");
				System.Threading.Thread.Sleep(1000);
				stream.Write(resp, 0, resp.Length);
				Console.WriteLine ("Port Number Sent");
			}
		}
	}
}
