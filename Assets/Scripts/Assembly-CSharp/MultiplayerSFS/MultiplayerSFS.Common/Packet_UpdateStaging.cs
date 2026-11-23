using System.Collections.Generic;
using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdateStaging : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public List<StageState> Stages { get; set; }

	public override PacketType Type => PacketType.UpdateStaging;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.WriteCollection(Stages, msg.Write);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		Stages = msg.ReadCollection((int count) => new List<StageState>(count), () => msg.Read<StageState>());
	}
}
}
