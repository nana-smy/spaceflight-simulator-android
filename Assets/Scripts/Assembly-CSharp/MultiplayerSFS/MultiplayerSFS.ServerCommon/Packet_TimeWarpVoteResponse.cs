using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class Packet_TimeWarpVoteResponse : Packet
{
	public int VoteId { get; set; }

	public bool Agreed { get; set; }

	public override PacketType Type => PacketType.TimeWarpVoteResponse;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(VoteId);
		((NetBuffer)msg).Write(Agreed);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		VoteId = ((NetBuffer)msg).ReadInt32();
		Agreed = ((NetBuffer)msg).ReadBoolean();
	}
}
}
