using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class Packet_HostChanged : Packet
{
	public int HostPlayerId { get; set; } = -1;

	public override PacketType Type => PacketType.HostChanged;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(HostPlayerId);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		HostPlayerId = ((NetBuffer)msg).ReadInt32();
	}
}
}
