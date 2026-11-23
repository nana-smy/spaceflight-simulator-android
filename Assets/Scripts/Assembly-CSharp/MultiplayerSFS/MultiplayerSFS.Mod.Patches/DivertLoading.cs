using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MultiplayerSFS.Common;
using SFS;
using SFS.Builds;
using SFS.Career;
using SFS.Input;
using SFS.Stats;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerSFS.Mod.Patches {
public class DivertLoading
{
	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(SavingCache), "Preload_WorldPersistent")]
	public class SavingCache_Preload_WorldPersistent
	{
		public static bool Prefix(SavingCache __instance, bool needsRocketsAndBranches)
		{
			if (ClientManager.multiplayerEnabled.Value)
			{
				ref SavingCache.Data<WorldSave> reference = ref __instance.FieldRef<SavingCache.Data<WorldSave>>("worldPersistent");
				if (reference == null || reference.result.data == null || (needsRocketsAndBranches && (reference.result.data.rockets == null || reference.result.data.branches == null)))
				{
					reference = new SavingCache.Data<WorldSave>();
					reference.result = (success: true, data: WorldSave.CreateEmptyQuicksave(Application.version), log: null);
				}
				return false;
			}
			return true;
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(GameManager), "LoadPersistentAndLaunch")]
	public class GameManager_LoadPersistentAndLaunch
	{
		public static bool Prefix(GameManager __instance)
		{
			if (ClientManager.multiplayerEnabled.Value)
			{
				try
				{
				SavingCache.main.Preload_WorldPersistent(needsRocketsAndBranches: true);
				SavingCache.main.FieldRef<SavingCache.Data<WorldSave>>("worldPersistent") = null;
				AccessTools.Method(typeof(GameManager), "ClearWorld").Invoke(__instance, null);
				CareerState.main.SetState(new WorldSave.CareerState());
				WorldTime.main.worldTime = ClientManager.world.WorldTime;
				WorldTime.main.SetTimewarpIndex_ForLoad(0);
				WorldView.main.SetViewLocation(Base.planetLoader.spaceCenter.LaunchPadLocation);
				WorldView.main.viewDistance.Value = 32f;
				LocalManager.OnLoadWorld();
				AstronautState.main.state = new WorldSave.Astronauts();
				Map.manager.mapMode.Value = false;
				Map.view.view.target.Value = Base.planetLoader.spaceCenter.Planet.mapPlanet;
				Map.view.view.position.Value = Base.planetLoader.spaceCenter.LaunchPadLocation.position;
				Map.view.view.distance.Value = Base.planetLoader.spaceCenter.LaunchPadLocation.position.y * 0.65;
				Map.navigation.SetTarget(Map.view.view.target.Value);
				PlayerController.main.player.Value = null;
				PlayerController.main.cameraDistance.Value = 32f;
				if (__instance.environment.environments != null)
				{
					SFS.World.Environment[] environments = __instance.environment.environments;
					for (int i = 0; i < environments.Length; i++)
					{
						environments[i].terrain?.LoadFully();
					}
				}
				LogManager.main.ClearBranches();
				if (SavingCache.main.TryLoadBuildPersistent(MsgDrawer.main, out var buildPersistent, eraseCache: false))
				{
					RocketManager.SpawnBlueprint(buildPersistent);
				}
				GameCamerasManager.main.InstantlyRotateCamera();
				return false;
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to load multiplayer world: " + ex);
					return false;
				}
			}
			return true;
		}
	}

}
}
