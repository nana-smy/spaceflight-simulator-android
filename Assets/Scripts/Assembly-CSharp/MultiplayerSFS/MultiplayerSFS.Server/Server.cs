using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Threading;
using Lidgren.Network;
using MultiplayerSFS.ServerCommon;
using SFS.IO;
using SFS.World;
using UnityEngine;

namespace MultiplayerSFS.Server {
public static class Server
{
	public static NetServer server;

	public static ServerSettings settings;

	public static WorldState world;

	public static Dictionary<IPEndPoint, ConnectedPlayer> connectedPlayers;

	private static DateTime lastAuthorityUpdate = DateTime.MinValue;

	private const int AuthorityUpdateIntervalMs = 200;

	private static DateTime lastWorldSave = DateTime.MinValue;

	private static HashSet<int> controlledRocketsCache = new HashSet<int>();

	private static Dictionary<int, Double2> playerPositionsCache = new Dictionary<int, Double2>();

	private static int currentVoteId = 0;

	private static bool isVoting = false;

	private static bool isTimeWarping = false;

	private static int hostPlayerId = -1;

	private static bool stopped = false;

	public static volatile bool isRunning = false;

	private static float currentTimeScale = 1f;

	private static bool currentPhysicsWarp = false;

	private static Dictionary<int, bool> currentVotes = new Dictionary<int, bool>();

	private static int totalVoters = 0;

	private static float requestedTimeScale = 1f;

	private static string requesterName = "";

	private static bool requestedPhysicsWarp = false;

	private static DateTime voteStartTime = DateTime.MinValue;

	public static void Initialize(ServerSettings settings)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		Server.settings = settings;
		NetPeerConfiguration val = new NetPeerConfiguration("multiplayersfs")
		{
			Port = settings.port,
			MaximumConnections = settings.maxConnections
		};
		val.EnableMessageType((NetIncomingMessageType)1);
		val.EnableMessageType((NetIncomingMessageType)4);
		val.EnableMessageType((NetIncomingMessageType)4096);
		val.EnableMessageType((NetIncomingMessageType)128);
		if (settings.networkTimeout > 0)
		{
			val.ConnectionTimeout = settings.networkTimeout;
		}
		try
		{
			// GCSettings.IsServerGC 是只读属性，无法在运行时设置；这里只调整延迟模式。
			GCSettings.LatencyMode = settings.enableServerGC ? GCLatencyMode.Batch : GCLatencyMode.Interactive;
		}
		catch (Exception ex)
		{
			Logger.Warning("Failed to apply GC settings: " + ex.Message);
		}
		try
		{
			world = new WorldState(settings.worldSavePath);
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to initialize world state: " + ex.Message);
			Logger.Error("World save path: " + settings.worldSavePath);
			Logger.Error("Please ensure the path exists and the application has proper permissions.");
			throw;
		}
		connectedPlayers = new Dictionary<IPEndPoint, ConnectedPlayer>();
		lastWorldSave = DateTime.Now;
		stopped = false;
		hostPlayerId = -1;
		isTimeWarping = false;
		currentTimeScale = 1f;
		currentPhysicsWarp = false;
		server = new NetServer(val);
		((NetPeer)server).Start();
	}

	public static void Run()
	{
		isRunning = true;
		try
		{
			Logger.Info($"Multiplayer SFS server started, listening for connections on port {((NetPeer)server).Port}...", important: true);
			while (!stopped)
			{
				Listen();
				if (stopped)
				{
					break;
				}
				int authorityInterval = settings.authorityUpdateInterval > 0 ? settings.authorityUpdateInterval : 200;
				if ((DateTime.Now - lastAuthorityUpdate).TotalMilliseconds >= authorityInterval)
				{
					UpdatePlayerAuthorities();
					lastAuthorityUpdate = DateTime.Now;
				}
				if (isVoting && settings.timeWarpVoteTimeout > 0 && (DateTime.Now - voteStartTime).TotalSeconds >= settings.timeWarpVoteTimeout)
				{
					isVoting = false;
					SendPacketToAll(new Packet_TimeWarpResult
					{
						VoteId = currentVoteId,
						Approved = false,
						TimeScale = 1f,
						PhysicsWarp = true
					}, null, (NetDeliveryMethod)67);
					Logger.Info("Time warp vote " + currentVoteId + " timed out");
				}
				if ((DateTime.Now - lastWorldSave).TotalSeconds >= (double)settings.worldSaveInterval)
				{
					world.SaveWorld();
					lastWorldSave = DateTime.Now;
				}
				Thread.Sleep(Math.Max(1, settings.serverTickRate));
			}
			Logger.Info("Server stopped.");
		}
		catch (Exception exception)
		{
			Logger.Error(exception);
			try
			{
				world?.SaveWorld();
			}
			catch (Exception saveEx)
			{
				Logger.Error("Failed to save world during shutdown: " + saveEx.Message);
			}
			try
			{
				server?.Shutdown("Server error");
			}
			catch (Exception shutdownEx)
			{
				Logger.Error("Failed to shutdown server cleanly: " + shutdownEx.Message);
			}
		}
		finally
		{
			isRunning = false;
		}
	}

	private static void ProcessSingleMessage(NetIncomingMessage msg)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Invalid comparison between Unknown and I4
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		NetIncomingMessageType messageType = msg.MessageType;
		if ((int)messageType <= 128)
		{
			if ((int)messageType <= 4)
			{
				if ((int)messageType != 1)
				{
					if ((int)messageType != 4)
					{
						goto IL_00ec;
					}
					OnPlayerConnectionAttempt(msg);
				}
				else
				{
					OnStatusChanged(msg);
				}
			}
			else
			{
				if ((int)messageType != 8)
				{
					if ((int)messageType == 128)
					{
						goto IL_0091;
					}
					goto IL_00ec;
				}
				OnIncomingPacket(msg);
			}
		}
		else if ((int)messageType <= 512)
		{
			if ((int)messageType == 256)
			{
				goto IL_0091;
			}
			if ((int)messageType != 512)
			{
				goto IL_00ec;
			}
			Logger.Warning("Lidgren Warning - \"" + ((NetBuffer)msg).ReadString() + "\".");
		}
		else if ((int)messageType != 1024)
		{
			if ((int)messageType != 4096)
			{
				goto IL_00ec;
			}
			OnLatencyUpdated(msg);
		}
		else
		{
			Logger.Error("Lidgren Error - \"" + ((NetBuffer)msg).ReadString() + "\".");
		}
		goto IL_0159;
		IL_0159:
		((NetPeer)server).Recycle(msg);
		return;
		IL_0091:
		Logger.Info("Lidgren Debug - \"" + ((NetBuffer)msg).ReadString() + "\".", important: true);
		goto IL_0159;
		IL_00ec:
		Logger.Warning($"Unhandled message type: {msg.MessageType} - {msg.DeliveryMethod} - {((NetBuffer)msg).LengthBytes} bytes.");
		goto IL_0159;
	}

	private static bool Listen()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		NetIncomingMessage val;
		for (; (val = ((NetPeer)server).ReadMessage()) != null; ((NetPeer)server).Recycle(val))
		{
			try
			{
			NetIncomingMessageType messageType = val.MessageType;
			if ((int)messageType <= 128)
			{
				if ((int)messageType <= 4)
				{
					if ((int)messageType == 1)
					{
						flag |= OnStatusChanged(val);
						continue;
					}
					if ((int)messageType == 4)
					{
						OnPlayerConnectionAttempt(val);
						continue;
					}
				}
				else
				{
					if ((int)messageType == 8)
					{
						flag |= OnIncomingPacket(val);
						continue;
					}
					if ((int)messageType == 128)
					{
						goto IL_009c;
					}
				}
			}
			else if ((int)messageType <= 512)
			{
				if ((int)messageType == 256)
				{
					goto IL_009c;
				}
				if ((int)messageType == 512)
				{
					Logger.Warning("Lidgren Warning - \"" + ((NetBuffer)val).ReadString() + "\".");
					continue;
				}
			}
			else
			{
				if ((int)messageType == 1024)
				{
					Logger.Error("Lidgren Error - \"" + ((NetBuffer)val).ReadString() + "\".");
					continue;
				}
				if ((int)messageType == 4096)
				{
					OnLatencyUpdated(val);
					continue;
				}
			}
			Logger.Warning($"Unhandled message type: {val.MessageType} - {val.DeliveryMethod} - {((NetBuffer)val).LengthBytes} bytes.");
			continue;
			IL_009c:
			Logger.Info("Lidgren Debug - \"" + ((NetBuffer)val).ReadString() + "\".", important: true);
			}
			catch (Exception ex)
			{
				Logger.Error("Error processing message: " + ex.Message);
			}
		}
		return flag;
	}

	public static ConnectedPlayer FindPlayer(NetConnection connection)
	{
		if (connectedPlayers.TryGetValue(connection.RemoteEndPoint, out ConnectedPlayer value))
		{
			return value;
		}
		return null;
	}

	private static bool HasRocketAuthority(ConnectedPlayer player, int rocketId)
	{
		if (player == null || rocketId < 0)
		{
			return false;
		}
		if (player.isAdmin)
		{
			return true;
		}
		if (world.rockets.TryGetValue(rocketId, out RocketState rocket) && rocket.ownerId == player.id)
		{
			return true;
		}
		return player.updateAuthority != null && player.updateAuthority.Contains(rocketId);
	}

	private static string FormatUsername(this string username)
	{
		if (!string.IsNullOrWhiteSpace(username))
		{
			return "'" + username + "'";
		}
		return "???";
	}

	public static void SendPacketToPlayer(NetConnection connection, Packet packet, NetDeliveryMethod method = (NetDeliveryMethod)67)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (connection == null)
		{
			Logger.Warning("Attempted to send packet to null connection.");
			return;
		}
		NetOutgoingMessage val = ((NetPeer)server).CreateMessage();
		((NetBuffer)val).Write((byte)packet.Type);
		val.Write(packet);
		((NetPeer)server).SendMessage(val, connection, method);
	}

	public static void SendPacketToAll(Packet packet, NetConnection except = null, NetDeliveryMethod method = (NetDeliveryMethod)67)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		NetOutgoingMessage val = ((NetPeer)server).CreateMessage();
		((NetBuffer)val).Write((byte)packet.Type);
		val.Write(packet);
		server.SendToAll(val, except, method, 0);
	}

	private static bool OnStatusChanged(NetIncomingMessage msg)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Invalid comparison between Unknown and I4
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		NetConnectionStatus val = (NetConnectionStatus)((NetBuffer)msg).ReadByte();
		string value = ((NetBuffer)msg).ReadString();
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		string value2 = ((connectedPlayer != null) ? connectedPlayer.username.FormatUsername() : null);
		Logger.Info($"Status of {value2} @ {msg.SenderEndPoint} changed to {val} - \"{value}\".");
		if ((int)val != 5)
		{
			if ((int)val == 7)
			{
				OnPlayerDisconnect(msg.SenderConnection);
				return true;
			}
			return false;
		}
		OnPlayerSuccessfulConnect(msg.SenderConnection);
		return false;
	}

	private static void OnPlayerConnectionAttempt(NetIncomingMessage msg)
	{
		NetConnection senderConnection = msg.SenderConnection;
		if (senderConnection.RemoteHailMessage == null)
		{
			Logger.Warning("Connection attempt with null hail message, denying.");
			senderConnection.Deny("Invalid join request");
			return;
		}
		Packet_JoinRequest packet_JoinRequest;
		try
		{
			packet_JoinRequest = senderConnection.RemoteHailMessage.Read<Packet_JoinRequest>();
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to parse join request: " + ex.Message);
			senderConnection.Deny("Invalid join request");
			return;
		}
		Logger.Info($"Recieved join request from {packet_JoinRequest.Username.FormatUsername()} @ {senderConnection.RemoteEndPoint}.", important: true);
		string text = "Connection approved!";
		if (connectedPlayers.Count >= settings.maxConnections && settings.maxConnections != 0)
		{
			text = $"Server is full ({connectedPlayers.Count}/{settings.maxConnections}).";
		}
		else if (string.IsNullOrWhiteSpace(packet_JoinRequest.Username))
		{
			text = "Username cannot be empty";
		}
		else if (settings.blockDuplicatePlayerNames && connectedPlayers.Values.Select((ConnectedPlayer player) => player.username).Contains(packet_JoinRequest.Username))
		{
			text = "Username '" + packet_JoinRequest.Username + "' is already in use";
		}
		else if (packet_JoinRequest.Password != settings.serverPassword && settings.serverPassword != "")
		{
			text = "Invalid password";
		}
		else
		{
			if (IsVersionAllowed(packet_JoinRequest.GameVersion, out string reason))
			{
				Logger.Info("Approved join request, sending world info...", important: true);
				ConnectedPlayer connectedPlayer = new ConnectedPlayer(packet_JoinRequest.Username);
				connectedPlayer.solarSystemName = packet_JoinRequest.SolarSystemName;
				connectedPlayers.Add(senderConnection.RemoteEndPoint, connectedPlayer);
				if (hostPlayerId == -1)
				{
					hostPlayerId = connectedPlayer.id;
				}
				NetOutgoingMessage val = ((NetPeer)server).CreateMessage();
				val.Write(new Packet_JoinResponse
				{
					PlayerId = connectedPlayer.id,
					UpdateRocketsPeriod = settings.updateRocketsPeriod,
					ChatMessageCooldown = settings.chatMessageCooldown,
					WorldTime = world.WorldTime,
					Difficulty = world.difficulty,
					SolarSystemName = world.solarSystemName,
					HostPlayerId = hostPlayerId
				});
				senderConnection.Approve(val);
				return;
			}
			text = reason;
		}
		Logger.Info("Denied join request - " + text, important: true);
		senderConnection.Deny(text);
	}

	private static bool IsVersionAllowed(string clientVersion, out string reason)
	{
		reason = "";
		if (string.IsNullOrWhiteSpace(settings.allowedGameVersions))
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(clientVersion))
		{
			reason = "Client version not provided";
			return false;
		}
		string[] array = settings.allowedGameVersions.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (clientVersion == text)
			{
				return true;
			}
			if (clientVersion.StartsWith(text + "."))
			{
				return true;
			}
		}
		reason = "Game version " + clientVersion + " is not allowed. Allowed versions: " + settings.allowedGameVersions;
		return false;
	}

	private static void OnPlayerSuccessfulConnect(NetConnection connection)
	{
		ConnectedPlayer connectedPlayer = FindPlayer(connection);
		if (connectedPlayer == null)
		{
			Logger.Warning("Missing new player while sending join response!");
			return;
		}
		if (hostPlayerId == -1)
		{
			hostPlayerId = connectedPlayer.id;
		}
		SendPacketToAll(new Packet_PlayerConnected
		{
			PlayerId = connectedPlayer.id,
			Username = connectedPlayer.username,
			IconColor = connectedPlayer.iconColor,
			PrintMessage = true
		}, connection, (NetDeliveryMethod)67);
		foreach (KeyValuePair<int, RocketState> rocket in world.rockets)
		{
			SendPacketToPlayer(connection, new Packet_CreateRocket
			{
				GlobalId = rocket.Key,
				Rocket = rocket.Value
			}, (NetDeliveryMethod)67);
		}
		foreach (KeyValuePair<IPEndPoint, ConnectedPlayer> connectedPlayer2 in connectedPlayers)
		{
			SendPacketToPlayer(connection, new Packet_PlayerConnected
			{
				PlayerId = connectedPlayer2.Value.id,
				Username = connectedPlayer2.Value.username,
				IconColor = connectedPlayer2.Value.iconColor,
				PrintMessage = false
			}, (NetDeliveryMethod)67);
			SendPacketToPlayer(connection, new Packet_UpdatePlayerControl
			{
				PlayerId = connectedPlayer2.Value.id,
				RocketId = connectedPlayer2.Value.controlledRocket
			}, (NetDeliveryMethod)67);
		}
		SendPacketToPlayer(connection, new Packet_UpdateCheatStatus
		{
			InfiniteFuel = world.infiniteFuel,
			NoAtmosphericDrag = world.noAtmosphericDrag,
			UnbreakableParts = world.unbreakableParts,
			NoGravity = world.noGravity,
			NoHeatDamage = world.noHeatDamage,
			NoBurnMarks = world.noBurnMarks,
			InfiniteBuildArea = world.infiniteBuildArea,
			PartClipping = world.partClipping
		}, (NetDeliveryMethod)67);
		if (!string.IsNullOrWhiteSpace(settings.motd))
		{
			System.Drawing.Color color = System.Drawing.Color.Blue;
			if (!string.IsNullOrWhiteSpace(settings.motdColor))
			{
				string motdColor = settings.motdColor;
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int alpha = 255;
				if (motdColor.Length == 6 || motdColor.Length == 8)
				{
					try
					{
						num = Convert.ToInt32(motdColor.Substring(0, 2), 16);
						num2 = Convert.ToInt32(motdColor.Substring(2, 2), 16);
						num3 = Convert.ToInt32(motdColor.Substring(4, 2), 16);
						if (motdColor.Length == 8)
						{
							alpha = Convert.ToInt32(motdColor.Substring(6, 2), 16);
						}
						color = System.Drawing.Color.FromArgb(alpha, num, num2, num3);
					}
					catch
					{
					}
				}
			}
			SendPacketToPlayer(connection, new Packet_SendChatMessage
			{
				SenderId = -1,
				Message = settings.motd,
				Color = color
			}, (NetDeliveryMethod)67);
		}
		if (isTimeWarping)
		{
			SendPacketToPlayer(connection, new Packet_TimeWarpResult
			{
				VoteId = currentVoteId,
				Approved = true,
				TimeScale = currentTimeScale,
				PhysicsWarp = currentPhysicsWarp
			}, (NetDeliveryMethod)67);
		}
	}

	private static void OnPlayerDisconnect(NetConnection connection)
	{
		ConnectedPlayer connectedPlayer = FindPlayer(connection);
		if (connectedPlayer != null)
		{
			SendPacketToAll(new Packet_PlayerDisconnected
			{
				PlayerId = connectedPlayer.id
			}, null, (NetDeliveryMethod)67);
			connectedPlayers.Remove(connection.RemoteEndPoint);
			if (connectedPlayer.id == hostPlayerId)
			{
				// 房主退出：直接关闭世界，不转移房主
				Logger.Info("Host left the world, shutting down server...", important: true);
				stopped = true;
				try
				{
					server.Shutdown("Host left the world");
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to shutdown server: " + ex.Message);
				}
				return;
			}
			UpdatePlayerAuthorities();
		}
	}

	private static void OnLatencyUpdated(NetIncomingMessage msg)
	{
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		if (connectedPlayer != null)
		{
			string value = connectedPlayer.username.FormatUsername();
			connectedPlayer.avgTripTime = msg.SenderConnection.AverageRoundtripTime;
			Logger.Info($"Average roundtrip time updated for {value} @ {msg.SenderEndPoint} - {1000f * connectedPlayer.avgTripTime}ms.");
			SendPacketToPlayer(msg.SenderConnection, new Packet_UpdateWorldTime
			{
				WorldTime = world.WorldTime + (double)(connectedPlayer.avgTripTime / 2f)
			}, (NetDeliveryMethod)67);
		}
	}

	private static void UpdatePlayerAuthorities()
	{
		if (connectedPlayers.Count == 0)
		{
			return;
		}
		foreach (ConnectedPlayer value3 in connectedPlayers.Values)
		{
			value3.updateAuthority.Clear();
			value3.loadRangeRocketIds.Clear();
		}
		if (world.rockets.Count == 0)
		{
			return;
		}
		controlledRocketsCache.Clear();
		playerPositionsCache.Clear();
		foreach (ConnectedPlayer value4 in connectedPlayers.Values)
		{
			if (world.rockets.TryGetValue(value4.controlledRocket, out RocketState value))
			{
				value4.updateAuthority.Add(value4.controlledRocket);
				value4.loadRangeRocketIds.Add(value4.controlledRocket);
				controlledRocketsCache.Add(value4.controlledRocket);
				playerPositionsCache[value4.id] = value.location.position;
			}
		}
		ConnectedPlayer host = null;
		foreach (ConnectedPlayer player in connectedPlayers.Values)
		{
			if (player.id == hostPlayerId)
			{
				host = player;
				break;
			}
		}
		double loadRangeSqr = settings.loadRange * settings.loadRange;
		foreach (int key in world.rockets.Keys)
		{
			if (controlledRocketsCache.Contains(key))
			{
				continue;
			}
			Double2 position = world.rockets[key].location.position;
			ConnectedPlayer inRangeHost = null;
			ConnectedPlayer inRangeBest = null;
			foreach (ConnectedPlayer value5 in connectedPlayers.Values)
			{
				if (!playerPositionsCache.TryGetValue(value5.id, out var playerPos))
				{
					continue;
				}
				double num2 = position.x - playerPos.x;
				double num3 = position.y - playerPos.y;
				if (num2 * num2 + num3 * num3 <= loadRangeSqr)
				{
					value5.loadRangeRocketIds.Add(key);
					if (value5.id == hostPlayerId)
					{
						inRangeHost = value5;
					}
					if (inRangeBest == null || value5.performanceScore > inRangeBest.performanceScore)
					{
						inRangeBest = value5;
					}
				}
			}
			ConnectedPlayer winner = inRangeHost ?? inRangeBest;
			if (winner != null)
			{
				winner.updateAuthority.Add(key);
			}
			else if (host != null)
			{
				host.updateAuthority.Add(key);
			}
		}
		foreach (KeyValuePair<IPEndPoint, ConnectedPlayer> connectedPlayer2 in connectedPlayers)
		{
			SendPacketToPlayer(((NetPeer)server).GetConnection(connectedPlayer2.Key), new Packet_UpdatePlayerAuthority
			{
				RocketIds = connectedPlayer2.Value.updateAuthority,
				LoadedRocketIds = connectedPlayer2.Value.loadRangeRocketIds
			}, (NetDeliveryMethod)67);
		}
	}

	private static bool OnIncomingPacket(NetIncomingMessage msg)
	{
		PacketType packetType = (PacketType)((NetBuffer)msg).ReadByte();
		switch (packetType)
		{
		case PacketType.UpdatePlayerControl:
			OnPacket_UpdatePlayerControl(msg);
			return true;
		case PacketType.UpdatePerformance:
			OnPacket_UpdatePerformance(msg);
			return false;
		case PacketType.UpdateCheatStatus:
			OnPacket_UpdateCheatStatus(msg);
			return false;
		case PacketType.SolarSystemFile:
			OnPacket_SolarSystemFile(msg);
			return false;
		case PacketType.UpdatePlayerColor:
			OnPacket_UpdatePlayerColor(msg);
			return false;
		case PacketType.SendChatMessage:
			OnPacket_SendChatMessage(msg);
			return false;
		case PacketType.CreateRocket:
			return OnPacket_CreateRocket(msg);
		case PacketType.DestroyRocket:
			OnPacket_DestroyRocket(msg);
			return true;
		case PacketType.UpdateRocketPrimary:
			OnPacket_UpdateRocketPrimary(msg);
			return false;
		case PacketType.UpdateRocketSecondary:
			OnPacket_UpdateRocketSecondary(msg);
			return false;
		case PacketType.DestroyPart:
			OnPacket_DestroyPart(msg);
			return false;
		case PacketType.UpdateStaging:
			OnPacket_UpdateStaging(msg);
			return false;
		case PacketType.UpdatePart_EngineModule:
			OnPacket_UpdatePart_EngineModule(msg);
			return false;
		case PacketType.UpdatePart_WheelModule:
			OnPacket_UpdatePart_WheelModule(msg);
			return false;
		case PacketType.UpdatePart_BoosterModule:
			OnPacket_UpdatePart_BoosterModule(msg);
			return false;
		case PacketType.UpdatePart_ParachuteModule:
			OnPacket_UpdatePart_ParachuteModule(msg);
			return false;
		case PacketType.UpdatePart_MoveModule:
			OnPacket_UpdatePart_MoveModule(msg);
			return false;
		case PacketType.UpdatePart_ResourceModule:
			OnPacket_UpdatePart_ResourceModule(msg);
			return false;
		case PacketType.UpdatePartHeat:
			OnPacket_UpdatePartHeat(msg);
			return false;
		case PacketType.TimeWarpRequest:
			OnPacket_TimeWarpRequest(msg);
			return false;
		case PacketType.TimeWarpVoteResponse:
			OnPacket_TimeWarpVoteResponse(msg);
			return false;
		case PacketType.JoinRequest:
			Logger.Warning("Recieved join request outside of connection attempt.");
			return false;
		case PacketType.JoinResponse:
		case PacketType.PlayerConnected:
		case PacketType.PlayerDisconnected:
		case PacketType.UpdatePlayerAuthority:
			Logger.Warning($"Recieved packet (of type {packetType}) intended for clients.");
			return false;
		default:
			Logger.Error($"Unhandled packet type: {packetType}, {((NetBuffer)msg).LengthBytes} bytes.");
			return false;
		}
	}

	private static void OnPacket_UpdatePlayerControl(NetIncomingMessage msg)
	{
		Packet_UpdatePlayerControl packet_UpdatePlayerControl = msg.Read<Packet_UpdatePlayerControl>();
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		if (connectedPlayer != null)
		{
			if (connectedPlayer.id == packet_UpdatePlayerControl.PlayerId)
			{
				connectedPlayer.controlledRocket = packet_UpdatePlayerControl.RocketId;
				SendPacketToAll(packet_UpdatePlayerControl, msg.SenderConnection, (NetDeliveryMethod)67);
				UpdatePlayerAuthorities();
			}
			else
			{
				Logger.Warning("Incorrect player id while trying to update controlled rocket!");
			}
		}
		else
		{
			Logger.Error("Missing connected player while trying to update controlled rocket!");
		}
	}

	private static void OnPacket_UpdatePerformance(NetIncomingMessage msg)
	{
		Packet_UpdatePerformance packet = msg.Read<Packet_UpdatePerformance>();
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		if (connectedPlayer != null && connectedPlayer.id == packet.PlayerId)
		{
			connectedPlayer.performanceScore = packet.PerformanceScore;
		}
	}

	private static void OnPacket_UpdateCheatStatus(NetIncomingMessage msg)
	{
		Packet_UpdateCheatStatus packet = msg.Read<Packet_UpdateCheatStatus>();
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		if (connectedPlayer == null)
		{
			return;
		}
		if (connectedPlayer.id != hostPlayerId)
		{
			SendPacketToPlayer(msg.SenderConnection, new Packet_UpdateCheatStatus
			{
				InfiniteFuel = world.infiniteFuel,
				NoAtmosphericDrag = world.noAtmosphericDrag,
				UnbreakableParts = world.unbreakableParts,
				NoGravity = world.noGravity,
				NoHeatDamage = world.noHeatDamage,
				NoBurnMarks = world.noBurnMarks,
				InfiniteBuildArea = world.infiniteBuildArea,
				PartClipping = world.partClipping
			}, (NetDeliveryMethod)67);
			Logger.Info("Non-host player " + connectedPlayer.username + " tried to change cheats, rejected");
			return;
		}
		world.infiniteFuel = packet.InfiniteFuel;
		world.noAtmosphericDrag = packet.NoAtmosphericDrag;
		world.unbreakableParts = packet.UnbreakableParts;
		world.noGravity = packet.NoGravity;
		world.noHeatDamage = packet.NoHeatDamage;
		world.noBurnMarks = packet.NoBurnMarks;
		world.infiniteBuildArea = packet.InfiniteBuildArea;
		world.partClipping = packet.PartClipping;
		SendPacketToAll(packet, msg.SenderConnection, (NetDeliveryMethod)67);
		Logger.Info("Host updated cheat status");
	}

	private static void OnPacket_SolarSystemFile(NetIncomingMessage msg)
	{
		Packet_SolarSystemFile packet = msg.Read<Packet_SolarSystemFile>();
		if (packet.FileName == "REQUEST")
		{
			SendSolarSystemFiles(msg.SenderConnection);
		}
	}

	private static void SendSolarSystemFiles(NetConnection connection)
	{
		if (string.IsNullOrEmpty(world.solarSystemName))
		{
			return;
		}
		try
		{
			FolderPath folder = FileLocations.SolarSystemsFolder.Extend(world.solarSystemName);
			if (!folder.FolderExists())
			{
				Logger.Info("Solar system folder not found: " + world.solarSystemName);
				SendPacketToPlayer(connection, new Packet_SolarSystemFile
				{
					FileName = "",
					Data = null,
					IsLast = true
				}, (NetDeliveryMethod)67);
				return;
			}
			string root = (string)folder;
			const int ChunkSize = 50000;
			foreach (FilePath file in folder.GetFilesInFolder(recursively: true))
			{
				string filePath = (string)file;
				if (filePath.EndsWith(".meta"))
				{
					continue;
				}
				string relPath = file.GetRelativePath(root).TrimStart('/', '\\');
				byte[] data = File.ReadAllBytes(filePath);
				for (int offset = 0; offset < data.Length; offset += ChunkSize)
				{
					int len = Math.Min(ChunkSize, data.Length - offset);
					byte[] chunk = new byte[len];
					Array.Copy(data, offset, chunk, 0, len);
					SendPacketToPlayer(connection, new Packet_SolarSystemFile
					{
						FileName = relPath,
						Data = chunk,
						IsLast = (offset + len >= data.Length)
					}, (NetDeliveryMethod)67);
				}
			}
			SendPacketToPlayer(connection, new Packet_SolarSystemFile
			{
				FileName = "",
				Data = null,
				IsLast = true
			}, (NetDeliveryMethod)67);
			Logger.Info("Sent solar system files to " + connection.RemoteEndPoint);
		}
		catch (Exception ex)
		{
			Logger.Error("Failed to send solar system files: " + ex.Message);
			SendPacketToPlayer(connection, new Packet_SolarSystemFile
			{
				FileName = "",
				Data = null,
				IsLast = true
			}, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePlayerColor(NetIncomingMessage msg)
	{
		Packet_UpdatePlayerColor packet_UpdatePlayerColor = msg.Read<Packet_UpdatePlayerColor>();
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		if (connectedPlayer != null)
		{
			if (connectedPlayer.id != packet_UpdatePlayerColor.PlayerId)
			{
				Logger.Warning("Player " + connectedPlayer.username + " tried to change another player's color!");
				return;
			}
			connectedPlayer.iconColor = packet_UpdatePlayerColor.Color;
			SendPacketToAll(packet_UpdatePlayerColor, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_SendChatMessage(NetIncomingMessage msg)
	{
		Packet_SendChatMessage packet_SendChatMessage = msg.Read<Packet_SendChatMessage>();
		ConnectedPlayer chatPlayer = FindPlayer(msg.SenderConnection);
		if (chatPlayer == null)
		{
			return;
		}
		if (settings.chatMessageCooldown > 0.0 && (DateTime.Now - chatPlayer.lastChatTime).TotalSeconds < settings.chatMessageCooldown)
		{
			SendPacketToPlayer(msg.SenderConnection, new Packet_SendChatMessage
			{
				SenderId = chatPlayer.id,
				Message = "Message cooldown active."
			}, (NetDeliveryMethod)67);
			return;
		}
		chatPlayer.lastChatTime = DateTime.Now;
		if (packet_SendChatMessage.Message != null && packet_SendChatMessage.Message.Length > 200)
		{
			packet_SendChatMessage.Message = packet_SendChatMessage.Message.Substring(0, 200);
		}
		packet_SendChatMessage.SenderId = chatPlayer.id;
		if (CommandManager.TryParse(packet_SendChatMessage.Message, out string name, out string[] args))
		{
			string message = CommandManager.TryRun(name, args, msg.SenderConnection);
			SendPacketToPlayer(msg.SenderConnection, new Packet_SendChatMessage
			{
				Message = message
			}, (NetDeliveryMethod)67);
		}
		else
		{
			SendPacketToAll(packet_SendChatMessage, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static bool OnPacket_CreateRocket(NetIncomingMessage msg)
	{
		Packet_CreateRocket packet_CreateRocket = msg.Read<Packet_CreateRocket>();
		ConnectedPlayer createPlayer = FindPlayer(msg.SenderConnection);
		if (world.rockets.ContainsKey(packet_CreateRocket.GlobalId))
		{
			if (createPlayer == null || !HasRocketAuthority(createPlayer, packet_CreateRocket.GlobalId))
			{
				Logger.Warning("Player without authority tried to overwrite rocket " + packet_CreateRocket.GlobalId);
				return false;
			}
			packet_CreateRocket.Rocket.ownerId = world.rockets[packet_CreateRocket.GlobalId].ownerId;
			world.rockets[packet_CreateRocket.GlobalId] = packet_CreateRocket.Rocket;
			SendPacketToAll(packet_CreateRocket, msg.SenderConnection, (NetDeliveryMethod)67);
			return true;
		}
		packet_CreateRocket.GlobalId = world.rockets.InsertNew(packet_CreateRocket.Rocket);
		ConnectedPlayer creator = FindPlayer(msg.SenderConnection);
		if (creator != null)
		{
			packet_CreateRocket.Rocket.ownerId = creator.id;
		}
		SendPacketToAll(packet_CreateRocket, null, (NetDeliveryMethod)67);
		return !packet_CreateRocket.ForLaunch;
	}

	private static void OnPacket_DestroyRocket(NetIncomingMessage msg)
	{
		Packet_DestroyRocket packet_DestroyRocket = msg.Read<Packet_DestroyRocket>();
		ConnectedPlayer destroyPlayer = FindPlayer(msg.SenderConnection);
		if (destroyPlayer == null || !HasRocketAuthority(destroyPlayer, packet_DestroyRocket.RocketId))
		{
			Logger.Warning("Player without authority tried to destroy rocket " + packet_DestroyRocket.RocketId);
			return;
		}
		if (world.rockets.Remove(packet_DestroyRocket.RocketId))
		{
			SendPacketToAll(packet_DestroyRocket, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdateRocketPrimary(NetIncomingMessage msg)
	{
		Packet_UpdateRocketPrimary packet_UpdateRocketPrimary = msg.Read<Packet_UpdateRocketPrimary>();
		ConnectedPlayer primaryPlayer = FindPlayer(msg.SenderConnection);
		if (primaryPlayer == null || !HasRocketAuthority(primaryPlayer, packet_UpdateRocketPrimary.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdateRocketPrimary.RocketId, out RocketState value))
		{
			value.UpdateRocketPrimary(packet_UpdateRocketPrimary);
			SendPacketToAll(packet_UpdateRocketPrimary, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdateRocketSecondary(NetIncomingMessage msg)
	{
		Packet_UpdateRocketSecondary packet_UpdateRocketSecondary = msg.Read<Packet_UpdateRocketSecondary>();
		ConnectedPlayer secondaryPlayer = FindPlayer(msg.SenderConnection);
		if (secondaryPlayer == null || !HasRocketAuthority(secondaryPlayer, packet_UpdateRocketSecondary.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdateRocketSecondary.RocketId, out RocketState value))
		{
			value.UpdateRocketSecondary(packet_UpdateRocketSecondary);
			SendPacketToAll(packet_UpdateRocketSecondary, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_DestroyPart(NetIncomingMessage msg)
	{
		Packet_DestroyPart packet_DestroyPart = msg.Read<Packet_DestroyPart>();
		ConnectedPlayer destroyPartPlayer = FindPlayer(msg.SenderConnection);
		if (destroyPartPlayer == null || !HasRocketAuthority(destroyPartPlayer, packet_DestroyPart.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_DestroyPart.RocketId, out RocketState value) && value.RemovePart(packet_DestroyPart.PartId))
		{
			SendPacketToAll(packet_DestroyPart, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdateStaging(NetIncomingMessage msg)
	{
		Packet_UpdateStaging packet_UpdateStaging = msg.Read<Packet_UpdateStaging>();
		ConnectedPlayer stagingPlayer = FindPlayer(msg.SenderConnection);
		if (stagingPlayer == null || !HasRocketAuthority(stagingPlayer, packet_UpdateStaging.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdateStaging.RocketId, out RocketState value))
		{
			value.stages = packet_UpdateStaging.Stages;
			SendPacketToAll(packet_UpdateStaging, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePart_EngineModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_EngineModule packet_UpdatePart_EngineModule = msg.Read<Packet_UpdatePart_EngineModule>();
		ConnectedPlayer enginePlayer = FindPlayer(msg.SenderConnection);
		if (enginePlayer == null || !HasRocketAuthority(enginePlayer, packet_UpdatePart_EngineModule.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdatePart_EngineModule.RocketId, out RocketState value) && value.parts.TryGetValue(packet_UpdatePart_EngineModule.PartId, out PartState value2))
		{
			value2.part.TOGGLE_VARIABLES["engine_on"] = packet_UpdatePart_EngineModule.EngineOn;
			SendPacketToAll(packet_UpdatePart_EngineModule, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePart_WheelModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_WheelModule packet_UpdatePart_WheelModule = msg.Read<Packet_UpdatePart_WheelModule>();
		ConnectedPlayer wheelPlayer = FindPlayer(msg.SenderConnection);
		if (wheelPlayer == null || !HasRocketAuthority(wheelPlayer, packet_UpdatePart_WheelModule.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdatePart_WheelModule.RocketId, out RocketState value) && value.parts.TryGetValue(packet_UpdatePart_WheelModule.PartId, out PartState value2))
		{
			value2.part.TOGGLE_VARIABLES["wheel_on"] = packet_UpdatePart_WheelModule.WheelOn;
			SendPacketToAll(packet_UpdatePart_WheelModule, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePart_BoosterModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_BoosterModule packet_UpdatePart_BoosterModule = msg.Read<Packet_UpdatePart_BoosterModule>();
		ConnectedPlayer boosterPlayer = FindPlayer(msg.SenderConnection);
		if (boosterPlayer == null || !HasRocketAuthority(boosterPlayer, packet_UpdatePart_BoosterModule.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdatePart_BoosterModule.RocketId, out RocketState value) && value.parts.TryGetValue(packet_UpdatePart_BoosterModule.PartId, out PartState value2))
		{
			value2.part.NUMBER_VARIABLES["fuel_percent"] = packet_UpdatePart_BoosterModule.FuelPercent;
			SendPacketToAll(packet_UpdatePart_BoosterModule, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePart_ParachuteModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_ParachuteModule packet_UpdatePart_ParachuteModule = msg.Read<Packet_UpdatePart_ParachuteModule>();
		ConnectedPlayer parachutePlayer = FindPlayer(msg.SenderConnection);
		if (parachutePlayer == null || !HasRocketAuthority(parachutePlayer, packet_UpdatePart_ParachuteModule.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdatePart_ParachuteModule.RocketId, out RocketState value) && value.parts.TryGetValue(packet_UpdatePart_ParachuteModule.PartId, out PartState value2))
		{
			value2.part.NUMBER_VARIABLES["animation_state"] = packet_UpdatePart_ParachuteModule.State;
			value2.part.NUMBER_VARIABLES["deploy_state"] = packet_UpdatePart_ParachuteModule.TargetState;
			SendPacketToAll(packet_UpdatePart_ParachuteModule, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePart_MoveModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_MoveModule packet_UpdatePart_MoveModule = msg.Read<Packet_UpdatePart_MoveModule>();
		ConnectedPlayer movePlayer = FindPlayer(msg.SenderConnection);
		if (movePlayer == null || !HasRocketAuthority(movePlayer, packet_UpdatePart_MoveModule.RocketId))
		{
			return;
		}
		if (world.rockets.TryGetValue(packet_UpdatePart_MoveModule.RocketId, out RocketState value) && value.parts.TryGetValue(packet_UpdatePart_MoveModule.PartId, out PartState value2))
		{
			value2.part.NUMBER_VARIABLES["state"] = packet_UpdatePart_MoveModule.Time;
			value2.part.NUMBER_VARIABLES["state_target"] = packet_UpdatePart_MoveModule.TargetTime;
			SendPacketToAll(packet_UpdatePart_MoveModule, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePart_ResourceModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_ResourceModule packet_UpdatePart_ResourceModule = msg.Read<Packet_UpdatePart_ResourceModule>();
		ConnectedPlayer resourcePlayer = FindPlayer(msg.SenderConnection);
		if (resourcePlayer == null || !HasRocketAuthority(resourcePlayer, packet_UpdatePart_ResourceModule.RocketId))
		{
			return;
		}
		if (!world.rockets.TryGetValue(packet_UpdatePart_ResourceModule.RocketId, out RocketState value))
		{
			return;
		}
		bool flag = false;
		foreach (int partId in packet_UpdatePart_ResourceModule.PartIds)
		{
			if (value.parts.TryGetValue(partId, out PartState value2))
			{
				value2.part.NUMBER_VARIABLES["fuel_percent"] = (float)packet_UpdatePart_ResourceModule.ResourcePercent;
				flag = true;
			}
		}
		if (flag)
		{
			SendPacketToAll(packet_UpdatePart_ResourceModule, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_UpdatePartHeat(NetIncomingMessage msg)
	{
		Packet_UpdatePartHeat packet = msg.Read<Packet_UpdatePartHeat>();
		ConnectedPlayer heatPlayer = FindPlayer(msg.SenderConnection);
		if (heatPlayer == null || !HasRocketAuthority(heatPlayer, packet.RocketId))
		{
			return;
		}
		if (world.rockets.ContainsKey(packet.RocketId))
		{
			SendPacketToAll(packet, msg.SenderConnection, (NetDeliveryMethod)67);
		}
	}

	private static void OnPacket_TimeWarpRequest(NetIncomingMessage msg)
	{
		Packet_TimeWarpRequest packet_TimeWarpRequest = msg.Read<Packet_TimeWarpRequest>();
		ConnectedPlayer connectedPlayer = FindPlayer(msg.SenderConnection);
		if (connectedPlayer == null)
		{
			return;
		}
		if (settings.timeWarpMaxScale > 0f && packet_TimeWarpRequest.TimeScale > settings.timeWarpMaxScale)
		{
			Logger.Info($"Time warp request {packet_TimeWarpRequest.TimeScale}x exceeds max scale {settings.timeWarpMaxScale}x, rejecting");
			return;
		}
		if (connectedPlayer.id != hostPlayerId)
		{
			if (settings.timeWarpVoteTimeout > 0)
			{
				if (isVoting)
				{
					Logger.Info("Time warp request ignored, vote already in progress.");
					return;
				}
				isVoting = true;
				currentVoteId++;
				currentVotes.Clear();
				totalVoters = connectedPlayers.Count;
				requestedTimeScale = packet_TimeWarpRequest.TimeScale;
				requestedPhysicsWarp = packet_TimeWarpRequest.PhysicsWarp;
				requesterName = connectedPlayer.username;
				voteStartTime = DateTime.Now;
				SendPacketToAll(new Packet_TimeWarpVote
				{
					TimeScale = requestedTimeScale,
					RequesterName = requesterName,
					VoteId = currentVoteId,
					PhysicsWarp = requestedPhysicsWarp
				}, null, (NetDeliveryMethod)67);
				Logger.Info(connectedPlayer.username + " started time warp vote " + currentVoteId + " for " + requestedTimeScale + "x");
			}
			else
			{
				foreach (KeyValuePair<IPEndPoint, ConnectedPlayer> kvp in connectedPlayers)
				{
					if (kvp.Value.id == hostPlayerId)
					{
						SendPacketToPlayer(((NetPeer)server).GetConnection(kvp.Key), new Packet_ShowToastMessage
						{
							Message = connectedPlayer.username + " 请求调整倍速到 " + packet_TimeWarpRequest.TimeScale + "x"
						}, (NetDeliveryMethod)67);
						break;
					}
				}
				Logger.Info(connectedPlayer.username + " requested time warp " + packet_TimeWarpRequest.TimeScale + "x (forwarded to host)");
			}
			return;
		}
		if (packet_TimeWarpRequest.TimeScale == 1f)
		{
			isTimeWarping = false;
			currentTimeScale = 1f;
			currentPhysicsWarp = false;
		}
		else
		{
			isTimeWarping = true;
			currentTimeScale = packet_TimeWarpRequest.TimeScale;
			currentPhysicsWarp = packet_TimeWarpRequest.PhysicsWarp;
		}
		world.SetTimeScale(currentTimeScale);
		SendPacketToAll(new Packet_TimeWarpResult
		{
			VoteId = -1,
			Approved = true,
			TimeScale = currentTimeScale,
			PhysicsWarp = currentPhysicsWarp,
			StopperName = connectedPlayer.username
		}, null, (NetDeliveryMethod)67);
		Logger.Info(connectedPlayer.username + " set time warp to " + currentTimeScale + "x");
	}

	private static void OnPacket_TimeWarpVoteResponse(NetIncomingMessage msg)
	{
		Packet_TimeWarpVoteResponse response = msg.Read<Packet_TimeWarpVoteResponse>();
		ConnectedPlayer voter = FindPlayer(msg.SenderConnection);
		if (voter == null)
		{
			return;
		}
		if (!isVoting || response.VoteId != currentVoteId)
		{
			return;
		}

		currentVotes[voter.id] = response.Agreed;
		int agreeCount = currentVotes.Count(kvp => kvp.Value);
		int rejectCount = currentVotes.Count - agreeCount;
		int required = (int)Math.Ceiling(totalVoters / 2.0);

		if (agreeCount >= required)
		{
			isVoting = false;
			isTimeWarping = requestedTimeScale != 1f;
			currentTimeScale = requestedTimeScale;
			currentPhysicsWarp = requestedPhysicsWarp;
			world.SetTimeScale(currentTimeScale);
			SendPacketToAll(new Packet_TimeWarpResult
			{
				VoteId = currentVoteId,
				Approved = true,
				TimeScale = currentTimeScale,
				PhysicsWarp = currentPhysicsWarp,
				StopperName = requesterName
			}, null, (NetDeliveryMethod)67);
			Logger.Info("Time warp vote " + currentVoteId + " approved (" + agreeCount + "/" + totalVoters + ")");
		}
		else if (rejectCount > totalVoters - required)
		{
			isVoting = false;
			SendPacketToAll(new Packet_TimeWarpResult
			{
				VoteId = currentVoteId,
				Approved = false,
				TimeScale = 1f,
				PhysicsWarp = true,
				RejecterName = voter.username
			}, null, (NetDeliveryMethod)67);
			Logger.Info("Time warp vote " + currentVoteId + " rejected");
		}
	}
}
}
