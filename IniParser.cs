using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchOverlay
{
	public static class IniParser
	{
		public static string configFilePath = "config.ini";

		public static string ip;
		public static string port;
		public static string behaviorName;

		public static string connectionAdress;
		public static string eventSubscriptionAdress;
		public static string broadcasterUserId;
		public static string clientId;
		public static string accessToken;

		public static string rewardPaintOne;
		public static string rewardPaintTen;

		public static string paintOne;
		public static string paintTen;
		public static string paintHundread;
		public static string paintRemoveOne;
		public static string paintRemoveAll;
		public static string paintFillScreenProgressive;
		public static string paintEmptyScreenProgressive;

		public static string raid;

		public static string showDeathCounter;
		public static string hideDeathCounter;
		public static string updateDeathCounter;

		public static string exit;

		public static void ReadConfig()
		{
			string[] lines = File.ReadAllLines("config.ini");
			foreach (var line in lines)
			{
				if (line.Contains("="))
				{
					string[] parts = line.Split('=');
					switch (parts[0])
					{
						case "ip":
							ip = parts[1];
							break;
						case "port":
							port = parts[1];
							break;
						case "behaviorName":
							behaviorName = parts[1];
							break;

						case "connectionAdress":
							connectionAdress = parts[1];
							break;
						case "eventSubscriptionAdress":
							eventSubscriptionAdress = parts[1];
							break;
						case "broadcasterUserId":
							broadcasterUserId = parts[1];
							break;
						case "clientId":
							clientId = parts[1];
							break;
						case "accessToken":
							accessToken = parts[1];
							break;

						case "rewardPaintOne":
							rewardPaintOne = parts[1];
							break;
						case "rewardPaintTen":
							rewardPaintTen = parts[1];
							break;

						case "paintOne":
							paintOne = parts[1];
							break;
						case "paintTen":
							paintTen = parts[1];
							break;
						case "paintHundread":
							paintHundread = parts[1];
							break;
						case "paintRemoveOne":
							paintRemoveOne = parts[1];
							break;
						case "paintRemoveAll":
							paintRemoveAll = parts[1];
							break;
						case "paintFillScreenProgressive":
							paintFillScreenProgressive = parts[1];
							break;
						case "paintEmptyScreenProgressive":
							paintEmptyScreenProgressive = parts[1];
							break;

						case "raid":
							raid = parts[1];
							break;

						case "showDeathCounter":
							showDeathCounter = parts[1];
							break;
						case "hideDeathCounter":
							hideDeathCounter = parts[1];
							break;
						case "updateDeathCounter":
							updateDeathCounter = parts[1];
							break;

						case "exit":
							exit = parts[1];
							break;

						default:
							break;
					}
				}
			}
		}
	}
}
