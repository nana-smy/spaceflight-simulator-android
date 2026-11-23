using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MultiplayerSFS.Common;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;

namespace MultiplayerSFS.Mod.Patches {
public class PartUpdateSyncing
{
	private static readonly HashSet<EngineModule> subscribedEngines = new HashSet<EngineModule>();
	private static readonly HashSet<ParachuteModule> subscribedParachutes = new HashSet<ParachuteModule>();

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(DockingPortModule), "Dock")]
	public static class DockingPortModule_Dock
	{
		public static bool Prefix(DockingPortModule __instance, DockingPortModule otherPort)
		{
			if ((bool)otherPort.Rocket.isPlayer)
			{
				return false;
			}
			if ((bool)ClientManager.multiplayerEnabled)
			{
				int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.Rocket);
				int syncedRocketID2 = LocalManager.GetSyncedRocketID(otherPort.Rocket);
				if (syncedRocketID == -1 || syncedRocketID2 == -1)
				{
					return false;
				}
				bool flag = false;
				bool flag2 = false;
				foreach (KeyValuePair<int, LocalPlayer> player in LocalManager.players)
				{
					flag |= (int)player.Value.controlledRocket == syncedRocketID;
					flag2 |= (int)player.Value.controlledRocket == syncedRocketID2;
					if (flag && flag2)
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(RocketManager), "MergeRockets")]
	public class RocketManager_MergeRockets
	{
		public static void Postfix(Rocket rocket_A, Rocket rocket_B)
		{
			if (!ClientManager.multiplayerEnabled || rocket_A == rocket_B)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(rocket_A);
			int syncedRocketID2 = LocalManager.GetSyncedRocketID(rocket_B);
			if (syncedRocketID < 0 || syncedRocketID2 < 0)
			{
				return;
			}
			if (!LocalManager.syncedRockets.TryGetValue(syncedRocketID, out LocalRocket localA) || !LocalManager.syncedRockets.TryGetValue(syncedRocketID2, out LocalRocket localB))
			{
				return;
			}

			// 原版 MergeRockets 保留 rocket_A、销毁 rocket_B。
			// 将 B 的部件映射合并进 A 的 LocalRocket，使 ToState() 能包含合并后的全部部件。
			foreach (KeyValuePair<int, Part> kvp in localB.parts)
			{
				if (kvp.Value != null && !localA.parts.ContainsValue(kvp.Value))
				{
					localA.parts.InsertNew(kvp.Value);
				}
			}
			LocalManager.syncedRockets.Remove(syncedRocketID2);
			LocalManager.updateAuthority.Remove(syncedRocketID2);
			LocalManager.loadedRockets.Remove(syncedRocketID2);

			// 若本机玩家控制的是被销毁的 B，则切换到保留下来的 A。
			LocalPlayer localPlayer = LocalManager.Player;
			if (localPlayer != null && ((int)localPlayer.controlledRocket == syncedRocketID || (int)localPlayer.controlledRocket == syncedRocketID2))
			{
				localPlayer.controlledRocket.Value = syncedRocketID;
			}
			else if ((bool)rocket_A.isPlayer || (bool)rocket_B.isPlayer)
			{
				ClientManager.SendPacket(new Packet_UpdatePlayerControl
				{
					PlayerId = ClientManager.playerId,
					RocketId = syncedRocketID
				});
			}

			foreach (KeyValuePair<int, LocalRocket> syncedRocket in LocalManager.syncedRockets)
			{
				ClientManager.SendPacket(new Packet_CreateRocket
				{
					WorldTime = ClientManager.world.WorldTime,
					GlobalId = syncedRocket.Key,
					Rocket = syncedRocket.Value.ToState()
				});
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(EngineModule), "Start")]
	public class EngineModule_Start
	{
		public static void Postfix(EngineModule __instance)
		{
			if (GameManager.main != null && (bool)ClientManager.multiplayerEnabled && subscribedEngines.Add(__instance))
			{
				__instance.engineOn.OnChange += new Action<bool, bool>(OnToggle);
			}
			void OnToggle(bool engineOn_old, bool engineOn_new)
			{
				if (engineOn_old != engineOn_new)
				{
					int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.GetComponentInParentTree<Rocket>());
					if (syncedRocketID >= 0)
					{
						LocalPlayer player = LocalManager.Player;
						if (player != null)
						{
							bool flag = player.controlledRocket.Value == syncedRocketID;
							if (LocalManager.updateAuthority.Contains(syncedRocketID) && flag)
							{
								Part componentInParent = __instance.GetComponentInParent<Part>();
								int localPartID = LocalManager.GetLocalPartID(syncedRocketID, componentInParent);
								if (localPartID < 0)
								{
									return;
								}
								ClientManager.SendPacket(new Packet_UpdatePart_EngineModule
								{
									WorldTime = ClientManager.world.WorldTime,
									RocketId = syncedRocketID,
									PartId = localPartID,
									EngineOn = engineOn_new
								});
							}
						}
					}
				}
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch]
	public class BoosterModuleUpdates
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(BoosterModule), "FixedUpdate");
			yield return AccessTools.Method(typeof(BoosterModule), "Fire");
			yield return AccessTools.Method(typeof(BoosterModule), "Fire_Instantly");
		}

		public static void Prefix(BoosterModule __instance, out (bool primed, float throttle) __state)
		{
			__state.primed = __instance.boosterPrimed.Value;
			__state.throttle = __instance.throttle_Out.Value;
		}

		public static void Postfix(BoosterModule __instance, (bool primed, float throttle) __state, MethodBase __originalMethod)
		{
			bool value = __instance.boosterPrimed.Value;
			float value2 = __instance.throttle_Out.Value;
			if (!ClientManager.multiplayerEnabled || !(GameManager.main != null) || (value == __state.primed && value2 == __state.throttle))
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.GetComponentInParentTree<Rocket>());
			if (syncedRocketID < 0)
			{
				return;
			}
			LocalPlayer player = LocalManager.Player;
			if (player != null)
			{
				bool flag = player.controlledRocket.Value == syncedRocketID;
				if (LocalManager.updateAuthority.Contains(syncedRocketID) && flag)
				{
					Part componentInParent = __instance.GetComponentInParent<Part>();
					int localPartID = LocalManager.GetLocalPartID(syncedRocketID, componentInParent);
					if (localPartID < 0)
					{
						return;
					}
					ClientManager.SendPacket(new Packet_UpdatePart_BoosterModule
					{
						WorldTime = ClientManager.world.WorldTime,
						RocketId = syncedRocketID,
						PartId = localPartID,
						Primed = value,
						Throttle = value2,
						FuelPercent = __instance.fuelPercent.Value
					});
				}
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(WheelModule), "ToggleEnabled")]
	public static class WheelModule_ToggleEnabled
	{
		public static void Postfix(WheelModule __instance)
		{
			if (!(GameManager.main != null) || !ClientManager.multiplayerEnabled)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.GetComponentInParentTree<Rocket>());
			if (syncedRocketID < 0)
			{
				return;
			}
			LocalPlayer player = LocalManager.Player;
			if (player != null)
			{
				bool flag = player.controlledRocket.Value == syncedRocketID;
				if (LocalManager.updateAuthority.Contains(syncedRocketID) && flag)
				{
					Part componentInParent = __instance.GetComponentInParent<Part>();
					int localPartID = LocalManager.GetLocalPartID(syncedRocketID, componentInParent);
					if (localPartID < 0)
					{
						return;
					}
					ClientManager.SendPacket(new Packet_UpdatePart_WheelModule
					{
						WorldTime = ClientManager.world.WorldTime,
						RocketId = syncedRocketID,
						PartId = localPartID,
						WheelOn = __instance.on.Value
					});
				}
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(ParachuteModule), "Start")]
	public class ParachuteModule_Start
	{
		public static void Postfix(ParachuteModule __instance)
		{
			if (GameManager.main != null && (bool)ClientManager.multiplayerEnabled && subscribedParachutes.Add(__instance))
			{
				__instance.targetState.OnChange += new Action(UpdateState);
			}
			void UpdateState()
			{
				int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.GetComponentInParentTree<Rocket>());
				if (syncedRocketID >= 0)
				{
					LocalPlayer player = LocalManager.Player;
					if (player != null)
					{
						bool flag = player.controlledRocket.Value == syncedRocketID;
						if (LocalManager.updateAuthority.Contains(syncedRocketID) && flag)
						{
							Part componentInParent = __instance.GetComponentInParent<Part>();
							int localPartID = LocalManager.GetLocalPartID(syncedRocketID, componentInParent);
							if (localPartID < 0)
							{
								return;
							}
							ClientManager.SendPacket(new Packet_UpdatePart_ParachuteModule
							{
								WorldTime = ClientManager.world.WorldTime,
								RocketId = syncedRocketID,
								PartId = localPartID,
								State = __instance.state.Value,
								TargetState = __instance.targetState.Value
							});
						}
					}
				}
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(MoveModule), "Toggle")]
	public static class MoveModule_Toggle
	{
		public static void Postfix(MoveModule __instance)
		{
			if (!(GameManager.main != null) || !ClientManager.multiplayerEnabled)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.GetComponentInParentTree<Rocket>());
			if (syncedRocketID < 0)
			{
				return;
			}
			LocalPlayer player = LocalManager.Player;
			if (player != null)
			{
				bool flag = player.controlledRocket.Value == syncedRocketID;
				if (LocalManager.updateAuthority.Contains(syncedRocketID) && flag)
				{
					Part componentInParent = __instance.GetComponentInParent<Part>();
					int localPartID = LocalManager.GetLocalPartID(syncedRocketID, componentInParent);
					if (localPartID < 0)
					{
						return;
					}
					ClientManager.SendPacket(new Packet_UpdatePart_MoveModule
					{
						WorldTime = ClientManager.world.WorldTime,
						RocketId = syncedRocketID,
						PartId = localPartID,
						Time = __instance.time.Value,
						TargetTime = __instance.targetTime.Value
					});
				}
			}
		}
	}
}
}
