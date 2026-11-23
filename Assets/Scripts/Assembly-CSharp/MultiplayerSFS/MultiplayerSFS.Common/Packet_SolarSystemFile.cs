using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_SolarSystemFile : Packet
{
	public string FileName { get; set; } = "";

	public byte[] Data { get; set; }

	public bool IsLast { get; set; }

	public override PacketType Type => PacketType.SolarSystemFile;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(FileName);
		msg.Write(Data == null ? 0 : Data.Length);
		if (Data != null && Data.Length > 0)
		{
			msg.Write(Data);
		}
		msg.Write(IsLast);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		FileName = msg.ReadString();
		if (FileName != "" && (FileName.Contains("..") || FileName.StartsWith("/") || FileName.StartsWith("\\")))
		{
			throw new System.IO.InvalidDataException("Invalid solar system file name: " + FileName);
		}
		int length = msg.ReadInt32();
		if (length < 0 || length > 10485760)
		{
			throw new System.IO.InvalidDataException("Invalid solar system file length: " + length);
		}
		Data = ((length > 0) ? msg.ReadBytes(length) : null);
		IsLast = msg.ReadBoolean();
	}
}
}
