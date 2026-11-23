using Lidgren.Network;
using SFS.World;

namespace MultiplayerSFS.Common {
public class Packet_DestroyPart : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public int PartId { get; set; } = -1;

	public bool CreateExplosion { get; set; }

	public DestructionReason Reason { get; set; }

	public override PacketType Type => PacketType.DestroyPart;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.Write(PartId);
		msg.Write(CreateExplosion);
		msg.Write((byte)Reason);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		PartId = msg.ReadInt32();
		CreateExplosion = msg.ReadBoolean();
		Reason = (DestructionReason)msg.ReadByte();
	}
}
}
