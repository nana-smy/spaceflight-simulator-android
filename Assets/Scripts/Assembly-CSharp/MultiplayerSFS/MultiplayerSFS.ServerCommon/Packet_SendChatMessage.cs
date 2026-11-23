using System.Drawing;
using Lidgren.Network;
using UnityEngine;

namespace MultiplayerSFS.ServerCommon {
public class Packet_SendChatMessage : Packet
{
	public int SenderId { get; set; } = -1;

	public string Message { get; set; }

	public System.Drawing.Color Color { get; set; } = System.Drawing.Color.White;

	public override PacketType Type => PacketType.SendChatMessage;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(SenderId);
		((NetBuffer)msg).Write(Message);
		((NetBuffer)msg).Write((float)(int)Color.R / 255f);
		((NetBuffer)msg).Write((float)(int)Color.G / 255f);
		((NetBuffer)msg).Write((float)(int)Color.B / 255f);
		((NetBuffer)msg).Write((float)(int)Color.A / 255f);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		SenderId = ((NetBuffer)msg).ReadInt32();
		Message = ((NetBuffer)msg).ReadString();
		float num = ((NetBuffer)msg).ReadFloat();
		float num2 = ((NetBuffer)msg).ReadFloat();
		float num3 = ((NetBuffer)msg).ReadFloat();
		float num4 = ((NetBuffer)msg).ReadFloat();
		Color = System.Drawing.Color.FromArgb((int)(num4 * 255f), (int)(num * 255f), (int)(num2 * 255f), (int)(num3 * 255f));
	}
}
}
