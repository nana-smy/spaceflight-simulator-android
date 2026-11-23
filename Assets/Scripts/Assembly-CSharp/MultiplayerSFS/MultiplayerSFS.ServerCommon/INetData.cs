using Lidgren.Network;

namespace MultiplayerSFS.ServerCommon {
public interface INetData
{
	void Serialize(NetOutgoingMessage msg);

	void Deserialize(NetIncomingMessage msg);
}
}
