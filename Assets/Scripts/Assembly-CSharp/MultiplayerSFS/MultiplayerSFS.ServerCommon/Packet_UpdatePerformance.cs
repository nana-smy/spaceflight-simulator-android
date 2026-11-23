using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class Packet_UpdatePerformance : Packet
{
	public int PlayerId { get; set; } = -1;

	public float PerformanceScore { get; set; }

	public override PacketType Type => PacketType.UpdatePerformance;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(PlayerId);
		((NetBuffer)msg).Write(PerformanceScore);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		PlayerId = ((NetBuffer)msg).ReadInt32();
		PerformanceScore = ((NetBuffer)msg).ReadFloat();
	}
}
}
