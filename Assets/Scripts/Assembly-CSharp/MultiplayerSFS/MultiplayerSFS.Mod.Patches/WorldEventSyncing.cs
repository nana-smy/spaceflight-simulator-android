using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MultiplayerSFS.Common;
using SFS.Parts;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace MultiplayerSFS.Mod.Patches {
public class WorldEventSyncing
{
	public class RocketManager_SpawnBlueprint
	{
		public static bool GetInMultiplayer()
		{
			return ClientManager.multiplayerEnabled;
		}

		public static void SyncLaunch(Rocket[] rockets)
		{
			Menu.loading.Open("Sending launch request to server...");
			Dictionary<Rocket, int> dictionary = new Dictionary<Rocket, int>(rockets.Length);
			foreach (Rocket rocket in rockets)
			{
				LocalRocket localRocket = new LocalRocket(rocket);
				int num = LocalManager.unsyncedRockets.InsertNew(localRocket);
				ClientManager.SendPacket(new Packet_CreateRocket
				{
					WorldTime = ClientManager.world.WorldTime,
					LocalId = num,
					ForLaunch = true,
					Rocket = localRocket.ToState()
				});
				dictionary.Add(rocket, num);
			}
			Rocket rocket2 = rockets.FirstOrDefault((Rocket r) => r.hasControl.Value) ?? ((rockets.Length != 0) ? rockets[0] : null);
			LocalManager.unsyncedToControl = ((rocket2 != null && dictionary.TryGetValue(rocket2, out var value)) ? value : (-1));
			if (TimeWarpVoting.IsTimeWarping)
			{
				TimeWarpVoting.SyncCurrentTimeWarp();
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(Part), "DestroyPart")]
	public class Part_DestroyPart
	{
		public static bool Prefix(Part __instance, bool createExplosion, ref DestructionReason reason)
		{
			if ((bool)ClientManager.multiplayerEnabled && GameManager.main != null)
			{
				if (reason == (DestructionReason)4)
				{
					reason = LocalManager.TrueDestructionReason;
					return true;
				}
				int syncedRocketID = LocalManager.GetSyncedRocketID(__instance.Rocket);
				if (syncedRocketID == -1)
				{
					return false;
				}
				int localPartID = LocalManager.GetLocalPartID(syncedRocketID, __instance);
				if (localPartID == -1)
				{
					Debug.LogError("Couldn't find destroyed part's id!");
					return false;
				}
				if (!LocalManager.updateAuthority.Contains(syncedRocketID))
				{
					return false;
				}
				if (ClientManager.world.rockets.TryGetValue(syncedRocketID, out RocketState worldRocket) && worldRocket != null)
				{
					worldRocket.RemovePart(localPartID);
				}
				else
				{
					Debug.LogWarning("Missing world rocket while destroying part: " + syncedRocketID);
				}
				ClientManager.SendPacket(new Packet_DestroyPart
				{
					WorldTime = ClientManager.world.WorldTime,
					RocketId = syncedRocketID,
					PartId = localPartID,
					CreateExplosion = createExplosion,
					Reason = reason
				});
				return true;
			}
			return true;
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(RocketManager), "DestroyRocket")]
	public static class RocketManager_DestroyRocket
	{
		public static bool Prefix(Rocket rocket, ref DestructionReason reason)
		{
			if ((bool)ClientManager.multiplayerEnabled)
			{
				if (reason == (DestructionReason)4)
				{
					reason = LocalManager.TrueDestructionReason;
					return true;
				}
				int syncedRocketID = LocalManager.GetSyncedRocketID(rocket);
				if (syncedRocketID < 0)
				{
					return false;
				}
				if (LocalManager.Player == null)
				{
					return false;
				}
				if (LocalManager.updateAuthority.Contains(syncedRocketID))
				{
					ClientManager.world.rockets.Remove(syncedRocketID);
					LocalManager.syncedRockets.Remove(syncedRocketID);
					LocalManager.updateAuthority.Remove(syncedRocketID);
					ClientManager.SendPacket(new Packet_DestroyRocket
					{
						WorldTime = ClientManager.world.WorldTime,
						RocketId = syncedRocketID,
						Reason = reason
					});
					return true;
				}
				return false;
			}
			return true;
		}

		public static void Postfix(Rocket rocket)
		{
			if (!ClientManager.multiplayerEnabled || !(PlayerController.main.player.Value is Rocket rocket2) || !(rocket2 != rocket))
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(rocket2);
			if (syncedRocketID >= 0)
			{
				ClientManager.SendPacket(new Packet_UpdatePlayerControl
				{
					PlayerId = ClientManager.playerId,
					RocketId = syncedRocketID
				});
				return;
			}
			syncedRocketID = LocalManager.GetUnsyncedRocketID(rocket2);
			if (syncedRocketID >= 0)
			{
				LocalManager.unsyncedToControl = syncedRocketID;
			}
			else
			{
				Debug.LogWarning("`RocketManager_DestroyRocket`: Player is controlling unregistered rocket!");
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(JointGroup), "RecreateRockets")]
	public class JointGroup_RecreateRockets
	{
		public static void Postfix(Rocket rocket, List<Rocket> childRockets)
		{
			if (!ClientManager.multiplayerEnabled)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(rocket);
			if (syncedRocketID == -1)
			{
				return;
			}
			if (!LocalManager.updateAuthority.Contains(syncedRocketID))
			{
				foreach (Rocket childRocket in childRockets)
				{
					RocketManager.DestroyRocket(childRocket, (DestructionReason)4);
				}
				return;
			}
			LocalRocket localRocket = LocalManager.syncedRockets[syncedRocketID];
			localRocket.parts.Clear();
			foreach (Part item in rocket.partHolder.partsSet)
			{
				localRocket.parts.InsertNew(item);
			}
			RocketState rocketState = localRocket.ToState();
			ClientManager.world.rockets[syncedRocketID] = rocketState;
			ClientManager.SendPacket(new Packet_CreateRocket
			{
				WorldTime = ClientManager.world.WorldTime,
				GlobalId = syncedRocketID,
				Rocket = rocketState
			});
			foreach (Rocket childRocket2 in childRockets)
			{
				LocalRocket localRocket2 = new LocalRocket(childRocket2);
				RocketState rocket2 = localRocket2.ToState();
				int localId = LocalManager.unsyncedRockets.InsertNew(localRocket2);
				ClientManager.SendPacket(new Packet_CreateRocket
				{
					WorldTime = ClientManager.world.WorldTime,
					LocalId = localId,
					Rocket = rocket2
				});
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch]
	public class StagingUpdates
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(StagingDrawer), "AddStage");
			yield return AccessTools.Method(typeof(StagingDrawer), "UseStage");
			yield return AccessTools.Method(typeof(StagingDrawer), "OnReorder");
			yield return AccessTools.Method(typeof(StagingDrawer), "RemoveStage");
			yield return AccessTools.Method(typeof(StagingDrawer), "TogglePartSelected");
		}

		public static void Postfix(StagingDrawer __instance)
		{
			if (!ClientManager.multiplayerEnabled)
			{
				return;
			}
			Staging value = __instance.FieldRef<Staging_Local>("staging").Value;
			int syncedRocketID = LocalManager.GetSyncedRocketID(value.rocket);
			LocalPlayer player = LocalManager.Player;
			if (player == null)
			{
				return;
			}
			bool flag = player.controlledRocket.Value == syncedRocketID;
			if (!LocalManager.updateAuthority.Contains(syncedRocketID) || !flag)
			{
				return;
			}
			if (GameManager.main != null && LocalManager.syncedRockets.TryGetValue(syncedRocketID, out var value2))
			{
				List<StageState> list = new List<StageState>(value.stages.Count);
				foreach (Stage stage in value.stages)
				{
					StageState item = new StageState
					{
						stageID = stage.stageId,
						partIDs = (from id in stage.parts.Select(value2.GetPartID)
							where id >= 0
							select id).ToList()
					};
					list.Add(item);
				}
				if (ClientManager.world.rockets.TryGetValue(syncedRocketID, out RocketState worldStagingRocket) && worldStagingRocket != null)
				{
					worldStagingRocket.stages = list;
				}
				else
				{
					Debug.LogWarning("Missing world rocket while updating staging: " + syncedRocketID);
				}
				ClientManager.SendPacket(new Packet_UpdateStaging
				{
					WorldTime = ClientManager.world.WorldTime,
					RocketId = syncedRocketID,
					Stages = list
				});
			}
			else
			{
				Debug.LogError("Missing local rocket when trying to send staging update!");
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(Rocket), "SFS.World.I_Physics.OnFixedUpdate")]
	public static class Rocket_OnFixedUpdate
	{
		public static bool Prefix(Rocket __instance)
		{
			if (!ClientManager.multiplayerEnabled)
			{
				return true;
			}
			if (__instance.rb2d.bodyType != RigidbodyType2D.Dynamic)
			{
				return false;
			}
			return true;
		}
	}
}
}
