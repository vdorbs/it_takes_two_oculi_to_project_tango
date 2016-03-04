using System;
using System.Threading;
using System.Collections.Generic;
using Vsync;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;

namespace Tango
{
	public class position
	{
		public float pos_x;
		public float pos_y;
		public float pos_z;
		public float rot;

		public position(float x, float y, float z, float r)
		{
			pos_x = x; pos_y = y; pos_z = z; rot = r;
		}
	}

	public class room
	{
		public string room_name;
		public Dictionary<string, position> playerLocs;
		public List<string> players;
		//Put anything else in here

		public room (string name)
		{
			room_name = name;
			playerLocs = new Dictionary<string, position> ();
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
			bytes = new Byte[256];
			data = null;
		}
		public void handle(){
			Console.WriteLine ("Hi I'm your personal thread");
			int totalBytesEchoed = 0;
			NetworkStream stream = client.GetStream();
			int i;
			while ((i = stream.Read (bytes, 0, bytes.Length)) != 0) {
				data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
				Console.WriteLine("Received: {0}", data);
			}
			//client.Close ();

		}
	}
	class MainClass
	{
		public static void Main (string[] args)
		{
			/*Dictionary<string, position> valueStore = new Dictionary<string, position> ();
			const int UPDATE = 0;
			const int LOOKUP = 1;
			const int REFRESH = 2;
			Console.WriteLine ("Hello World!");
			VsyncSystem.Start ();
			Console.WriteLine ("VSYNC STARTED");

			Group g = new Group ("dataHolder");
			g.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine("New View: " + v);
				Console.Title = "View " + v.viewid + ", my rank=" + v.GetMyRank();
			};
			g.Handlers[UPDATE] += (Action<string,float,float,float,float>) delegate(string s, float x, float y, float z, float r) {
				VsyncSystem.WriteLine("IN UPDATE");
				position player_pos = new position(x, y, z, r);
				valueStore[s] = player_pos;
			};
			g.Handlers[LOOKUP] += (Action<string>)delegate(string s) {
				VsyncSystem.WriteLine("IN LOOKUP");
				g.Reply(valueStore[s]);
			};
			g.Handlers [REFRESH] += (Action)delegate() {
				g.Reply(valueStore);
			};
			//g.MakeChkpt += (Vsync.ChkptMaker)delegate(View nv) {
			//	g.SendChkpt(valueStore);
			//	g.EndOfChkpt();
			//};
			//g.LoadChkpt += (loadVSchkpt)delegate(Dictionary<string, position> vs) {
			//	valueStore = vs;
			//};
			g.Join ();

			//for (int n = 0; n < 10; n++)
				//g.OrderedSend (UPDATE);
			VsyncSystem.WaitForever ();*/

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
