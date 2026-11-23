using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using MultiplayerSFS.ServerCommon;

namespace MultiplayerSFS.Server {
public class DestroyCommand : Command
{
	public override string Description => $"\n        |Destroys all rockets that meet the criteria provided in the arguments. This command requires admin privileges.\n        |{"destroy".FormatCommand("-a")}: Destroys <b>all</b> rockets in the world.\n        |{"destroy".FormatCommand("-p {planet}")}: Destroys all rockets at a provided planet.\n        ".CleanDescription();

	public override string Run(string[] args, NetConnection sender)
	{
		if (!CheckAdmin(sender))
		{
			return "You do not have the required admin privileges to use this command.";
		}
		if (args.Length == 0)
		{
			return "Not enough arguments provided.";
		}
		if (args[0] == "-a")
		{
			Logger.Info("Recieved " + "destroy".FormatCommand("-a") + " command, destroying all rockets...");
			int value = DestroyRockets(Server.world.rockets.Keys.ToList());
			return $"Destroyed {value} rocket(s).";
		}
		if (args[0] == "-p")
		{
			if (args.Length < 2)
			{
				return "Not enough arguments provided.";
			}
			string planetName = string.Join(" ", args.Skip(1));
			Logger.Info($"Recieved {"destroy".FormatCommand("-p " + planetName)} command, destroying all rockets at \"{planetName}\"...");
			List<int> ids = (from kvp in Server.world.rockets
				where kvp.Value.location.address == planetName
				select kvp.Key).ToList();
			int value2 = DestroyRockets(ids);
			return $"Destroyed {value2} rocket(s) at \"{planetName}\".";
		}
		return "Provided arguments were invalid.";
	}

	private int DestroyRockets(IEnumerable<int> ids)
	{
		int num = 0;
		foreach (int id in ids)
		{
			if (Server.world.rockets.Remove(id))
			{
				Server.SendPacketToAll(new Packet_DestroyRocket
				{
					RocketId = id
				}, null, (NetDeliveryMethod)67);
				num++;
			}
		}
		return num;
	}
}
}
