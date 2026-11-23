using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class Packet_SolarSystemFile : Packet
{
	public string FileName { get; set; } = "";

	public byte[] Data { get; set; }

	public bool IsLast { get; set; }

	public override PacketType Type => PacketType.SolarSystemFile;

	public override void Serialize(NetOutgoingMessage msg)
	{
		((NetBuffer)msg).Write(FileName);
		((NetBuffer)msg).Write(Data == null ? 0 : Data.Length);
		if (Data != null && Data.Length > 0)
		{
			((NetBuffer)msg).Write(Data);
		}
		((NetBuffer)msg).Write(IsLast);
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		FileName = ((NetBuffer)msg).ReadString();
		if (FileName != "" && (FileName.Contains("..") || FileName.StartsWith("/") || FileName.StartsWith("\\")))
		{
			throw new System.IO.InvalidDataException("Invalid solar system file name: " + FileName);
		}
		int length = ((NetBuffer)msg).ReadInt32();
		if (length < 0 || length > 10485760)
		{
			throw new System.IO.InvalidDataException("Invalid solar system file length: " + length);
		}
		Data = ((length > 0) ? ((NetBuffer)msg).ReadBytes(length) : null);
		IsLast = ((NetBuffer)msg).ReadBoolean();
	}
}
}
