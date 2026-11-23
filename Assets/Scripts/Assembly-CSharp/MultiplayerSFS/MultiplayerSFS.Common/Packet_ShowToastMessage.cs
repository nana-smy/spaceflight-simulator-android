using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_ShowToastMessage : Packet
{
	public string Message { get; set; }

	public override PacketType Type => PacketType.ShowToastMessage;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(Message);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		Message = msg.ReadString();
	}
}
}
