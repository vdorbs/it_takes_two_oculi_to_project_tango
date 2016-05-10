using System;
using System.Collections.Generic;
using System.Linq;

//Thank you to http://stackoverflow.com/questions/23729477/converting-dictionary-string-string-to-json-string and user phoenixinobi for this class!

namespace Server
{
	public static class Extensions
	{
		public static string FromDictionaryToJson(this Dictionary<string, List<int>> dictionary)
		{
			Dictionary<string, string> dct = new Dictionary<string, string> ();
			foreach(KeyValuePair<string, List<int>> entry in dictionary){
				String lst = "";
				foreach (int i in entry.Value) {
					lst = lst + i.ToString () + ",";
				}
				if (lst.Length > 0) {
					lst = lst.Substring (0, lst.Length - 1);
				}
				dct [entry.Key] = lst;
			}
			var kvs = dct.Select(kvp => string.Format("\"{0}\":\"{1}\"", kvp.Key, string.Join(",", kvp.Value)));
			Console.WriteLine ("{0}", string.Concat ("{", string.Join (",", kvs), "}"));
			return string.Concat("{", string.Join(",", kvs), "}");
		}

		public static Dictionary<string, List<int>> FromJsonToDictionary(this string json)
		{
			if (json == "{}") {
				return new Dictionary<string, List<int>> ();
			}
			string[] keyValueArray = json.Replace("{", string.Empty).Replace("}", string.Empty).Replace("\"", string.Empty).Split(',');
			Dictionary<string, string> dct = keyValueArray.ToDictionary(item => item.Split(':')[0], item => item.Split(':')[1]);
			Dictionary <string,List<int>> dictionary = new Dictionary<string, List<int>> ();
			foreach(KeyValuePair<string, string> entry in dct){
				List<int> l = new List<int> ();
				string[] l_string = entry.Value.Split (',');
				foreach (string s in l_string) {
					l.Add (Int32.Parse(s));
				}
				dictionary [entry.Key] = l;
			}
			return dictionary;
		}
	}

}
