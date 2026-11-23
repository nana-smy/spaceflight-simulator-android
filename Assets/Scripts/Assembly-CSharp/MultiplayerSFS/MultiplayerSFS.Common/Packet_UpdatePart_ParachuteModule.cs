using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePart_ParachuteModule : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public int PartId { get; set; } = -1;

	public float State { get; set; }

	public float TargetState { get; set; }

	public override PacketType Type => PacketType.UpdatePart_ParachuteModule;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.Write(PartId);
		msg.Write(State);
		msg.Write(TargetState);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		PartId = msg.ReadInt32();
		State = msg.ReadFloat();
		TargetState = msg.ReadFloat();
	}
}
}
