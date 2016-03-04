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


	public class handler
	{
		private TcpClient client;
		private Byte[] bytes;
		private string data;
		public handler(TcpClient c){
			client = c;
			bytes = new Byte[512];
			data = null;
		}

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
			for (int i = indexFirstDataByte; i < length; i++) {
				decoded [j] = (Byte) (bytes[i] ^ masks [j % 4]);
				j++;
			}
			return decoded;
		}

		public void handle(){
			Console.WriteLine ("Hi I'm your personal thread");
			NetworkStream stream = client.GetStream();
			int i;
			while ((i = stream.Read (bytes, 0, bytes.Length)) != 0) {
				data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
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
					Byte[] decoded = decode ();
					//String result = System.Text.Encoding.ASCII.GetString(bytes, 0, decoded.Length);
					Console.WriteLine ("Decoded: {0}", System.Text.Encoding.UTF8.GetString (decoded, 0, decoded.Length));
				}
				Byte[] r = Encoding.UTF8.GetBytes("Hello from the server side");
				Byte[] resp = encode(r);
				stream.Write(resp, 0, resp.Length);
			}
			//client.Close ();


		}
	}
	class MainClass
	{
		public static void Main (string[] args)
		{
			Dictionary<string, string> valueStore = new Dictionary<string, string> ();
			valueStore ["hey"] = "you!";
			const int UPDATE = 0;
			const int LOOKUP = 1;
			const int REFRESH = 2;
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
			//g.MakeChkpt += (Vsync.ChkptMaker)delegate(View nv) {
			//	g.SendChkpt(valueStore);
			//	g.EndOfChkpt();
			//};
			//g.LoadChkpt += (loadVSchkpt)delegate(Dictionary<string, position> vs) {
			//	valueStore = vs;
			//};
			g.Join ();

			List<string> valuesDic = new List<string> ();
			g.Query (1, REFRESH, new EOLMarker() , valuesDic);
			Console.WriteLine (valuesDic [0]);



			//for (int n = 0; n < 10; n++)
				//g.OrderedSend (UPDATE);

			TcpListener server = new TcpListener (7569);
			server.Start ();
			Console.WriteLine("Server has started on 127.0.0.1:7569.{0}Waiting for a connection...", Environment.NewLine);
			while (true) {
				TcpClient client = server.AcceptTcpClient ();
				handler h = new handler (client);
				Thread handler = new Thread (new ThreadStart (h.handle));
				handler.Start ();
				Console.WriteLine ("A Client Connected!");
			}
		}
	}
}