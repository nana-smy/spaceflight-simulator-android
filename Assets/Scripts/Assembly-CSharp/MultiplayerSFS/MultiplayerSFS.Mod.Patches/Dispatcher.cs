using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerSFS.Common;
using MultiplayerSFS.Mod;
using SFS;
using SFS.Builds;
using SFS.Career;
using SFS.Input;
using SFS.IO;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.Analytics;
using SFS.Translations;
using SFS.Stats;
using SFS.Cameras;

namespace MultiplayerSFS.Mod.Patches
{
	/// <summary>
	/// IL2CPP 兼容层：原 Harmony 运行时补丁的替代。
	/// 原版源码的关键方法中显式调用这里的方法，实现同样的拦截/同步逻辑。
	/// </summary>
	public static class Dispatcher
	{
		private static readonly HashSet<EngineModule> subscribedEngines = new HashSet<EngineModule>();
		private static readonly HashSet<ParachuteModule> subscribedParachutes = new HashSet<ParachuteModule>();

		public static void Initialize()
		{
			// 预留：需要在场景加载时挂接的事件可在这里注册
		}

		// ===== 屏幕暂停控制（原 DisablePausing）=====

		public static bool AllowScreenPause() => !ClientManager.multiplayerEnabled.Value;
		// ===== 存档控制（原 DisableWorldSaving / DisableReverting）=====

		public static bool AllowWorldPersistentSave() => !ClientManager.multiplayerEnabled.Value;

		public static bool AllowGameManagerUpdate() => !ClientManager.multiplayerEnabled.Value;

		public static bool UpdateWorldPlaytime(WorldBaseManager instance)
		{
			if (ClientManager.multiplayerEnabled.Value)
			{
				if (instance.settings == null)
				{
					return false;
				}
				instance.settings.playtime.lastPlayedTime_Ticks = DateTime.Now.Ticks;
				instance.settings.playtime.totalPlayTime_Seconds += 10.0;
				return false;
			}
			return true;
		}

		public static bool AllowRevert() => !ClientManager.multiplayerEnabled.Value;

		public static bool AllowSaveRevertToLaunch() => !ClientManager.multiplayerEnabled.Value;

		public static bool AllowGameMenusUpdateTopCenter() => !ClientManager.multiplayerEnabled.Value;

		// ===== 统计禁用（原 DisableStats）=====

		public static bool AllowStatsRecording() => !ClientManager.multiplayerEnabled.Value;

		// ===== 存档 UI 禁用（原 DisableWorldSavingUI）=====

		public static bool AllowOpenSaveMenu()
		{
			if (ClientManager.multiplayerEnabled.Value && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "World_PC")
			{
				MsgDrawer.main.Log(MultiplayerLoc.L("联机模式下禁用保存", "Saving is disabled in multiplayer"));
				return false;
			}
			return true;
		}

		// ===== 联机世界加载（原 DivertLoading）=====

		/// <summary>
		/// 联机模式下返回"空存档"替代原版世界读取。非联机时不做任何事（由原方法继续执行）。
		/// 由 SavingCache.Preload_WorldPersistent 在开头调用。
		/// </summary>
		public static void PreloadWorldPersistent(SavingCache instance, bool needsRocketsAndBranches)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return; // 非联机：不拦截，原方法继续
			}
			ref SavingCache.Data<WorldSave> reference = ref instance.worldPersistent;
			if (reference == null || reference.result.data == null || (needsRocketsAndBranches && (reference.result.data.rockets == null || reference.result.data.branches == null)))
			{
				reference = new SavingCache.Data<WorldSave>();
				reference.result = (success: true, data: WorldSave.CreateEmptyQuicksave(Application.version), log: null);
			}
		}

		// ===== 联机世界启动（原 DivertLoading.GameManager_LoadPersistentAndLaunch）=====

		public static bool LoadPersistentAndLaunch(GameManager instance)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return true;
			}
			try
			{
				SavingCache.main.Preload_WorldPersistent(needsRocketsAndBranches: true);
				SavingCache.main.worldPersistent = null;
				instance.ClearWorld();
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
				if (instance.environment.environments != null)
				{
					SFS.World.Environment[] environments = instance.environment.environments;
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

		// ===== 建造存档转发（原 DivertBuildSavingAndLoading）=====

		public static bool SaveBuildPersistent(SavingCache instance, Blueprint new_BuildPersistent, bool cache)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return true;
			}
			instance.buildPersistent = SavingCache.Data<Blueprint>.Cache(new_BuildPersistent, cache);
			string version = Application.version;
			SavingCache.SaveAsync(delegate
			{
				Blueprint.Save(Main.buildPersistentFolder, new_BuildPersistent, version);
			});
			return false;
		}

		public static bool PreloadBlueprintPersistent(SavingCache instance)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return true;
			}
			ref SavingCache.Data<Blueprint> reference = ref instance.buildPersistent;
			if (reference == null)
			{
				FolderPath path = Main.buildPersistentFolder;
				MsgCollector logger = new MsgCollector();
				reference = new SavingCache.Data<Blueprint>
				{
					thread = new System.Threading.Thread((System.Threading.ThreadStart)delegate
					{
						Blueprint loaded = null;
						string logMsg = null;
						if (path.FolderExists() && Blueprint.TryLoad(path, logger, out loaded))
						{
							logMsg = (logger.msg.Length > 0) ? logger.msg.ToString() : null;
							instance.buildPersistent.result = (success: true, data: loaded, log: logMsg);
						}
						else
						{
							instance.buildPersistent.result = (success: false, data: null, log: null);
						}
					})
				};
				reference.thread.Start();
			}
			return false;
		}

		// ===== 火箭切换控制（原 DivertRocketSwitching）=====

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

		public static bool ShouldSmoothChangePlayer(Player newPlayer)
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

		public static bool CheckSwitch(List<Rocket> rockets)
		{
			if (rockets[0] != null)
			{
				return TrySwitchPlayer(rockets[0]);
			}
			return false;
		}

		// ===== 时间扭曲同步（原 DisableTimewarp）=====

		public static void OnLocalTimewarpChanged()
		{
			if (ClientManager.multiplayerEnabled.Value)
			{
				TimeWarpVoting.OnLocalTimewarpChanged();
			}
		}

		// ===== 输入控制（原 EditUI.Screen_Game_ProcessInput）=====

		public static bool AllowProcessInput() => !ChatWindow.InputSelected;

		// ===== 零件状态同步（原 PartUpdateSyncing）=====

		public static void OnDockingPortDock(DockingPortModule port, DockingPortModule otherPort)
		{
			if ((bool)otherPort.Rocket.isPlayer)
			{
				// 原补丁返回 false 阻止对接，这里直接处理
				return;
			}
			// 逻辑保留：阻止被他人控制的火箭对接
		}

		public static bool AllowDock(DockingPortModule __instance, DockingPortModule otherPort)
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

		public static void OnMergeRockets(Rocket rocket_A, Rocket rocket_B)
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
		public static void OnWheelToggle(WheelModule instance)
		{
			if (!(GameManager.main != null) || !ClientManager.multiplayerEnabled)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(instance.GetComponentInParentTree<Rocket>());
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
					Part componentInParent = instance.GetComponentInParent<Part>();
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
						WheelOn = instance.on.Value
					});
				}
			}
		}

		public static void OnMoveToggle(MoveModule instance)
		{
			if (!(GameManager.main != null) || !ClientManager.multiplayerEnabled)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(instance.GetComponentInParentTree<Rocket>());
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
					Part componentInParent = instance.GetComponentInParent<Part>();
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
						Time = instance.time.Value,
						TargetTime = instance.targetTime.Value
					});
				}
			}
		}

		public static void OnBoosterUpdate(BoosterModule instance)
		{
			if (!ClientManager.multiplayerEnabled || !(GameManager.main != null))
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(instance.GetComponentInParentTree<Rocket>());
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
					Part componentInParent = instance.GetComponentInParent<Part>();
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
						Primed = instance.boosterPrimed.Value,
						Throttle = instance.throttle_Out.Value,
						FuelPercent = instance.fuelPercent.Value
					});
				}
			}
		}
		public static void OnRocketsRecreated(Rocket rocket, List<Rocket> childRockets)
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
		public static void SyncStaging(StagingDrawer drawer)
		{
			if (!ClientManager.multiplayerEnabled)
			{
				return;
			}
			Staging_Local value = drawer.GetStaging();
			if (value.Value == null)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(value.Value.rocket);
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
				List<StageState> list = new List<StageState>(value.Value.stages.Count);
				foreach (Stage stage in value.Value.stages)
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
		public static void SubscribeEngine(EngineModule instance)
		{
			if (GameManager.main != null && (bool)ClientManager.multiplayerEnabled && subscribedEngines.Add(instance))
			{
				instance.engineOn.OnChange += new Action<bool, bool>(delegate (bool engineOn_old, bool engineOn_new)
				{
					if (engineOn_old != engineOn_new)
					{
						SendEngineUpdate(instance, engineOn_new);
					}
				});
			}
		}

		private static void SendEngineUpdate(EngineModule instance, bool engineOn_new)
		{
			int syncedRocketID = LocalManager.GetSyncedRocketID(instance.GetComponentInParentTree<Rocket>());
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
					Part componentInParent = instance.GetComponentInParent<Part>();
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

		public static void SubscribeParachute(ParachuteModule instance)
		{
			if (GameManager.main != null && (bool)ClientManager.multiplayerEnabled && subscribedParachutes.Add(instance))
			{
				instance.targetState.OnChange += new Action(delegate
				{
					SendParachuteUpdate(instance);
				});
			}
		}

		private static void SendParachuteUpdate(ParachuteModule instance)
		{
			int syncedRocketID = LocalManager.GetSyncedRocketID(instance.GetComponentInParentTree<Rocket>());
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
					Part componentInParent = instance.GetComponentInParent<Part>();
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
						State = instance.state.Value,
						TargetState = instance.targetState.Value
					});
				}
			}
		}

		// ===== 世界事件同步（原 WorldEventSyncing）=====

		public static void OnDestroyPart(Part instance, bool createExplosion, ref DestructionReason reason)
		{
			if ((bool)ClientManager.multiplayerEnabled && GameManager.main != null)
			{
				if (reason == (DestructionReason)4)
				{
					reason = LocalManager.TrueDestructionReason;
					return;
				}
				int syncedRocketID = LocalManager.GetSyncedRocketID(instance.Rocket);
				if (syncedRocketID == -1)
				{
					return;
				}
				int localPartID = LocalManager.GetLocalPartID(syncedRocketID, instance);
				if (localPartID == -1)
				{
					Debug.LogError("Couldn't find destroyed part's id!");
					return;
				}
				if (!LocalManager.updateAuthority.Contains(syncedRocketID))
				{
					return;
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
			}
		}

		public static bool OnDestroyRocket(Rocket rocket, ref DestructionReason reason)
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

		public static void OnRocketDestroyed(Rocket rocket)
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

		public static bool AllowRocketPhysics(Rocket rocket)
		{
			if (!ClientManager.multiplayerEnabled)
			{
				return true;
			}
			return rocket.rb2d.bodyType == RigidbodyType2D.Dynamic;
		}

		// ===== 地图图标颜色（原 EditUI.MapIcon_UpdateAlpha）=====

		public static void UpdateMapIconAlpha(MapIcon icon)
		{
			if (!ClientManager.multiplayerEnabled)
			{
				return;
			}
			int syncedRocketID = LocalManager.GetSyncedRocketID(icon.GetComponent<Rocket>());
			foreach (LocalPlayer value in LocalManager.players.Values)
			{
				if (value.controlledRocket.Value == syncedRocketID)
				{
					SpriteRenderer componentInChildren = icon.mapIcon.GetComponentInChildren<SpriteRenderer>();
					componentInChildren.color = new Color(componentInChildren.color.r * value.iconColor.r, componentInChildren.color.g * value.iconColor.g, componentInChildren.color.b * value.iconColor.b, componentInChildren.color.a);
					break;
				}
			}
		}

		// ===== 联机自定义菜单（原 EditUI）=====

		public static bool OpenHubMenu(HubManager instance)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return true; // 非联机：走原版菜单
			}
			ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
			List<MenuElement> list = new List<MenuElement>();
			list.Add(new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize));
			if (ClientManager.IsHost)
			{
				list.Add(ButtonBuilder.CreateIconButton(carrier, buttonIcons.cheats, () => Loc.main.Open_Cheats_Button, SandboxSettings.main.Open, CloseMode.None));
				list.Add(ElementGenerator.VerticalSpace(10));
			}
			list.Add(ButtonBuilder.CreateIconButton(carrier, buttonIcons.settings, () => Loc.main.Open_Settings_Button, Menu.settings.Open, CloseMode.None));
			list.Add(ElementGenerator.VerticalSpace(10));
			list.Add(ButtonBuilder.CreateIconButton(carrier, buttonIcons.exit, () => Loc.main.Exit_To_Main_Menu, delegate
			{
				ClientManager.client?.Shutdown("Left world");
				instance.ExitToMainMenu();
			}, CloseMode.None));
			MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Current, list.ToArray());
			return false;
		}

		public static bool OpenBuildMenu(BuildManager instance)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return true;
			}
			ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
			List<MenuElement> list = new List<MenuElement>();
			list.Add(new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize));
			list.Add(new SizeSyncerBuilder(out var carrier2).HorizontalMode(SizeMode.MaxChildSize));
			list.Add(ElementGenerator.VerticalSpace(10));
			list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.save, () => Loc.main.Save_Blueprint, instance.OpenSaveMenu, CloseMode.None), ButtonBuilder.CreateIconButton(carrier, buttonIcons.load, () => Loc.main.Load_Blueprint, instance.OpenLoadMenu, CloseMode.None)));
			list.Add(ElementGenerator.VerticalSpace(10));
			list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.moveRocket, () => Loc.main.Move_Rocket_Button, delegate
			{
				instance.selector.Select(instance.buildGrid.activeGrid.partsHolder.GetArray());
			}, CloseMode.Current), ButtonBuilder.CreateIconButton(carrier, buttonIcons.clear, () => Loc.main.Clear_Confirm, instance.AskClear, CloseMode.Current)));
			List<MenuElement> list2 = list;
			if (RemoteSettings.GetBool("Example_Rockets", defaultValue: true) || RemoteSettings.GetBool("Video_Tutorials", defaultValue: true))
			{
				list2.Add(ElementGenerator.VerticalSpace(25));
			}
			if (RemoteSettings.GetBool("Example_Rockets", defaultValue: true))
			{
				list2.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exampleRockets, () => Loc.main.Example_Rockets_OpenMenu, delegate
				{
					instance.OpenExampleRocketsMenu();
				}, CloseMode.None));
			}
			if (RemoteSettings.GetBool("Video_Tutorials", defaultValue: true))
			{
				list2.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.videoTutorials, () => Loc.main.Video_Tutorials_OpenButton, HomeManager.OpenTutorials_Static, CloseMode.None));
			}
			list2.Add(ElementGenerator.VerticalSpace(10));
			list2.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.shareRocket, () => Loc.main.Share_Button, instance.UploadPC, CloseMode.Current));
			list2.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.cheats, () => Loc.main.Open_Cheats_Button, SandboxSettings.main.Open, CloseMode.None));
			list2.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.settings, () => Loc.main.Open_Settings_Button, Menu.settings.Open, CloseMode.None));
			list2.Add(ElementGenerator.VerticalSpace(10));
			list2.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exit, () => Loc.main.Exit_To_Space_Center, delegate
			{
				Base.sceneLoader.LoadHubScene();
			}, CloseMode.None));
			MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Current, list2.ToArray());
			return false;
		}

		public static bool OpenGameMenu(GameManager instance)
		{
			if (!ClientManager.multiplayerEnabled.Value)
			{
				return true;
			}
			ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
			SizeSyncerBuilder.Carrier carrier;
			SizeSyncerBuilder.Carrier carrier2;
			List<MenuElement> list = new List<MenuElement>();
			list.Add(new SizeSyncerBuilder(out carrier).HorizontalMode(SizeMode.MaxChildSize));
			list.Add(new SizeSyncerBuilder(out carrier2).HorizontalMode(SizeMode.MaxChildSize));
			list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.newRocket, () => Loc.main.Build_New_Rocket, instance.ExitToBuild, CloseMode.None));
			list.Add(ElementGenerator.VerticalSpace(10));
			if (ClientManager.IsHost)
			{
				list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.cheats, () => Loc.main.Open_Cheats_Button, SandboxSettings.main.Open, CloseMode.None));
				list.Add(ElementGenerator.VerticalSpace(10));
			}
			list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.settings, () => Loc.main.Open_Settings_Button, Menu.settings.Open, CloseMode.None));
			list.Add(ElementGenerator.VerticalSpace(10));
			list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exit, () => Loc.main.Exit_To_Space_Center, instance.ExitToHub, CloseMode.None));
			MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Current, list.ToArray());
			return false;
		}
	}
}

// verify-050153

// verify2-050731

// verify3-050953
