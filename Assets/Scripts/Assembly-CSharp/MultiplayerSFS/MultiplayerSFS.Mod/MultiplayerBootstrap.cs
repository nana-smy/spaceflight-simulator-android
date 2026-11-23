using UnityEngine;

namespace MultiplayerSFS.Mod
{
	// 内置启动器：游戏启动后自动初始化联机功能，不依赖外部 Mods 文件夹。
	public static class MultiplayerBootstrap
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			if (Main.main != null)
			{
				return;
			}
			Main.main = new Main();
			Main.main.ModFolder = Application.persistentDataPath;
			Main.main.Early_Load();
			Main.main.Load();
			GameObject pumpObject = new GameObject("MultiplayerSFS - ClientManager Pump");
			Object.DontDestroyOnLoad(pumpObject);
			pumpObject.AddComponent<ClientManagerPump>();
		}
	}
}
