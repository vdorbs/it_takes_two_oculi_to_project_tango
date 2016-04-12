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
	public class Server
	{
		public Server ()
		{
			
		}
	
		public static void Main(String[] Args){
			TcpListener server = new TcpListener (7000);
			server.Start ();
			Console.WriteLine("Server has started on 127.0.0.1:7000.{0}Waiting for a connection...", Environment.NewLine);
			while (true) {
				TcpClient client = server.AcceptTcpClient ();
				NetworkStream stream = client.GetStream();
				Byte[] q = Encoding.UTF8.GetBytes("7569");
				Byte[] resp = Helpers.Connections.encode(q);
				stream.Write(resp, 0, resp.Length);
				Console.WriteLine ("A Client Connected! Port Number sent.");
			}
		}
	}
}

