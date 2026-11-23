using System;
using SFS.Variables;
using UnityEngine;

namespace MultiplayerSFS.Mod {
public class LocalPlayer
{
	public string username;

	public Int_Local controlledRocket;

	public Color iconColor;

	public LocalPlayer(string username, Color color)
	{
		this.username = username;
		controlledRocket = new Int_Local
		{
			Value = -1
		};
		controlledRocket.OnChange += new Action<int, int>(OnControlledRocketChange);
		iconColor = color;
	}

	public void OnControlledRocketChange(int oldId, int newId)
	{
		// 只有本机玩家的控制权变化才需要更新本地权威/加载状态。
		if (this != LocalManager.Player)
		{
			return;
		}

		if (oldId >= 0)
		{
			LocalManager.updateAuthority.Remove(oldId);
			LocalManager.loadedRockets.Remove(oldId);
		}

		if (newId >= 0)
		{
			LocalManager.updateAuthority.Add(newId);
			LocalManager.loadedRockets.Add(newId);

			if (LocalManager.syncedRockets.TryGetValue(newId, out var localRocket) && localRocket.rocket != null && SFS.World.PlayerController.main != null)
			{
				SFS.World.PlayerController.main.player.Value = localRocket.rocket;
			}
		}
	}
}
}
