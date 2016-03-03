using System;
using System.Threading;
using System.Collections.Generic;
using Vsync;

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

	class MainClass
	{
		public static void Main (string[] args)
		{
			Dictionary<string, position> valueStore = new Dictionary<string, position> ();
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
			g.Join ();


			//for (int n = 0; n < 10; n++)
				//g.OrderedSend (UPDATE);
			VsyncSystem.WaitForever ();

		}
	}
}
