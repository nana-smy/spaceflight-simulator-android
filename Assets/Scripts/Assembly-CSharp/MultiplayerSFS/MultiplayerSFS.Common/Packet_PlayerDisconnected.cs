using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_PlayerDisconnected : Packet
{
	public int PlayerId { get; set; } = -1;

	public override PacketType Type => PacketType.PlayerDisconnected;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(PlayerId);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		PlayerId = msg.ReadInt32();
	}
}
}
