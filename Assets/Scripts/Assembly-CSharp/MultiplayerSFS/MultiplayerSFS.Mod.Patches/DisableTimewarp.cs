using HarmonyLib;

using SFS.World;

using SFS.World.Maps;



namespace MultiplayerSFS.Mod.Patches {

public class DisableTimewarp

{

// 联机时不再拦截倍速操作，而是让单机倍速逻辑正常执行，

// 执行完后把 WorldTime 的最终状态同步给服务器。

[HarmonyPriority(500)]

[HarmonyPatch(typeof(WorldTime), "AccelerateTime")]

public class WorldTime_AccelerateTime

{

public static void Postfix()

{

if (ClientManager.multiplayerEnabled.Value)

{

TimeWarpVoting.OnLocalTimewarpChanged();

}

}

}



[HarmonyPriority(500)]

[HarmonyPatch(typeof(WorldTime), "DecelerateTime")]

public class WorldTime_DecelerateTime

{

public static void Postfix()

{

if (ClientManager.multiplayerEnabled.Value)

{

TimeWarpVoting.OnLocalTimewarpChanged();

}

}

}



[HarmonyPriority(500)]

[HarmonyPatch(typeof(TimewarpTo), "StartTimewarp")]

public class TimewarpTo_StartTimewarp

{

public static void Postfix()

{

if (ClientManager.multiplayerEnabled.Value)

{

TimeWarpVoting.OnLocalTimewarpChanged();

}

}

}

}

}
