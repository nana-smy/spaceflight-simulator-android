using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MultiplayerSFS.Common;
using SFS.World;
using UnityEngine;

namespace MultiplayerSFS.Mod.Patches
{
	public class DivertRocketSwitching
	{
		[HarmonyPriority(500)]
		[HarmonyPatch(typeof(PlayerController), "SmoothChangePlayer")]
		public static class PlayerController_SmoothChangePlayer
		{
			public static bool Prefix(Player newPlayer)
			{
				if (TrySwitchPlayer(newPlayer))
				{
					if (PlayerController.main.player.Value == null)
					{
						PlayerController.main.player.Value = newPlayer;
					}
					return true;
				}
				return false;
			}
		}

		public static bool CheckSwitch(List<Rocket> rockets)
		{
			if (rockets[0] != null)
			{
				return TrySwitchPlayer(rockets[0]);
			}
			return false;
		}

		public static bool TrySwitchPlayer(Player player)
		{
			if ((bool)ClientManager.multiplayerEnabled)
			{
				if (player is Rocket rocket)
				{
					int id = LocalManager.GetSyncedRocketID(rocket);
					if (id >= 0)
					{
						if (LocalManager.players.Any((KeyValuePair<int, LocalPlayer> kvp) => kvp.Key != ClientManager.playerId && (int)kvp.Value.controlledRocket == id))
						{
							return false;
						}
						if (ClientManager.world.rockets.TryGetValue(id, out RocketState state) && state.ownerId != -1 && state.ownerId != ClientManager.playerId)
						{
							return false;
						}
						if (LocalManager.Player == null)
						{
							Debug.LogError("`TrySwitchPlayer`: LocalManager.Player is null!");
							return false;
						}
						LocalManager.Player.controlledRocket.Value = id;
						LocalManager.updateAuthority.Add(id);
						LocalManager.loadedRockets.Add(id);
						ClientManager.SendPacket(new Packet_UpdatePlayerControl
						{
							PlayerId = ClientManager.playerId,
							RocketId = id
						});
						Debug.Log("[Multiplayer] TrySwitchPlayer: switched to rocket " + id + ", hasControl=" + rocket.hasControl.Value + ", physicsMode=" + rocket.physics.PhysicsMode);
						return true;
					}
					id = LocalManager.GetUnsyncedRocketID(rocket);
					if (id >= 0)
					{
						LocalManager.unsyncedToControl = id;
						return false;
					}
					Debug.LogError("`TrySwitchPlayer`: `player` isn't registered!");
					return false;
				}
				if ((object)player == null)
				{
					Debug.LogError("`TrySwitchPlayer`: `player` is null!");
					return false;
				}
				Debug.LogError("`TrySwitchPlayer`: `player` is not a rocket!");
				return false;
			}
			return true;
		}
	}
}
