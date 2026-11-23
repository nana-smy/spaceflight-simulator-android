using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePerformance : Packet
{
	public int PlayerId { get; set; } = -1;

	public float PerformanceScore { get; set; }

	public override PacketType Type => PacketType.UpdatePerformance;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(PlayerId);
		msg.Write(PerformanceScore);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		PlayerId = msg.ReadInt32();
		PerformanceScore = msg.ReadFloat();
	}
}
}
