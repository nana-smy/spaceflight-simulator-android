using System.Collections.Generic;
using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePart_ResourceModule : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public double ResourcePercent { get; set; }

	public HashSet<int> PartIds { get; set; }

	public override PacketType Type => PacketType.UpdatePart_ResourceModule;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.Write(ResourcePercent);
		msg.WriteCollection(PartIds, msg.Write);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		ResourcePercent = msg.ReadDouble();
		PartIds = msg.ReadCollection((int count) => new HashSet<int>(count), msg.ReadInt32);
	}
}
}
