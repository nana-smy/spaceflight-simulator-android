using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_HostChanged : Packet
{
	public int HostPlayerId { get; set; } = -1;

	public override PacketType Type => PacketType.HostChanged;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(HostPlayerId);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		HostPlayerId = msg.ReadInt32();
	}
}
}
