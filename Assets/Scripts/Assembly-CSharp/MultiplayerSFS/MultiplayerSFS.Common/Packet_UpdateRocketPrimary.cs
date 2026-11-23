using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdateRocketPrimary : Packet
{
	public double WorldTime { get; set; } = double.NaN;

	public int RocketId { get; set; } = -1;

	public NetLocation Location { get; set; }

	public float Rotation { get; set; }

	public float AngularVelocity { get; set; }

	public override PacketType Type => PacketType.UpdateRocketPrimary;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(WorldTime);
		msg.Write(RocketId);
		msg.Write(Location);
		msg.Write(Rotation);
		msg.Write(AngularVelocity);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		WorldTime = msg.ReadDouble();
		RocketId = msg.ReadInt32();
		Location = msg.Read<NetLocation>();
		Rotation = msg.ReadFloat();
		AngularVelocity = msg.ReadFloat();
	}
}
}
