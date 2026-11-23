using Lidgren.Network;

namespace MultiplayerSFS.Server {
public class SaveCommand : Command
{
	public override string Description => "Manually saves the world state. This command requires admin privileges.";

	public override string Run(string[] args, NetConnection sender)
	{
		if (!CheckAdmin(sender))
		{
			return "You do not have the required admin privileges to use this command.";
		}
		if (args.Length != 0)
		{
			return "Too many arguments provided. Usage: /save";
		}
		Logger.Info("Manual world save initiated by admin.", important: true);
		Server.world.SaveWorld();
		return "World saved successfully!";
	}
}
}
