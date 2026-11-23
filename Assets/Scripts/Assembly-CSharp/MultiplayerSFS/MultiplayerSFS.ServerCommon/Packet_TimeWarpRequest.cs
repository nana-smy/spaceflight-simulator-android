using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class Packet_TimeWarpRequest : Packet
{
	public float TimeScale { get; set; }

	public string RequesterName { get; set; }

	public bool PhysicsWarp { get; set; }

	public override PacketType Type => PacketType.TimeWarpRequest;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(TimeScale);
		((NetBuffer)msg).Write(RequesterName);
		((NetBuffer)msg).Write(PhysicsWarp);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		TimeScale = ((NetBuffer)msg).ReadFloat();
		RequesterName = ((NetBuffer)msg).ReadString();
		PhysicsWarp = ((NetBuffer)msg).ReadBoolean();
	}
}
}
