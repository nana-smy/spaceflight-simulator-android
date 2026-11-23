using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePlayerControl : Packet
{
	public int PlayerId { get; set; } = -1;

	public int RocketId { get; set; } = -1;

	public override PacketType Type => PacketType.UpdatePlayerControl;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(PlayerId);
		msg.Write(RocketId);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		PlayerId = msg.ReadInt32();
		RocketId = msg.ReadInt32();
	}
}
}
