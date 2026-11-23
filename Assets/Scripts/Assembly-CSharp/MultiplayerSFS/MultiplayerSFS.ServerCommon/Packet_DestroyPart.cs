using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class Packet_DestroyPart : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public int PartId { get; set; } = -1;

	public bool CreateExplosion { get; set; }

	public int Reason { get; set; }

	public override PacketType Type => PacketType.DestroyPart;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(WorldTime);
		((NetBuffer)msg).Write(RocketId);
		((NetBuffer)msg).Write(PartId);
		((NetBuffer)msg).Write(CreateExplosion);
		((NetBuffer)msg).Write((byte)Reason);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = ((NetBuffer)msg).ReadDouble();
		RocketId = ((NetBuffer)msg).ReadInt32();
		PartId = ((NetBuffer)msg).ReadInt32();
		CreateExplosion = ((NetBuffer)msg).ReadBoolean();
		Reason = ((NetBuffer)msg).ReadByte();
	}
}
}
