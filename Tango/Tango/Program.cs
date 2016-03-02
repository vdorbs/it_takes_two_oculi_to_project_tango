using System;
using System.Threading;
using Vsync;

namespace Tango
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			VsyncSystem.Start ();
			Console.WriteLine ("VSYNC STARTED");

			Group g = new Group ("dataHolder");
			g.ViewHandlers += (ViewHandler)delegate(View v) {
				VsyncSystem.WriteLine("New View: " + v);
				Console.Title = "View " + v.viewid + ", my rank=" + v.GetMyRank();
			};
			g.Join ();
			VsyncSystem.WaitForever ();

		}
	}
}
