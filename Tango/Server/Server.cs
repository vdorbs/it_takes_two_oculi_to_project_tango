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
using Helpers;

namespace Server
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

				Byte[] bytes = new Byte[4096];
				String data = null;
				int i = Helpers.Connections.checkRead(stream, bytes);
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
				}
				Byte[] q = Encoding.UTF8.GetBytes("7569");
				Byte[] resp = Helpers.Connections.encode(q);
				stream.Write(resp, 0, resp.Length);
				Console.WriteLine ("A Client Connected! Port Number sent.");
			}
		}
	}
}
