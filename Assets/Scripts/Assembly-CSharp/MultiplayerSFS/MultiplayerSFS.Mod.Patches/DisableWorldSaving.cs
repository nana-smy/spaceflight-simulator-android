using System;
using HarmonyLib;
using SFS.World;
using SFS.WorldBase;

namespace MultiplayerSFS.Mod.Patches {
public class DisableWorldSaving
{
	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(SavingCache), "UpdateWorldPersistent")]
	public class SavingCache_UpdateWorldPersistent
	{
		public static bool Prefix()
		{
			// 只阻止原版世界持久化写入，不在保存/更新路径上清空联机火箭状态。
			// 清空同步状态应只发生在明确的断线/退出流程中（见 ClientManager.Disconnect）。
			if (ClientManager.multiplayerEnabled.Value)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(SavingCache), "SaveWorldPersistent")]
	public class SavingCache_SaveWorldPersistent
	{
		public static bool Prefix()
		{
			return !ClientManager.multiplayerEnabled.Value;
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(GameManager), "Update")]
	public class GameManager_Update
	{
		public static bool Prefix()
		{
			return !ClientManager.multiplayerEnabled.Value;
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(WorldBaseManager), "UpdateWorldPlaytime")]
	public class WorldBaseManager_UpdateWorldPlaytime
	{
		public static bool Prefix(WorldBaseManager __instance)
		{
			if (ClientManager.multiplayerEnabled.Value)
			{
				if (__instance.settings == null)
				{
					return false;
				}
				__instance.settings.playtime.lastPlayedTime_Ticks = DateTime.Now.Ticks;
				__instance.settings.playtime.totalPlayTime_Seconds += 10.0;
				return false;
			}
			return true;
		}
	}
}
}
