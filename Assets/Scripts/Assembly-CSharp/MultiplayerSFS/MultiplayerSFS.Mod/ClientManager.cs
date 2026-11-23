using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Lidgren.Network;
using MultiplayerSFS.Common;
using SFS;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace MultiplayerSFS.Mod {
public static class ClientManager
{
	public static Bool_Local multiplayerEnabled = new Bool_Local
	{
		Value = false
	};

	public static NetClient client;

	public static WorldState world;

	public static int playerId;

	public static int hostPlayerId = -1;

	public static bool IsHost => multiplayerEnabled.Value && playerId >= 0 && playerId == hostPlayerId;

	private static bool pumpEnabled = false;

	private static TaskCompletionSource<bool> solarSystemDownloadTcs;

	private static Dictionary<string, MemoryStream> solarSystemFileBuffers = new Dictionary<string, MemoryStream>();

	public static async Task TryConnect(JoinInfo info)
	{
		if (client != null && client.Status != NetPeerStatus.NotRunning)
		{
			client.Shutdown("Re-attempting join request");
		}
		NetPeerConfiguration netPeerConfiguration = new NetPeerConfiguration("multiplayersfs");
		netPeerConfiguration.ConnectionTimeout = 5f;
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.StatusChanged);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.UnconnectedData);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
		client = new NetClient(netPeerConfiguration);
		client.Start();
		NetOutgoingMessage netOutgoingMessage = client.CreateMessage();
		string text = "";
		try
		{
			string path = Path.Combine(Application.dataPath, "Custom Solar Systems");
			if (Directory.Exists(path))
			{
				string[] directories = Directory.GetDirectories(path);
				if (directories.Length != 0)
				{
					text = Path.GetFileName(directories[0]);
					Debug.Log("Found local solar system: " + text);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Failed to check local solar systems: " + ex.Message);
		}
		string version = Application.version;
		Debug.Log("Client game version: " + version);
		netOutgoingMessage.Write(new Packet_JoinRequest
		{
			Username = info.username,
			Password = info.password,
			SolarSystemName = text,
			GameVersion = version
		});
		client.Connect(new IPEndPoint(info.address, info.port), netOutgoingMessage);
		pumpEnabled = false;
		Menu.loading.Open(MultiplayerLoc.L("等待服务器响应...", "Waiting for server response..."));
		string denialReason = MultiplayerLoc.L("无法连接到服务器...", "Unable to connect to server...");
		while (true)
		{
			NetIncomingMessage netIncomingMessage;
			if ((netIncomingMessage = client.ReadMessage()) == null)
			{
				await Task.Delay(10);
				continue;
			}
			switch (netIncomingMessage.MessageType)
			{
			case NetIncomingMessageType.Error:
				Debug.LogError("Lidgren Error: Corrupted packet!");
				break;
			case NetIncomingMessageType.ErrorMessage:
				Debug.LogError("Lidgren Error: \"" + netIncomingMessage.ReadString() + "\".");
				break;
			case NetIncomingMessageType.WarningMessage:
				Debug.LogWarning("Lidgren Warning: \"" + netIncomingMessage.ReadString() + "\".");
				break;
			case NetIncomingMessageType.VerboseDebugMessage:
			case NetIncomingMessageType.DebugMessage:
				Debug.Log("Lidgren Debug: \"" + netIncomingMessage.ReadString() + "\".");
				break;
			case NetIncomingMessageType.StatusChanged:
				switch ((NetConnectionStatus)netIncomingMessage.ReadByte())
				{
				case NetConnectionStatus.Connected:
					SafeCloseLoading();
					client.Recycle(netIncomingMessage);
					await LoadWorld();
					pumpEnabled = true;
					return;
				case NetConnectionStatus.Disconnected:
				{
					string reason = denialReason;
					try
					{
						string remoteReason = netIncomingMessage.ReadString();
						if (!string.IsNullOrWhiteSpace(remoteReason))
						{
							reason = remoteReason;
						}
					}
					catch (Exception)
					{
					}
					SafeCloseLoading();
					MsgDrawer.main.Log(reason);
					client.Recycle(netIncomingMessage);
					Disconnect("Connection failed");
					return;
				}
				}
				break;
			default:
				Debug.LogWarning($"Recieved unhandled message type ({netIncomingMessage.MessageType}) when attempting to connect to server.");
				break;
			}
			client.Recycle(netIncomingMessage);
		}
	}

	private static void SafeCloseLoading()
	{
		try
		{
			if (Menu.loading != null)
			{
				Menu.loading.Close();
			}
		}
		catch (Exception)
		{
		}
	}

	public static async Task LoadWorld()
	{
		Menu.loading.Open(MultiplayerLoc.L("正在加载联机世界...", "Loading multiplayer world..."));
		if (client.ServerConnection == null || client.ServerConnection.RemoteHailMessage == null)
		{
			SafeCloseLoading();
			MsgDrawer.main.Log(MultiplayerLoc.L("服务器响应异常，无法加载世界", "Invalid server response, unable to load world"));
			client.Shutdown("Invalid server response");
			return;
		}
		Packet_JoinResponse packet_JoinResponse = client.ServerConnection.RemoteHailMessage.Read<Packet_JoinResponse>();
		playerId = packet_JoinResponse.PlayerId;
		hostPlayerId = packet_JoinResponse.HostPlayerId;
		LocalManager.updateRocketsPeriod = packet_JoinResponse.UpdateRocketsPeriod;
		LocalManager.Initialize();
		ChatWindow.CreateCooldownTimer(packet_JoinResponse.ChatMessageCooldown);
		world = new WorldState
		{
			initWorldTime = packet_JoinResponse.WorldTime,
			difficulty = packet_JoinResponse.Difficulty,
			solarSystemName = packet_JoinResponse.SolarSystemName
		};
		if (!string.IsNullOrEmpty(packet_JoinResponse.SolarSystemName))
		{
			bool exists = FileLocations.SolarSystemsFolder.Extend(packet_JoinResponse.SolarSystemName).FolderExists();
			Debug.Log("Solar system '" + packet_JoinResponse.SolarSystemName + "' exists locally: " + exists);
			if (!exists)
			{
				await DownloadSolarSystem(packet_JoinResponse.SolarSystemName);
			}
		}
		WorldSettings worldSettings = new WorldSettings(new SolarSystemReference(packet_JoinResponse.SolarSystemName), new Difficulty
		{
			difficulty = world.difficulty
		}, new WorldMode(WorldMode.Mode.Sandbox)
		{
			allowQuicksaves = false
		}, new WorldPlaytime(), new SandboxSettings.Data());
		Base.worldBase.EnterWorld(null, worldSettings, new Action(Base.sceneLoader.LoadHubScene));
	}

	public static void Update()
	{
		if (client == null || client.Status == NetPeerStatus.NotRunning || !pumpEnabled)
		{
			return;
		}
		Listen();
	}

	public static void Listen()
	{
		if (client == null || client.Status != NetPeerStatus.Running)
		{
			return;
		}
		NetIncomingMessage netIncomingMessage;
		while ((netIncomingMessage = client.ReadMessage()) != null)
		{
			switch (netIncomingMessage.MessageType)
			{
			case NetIncomingMessageType.Error:
				Debug.LogError("Lidgren Error: Corrupted packet!");
				break;
			case NetIncomingMessageType.ErrorMessage:
				Debug.LogError("Lidgren Error: \"" + netIncomingMessage.ReadString() + "\".");
				break;
			case NetIncomingMessageType.WarningMessage:
				Debug.LogWarning("Lidgren Warning: \"" + netIncomingMessage.ReadString() + "\".");
				break;
			case NetIncomingMessageType.VerboseDebugMessage:
			case NetIncomingMessageType.DebugMessage:
				Debug.Log("Lidgren Debug: \"" + netIncomingMessage.ReadString() + "\".");
				break;
			case NetIncomingMessageType.Data:
				HandlePacket(netIncomingMessage);
				break;
			case NetIncomingMessageType.StatusChanged:
				if (netIncomingMessage.ReadByte() == 7)
				{
					pumpEnabled = false;
					SceneLoader.ExitToMainMenu();
					client.Shutdown("Disconnected by server.");
					client.Recycle(netIncomingMessage);
					Disconnect("Disconnected by server");
					return;
				}
				break;
			default:
				Debug.LogWarning($"Unhandled message type ({netIncomingMessage.MessageType})!");
				break;
			}
			client.Recycle(netIncomingMessage);
		}
	}

	public static void HandlePacket(NetIncomingMessage msg)
	{
		PacketType packetType = (PacketType)msg.ReadByte();
		if (Packet.ShouldDebug(packetType))
		{
			Debug.Log($"Recieved packet of type {packetType}.");
		}
		switch (packetType)
		{
		case PacketType.PlayerConnected:
			OnPacket_PlayerConnected(msg);
			break;
		case PacketType.PlayerDisconnected:
			OnPacket_PlayerDisconnected(msg);
			break;
		case PacketType.UpdatePlayerControl:
			OnPacket_UpdatePlayerControl(msg);
			break;
		case PacketType.UpdatePlayerAuthority:
			OnPacket_UpdatePlayerAuthority(msg);
			break;
		case PacketType.UpdateWorldTime:
			OnPacket_UpdateWorldTime(msg);
			break;
		case PacketType.UpdatePlayerColor:
			OnPacket_UpdatePlayerColor(msg);
			break;
		case PacketType.SendChatMessage:
			OnPacket_SendChatMessage(msg);
			break;
		case PacketType.ShowToastMessage:
			OnPacket_ShowToastMessage(msg);
			break;
		case PacketType.UpdateCheatStatus:
			OnPacket_UpdateCheatStatus(msg);
			break;
		case PacketType.SolarSystemFile:
			OnPacket_SolarSystemFile(msg);
			break;
		case PacketType.TimeWarpResult:
			OnPacket_TimeWarpResult(msg);
			break;
		case PacketType.TimeWarpVote:
			OnPacket_TimeWarpVote(msg);
			break;
		case PacketType.CreateRocket:
			OnPacket_CreateRocket(msg);
			break;
		case PacketType.DestroyRocket:
			OnPacket_DestroyRocket(msg);
			break;
		case PacketType.UpdateRocketPrimary:
			OnPacket_UpdateRocketPrimary(msg);
			break;
		case PacketType.UpdateRocketSecondary:
			OnPacket_UpdateRocketSecondary(msg);
			break;
		case PacketType.DestroyPart:
			OnPacket_DestroyPart(msg);
			break;
		case PacketType.UpdateStaging:
			OnPacket_UpdateStaging(msg);
			break;
		case PacketType.UpdatePart_EngineModule:
			OnPacket_UpdatePart_EngineModule(msg);
			break;
		case PacketType.UpdatePart_WheelModule:
			OnPacket_UpdatePart_WheelModule(msg);
			break;
		case PacketType.UpdatePart_BoosterModule:
			OnPacket_UpdatePart_BoosterModule(msg);
			break;
		case PacketType.UpdatePart_ParachuteModule:
			OnPacket_UpdatePart_ParachuteModule(msg);
			break;
		case PacketType.UpdatePart_MoveModule:
			OnPacket_UpdatePart_MoveModule(msg);
			break;
		case PacketType.UpdatePart_ResourceModule:
			OnPacket_UpdatePart_ResourceModule(msg);
			break;
		case PacketType.UpdatePartHeat:
			OnPacket_UpdatePartHeat(msg);
			break;
		case PacketType.JoinResponse:
			Debug.LogWarning("Recieved server info packet outside of connection attempt.");
			break;
		case PacketType.JoinRequest:
			Debug.LogWarning($"Recieved packet (of type {packetType}) intended for the server.");
			break;
		default:
			Debug.LogWarning($"Unhandled packet type ({packetType})!");
			break;
		}
	}

	public static void SendPacket(Packet packet, NetDeliveryMethod method = NetDeliveryMethod.ReliableOrdered)
	{
		if (client == null || client.Status != NetPeerStatus.Running)
		{
			Debug.LogWarning("Attempted to send packet while client is not running: " + packet.Type);
			return;
		}
		if (Packet.ShouldDebug(packet.Type))
		{
			Debug.Log($"Sending packet of type {packet.Type}.");
		}
		NetOutgoingMessage netOutgoingMessage = client.CreateMessage();
		netOutgoingMessage.Write((byte)packet.Type);
		netOutgoingMessage.Write(packet);
		client.SendMessage(netOutgoingMessage, method);
	}

	public static void Disconnect(string reason)
	{
		multiplayerEnabled.Value = false;
		pumpEnabled = false;
		if (client != null)
		{
			if (client.Status != NetPeerStatus.NotRunning)
			{
				client.Shutdown(reason ?? "Disconnected");
			}
			client = null;
		}
		ChatWindow.DestroyUI();
		ChatWindow.DestroyCooldownTimer();
		LocalManager.DisableUpdateTimer();
		LocalManager.players?.Clear();
		LocalManager.syncedRockets?.Clear();
		LocalManager.unsyncedRockets?.Clear();
		LocalManager.updateAuthority?.Clear();
		LocalManager.loadedRockets?.Clear();
		LocalManager.unsyncedToControl = -1;
	}

	private static void OnPacket_PlayerConnected(NetIncomingMessage msg)
	{
		Packet_PlayerConnected packet_PlayerConnected = msg.Read<Packet_PlayerConnected>();
		if (LocalManager.players.ContainsKey(packet_PlayerConnected.PlayerId))
		{
			LocalManager.players[packet_PlayerConnected.PlayerId] = new LocalPlayer(packet_PlayerConnected.Username, packet_PlayerConnected.IconColor);
		}
		else
		{
			LocalManager.players.Add(packet_PlayerConnected.PlayerId, new LocalPlayer(packet_PlayerConnected.Username, packet_PlayerConnected.IconColor));
		}
		if (packet_PlayerConnected.PrintMessage)
		{
			string text = MultiplayerLoc.L(packet_PlayerConnected.Username + " 已连接", packet_PlayerConnected.Username + " connected");
			MsgDrawer.main.Log(text);
			ChatWindow.AddMessage(new ChatMessage(text));
		}
	}

	private static void OnPacket_PlayerDisconnected(NetIncomingMessage msg)
	{
		Packet_PlayerDisconnected packet_PlayerDisconnected = msg.Read<Packet_PlayerDisconnected>();
		if (LocalManager.players.TryGetValue(packet_PlayerDisconnected.PlayerId, out var value))
		{
			string text = MultiplayerLoc.L(value.username + " 已断开连接", value.username + " disconnected");
			MsgDrawer.main.Log(text);
			ChatWindow.AddMessage(new ChatMessage(text));
			LocalManager.players.Remove(packet_PlayerDisconnected.PlayerId);
		}
	}

	private static void OnPacket_UpdatePlayerControl(NetIncomingMessage msg)
	{
		Packet_UpdatePlayerControl packet_UpdatePlayerControl = msg.Read<Packet_UpdatePlayerControl>();
		if (LocalManager.players.TryGetValue(packet_UpdatePlayerControl.PlayerId, out var value))
		{
			value.controlledRocket.Value = packet_UpdatePlayerControl.RocketId;
		}
		else
		{
			Debug.LogError("Missing player while trying to update controlled rocket!");
		}
	}

	private static void OnPacket_UpdatePlayerAuthority(NetIncomingMessage msg)
	{
		Packet_UpdatePlayerAuthority packet = msg.Read<Packet_UpdatePlayerAuthority>();
		LocalManager.updateAuthority = packet.RocketIds;
		if (packet.LoadedRocketIds != null)
		{
			LocalManager.loadedRockets = packet.LoadedRocketIds;
		}
		foreach (int item in LocalManager.updateAuthority)
		{
			LocalManager.syncedRockets.TryGetValue(item, out var _);
		}
	}

	private static void OnPacket_UpdateWorldTime(NetIncomingMessage msg)
	{
		Packet_UpdateWorldTime packet_UpdateWorldTime = msg.Read<Packet_UpdateWorldTime>();
		if (WorldTime.main != null)
		{
			WorldTime main = WorldTime.main;
			double worldTime = (world.WorldTime = packet_UpdateWorldTime.WorldTime);
			main.worldTime = worldTime;
		}
	}

	private static void OnPacket_UpdatePlayerColor(NetIncomingMessage msg)
	{
		Packet_UpdatePlayerColor packet_UpdatePlayerColor = msg.Read<Packet_UpdatePlayerColor>();
		if (LocalManager.players.TryGetValue(packet_UpdatePlayerColor.PlayerId, out var value))
		{
			value.iconColor = packet_UpdatePlayerColor.Color;
			ChatWindow.OnPlayerColorChange(packet_UpdatePlayerColor.PlayerId, packet_UpdatePlayerColor.Color);
		}
	}

	private static void OnPacket_SendChatMessage(NetIncomingMessage msg)
	{
		Packet_SendChatMessage packet_SendChatMessage = msg.Read<Packet_SendChatMessage>();
		ChatWindow.AddMessage(new ChatMessage(packet_SendChatMessage.Message, packet_SendChatMessage.SenderId, packet_SendChatMessage.Color));
	}

	private static void OnPacket_ShowToastMessage(NetIncomingMessage msg)
	{
		ToastHelper.ShowToast(msg.Read<Packet_ShowToastMessage>().Message);
	}

	private static void OnPacket_UpdateCheatStatus(NetIncomingMessage msg)
	{
		Packet_UpdateCheatStatus packet_UpdateCheatStatus = msg.Read<Packet_UpdateCheatStatus>();
		if (SandboxSettings.main != null)
		{
			SandboxSettings.main.settings.infiniteFuel = packet_UpdateCheatStatus.InfiniteFuel;
			SandboxSettings.main.settings.noAtmosphericDrag = packet_UpdateCheatStatus.NoAtmosphericDrag;
			SandboxSettings.main.settings.unbreakableParts = packet_UpdateCheatStatus.UnbreakableParts;
			SandboxSettings.main.settings.noGravity = packet_UpdateCheatStatus.NoGravity;
			SandboxSettings.main.settings.noHeatDamage = packet_UpdateCheatStatus.NoHeatDamage;
			SandboxSettings.main.settings.noBurnMarks = packet_UpdateCheatStatus.NoBurnMarks;
			SandboxSettings.main.settings.infiniteBuildArea = packet_UpdateCheatStatus.InfiniteBuildArea;
			SandboxSettings.main.settings.partClipping = packet_UpdateCheatStatus.PartClipping;
			SandboxSettings.main.UpdateUI(instantAnimation: false);
			Debug.Log($"Cheat status updated: InfiniteFuel={packet_UpdateCheatStatus.InfiniteFuel}, NoAtmosphericDrag={packet_UpdateCheatStatus.NoAtmosphericDrag}");
		}
	}

	public static void OnLocalCheatChanged()
	{
		if (!multiplayerEnabled.Value || SandboxSettings.main == null)
		{
			return;
		}
		if (!IsHost)
		{
			return;
		}
		SendPacket(new Packet_UpdateCheatStatus
		{
			InfiniteFuel = SandboxSettings.main.settings.infiniteFuel,
			NoAtmosphericDrag = SandboxSettings.main.settings.noAtmosphericDrag,
			UnbreakableParts = SandboxSettings.main.settings.unbreakableParts,
			NoGravity = SandboxSettings.main.settings.noGravity,
			NoHeatDamage = SandboxSettings.main.settings.noHeatDamage,
			NoBurnMarks = SandboxSettings.main.settings.noBurnMarks,
			InfiniteBuildArea = SandboxSettings.main.settings.infiniteBuildArea,
			PartClipping = SandboxSettings.main.settings.partClipping
		});
	}

	private static async Task DownloadSolarSystem(string solarSystemName)
	{
		solarSystemDownloadTcs = new TaskCompletionSource<bool>();
		foreach (MemoryStream stream in solarSystemFileBuffers.Values)
		{
			stream.Dispose();
		}
		solarSystemFileBuffers.Clear();
		Menu.loading.Open(MultiplayerLoc.L("正在下载扩展包 " + solarSystemName + "...", "Downloading expansion pack " + solarSystemName + "..."));
		SendPacket(new Packet_SolarSystemFile
		{
			FileName = "REQUEST",
			Data = null,
			IsLast = false
		});
		Task timeoutTask = Task.Delay(30000);
		Task downloadTask = solarSystemDownloadTcs.Task;
		// 等待下载包期间必须主动读取网络消息，否则服务器发来的文件包永远不会被处理。
		while (client != null && client.Status == NetPeerStatus.Running && !downloadTask.IsCompleted && !timeoutTask.IsCompleted)
		{
			Listen();
			await Task.Delay(10);
		}
		if (!downloadTask.IsCompleted)
		{
			solarSystemDownloadTcs.TrySetResult(result: false);
			Debug.LogWarning("Solar system download timed out after 30s: " + solarSystemName);
		}
		Menu.loading.Close();
	}

	private static void OnPacket_SolarSystemFile(NetIncomingMessage msg)
	{
		Packet_SolarSystemFile packet = msg.Read<Packet_SolarSystemFile>();
		if (packet.FileName == "")
		{
			if (solarSystemDownloadTcs != null)
			{
				solarSystemDownloadTcs.TrySetResult(result: true);
				solarSystemDownloadTcs = null;
			}
			return;
		}
		if (!solarSystemFileBuffers.TryGetValue(packet.FileName, out MemoryStream buffer))
		{
			buffer = new MemoryStream();
			solarSystemFileBuffers[packet.FileName] = buffer;
		}
		if (packet.Data != null && packet.Data.Length > 0)
		{
			buffer.Write(packet.Data, 0, packet.Data.Length);
		}
		if (packet.IsLast)
		{
			string fullPath = Path.Combine((string)FileLocations.SolarSystemsFolder.Extend(world.solarSystemName), packet.FileName.TrimStart('/', '\\'));
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
				File.WriteAllBytes(fullPath, buffer.ToArray());
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to write solar system file " + packet.FileName + ": " + ex.Message);
			}
			buffer.Dispose();
			solarSystemFileBuffers.Remove(packet.FileName);
		}
	}

	private static void OnPacket_TimeWarpVote(NetIncomingMessage msg)
	{
		Packet_TimeWarpVote vote = msg.Read<Packet_TimeWarpVote>();
		TimeWarpVoting.OnVoteReceived(vote);
	}

	private static void OnPacket_TimeWarpResult(NetIncomingMessage msg)
	{
		TimeWarpVoting.OnVoteResultReceived(msg.Read<Packet_TimeWarpResult>());
	}

	private static void OnPacket_CreateRocket(NetIncomingMessage msg)
	{
		Packet_CreateRocket packet_CreateRocket = msg.Read<Packet_CreateRocket>();
		world.rockets[packet_CreateRocket.GlobalId] = packet_CreateRocket.Rocket;
		LocalManager.OnPacket_CreateRocket(packet_CreateRocket);
	}

	private static void OnPacket_DestroyRocket(NetIncomingMessage msg)
	{
		Packet_DestroyRocket packet_DestroyRocket = msg.Read<Packet_DestroyRocket>();
		world.rockets.Remove(packet_DestroyRocket.RocketId);
		LocalManager.DestroyLocalRocket(packet_DestroyRocket.RocketId, packet_DestroyRocket.Reason);
	}

	private static void OnPacket_UpdateRocketPrimary(NetIncomingMessage msg)
	{
		Packet_UpdateRocketPrimary packet_UpdateRocketPrimary = msg.Read<Packet_UpdateRocketPrimary>();
		if (world.rockets.TryGetValue(packet_UpdateRocketPrimary.RocketId, out var value))
		{
			value.UpdateRocketPrimary(packet_UpdateRocketPrimary);
			Interpolator.AddPacketToQueue(packet_UpdateRocketPrimary, packet_UpdateRocketPrimary.RocketId, packet_UpdateRocketPrimary.WorldTime);
		}
		else
		{
			Debug.Log("Missing rocket from world state!!!");
		}
	}

	private static void OnPacket_UpdateRocketSecondary(NetIncomingMessage msg)
	{
		Packet_UpdateRocketSecondary packet_UpdateRocketSecondary = msg.Read<Packet_UpdateRocketSecondary>();
		if (world.rockets.TryGetValue(packet_UpdateRocketSecondary.RocketId, out var value))
		{
			value.UpdateRocketSecondary(packet_UpdateRocketSecondary);
			Interpolator.AddPacketToQueue(packet_UpdateRocketSecondary, packet_UpdateRocketSecondary.RocketId, packet_UpdateRocketSecondary.WorldTime);
		}
	}

	private static void OnPacket_DestroyPart(NetIncomingMessage msg)
	{
		Packet_DestroyPart packet_DestroyPart = msg.Read<Packet_DestroyPart>();
		if (world.rockets.TryGetValue(packet_DestroyPart.RocketId, out var value))
		{
			value.RemovePart(packet_DestroyPart.PartId);
			Interpolator.AddPacketToQueue(packet_DestroyPart, packet_DestroyPart.RocketId, packet_DestroyPart.WorldTime);
		}
	}

	private static void OnPacket_UpdateStaging(NetIncomingMessage msg)
	{
		Packet_UpdateStaging packet_UpdateStaging = msg.Read<Packet_UpdateStaging>();
		if (world.rockets.TryGetValue(packet_UpdateStaging.RocketId, out var value))
		{
			value.stages = packet_UpdateStaging.Stages;
			Interpolator.AddPacketToQueue(packet_UpdateStaging, packet_UpdateStaging.RocketId, packet_UpdateStaging.WorldTime);
		}
	}

	private static void OnPacket_UpdatePart_EngineModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_EngineModule packet_UpdatePart_EngineModule = msg.Read<Packet_UpdatePart_EngineModule>();
		if (world.rockets.TryGetValue(packet_UpdatePart_EngineModule.RocketId, out var value))
		{
			if (value.parts.TryGetValue(packet_UpdatePart_EngineModule.PartId, out var value2))
			{
				value2.part.TOGGLE_VARIABLES["engine_on"] = packet_UpdatePart_EngineModule.EngineOn;
			}
			Interpolator.AddPacketToQueue(packet_UpdatePart_EngineModule, packet_UpdatePart_EngineModule.RocketId, packet_UpdatePart_EngineModule.WorldTime);
		}
	}

	private static void OnPacket_UpdatePart_WheelModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_WheelModule packet_UpdatePart_WheelModule = msg.Read<Packet_UpdatePart_WheelModule>();
		if (world.rockets.TryGetValue(packet_UpdatePart_WheelModule.RocketId, out var value))
		{
			if (value.parts.TryGetValue(packet_UpdatePart_WheelModule.PartId, out var value2))
			{
				value2.part.TOGGLE_VARIABLES["wheel_on"] = packet_UpdatePart_WheelModule.WheelOn;
			}
			Interpolator.AddPacketToQueue(packet_UpdatePart_WheelModule, packet_UpdatePart_WheelModule.RocketId, packet_UpdatePart_WheelModule.WorldTime);
		}
	}

	private static void OnPacket_UpdatePart_BoosterModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_BoosterModule packet_UpdatePart_BoosterModule = msg.Read<Packet_UpdatePart_BoosterModule>();
		if (world.rockets.TryGetValue(packet_UpdatePart_BoosterModule.RocketId, out var value))
		{
			if (value.parts.TryGetValue(packet_UpdatePart_BoosterModule.PartId, out var value2))
			{
				value2.part.NUMBER_VARIABLES["fuel_percent"] = packet_UpdatePart_BoosterModule.FuelPercent;
			}
			Interpolator.AddPacketToQueue(packet_UpdatePart_BoosterModule, packet_UpdatePart_BoosterModule.RocketId, packet_UpdatePart_BoosterModule.WorldTime);
		}
	}

	private static void OnPacket_UpdatePart_ParachuteModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_ParachuteModule packet_UpdatePart_ParachuteModule = msg.Read<Packet_UpdatePart_ParachuteModule>();
		if (world.rockets.TryGetValue(packet_UpdatePart_ParachuteModule.RocketId, out var value))
		{
			if (value.parts.TryGetValue(packet_UpdatePart_ParachuteModule.PartId, out var value2))
			{
				value2.part.NUMBER_VARIABLES["animation_state"] = packet_UpdatePart_ParachuteModule.State;
				value2.part.NUMBER_VARIABLES["deploy_state"] = packet_UpdatePart_ParachuteModule.TargetState;
			}
			Interpolator.AddPacketToQueue(packet_UpdatePart_ParachuteModule, packet_UpdatePart_ParachuteModule.RocketId, packet_UpdatePart_ParachuteModule.WorldTime);
		}
	}

	private static void OnPacket_UpdatePart_MoveModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_MoveModule packet_UpdatePart_MoveModule = msg.Read<Packet_UpdatePart_MoveModule>();
		if (world.rockets.TryGetValue(packet_UpdatePart_MoveModule.RocketId, out var value))
		{
			if (value.parts.TryGetValue(packet_UpdatePart_MoveModule.PartId, out var value2))
			{
				value2.part.NUMBER_VARIABLES["state"] = packet_UpdatePart_MoveModule.Time;
				value2.part.NUMBER_VARIABLES["state_target"] = packet_UpdatePart_MoveModule.TargetTime;
			}
			Interpolator.AddPacketToQueue(packet_UpdatePart_MoveModule, packet_UpdatePart_MoveModule.RocketId, packet_UpdatePart_MoveModule.WorldTime);
		}
	}

	private static void OnPacket_UpdatePart_ResourceModule(NetIncomingMessage msg)
	{
		Packet_UpdatePart_ResourceModule packet_UpdatePart_ResourceModule = msg.Read<Packet_UpdatePart_ResourceModule>();
		if (!world.rockets.TryGetValue(packet_UpdatePart_ResourceModule.RocketId, out var value))
		{
			return;
		}
		foreach (int partId in packet_UpdatePart_ResourceModule.PartIds)
		{
			if (value.parts.TryGetValue(partId, out var value2))
			{
				value2.part.NUMBER_VARIABLES["fuel_percent"] = packet_UpdatePart_ResourceModule.ResourcePercent;
			}
		}
		Interpolator.AddPacketToQueue(packet_UpdatePart_ResourceModule, packet_UpdatePart_ResourceModule.RocketId, packet_UpdatePart_ResourceModule.WorldTime);
	}

	private static void OnPacket_UpdatePartHeat(NetIncomingMessage msg)
	{
		Packet_UpdatePartHeat packet = msg.Read<Packet_UpdatePartHeat>();
		if (LocalManager.updateAuthority.Contains(packet.RocketId))
		{
			return;
		}
		if (!LocalManager.syncedRockets.TryGetValue(packet.RocketId, out var localRocket) || localRocket.rocket == null)
		{
			return;
		}
		for (int i = 0; i < packet.PartIds.Count && i < packet.Temperatures.Count; i++)
		{
			if (localRocket.parts.TryGetValue(packet.PartIds[i], out var part) && part != null)
			{
				part.Temperature = packet.Temperatures[i];
			}
		}
	}
}
}
