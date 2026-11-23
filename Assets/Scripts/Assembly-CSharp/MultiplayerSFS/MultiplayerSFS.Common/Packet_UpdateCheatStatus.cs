using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdateCheatStatus : Packet
{
	public bool InfiniteFuel { get; set; }

	public bool NoAtmosphericDrag { get; set; }

	public bool UnbreakableParts { get; set; }

	public bool NoGravity { get; set; }

	public bool NoHeatDamage { get; set; }

	public bool NoBurnMarks { get; set; }

	public bool InfiniteBuildArea { get; set; }

	public bool PartClipping { get; set; }

	public override PacketType Type => PacketType.UpdateCheatStatus;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(InfiniteFuel);
		msg.Write(NoAtmosphericDrag);
		msg.Write(UnbreakableParts);
		msg.Write(NoGravity);
		msg.Write(NoHeatDamage);
		msg.Write(NoBurnMarks);
		msg.Write(InfiniteBuildArea);
		msg.Write(PartClipping);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		InfiniteFuel = msg.ReadBoolean();
		NoAtmosphericDrag = msg.ReadBoolean();
		UnbreakableParts = msg.ReadBoolean();
		NoGravity = msg.ReadBoolean();
		NoHeatDamage = msg.ReadBoolean();
		NoBurnMarks = msg.ReadBoolean();
		InfiniteBuildArea = msg.ReadBoolean();
		PartClipping = msg.ReadBoolean();
	}
}
}
