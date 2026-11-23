using Lidgren.Network;

namespace MultiplayerSFS.Server {
public class HelpCommand : Command
{
	public override string Description => "Provides a description of a given command, or some general help info if no command is given.";

	public override string Run(string[] args, NetConnection sender)
	{
		if (args.Length == 0)
		{
			string value = "list".FormatCommand();
			string value2 = "help".FormatCommand("{command}");
			return $"\n                |Commands can be used to manage the server, its players, and the multiplayer world.\n                |Use {value} to get a list of available commands, or {value2} to get a description of a specific command.\n                ".CleanDescription();
		}
		if (args.Length == 1)
		{
			if (CommandManager.commands.TryGetValue(args[0], out Command value3))
			{
				return value3.Description;
			}
			string value4 = args[0].FormatCommand();
			string value5 = "list".FormatCommand();
			return $"The command {value4} does not exist! Use {value5} to get a list of available commands.";
		}
		string value6 = "list".FormatCommand();
		string value7 = "help".FormatCommand("{command}");
		return $"\n                |Too many arguments provided.\n                |Use {value6} to get a list of available commands, or {value7} to get a description of a specific command.\n                ".CleanDescription();
	}
}
}
