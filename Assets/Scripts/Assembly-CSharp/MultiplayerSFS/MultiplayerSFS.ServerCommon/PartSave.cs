using System.Collections.Generic;
using Lidgren.Network;
using SFS.Parts.Modules;
using UnityEngine;

namespace MultiplayerSFS.ServerCommon {
public class PartSave : INetData
{
	public string name;

	public Vector2 position;

	public Orientation orientation;

	public float temperature;

	public Dictionary<string, double> NUMBER_VARIABLES;

	public Dictionary<string, bool> TOGGLE_VARIABLES;

	public Dictionary<string, string> TEXT_VARIABLES;

	public BurnMark.BurnSave burns;

	public PartSave()
	{
		NUMBER_VARIABLES = new Dictionary<string, double>();
		TOGGLE_VARIABLES = new Dictionary<string, bool>();
		TEXT_VARIABLES = new Dictionary<string, string>();
	}

	public void Serialize(NetOutgoingMessage msg)
	{
		msg.WriteCompressedString(name);
		msg.WriteCompressedVector2(position);
		msg.WriteCompressedOrientation(orientation);
		msg.WriteCompressedFloat(temperature);
		msg.WriteCollection<KeyValuePair<string, double>>(NUMBER_VARIABLES, delegate(KeyValuePair<string, double> kvp)
		{
			msg.WriteCompressedString(kvp.Key);
			msg.WriteCompressedDouble(kvp.Value);
		});
		msg.WriteCollection<KeyValuePair<string, bool>>(TOGGLE_VARIABLES, delegate(KeyValuePair<string, bool> kvp)
		{
			msg.WriteCompressedString(kvp.Key);
			((NetBuffer)msg).Write(kvp.Value);
		});
		msg.WriteCollection<KeyValuePair<string, string>>(TEXT_VARIABLES, delegate(KeyValuePair<string, string> kvp)
		{
			msg.WriteCompressedString(kvp.Key);
			msg.WriteCompressedString(kvp.Value);
		});
		msg.WriteCompressedBurnSave(burns);
	}

	public void Deserialize(NetIncomingMessage msg)
	{
		name = msg.ReadCompressedString();
		position = msg.ReadCompressedVector2();
		orientation = msg.ReadCompressedOrientation();
		temperature = msg.ReadCompressedFloat();
		NUMBER_VARIABLES = msg.ReadCollection((int count) => new Dictionary<string, double>(count), () => new KeyValuePair<string, double>(msg.ReadCompressedString(), msg.ReadCompressedDouble()));
		TOGGLE_VARIABLES = msg.ReadCollection((int count) => new Dictionary<string, bool>(count), () => new KeyValuePair<string, bool>(msg.ReadCompressedString(), ((NetBuffer)msg).ReadBoolean()));
		TEXT_VARIABLES = msg.ReadCollection((int count) => new Dictionary<string, string>(count), () => new KeyValuePair<string, string>(msg.ReadCompressedString(), msg.ReadCompressedString()));
		burns = msg.ReadCompressedBurnSave();
	}
}
}
