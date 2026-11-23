using Lidgren.Network;

namespace MultiplayerSFS.Server {
public class AdminCommand : Command
{
	public override string Description => $"\n        |{"admin".FormatCommand("{password}")}: Grants admin privileges if the correct admin password is provided.\n        |{"admin".FormatCommand()}: Disables admin privileges if they were already enabled.\n        ".CleanDescription();

	public override string Run(string[] args, NetConnection sender)
	{
		ConnectedPlayer connectedPlayer = Server.FindPlayer(sender);
		if (connectedPlayer != null)
		{
			if (args.Length != 0)
			{
				if (connectedPlayer.isAdmin)
				{
					return "You already have admin privileges.";
				}
				if (string.IsNullOrWhiteSpace(Server.settings.adminPassword))
				{
					return "Admin privileges are disabled on this server.";
				}
				if (string.Join(" ", args) == Server.settings.adminPassword)
				{
					connectedPlayer.isAdmin = true;
					return "Admin privileges granted!";
				}
				return "Incorrect admin password.";
			}
			if (connectedPlayer.isAdmin)
			{
				connectedPlayer.isAdmin = false;
				return "Admin privileges removed.";
			}
			return "You must provide the admin password to gain admin privileges.";
		}
		return "Could not find player!";
	}
}
}
