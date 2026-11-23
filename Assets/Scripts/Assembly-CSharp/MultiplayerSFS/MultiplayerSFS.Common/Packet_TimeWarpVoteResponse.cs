using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_TimeWarpVoteResponse : Packet
{
	public int VoteId { get; set; }

	public bool Agreed { get; set; }

	public override PacketType Type => PacketType.TimeWarpVoteResponse;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(VoteId);
		msg.Write(Agreed);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		VoteId = msg.ReadInt32();
		Agreed = msg.ReadBoolean();
	}
}
}
