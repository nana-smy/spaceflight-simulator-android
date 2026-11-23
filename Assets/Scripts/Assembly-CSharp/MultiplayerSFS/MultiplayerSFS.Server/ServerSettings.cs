using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MultiplayerSFS.Server {
public class ServerSettings
{
	[ServerConfigVariable(new string[] { "The file path to the multiplayer world's save folder." })]
	public string worldSavePath = "./World";

	[ServerConfigVariable(new string[] { "Port used by the server. Generally should not be changed, as this is also the default port for the client's join menu." })]
	public int port = 9806;

	[ServerConfigVariable(new string[] { "Password required by players to access the multiplayer server.", "WARNING: Leaving blank can & will allow any player who knows the server's IP address to join!" })]
	public string serverPassword = "";

	[ServerConfigVariable(new string[] { "Password used by a player to gain admin privileges, which allows the use of various powerful commands through the in-game multiplayer chat.", "Leaving this blank will prevent the use of admin privileges by any connected player.", "WARNING: These commands include the ability to destroy any & all rockets, kick any player, and more, so be careful with sharing this password!" })]
	public string adminPassword = "ADMIN123";

	[ServerConfigVariable(new string[] { "The maximum number of connected players allowed at any one time." })]
	public int maxConnections = 16;

	[ServerConfigVariable(new string[] { "Prevents players from joining if their username is already in use on the server." })]
	public bool blockDuplicatePlayerNames;

	[ServerConfigVariable(new string[] { "Cooldown time (in seconds) during which a player cannot send another message in the multiplayer chat.", "Used to reduce spam and similar issues. Set to 0 if you want to disable the cooldown." })]
	public double chatMessageCooldown = 3.0;

	[ServerConfigVariable(new string[] { "Comma-separated list of allowed game versions. Players with versions not in this list will be rejected.", "Leave empty to allow all versions. Example: '1.5.10.2'", "Only major versions need to match (e.g., 1.5.x.x are considered the same major version)." })]
	public string allowedGameVersions = "";

	[ServerConfigVariable(new string[] { "Message of the day displayed to players when they join.", "Leave empty to disable." })]
	public string motd = "Welcome to the server!";

	[ServerConfigVariable(new string[] { "Color of the MOTD message.", "Format: hex color code (without #). Example: 0000FFFF for blue with full opacity.", "Supports RRGGBB (6 digits) or RRGGBBAA (8 digits) format.", "Default: 0000FFFF (blue with full opacity)" })]
	public string motdColor = "0000FFFF";

	[ServerConfigVariable(new string[] { "Rocket update interval in milliseconds. Updates per second = 1000 / updateRocketsPeriod", "Do not adjust unless you know what you are doing." })]
	public double updateRocketsPeriod = 20.0;

	[ServerConfigVariable(new string[] { "Distance for player authority over nearby rockets. Should be above game's load distance.", "Do not adjust unless you know what you are doing." })]
	public double loadRange = 7500.0;

	[ServerConfigVariable(new string[] { "Authority update interval in milliseconds. Lower values increase CPU usage.", "Do not adjust unless you know what you are doing." })]
	public int authorityUpdateInterval = 200;

	[ServerConfigVariable(new string[] { "Maximum time warp scale allowed on the server. Set to 0 for unlimited.", "Do not adjust unless you know what you are doing." })]
	public float timeWarpMaxScale;

	[ServerConfigVariable(new string[] { "Time warp voting timeout in seconds. Set to 0 to disable.", "Do not adjust unless you know what you are doing." })]
	public int timeWarpVoteTimeout = 30;

	[ServerConfigVariable(new string[] { "Network timeout in seconds for client connections.", "Do not adjust unless you know what you are doing." })]
	public int networkTimeout = 15;

	[ServerConfigVariable(new string[] { "Maximum packet size in bytes.", "Do not adjust unless you know what you are doing." })]
	public int maxPacketSize = 65536;

	[ServerConfigVariable(new string[] { "Enable network compression to reduce bandwidth.", "Do not adjust unless you know what you are doing." })]
	public bool enableNetworkCompression;

	[ServerConfigVariable(new string[] { "Server tick rate in milliseconds. Minimum is 1ms.", "Do not adjust unless you know what you are doing." })]
	public int serverTickRate = 1;

	[ServerConfigVariable(new string[] { "Enable Server GC mode for better performance on multi-core systems.", "Uses more memory but reduces GC pause times.", "Do not adjust unless you know what you are doing." })]
	public bool enableServerGC = true;

	[ServerConfigVariable(new string[] { "Enable concurrent garbage collection to reduce pause times.", "Do not adjust unless you know what you are doing." })]
	public bool gcConcurrent = true;

	[ServerConfigVariable(new string[] { "Number of GC heaps. 0 = auto (usually equals CPU core count).", "Do not adjust unless you know what you are doing." })]
	public int gcHeapCount;

	[ServerConfigVariable(new string[] { "World auto-save interval in seconds." })]
	public int worldSaveInterval = 300;

	public string Serialize()
	{
		StringBuilder stringBuilder = new StringBuilder();
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			ServerConfigVariable customAttribute = fieldInfo.GetCustomAttribute<ServerConfigVariable>();
			if (customAttribute == null)
			{
				continue;
			}
			if (customAttribute.Comment.Length != 0)
			{
				string[] comment = customAttribute.Comment;
				foreach (string text in comment)
				{
					stringBuilder.AppendLine("# " + text);
				}
			}
			object value = fieldInfo.GetValue(this);
			string text2 = ((value is IFormattable formattable) ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString());
			stringBuilder.Append(fieldInfo.Name + "=" + text2 + "\n\n");
		}
		return stringBuilder.ToString().TrimEnd('\n');
	}

	public static ServerSettings Deserialize(string input)
	{
		try
		{
			ServerSettings serverSettings = new ServerSettings();
			string[] array = input.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].TrimStart(' ').Replace("\r", "");
				if (text.Length != 0 && !text.StartsWith("#"))
				{
					int num = text.IndexOf('=');
					if (num == -1)
					{
						throw new Exception("= character not found.");
					}
					string text2 = text.Substring(0, num);
					string value = ((text.Length > num + 1) ? text.Substring(num + 1) : "");
					try
					{
						FieldInfo field = serverSettings.GetType().GetField(text2);
						field.SetValue(serverSettings, Convert.ChangeType(value, field.FieldType, CultureInfo.InvariantCulture));
						Logger.Info(text2 + ": " + field.GetValue(serverSettings));
					}
					catch (Exception innerException)
					{
						throw new Exception("Variable deserialization error (" + text2 + ")", innerException);
					}
				}
			}
			return serverSettings;
		}
		catch (Exception innerException2)
		{
			throw new Exception("Config deserialization failed", innerException2);
		}
	}
}
}
