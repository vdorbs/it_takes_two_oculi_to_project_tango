using System;
using System.Collections.Generic;
using System.Linq;

//Thank you to http://stackoverflow.com/questions/23729477/converting-dictionary-string-string-to-json-string and user phoenixinobi for this class!

namespace Tango
{
	public static class Extensions
	{
		public static string FromDictionaryToJson(this Dictionary<string, string> dictionary)
		{
			var kvs = dictionary.Select(kvp => string.Format("\"{0}\":\"{1}\"", kvp.Key, string.Join(",", kvp.Value)));
			return string.Concat("{", string.Join(",", kvs), "}");
		}

		public static Dictionary<string, string> FromJsonToDictionary(this string json)
		{
			string[] keyValueArray = json.Replace("{", string.Empty).Replace("}", string.Empty).Replace("\"", string.Empty).Split(',');
			return keyValueArray.ToDictionary(item => item.Split(':')[0], item => item.Split(':')[1]);
		}
	}

}

