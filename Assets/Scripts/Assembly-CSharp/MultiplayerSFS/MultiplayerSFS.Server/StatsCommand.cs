using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using MultiplayerSFS.ServerCommon;

namespace MultiplayerSFS.Server {
public class StatsCommand : Command
{
	public override string Description => "Prints various stats about the multiplayer server.";

	public override string Run(string[] args, NetConnection sender)
	{
		if (args.Length != 0)
		{
			return "Too many arguments provided.";
		}
		ConnectedPlayer connectedPlayer = Server.FindPlayer(sender);
		if (connectedPlayer != null)
		{
			return $"\n                |• Number of players: {Server.connectedPlayers.Count}\n                |• Number of rockets: {Server.world.rockets.Count}\n                |• Number of parts: {Server.world.rockets.Sum<KeyValuePair<int, RocketState>>((KeyValuePair<int, RocketState> r) => r.Value.parts.Count)}\n                |• Connection latency: {1000f * connectedPlayer.avgTripTime}ms\n                |• World time: {Server.world.WorldTime}\n                ".CleanDescription();
		}
		return "Could not find player!";
	}
}
}
