using System;
using System.IO;

namespace MultiplayerSFS.Server {
public class Program
{
	private const string CONFIG_FILENAME = "serverconfig.json";

	private static readonly bool DEV_MODE = false;

	public static void Main()
	{
		try
		{
			ServerSettings serverSettings;
			if (DEV_MODE)
			{
				Logger.Info("Dev mode enabled, running with default settings...", important: true);
				serverSettings = new ServerSettings();
				File.Delete("serverconfig.json");
			}
			else if (!File.Exists("serverconfig.json"))
			{
				Logger.Info("'serverconfig.json' not found, running with default settings...", important: true);
				serverSettings = new ServerSettings();
				File.WriteAllText("serverconfig.json", serverSettings.Serialize());
			}
			else
			{
				Logger.Info("Loading server settings from 'serverconfig.json'...", important: true);
				serverSettings = ServerSettings.Deserialize(File.ReadAllText("serverconfig.json"));
			}
			Server.Initialize(serverSettings);
			Server.Run();
		}
		catch (Exception exception)
		{
			Logger.Error(exception);
		}
	}
}
}
