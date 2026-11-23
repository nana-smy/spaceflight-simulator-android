using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_TimeWarpVote : Packet
{
	public float TimeScale { get; set; }

	public string RequesterName { get; set; }

	public int VoteId { get; set; }

	public bool PhysicsWarp { get; set; }

	public override PacketType Type => PacketType.TimeWarpVote;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(TimeScale);
		msg.Write(RequesterName);
		msg.Write(VoteId);
		msg.Write(PhysicsWarp);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		TimeScale = msg.ReadFloat();
		RequesterName = msg.ReadString();
		VoteId = msg.ReadInt32();
		PhysicsWarp = msg.ReadBoolean();
	}
}
}
