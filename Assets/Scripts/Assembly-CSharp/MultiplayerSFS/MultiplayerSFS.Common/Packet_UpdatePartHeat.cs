using System.Collections.Generic;
using Lidgren.Network;

namespace MultiplayerSFS.Common {
public class Packet_UpdatePartHeat : Packet
{
	public int RocketId { get; set; } = -1;

	public List<int> PartIds { get; set; }

	public List<float> Temperatures { get; set; }

	public override PacketType Type => PacketType.UpdatePartHeat;

	public override void Serialize(NetOutgoingMessage msg)
	{
		msg.Write(RocketId);
		msg.Write(PartIds.Count);
		foreach (int id in PartIds)
		{
			msg.Write(id);
		}
		msg.Write(Temperatures.Count);
		foreach (float temperature in Temperatures)
		{
			msg.Write(temperature);
		}
	}

	public override void Deserialize(NetIncomingMessage msg)
	{
		RocketId = msg.ReadInt32();
		int partCount = msg.ReadInt32();
		if (partCount < 0 || partCount > 100000)
		{
			throw new System.IO.InvalidDataException("Invalid part count: " + partCount);
		}
		PartIds = new List<int>(partCount);
		for (int i = 0; i < partCount; i++)
		{
			PartIds.Add(msg.ReadInt32());
		}
		int tempCount = msg.ReadInt32();
		if (tempCount != partCount)
		{
			throw new System.IO.InvalidDataException("Part/temperature count mismatch: " + partCount + " vs " + tempCount);
		}
		Temperatures = new List<float>(tempCount);
		for (int j = 0; j < tempCount; j++)
		{
			Temperatures.Add(msg.ReadFloat());
		}
	}
}
}
