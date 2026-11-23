using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MultiplayerSFS.ServerCommon;
using UnityEngine;

namespace MultiplayerSFS.Server {
public class ConnectedPlayer
{
	public int id;

	public bool isAdmin;

	public string username;

	public Color iconColor;

	public float avgTripTime;

	public float performanceScore;

	public float loadRange;

	public string solarSystemName = "";

	public int controlledRocket;

	public HashSet<int> updateAuthority;

	public HashSet<int> loadRangeRocketIds;

	public DateTime lastChatTime = DateTime.MinValue;

	private static readonly System.Random colorRandom = new System.Random();

	private static Color GetRandomColor()
	{
		return Color.HSVToRGB((float)Math.Round(100.0 * colorRandom.NextDouble()) / 100f, 1f, 1f);
	}

	public ConnectedPlayer(string playerName)
	{
		id = Server.connectedPlayers.Select<KeyValuePair<IPEndPoint, ConnectedPlayer>, int>((KeyValuePair<IPEndPoint, ConnectedPlayer> kvp) => kvp.Value.id).ToHashSet().InsertNew();
		isAdmin = false;
		username = playerName;
		iconColor = GetRandomColor();
		avgTripTime = 0f;
		performanceScore = 0f;
		loadRange = (float)Server.settings.loadRange;
		controlledRocket = -1;
		updateAuthority = new HashSet<int>();
		loadRangeRocketIds = new HashSet<int>();
	}
}
}
