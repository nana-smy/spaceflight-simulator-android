using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePart_MoveModule : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public int PartId { get; set; } = -1;

	public float Time { get; set; }

	public float TargetTime { get; set; }

	public override PacketType Type => PacketType.UpdatePart_MoveModule;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.Write(PartId);
		msg.Write(Time);
		msg.Write(TargetTime);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		PartId = msg.ReadInt32();
		Time = msg.ReadFloat();
		TargetTime = msg.ReadFloat();
	}
}
}
