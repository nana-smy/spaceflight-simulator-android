using Lidgren.Network;

namespace MultiplayerSFS.Server {
public abstract class Command
{
	public abstract string Description { get; }

	public abstract string Run(string[] args, NetConnection sender);

	public bool CheckAdmin(NetConnection sender)
	{
		return Server.FindPlayer(sender)?.isAdmin ?? false;
	}
}
}
