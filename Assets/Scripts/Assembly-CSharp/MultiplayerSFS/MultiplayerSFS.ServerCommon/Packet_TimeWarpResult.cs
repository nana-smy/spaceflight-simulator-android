using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
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
		((NetBuffer)msg).Write(VoteId);
		((NetBuffer)msg).Write(Approved);
		((NetBuffer)msg).Write(TimeScale);
		((NetBuffer)msg).Write(PhysicsWarp);
		((NetBuffer)msg).Write(StopperName);
		((NetBuffer)msg).Write(RejecterName);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		VoteId = ((NetBuffer)msg).ReadInt32();
		Approved = ((NetBuffer)msg).ReadBoolean();
		TimeScale = ((NetBuffer)msg).ReadFloat();
		PhysicsWarp = ((NetBuffer)msg).ReadBoolean();
		StopperName = ((NetBuffer)msg).ReadString();
		RejecterName = ((NetBuffer)msg).ReadString();
	}
}
}
