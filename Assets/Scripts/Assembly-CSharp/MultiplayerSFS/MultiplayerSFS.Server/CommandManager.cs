using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace MultiplayerSFS.Server {
public static class CommandManager
{
	public const string CommandPrefix = "/";

	internal static readonly Dictionary<string, Command> commands = new Dictionary<string, Command>
	{
		{
			"help",
			new HelpCommand()
		},
		{
			"list",
			new ListCommand()
		},
		{
			"admin",
			new AdminCommand()
		},
		{
			"destroy",
			new DestroyCommand()
		},
		{
			"stats",
			new StatsCommand()
		},
		{
			"broadcast",
			new BroadcastCommand()
		},
		{
			"cheat",
			new CheatCommand()
		},
		{
			"save",
			new SaveCommand()
		}
	};

	public static bool TryParse(string input, out string name, out string[] args)
	{
		if (input.Length > 1 && input.StartsWith("/"))
		{
			name = "";
			int i;
			for (i = 1; i < input.Length; i++)
			{
				char c = input[i];
				if (c == ' ')
				{
					break;
				}
				name += c;
			}
			args = input.Substring(i).Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			return !string.IsNullOrWhiteSpace(name);
		}
		name = null;
		args = null;
		return false;
	}

	public static string TryRun(string name, string[] args, NetConnection sender)
	{
		string text = name.FormatCommand();
		if (commands.TryGetValue(name, out Command value))
		{
			try
			{
				return value.Run(args, sender);
			}
			catch (Exception ex)
			{
				Logger.Error(new Exception(text + " command raised an exception!", ex));
				return text + " resulted in an error:\n" + ex.Message;
			}
		}
		return "Could not find command " + text + ".";
	}

	public static bool RegisterCommand(string name, Command command)
	{
		if (!commands.ContainsKey(name))
		{
			commands.Add(name, command);
			return true;
		}
		return false;
	}
}
}
