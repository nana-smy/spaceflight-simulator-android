using Lidgren.Network;
using UnityEngine;

namespace MultiplayerSFS.Common {
public class Packet_SendChatMessage : Packet
{
	public int SenderId { get; set; } = -1;

	public string Message { get; set; }

	public Color Color { get; set; } = Color.white;

	public override PacketType Type => PacketType.SendChatMessage;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(SenderId);
		msg.Write(Message);
		msg.Write(Color.r);
		msg.Write(Color.g);
		msg.Write(Color.b);
		msg.Write(Color.a);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		SenderId = msg.ReadInt32();
		Message = msg.ReadString();
		float r = msg.ReadFloat();
		float g = msg.ReadFloat();
		float b = msg.ReadFloat();
		float a = msg.ReadFloat();
		Color = new Color(r, g, b, a);
	}
}
}
