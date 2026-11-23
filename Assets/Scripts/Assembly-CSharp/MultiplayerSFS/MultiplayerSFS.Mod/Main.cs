using System;
using System.Collections.Generic;
using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using MultiplayerSFS.Common;
using SFS.Audio;
using SFS.IO;
using SFS.Translations;
using SFS.UI;
using UnityEngine;

namespace MultiplayerSFS.Mod {
public class Main : ModLoader.Mod
{
	public static Main main;

	private static bool multiplayerButtonLanguageSubscribed;

	private static TextAdapter multiplayerButtonText;

	public static FolderPath buildPersistentFolder;

	public override string ModNameID => "multiplayersfs";

	public override string DisplayName => "SFS Multiplayer";

	public override string Author => "Astro The Rabbit, VerdiX,SFSGamer";

	public override string MinimumGameVersionNecessary => "1.5.10.2";

	public override string ModVersion => "0.3";

	public override string Description => "Adds multiplayer to SFS!";

	public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.5" } };

	public override void Early_Load()
	{
		// IL2CPP 模式：不再使用 Harmony 运行时补丁（AOT 下不可用）。
		// 补丁逻辑已改写到原版源码中的显式调用点（见 MultiplayerSFS.Mod.Patches 的 Dispatcher）。
		MultiplayerSFS.Mod.Patches.Dispatcher.Initialize();
		main = this;
	}

	public override void Load()
	{
		SceneHelper.OnWorldSceneLoaded += (Action)delegate
		{
			if ((bool)ClientManager.multiplayerEnabled)
			{
				ChatWindow.CreateUI("world");
			}
		};
		SceneHelper.OnWorldSceneUnloaded += (Action)delegate
		{
			if ((bool)ClientManager.multiplayerEnabled)
			{
				if (LocalManager.Player != null)
				{
					LocalManager.Player.controlledRocket.Value = -1;
				}
				ClientManager.SendPacket(new Packet_UpdatePlayerControl
				{
					PlayerId = ClientManager.playerId,
					RocketId = -1
				});
				ChatWindow.DestroyUI();
			}
		};
		SceneHelper.OnBuildSceneLoaded += (Action)delegate
		{
			ChatWindow.CreateUI("build");
		};
		SceneHelper.OnBuildSceneUnloaded += (Action)delegate
		{
			ChatWindow.DestroyUI();
		};
		SceneHelper.OnHubSceneLoaded += (Action)delegate
		{
			ChatWindow.CreateUI("hub");
		};
		SceneHelper.OnHubSceneUnloaded += (Action)delegate
		{
			ChatWindow.DestroyUI();
		};
		SceneHelper.OnHomeSceneLoaded += new Action(AddMultiplayerButton);
		buildPersistentFolder = new FolderPath(base.ModFolder).Extend(".BlueprintPersistent");
		Application.quitting += delegate
		{
			ClientManager.client?.Shutdown("Application quitting");
		};
		ClientManager.multiplayerEnabled.OnChange += (Action<bool>)delegate(bool value)
		{
			Application.runInBackground = value;
			if (!value)
			{
				ChatWindow.DestroyCooldownTimer();
			}
		};

		EnsurePumpCreated();
	}

	private static void EnsurePumpCreated()
	{
		if (UnityEngine.Object.FindObjectOfType<ClientManagerPump>() != null)
		{
			return;
		}

		GameObject pumpObject = new GameObject("MultiplayerSFS - ClientManager Pump");
		UnityEngine.Object.DontDestroyOnLoad(pumpObject);
		pumpObject.AddComponent<ClientManagerPump>();
	}

	public static void AddMultiplayerButton()
	{
		if (GameObject.Find("Multiplayer SFS - Button") != null)
		{
			return;
		}
		GameObject buttons = GameObject.Find("Buttons");
		GameObject playButton = GameObject.Find("Play Button");
		if (buttons == null || playButton == null)
		{
			return;
		}
		ClientManager.multiplayerEnabled.Value = false;
		GameObject gameObject2 = UnityEngine.Object.Instantiate(playButton, buttons.transform, worldPositionStays: true);
		gameObject2.GetComponent<RectTransform>().SetSiblingIndex(playButton.GetComponent<RectTransform>().GetSiblingIndex() + 1);
		TextAdapter componentInChildren = gameObject2.GetComponentInChildren<TextAdapter>();
		UnityEngine.Object.Destroy(gameObject2.GetComponent<TranslationSelector>());
		gameObject2.name = "Multiplayer SFS - Button";
		componentInChildren.Text = MultiplayerLoc.L("联机", "Multiplayer");
		multiplayerButtonText = componentInChildren;
		if (!multiplayerButtonLanguageSubscribed)
		{
			Loc.OnChange += UpdateMultiplayerButtonLanguage;
			multiplayerButtonLanguageSubscribed = true;
		}
		ButtonPC component = gameObject2.GetComponent<ButtonPC>();
		component.holdEvent = new HoldUnityEvent();
		component.clickEvent = new ClickUnityEvent();
		component.clickEvent.AddListener(delegate
		{
			SoundPlayer.main.clickSound.Play();
			MultiplayerMenu.OpenMenu();
		});
	}

	private static void UpdateMultiplayerButtonLanguage()
	{
		if (multiplayerButtonText != null)
		{
			multiplayerButtonText.Text = MultiplayerLoc.L("联机", "Multiplayer");
		}
		else
		{
			GameObject button = GameObject.Find("Multiplayer SFS - Button");
			if (button != null)
			{
				TextAdapter text = button.GetComponentInChildren<TextAdapter>();
				if (text != null)
				{
					text.Text = MultiplayerLoc.L("联机", "Multiplayer");
				}
			}
		}
	}
}
}
