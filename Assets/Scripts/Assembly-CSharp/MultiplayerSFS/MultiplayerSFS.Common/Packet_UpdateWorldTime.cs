using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdateWorldTime : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public override PacketType Type => PacketType.UpdateWorldTime;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
	}
}
}
