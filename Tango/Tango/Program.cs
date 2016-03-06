using System;
using System.Threading;
using System.Collections.Generic;
using Vsync;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;

namespace Tango
{
	//Will be responsible for storing rooms 
	public class room
	{
		public string room_name;
		public Dictionary<string, string> playerLocs;
		public List<string> players;
		//Put anything else in here

		public room (string name)
		{
			room_name = name;
			playerLocs = new Dictionary<string, string> ();
			players = new List<string> ();
		}

	}

	//Responsible for handling a single client 
	public class handler
	{
		private TcpClient client; //A reference to the client this thread is handling
		private Byte[] bytes; //The current input from the client 
		private string data; // String representation on bytes
		private Vsync.Group g; // The Vsync group this server is a member of
		private NetworkStream strm; //Input Stream for communication
		string ID; //User ID
		System.Timers.Timer t; // Thread timer used for sending dictionary

		//Constructor
		public handler(TcpClient c, Vsync.Group g){
			client = c;
			bytes = new Byte[4096];
			data = null;
			this.g = g;
			strm = null;
			ID = Path.GetRandomFileName ().Replace (".", "");
			t = new System.Timers.Timer ();
		}

		//Encodes data to be sent accross socket connection in JS readible format
		private Byte[] encode(Byte[] raw){
			Byte[] formatted;
			int indexStart = -1;
			if (raw.Length <= 125) {
				formatted = new Byte[2 + raw.Length];
				formatted [1] = (Byte) raw.Length;
				indexStart = 2;
			} else if (raw.Length >= 126 && raw.Length <= 65535) {
				formatted = new Byte[4 + raw.Length];
				formatted [1] = 126;
				formatted [2] = (Byte)((raw.Length >> 8) & 255);
				formatted [3] = (Byte)(raw.Length & 255);
				indexStart = 4;
			} else {
				formatted = new Byte[10 + raw.Length];
				formatted [1] = 127;
				int shift = 56;
				for (int i = 2; i < 10; i++) {
					formatted [i] = (Byte)((raw.Length >> shift) & 255);
					shift = shift - 8;
				}
				indexStart = 10;
			}

			formatted [0] = 129;
			Array.Copy (raw, 0, formatted, indexStart, raw.Length);
			return formatted;
		}

		//Decodes data sent from JS Socket
		private Byte[] decode(){
			Byte secondbyte = bytes [1];
			int length = secondbyte & 127;
			int indexFirstMask = 2;
			if (length == 126) {
				indexFirstMask = 4;
			} else if (length == 127) {
				indexFirstMask = 10;
			}
			Byte[] masks = new Byte[4];
			Array.Copy (bytes, indexFirstMask, masks, 0, 4);
			int indexFirstDataByte = indexFirstMask + 4;
			int datalength = bytes.Length - indexFirstDataByte;
			Byte[] decoded = new Byte[datalength];
			int j = 0;
			for (int i = indexFirstDataByte;
				i < length + 6; i++) {
				decoded [j] = (Byte) (bytes[i] ^ masks [j % 4]);
				j++;
			}
			return decoded;
		}

		//Sends dictionary accross network stream 
		public void send_info(Object source, System.Timers.ElapsedEventArgs e) {
			try{
				List<string> valuesDic = new List<string> ();
				g.Query (1, 2, new EOLMarker() , valuesDic);
				Byte[] r = Encoding.UTF8.GetBytes(valuesDic [0]);
				Byte[] resp = encode(r);
				strm.Write(resp, 0, resp.Length);
			}
			catch(Exception err){
				//Fail Loudly
				Console.WriteLine(err);
			}
		}

		//Parses data recieved from client and updates dictionary
		public void parseAndPut(Byte[] coord){
			string c = System.Text.Encoding.UTF8.GetString (coord);
			Console.WriteLine (c);
			c = c.Replace("{", String.Empty).Replace("\"", String.Empty).Replace("}", String.Empty);
			Console.WriteLine (c);
			String[] parts = c.Split (':');
			try {
				String key = parts [0].Trim();
				String value = parts [1].Trim();
				Console.WriteLine (key);
				Console.WriteLine (value);
				g.OrderedSend (0, key, value); 
			}
			catch(Exception e) {
				//Fail Silently 
			}
		}

		public int checkRead(Stream s){
			try{
				return s.Read(bytes, 0, bytes.Length);
			}
			catch (Exception e){
				return 0;
			}
		}
		//The main method callback method responsible for recieving and processing input on a loop
		public void handle(){
			NetworkStream stream = client.GetStream();
			this.strm = stream;
			int i = checkRead(strm);
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
					Byte[] resp = encode(q);
					stream.Write(resp, 0, resp.Length);

					//Start the timer and send dictionary
					t.Elapsed += send_info;
					t.Interval = 16;
					t.Start ();
					i = checkRead(strm);

				} else {
					//decode input and put into dictionary if valid location data
					Byte[] decoded = decode ();
					parseAndPut(decoded);
					i = checkRead(strm);
					Console.WriteLine ("Decoded: {0}", System.Text.Encoding.UTF8.GetString (decoded, 0, decoded.Length));
				}
			}
			//Stop timer, remove user, close connection
			Console.WriteLine ("Exiting While");
			t.Stop ();
			g.OrderedSend (3, ID);
			client.Close ();
		}
	}
	class MainClass
	{
		public static void Main (string[] args)
		{
			//Set up vsync dictionary and handlers
			Dictionary<string, string> valueStore = new Dictionary<string, string> ();
			valueStore ["user1"] = "0,1,0,1,1,2";
			valueStore ["user2"] = "1,1,1,1,1,1";
			const int UPDATE = 0;
			const int LOOKUP = 1;
			const int REFRESH = 2;
			const int REMOVE = 3;
			Console.WriteLine ("Hello World!");
			VsyncSystem.Start ();
			Console.WriteLine ("VSYNC STARTED");

			Vsync.Group g = new Vsync.Group ("dataHolder");
			g.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine("New View: " + v);
				Console.Title = "View " + v.viewid + ", my rank=" + v.GetMyRank();
			};
			g.Handlers[UPDATE] += (Action<string,string>) delegate(string username, string val) {
				VsyncSystem.WriteLine("IN UPDATE");
				valueStore[username] = val;
			};
			g.Handlers[LOOKUP] += (Action<string>)delegate(string s) {
				VsyncSystem.WriteLine("IN LOOKUP");
				g.Reply(valueStore[s]);
			};
			g.Handlers [REFRESH] += (Action)delegate() {
				string reply = Extensions.FromDictionaryToJson(valueStore);
				g.Reply(reply);
			};
			g.Handlers [REMOVE] += (Action<string>)delegate(string s) {
				VsyncSystem.WriteLine("DELETING USER " + s);
				valueStore.Remove(s);
			};
			/*g.MakeChkpt += (Vsync.ChkptMaker)delegate(View nv) {
				g.SendChkpt(valueStore);
				g.EndOfChkpt();
			};
			g.LoadChkpt += (loadVSchkpt)delegate(Dictionary<string, position> vs) {
			valueStore = vs;
			}; */
			g.Join ();

			//Quick visual check to make sure initialization goes smoothely
			//UNNECESSARY -> TAKE OUT LATER
			List<string> valuesDic = new List<string> ();
			g.Query (1, REFRESH, new EOLMarker() , valuesDic);
			Console.WriteLine (valuesDic [0]);

			//Set up TCP Listener
			TcpListener server = new TcpListener (7569);
			server.Start ();
			Console.WriteLine("Server has started on 127.0.0.1:7569.{0}Waiting for a connection...", Environment.NewLine);
			//Listen for connections
			while (true) {
				TcpClient client = server.AcceptTcpClient ();
				handler h = new handler (client, g);
				//Spin off new thread to handle clients as they arrive 
				Thread handler = new Thread (new ThreadStart (h.handle));
				handler.Start ();
				Console.WriteLine ("A Client Connected!");
			}
		}
	}
}