using System.Collections.Generic;
using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public class StageState : INetData
{
	public int stageID;

	public List<int> partIDs;

	public StageState()
	{
		partIDs = new List<int>();
	}

	public void Serialize(NetOutgoingMessage msg)
	{
		msg.WriteCompressedInt(stageID);
		msg.WriteCollection(partIDs, msg.WriteCompressedInt);
	}

	public void Deserialize(NetIncomingMessage msg)
	{
		stageID = msg.ReadCompressedInt();
		partIDs = msg.ReadCollection((int count) => new List<int>(), msg.ReadCompressedInt);
	}
}
}
