using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_TimeWarpRequest : Packet
{
	public float TimeScale { get; set; }

	public string RequesterName { get; set; }

	public bool PhysicsWarp { get; set; }

	public override PacketType Type => PacketType.TimeWarpRequest;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(TimeScale);
		msg.Write(RequesterName);
		msg.Write(PhysicsWarp);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		TimeScale = msg.ReadFloat();
		RequesterName = msg.ReadString();
		PhysicsWarp = msg.ReadBoolean();
	}
}
}
