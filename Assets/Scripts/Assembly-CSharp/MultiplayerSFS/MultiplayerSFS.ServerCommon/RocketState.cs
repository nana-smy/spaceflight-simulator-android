using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;

namespace MultiplayerSFS.ServerCommon {
public class RocketState : INetData
{
	public string rocketName;

	public NetLocation location;

	public float rotation;

	public float angularVelocity;

	public bool throttleOn;

	public float throttlePercent;

	public bool RCS;

	public float input_Turn;

	public Vector2 input_Raw;

	public Vector2 input_Horizontal;

	public Vector2 input_Vertical;

	public Dictionary<int, PartState> parts;

	public List<JointState> joints;

	public List<StageState> stages;

	public int ownerId = -1;

	public RocketState()
	{
		parts = new Dictionary<int, PartState>();
		joints = new List<JointState>();
		stages = new List<StageState>();
		location = new NetLocation();
	}

	public void Serialize(NetOutgoingMessage msg)
	{
		msg.WriteCompressedString(rocketName);
		msg.Write(location);
		msg.WriteCompressedFloat(rotation);
		msg.WriteCompressedFloat(angularVelocity);
		((NetBuffer)msg).Write(throttleOn);
		msg.WriteCompressedFloat(throttlePercent);
		((NetBuffer)msg).Write(RCS);
		((NetBuffer)msg).Write(input_Turn);
		((NetBuffer)msg).Write(input_Raw.x);
		((NetBuffer)msg).Write(input_Raw.y);
		((NetBuffer)msg).Write(input_Horizontal.x);
		((NetBuffer)msg).Write(input_Horizontal.y);
		((NetBuffer)msg).Write(input_Vertical.x);
		((NetBuffer)msg).Write(input_Vertical.y);
		((NetBuffer)msg).Write(ownerId);
		msg.WriteCollection<KeyValuePair<int, PartState>>(parts, delegate(KeyValuePair<int, PartState> kvp)
		{
			msg.WriteCompressedInt(kvp.Key);
			msg.Write(kvp.Value);
		});
		msg.WriteCollection(joints, msg.Write);
		msg.WriteCollection(stages, msg.Write);
	}

	public void Deserialize(NetIncomingMessage msg)
	{
		rocketName = msg.ReadCompressedString();
		location = msg.Read<NetLocation>();
		rotation = msg.ReadCompressedFloat();
		angularVelocity = msg.ReadCompressedFloat();
		throttleOn = ((NetBuffer)msg).ReadBoolean();
		throttlePercent = msg.ReadCompressedFloat();
		RCS = ((NetBuffer)msg).ReadBoolean();
		input_Turn = ((NetBuffer)msg).ReadFloat();
		input_Raw = new Vector2(((NetBuffer)msg).ReadFloat(), ((NetBuffer)msg).ReadFloat());
		input_Horizontal = new Vector2(((NetBuffer)msg).ReadFloat(), ((NetBuffer)msg).ReadFloat());
		input_Vertical = new Vector2(((NetBuffer)msg).ReadFloat(), ((NetBuffer)msg).ReadFloat());
		ownerId = ((NetBuffer)msg).ReadInt32();
		parts = msg.ReadCollection((int count) => new Dictionary<int, PartState>(), () => new KeyValuePair<int, PartState>(msg.ReadCompressedInt(), msg.Read<PartState>()));
		joints = msg.ReadCollection((int count) => new List<JointState>(count), () => msg.Read<JointState>());
		stages = msg.ReadCollection((int count) => new List<StageState>(count), () => msg.Read<StageState>());
	}

	public void UpdateRocketPrimary(Packet_UpdateRocketPrimary packet)
	{
		location = packet.Location;
		rotation = packet.Rotation;
		angularVelocity = packet.AngularVelocity;
	}

	public void UpdateRocketSecondary(Packet_UpdateRocketSecondary packet)
	{
		input_Turn = packet.Input_Turn;
		input_Raw = packet.Input_Raw;
		input_Horizontal = packet.Input_Horizontal;
		input_Vertical = packet.Input_Vertical;
		throttlePercent = packet.ThrottlePercent;
		throttleOn = packet.ThrottleOn;
		RCS = packet.RCS;
	}

	public bool RemovePart(int id)
	{
		joints.RemoveAll((JointState j) => j.id_A == id || j.id_B == id);
		foreach (StageState stage in stages)
		{
			stage.partIDs.RemoveAll((int p) => p == id);
		}
		return parts.Remove(id);
	}
}
}
