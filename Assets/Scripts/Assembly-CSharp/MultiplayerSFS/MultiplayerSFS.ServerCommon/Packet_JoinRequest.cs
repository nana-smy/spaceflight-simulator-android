using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
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
		((NetBuffer)msg).Write(ProtocolVersion);
		((NetBuffer)msg).Write(Username);
		((NetBuffer)msg).Write(Password);
		((NetBuffer)msg).Write(SolarSystemName);
		((NetBuffer)msg).Write(GameVersion);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		ProtocolVersion = ((NetBuffer)msg).ReadInt32();
		Username = ((NetBuffer)msg).ReadString();
		Password = ((NetBuffer)msg).ReadString();
		SolarSystemName = ((NetBuffer)msg).ReadString();
		GameVersion = ((NetBuffer)msg).ReadString();
	}
}
}
