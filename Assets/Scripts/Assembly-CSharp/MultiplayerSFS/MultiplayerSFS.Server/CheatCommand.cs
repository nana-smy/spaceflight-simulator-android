using Lidgren.Network;
using MultiplayerSFS.ServerCommon;

namespace MultiplayerSFS.Server {
public class CheatCommand : Command
{
	public override string Description => $"\n        |Controls cheat settings for players. This command requires admin privileges.\n        |{"cheat".FormatCommand("<cheat> <value>")}:\n        |  <cheat>: The cheat to modify (e.g., infinitefuel)\n        |  <value>: true or false to enable or disable the cheat\n        |Example: {"cheat".FormatCommand("infinitefuel true")}\n        ".CleanDescription();

	public override string Run(string[] args, NetConnection sender)
	{
		if (!CheckAdmin(sender))
		{
			return "You do not have the required admin privileges to use this command.";
		}
		if (args.Length != 2)
		{
			return "Incorrect number of arguments. Usage: /cheat <cheat> <true|false>";
		}
		string text = args[0].ToLower();
		if (!CommandExtensions.TryParseBool(args[1], out var result))
		{
			return "Invalid value. Please use 'true' or 'false'.";
		}
		switch (text)
		{
		case "infinitefuel":
			Server.world.infiniteFuel = result;
			break;
		case "noatmosphericdrag":
			Server.world.noAtmosphericDrag = result;
			break;
		case "unbreakableparts":
			Server.world.unbreakableParts = result;
			break;
		case "nogravity":
			Server.world.noGravity = result;
			break;
		case "noheatdamage":
			Server.world.noHeatDamage = result;
			break;
		case "noburnmarks":
			Server.world.noBurnMarks = result;
			break;
		case "infinitebuildarea":
			Server.world.infiniteBuildArea = result;
			break;
		case "partclipping":
			Server.world.partClipping = result;
			break;
		default:
			return "Unknown cheat: " + text;
		}
		Server.SendPacketToAll(new Packet_UpdateCheatStatus
		{
			InfiniteFuel = Server.world.infiniteFuel,
			NoAtmosphericDrag = Server.world.noAtmosphericDrag,
			UnbreakableParts = Server.world.unbreakableParts,
			NoGravity = Server.world.noGravity,
			NoHeatDamage = Server.world.noHeatDamage,
			NoBurnMarks = Server.world.noBurnMarks,
			InfiniteBuildArea = Server.world.infiniteBuildArea,
			PartClipping = Server.world.partClipping
		}, null, (NetDeliveryMethod)67);
		return $"Set {text} to {result}";
	}
}
}
