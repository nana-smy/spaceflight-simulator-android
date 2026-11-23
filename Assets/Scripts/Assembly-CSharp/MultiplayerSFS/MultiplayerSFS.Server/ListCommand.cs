using System.Linq;
using Lidgren.Network;

namespace MultiplayerSFS.Server {
public class ListCommand : Command
{
	public override string Description => "Provides a list of available multiplayer commands.";

	public override string Run(string[] args, NetConnection sender)
	{
		if (args.Length != 0)
		{
			return "Too many arguments provided.";
		}
		return "List of available multiplayer commands:\n  " + string.Join("\n  ", CommandManager.commands.Keys.Select((string n) => n.FormatCommand()));
	}
}
}
