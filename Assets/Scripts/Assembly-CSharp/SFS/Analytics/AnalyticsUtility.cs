using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;

namespace SFS.Analytics
{
	public static class AnalyticsUtility
	{
		public static void SendEvent(string eventName, params (string, object)[] eventData)
		{
			SendEvent(eventName, eventData.ToDictionary(((string, object) x) => x.Item1, ((string, object) x) => x.Item2));
		}

		public static void SendEvent(string eventName, Dictionary<string, object> eventData)
		{
			if (!Application.isEditor)
			{
				UnityEngine.Analytics.Analytics.CustomEvent(eventName, eventData);
			}
		}

		public static void SendEventToJordiServer(string eventName, params (string, object)[] eventData)
		{
			if (!Application.isEditor)
			{
				try
				{
					Dictionary<string, string> dictionary = eventData.ToDictionary(((string, object) a) => a.Item1, ((string, object) b) => b.Item2.ToString());
					dictionary.Add("%event_name%", eventName);
					UnityWebRequest uwr = UnityWebRequest.Post("https://jmnet.one/san/post.php", dictionary);
					uwr.SendWebRequest().completed += delegate
					{
						uwr.Dispose();
					};
				}
				catch
				{
				}
			}
			if (!Application.isEditor)
			{
				SendEvent(eventName, eventData);
			}
		}
	}
}
