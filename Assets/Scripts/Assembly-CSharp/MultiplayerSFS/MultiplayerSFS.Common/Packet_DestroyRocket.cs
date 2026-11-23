using Lidgren.Network;
using SFS.World;

namespace MultiplayerSFS.Common {
public class Packet_DestroyRocket : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public DestructionReason Reason { get; set; }

	public override PacketType Type => PacketType.DestroyRocket;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.Write((byte)Reason);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		Reason = (DestructionReason)msg.ReadByte();
	}
}
}
