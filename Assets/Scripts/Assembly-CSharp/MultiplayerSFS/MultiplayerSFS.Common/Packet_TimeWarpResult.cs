using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_TimeWarpResult : Packet
{
	public int VoteId { get; set; }

	public bool Approved { get; set; }

	public float TimeScale { get; set; }

	public bool PhysicsWarp { get; set; }

	public string StopperName { get; set; } = "";

	public string RejecterName { get; set; } = "";

	public override PacketType Type => PacketType.TimeWarpResult;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(VoteId);
		msg.Write(Approved);
		msg.Write(TimeScale);
		msg.Write(PhysicsWarp);
		msg.Write(StopperName);
		msg.Write(RejecterName);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		VoteId = msg.ReadInt32();
		Approved = msg.ReadBoolean();
		TimeScale = msg.ReadFloat();
		PhysicsWarp = msg.ReadBoolean();
		StopperName = msg.ReadString();
		RejecterName = msg.ReadString();
	}
}
}
