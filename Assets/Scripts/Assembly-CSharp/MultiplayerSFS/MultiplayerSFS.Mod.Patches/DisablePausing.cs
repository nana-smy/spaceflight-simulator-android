using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using SFS.Input;
using SFS.World;
using UnityEngine;

namespace MultiplayerSFS.Mod.Patches {
public class DisablePausing
{
	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(ScreenManager), "Awake")]
	public class ScreenManager_Awake
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = instructions.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(AccessTools.PropertySetter(typeof(Time), "timeScale")))
				{
					if (i > 0)
					{
						list[i - 1].opcode = OpCodes.Nop;
					}
					list.RemoveAt(i);
					break;
				}
			}
			return list;
		}

		public static void Postfix(ScreenManager __instance)
		{
			if (!ClientManager.multiplayerEnabled.Value && !__instance.selfInitialize)
			{
				Time.timeScale = 0f;
			}
		}
	}

	[HarmonyPriority(500)]
	[HarmonyPatch(typeof(ScreenManager), "OpenScreen")]
	public class ScreenManager_OpenScreen
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = instructions.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Calls(AccessTools.PropertySetter(typeof(Time), "timeScale")))
				{
					if (i - 14 >= 0 && i - 14 + 15 <= list.Count)
					{
						list.RemoveRange(i - 14, 15);
					}
					else
					{
						// 越界时保守处理：只移除当前指令，避免破坏 IL。
						list[i].opcode = OpCodes.Nop;
					}
					break;
				}
			}
			return list;
		}

		public static void Postfix(ScreenManager __instance)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				Time.timeScale = (__instance.CurrentScreen.PauseWhileOpen ? 0f : ((WorldTime.main != null) ? WorldTime.main.TimeScale : 1f));
			}
		}
	}
}
}
