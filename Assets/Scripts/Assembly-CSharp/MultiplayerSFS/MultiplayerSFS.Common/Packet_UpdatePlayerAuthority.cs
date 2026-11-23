using System.Collections.Generic;
using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePlayerAuthority : Packet
{
	public HashSet<int> RocketIds { get; set; }

	public HashSet<int> LoadedRocketIds { get; set; }

	public override PacketType Type => PacketType.UpdatePlayerAuthority;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.WriteCollection(RocketIds, msg.Write);
		msg.WriteCollection(LoadedRocketIds, msg.Write);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		RocketIds = msg.ReadCollection((int count) => new HashSet<int>(count), msg.ReadInt32);
		LoadedRocketIds = msg.ReadCollection((int count) => new HashSet<int>(count), msg.ReadInt32);
	}
}
}
