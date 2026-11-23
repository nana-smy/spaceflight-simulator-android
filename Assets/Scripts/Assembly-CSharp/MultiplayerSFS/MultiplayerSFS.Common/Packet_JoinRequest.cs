using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_JoinRequest : Packet
{
	public string Username { get; set; }

	public string Password { get; set; }

	public string SolarSystemName { get; set; } = "";

	public string GameVersion { get; set; } = "";

	public int ProtocolVersion { get; set; } = 1;

	public override PacketType Type => PacketType.JoinRequest;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(ProtocolVersion);
		msg.Write(Username);
		msg.Write(Password);
		msg.Write(SolarSystemName);
		msg.Write(GameVersion);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		ProtocolVersion = msg.ReadInt32();
		Username = msg.ReadString();
		Password = msg.ReadString();
		SolarSystemName = msg.ReadString();
		GameVersion = msg.ReadString();
	}
}
}
