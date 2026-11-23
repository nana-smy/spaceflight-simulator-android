using Lidgren.Network;
using SFS.WorldBase;

namespace MultiplayerSFS.Common {
public class Packet_JoinResponse : Packet
{
	public int PlayerId { get; set; } = -1;

	public double UpdateRocketsPeriod { get; set; }

	public double ChatMessageCooldown { get; set; }

	public double WorldTime { get; set; } = double.NaN;

	public double SendTime { get; set; }

	public Difficulty.DifficultyType Difficulty { get; set; }

	public string SolarSystemName { get; set; } = "";

	public int HostPlayerId { get; set; } = -1;

	public override PacketType Type => PacketType.JoinResponse;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(PlayerId);
		msg.Write(UpdateRocketsPeriod);
		msg.Write(ChatMessageCooldown);
		msg.Write(WorldTime);
		msg.Write(SendTime);
		msg.Write((byte)Difficulty);
		msg.Write(SolarSystemName);
		msg.Write(HostPlayerId);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		PlayerId = msg.ReadInt32();
		UpdateRocketsPeriod = msg.ReadDouble();
		ChatMessageCooldown = msg.ReadDouble();
		WorldTime = msg.ReadDouble();
		SendTime = msg.ReadDouble();
		Difficulty = (Difficulty.DifficultyType)msg.ReadByte();
		SolarSystemName = msg.ReadString();
		HostPlayerId = msg.ReadInt32();
	}
}
}
