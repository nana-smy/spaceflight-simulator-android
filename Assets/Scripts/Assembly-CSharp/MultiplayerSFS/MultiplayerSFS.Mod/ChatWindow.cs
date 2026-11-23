using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using HarmonyLib;
using MultiplayerSFS.Common;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using SFS.UI.ModGUI;
using TMPro;
using UnityEngine;

namespace MultiplayerSFS.Mod {
public static class ChatWindow
{
	public static readonly int windowID = Builder.GetRandomID();

	public static readonly int maxMessagesCount = 100;

	public static Color defaultInputColor;

	internal const int WindowWidth = 500;

	internal const int WindowHeight = 700;

	internal const int InnerWidth = 480;

	internal const int MessageWidth = 470;

	public static GameObject holder_window;

	public static Window window;

	private static readonly (string zh, string en, Color color)[] PresetColors = new (string, string, Color)[]
	{
		("红", "Red", new Color(1f, 0.25f, 0.25f, 1f)),
		("橙", "Orange", new Color(1f, 0.6f, 0.1f, 1f)),
		("黄", "Yellow", new Color(1f, 0.95f, 0.25f, 1f)),
		("绿", "Green", new Color(0.3f, 0.9f, 0.3f, 1f)),
		("青", "Cyan", new Color(0.25f, 0.9f, 0.9f, 1f)),
		("蓝", "Blue", new Color(0.4f, 0.65f, 1f, 1f)),
		("紫", "Purple", new Color(0.8f, 0.4f, 1f, 1f)),
		("粉", "Pink", new Color(1f, 0.5f, 0.8f, 1f)),
	};

	public static Window window_messages;

	private static Label label_nameColor;

	private static readonly List<SFS.UI.ModGUI.Button> colorButtons = new List<SFS.UI.ModGUI.Button>();

	private static TextMeshProUGUI inputPlaceholder;

	public static readonly List<ChatMessage> messages = new List<ChatMessage>();

	public static Timer cooldownTimer;

	public static bool canSendMessage = true;

	public static TextInput input_sendMessage;

	private static Queue mainThreadActions = new Queue();

	private static object mainThreadActionsLock = new object();

	private static double cooldownSeconds = 0.0;

	public static bool InputSelected { get; private set; }

	public static int LastSenderId
	{
		get
		{
			int result = int.MinValue;
			foreach (ChatMessage message in messages)
			{
				if (message.label_message != null)
				{
					result = message.senderId;
				}
			}
			return result;
		}
	}

	private static void ApplyLanguage()
	{
		if (window != null)
		{
			window.Title = MultiplayerLoc.L("聊天", "Chat");
		}
		if (label_nameColor != null)
		{
			label_nameColor.Text = MultiplayerLoc.L("名字颜色", "Name Color");
		}
		for (int i = 0; i < colorButtons.Count && i < PresetColors.Length; i++)
		{
			if (colorButtons[i] != null)
			{
				colorButtons[i].Text = MultiplayerLoc.L(PresetColors[i].zh, PresetColors[i].en);
			}
		}
		if (inputPlaceholder != null)
		{
			inputPlaceholder.text = MultiplayerLoc.L("输入聊天消息…", "Type a chat message…");
		}
	}

	private static void SetButtonPriority(SFS.UI.ModGUI.Button button, int priority)
	{
		ButtonPC buttonPC = button.gameObject.GetComponent<ButtonPC>();
		if (buttonPC != null)
		{
			buttonPC.layoutPriority = priority;
			TouchElements.RemoveElement(buttonPC);
			TouchElements.AddElement(buttonPC);
		}
	}

	public static async void CreateUI(string sceneName)
	{
		if (holder_window != null || !ClientManager.multiplayerEnabled)
		{
			return;
		}
		try
		{
		while (LocalManager.Player == null)
		{
			if (!(bool)ClientManager.multiplayerEnabled)
			{
				return;
			}
			await Task.Yield();
		}
		holder_window = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Multiplayer SFS - Chat Window Holder");
		if (holder_window.GetComponent<ChatWindowPump>() == null)
		{
			holder_window.AddComponent<ChatWindowPump>();
		}
		window = Builder.CreateWindow(holder_window.transform, windowID, 500, 700, -380, 250, draggable: true, savePosition: true, 1f, MultiplayerLoc.L("聊天", "Chat"));
		RectTransform chatMask = window.ChildrenHolder.parent as RectTransform;
		if (chatMask != null)
		{
			chatMask.sizeDelta = new Vector2(-10f, -60f);
		}
		ButtonPC windowDragButton = window.gameObject.GetComponent<ButtonPC>();
		if (windowDragButton != null)
		{
			windowDragButton.layoutPriority = 1;
			TouchElements.RemoveElement(windowDragButton);
			TouchElements.AddElement(windowDragButton);
		}
		window.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical);
		int num = 478;
		TMP_FontAsset chineseFont = ChatMessage.GetChineseFont();
		label_nameColor = Builder.CreateLabel(window, 480, 30, 0, 0, MultiplayerLoc.L("名字颜色", "Name Color"));
		Container colorRow = Builder.CreateContainer(window);
		colorRow.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleCenter, 4f);
		UnityEngine.UI.ContentSizeFitter fitter = colorRow.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
		if (fitter != null)
		{
			fitter.enabled = false;
		}
		colorRow.Size = new Vector2(480f, 42f);
		colorButtons.Clear();
		for (int i = 0; i < PresetColors.Length; i++)
		{
			Color preset = PresetColors[i].color;
			string presetName = MultiplayerLoc.L(PresetColors[i].zh, PresetColors[i].en);
			var presetButton = Builder.CreateButton(colorRow, 54, 40, 0, 0, delegate
			{
				SetPlayerColor(preset);
			}, presetName);
			colorButtons.Add(presetButton);
			ApplyButtonColor(presetButton, preset);
			SetButtonPriority(presetButton, 2);
		}
		Container bottomRow = Builder.CreateContainer(window);
		bottomRow.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.MiddleCenter, 0f);
		UnityEngine.UI.ContentSizeFitter bottomFitter = bottomRow.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
		if (bottomFitter != null)
		{
			bottomFitter.enabled = false;
		}
		bottomRow.Size = new Vector2(480f, num + 50f);

		window_messages = Builder.CreateWindow(bottomRow, Builder.GetRandomID(), 480, num, 0, 0, draggable: false, savePosition: false);
		RectTransform msgMask = window_messages.ChildrenHolder.parent as RectTransform;
		if (msgMask != null)
		{
			msgMask.sizeDelta = new Vector2(-10f, -60f);
		}
		ButtonPC msgDragButton = window_messages.gameObject.GetComponent<ButtonPC>();
		if (msgDragButton != null)
		{
			msgDragButton.enabled = false;
		}
		window_messages.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.LowerLeft, 5f, new RectOffset(5, 5, 5, 5));
		window_messages.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
		foreach (ChatMessage message in messages)
		{
			message.CreateUI();
		}
		input_sendMessage = Builder.CreateTextInput(bottomRow, 480, 50);
		defaultInputColor = input_sendMessage.FieldColor;
		input_sendMessage.field.onSubmit.AddListener(OnMessageSubmit);
		input_sendMessage.field.onSelect.AddListener(delegate
		{
			InputSelected = true;
		});
		input_sendMessage.field.onDeselect.AddListener(delegate
		{
			InputSelected = false;
		});
		input_sendMessage.field.textComponent.alignment = TextAlignmentOptions.Left;
		input_sendMessage.field.textComponent.fontSize = 20f;
		input_sendMessage.field.textComponent.font = chineseFont;
		input_sendMessage.field.textComponent.ForceMeshUpdate();
		if (input_sendMessage.field.placeholder != null)
		{
			inputPlaceholder = input_sendMessage.field.placeholder as TextMeshProUGUI;
			if (inputPlaceholder != null)
			{
				inputPlaceholder.font = chineseFont;
				inputPlaceholder.text = MultiplayerLoc.L("输入聊天消息…", "Type a chat message…");
			}
		}
		ChangeCooldownStatus(canSendMessage);
		Loc.OnChange += ApplyLanguage;
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	public static void DestroyUI()
	{
		if (holder_window == null)
		{
			return;
		}
		Loc.OnChange -= ApplyLanguage;
		foreach (ChatMessage message in messages)
		{
			message.DestroyUI();
		}
		UnityEngine.Object.Destroy(holder_window);
		holder_window = null;
		window = null;
		window_messages = null;
		input_sendMessage = null;
		label_nameColor = null;
		colorButtons.Clear();
		inputPlaceholder = null;
	}

	public static void SetPlayerColor(Color color)
	{
		if (LocalManager.Player == null)
		{
			return;
		}
		LocalManager.Player.iconColor = color;
		ClientManager.SendPacket(new Packet_UpdatePlayerColor
		{
			PlayerId = ClientManager.playerId,
			Color = color
		});
		OnPlayerColorChange(ClientManager.playerId, color);
	}

	private static void ApplyButtonColor(SFS.UI.ModGUI.Button button, Color color)
	{
		button.TextColor = color;
		ButtonPC buttonPC = button.gameObject.GetComponent<ButtonPC>();
		if (buttonPC != null)
		{
			buttonPC.Color = color;
			buttonPC.OverColor = color;
		}
	}

	public static void OnMessageSubmit(string message)
	{
		if (!string.IsNullOrEmpty(message) && canSendMessage)
		{
			AddMessage(new ChatMessage(message, ClientManager.playerId));
			if (cooldownTimer != null)
			{
				ChangeCooldownStatus(canSend: false);
				cooldownTimer.Stop();
				cooldownTimer.Start();
			}
			ClientManager.SendPacket(new Packet_SendChatMessage
			{
				SenderId = ClientManager.playerId,
				Message = message,
				Color = (LocalManager.Player != null) ? LocalManager.Player.iconColor : Color.white
			});
			input_sendMessage.Text = "";
			input_sendMessage.field.ActivateInputField();
		}
	}

	public static void CreateCooldownTimer(double cooldown)
	{
		if (cooldownTimer != null || !(cooldown > 0.0))
		{
			return;
		}
		cooldownSeconds = cooldown;
		cooldownTimer = new Timer
		{
			Interval = 1000.0 * cooldown,
			AutoReset = false
		};
		cooldownTimer.Elapsed += delegate
		{
			lock (mainThreadActionsLock)
			{
				mainThreadActions.Enqueue((Action)delegate
				{
					ChangeCooldownStatus(canSend: true);
				});
			}
		};
	}

	public static void Update()
	{
		while (true)
		{
			Action action = null;
			lock (mainThreadActionsLock)
			{
				if (mainThreadActions.Count <= 0)
				{
					break;
				}
				action = (Action)mainThreadActions.Dequeue();
			}
			action?.Invoke();
		}
	}

	public static void DestroyCooldownTimer()
	{
		if (cooldownTimer != null)
		{
			cooldownTimer.Dispose();
			cooldownTimer = null;
		}
	}

	public static void ChangeCooldownStatus(bool canSend)
	{
		canSendMessage = canSend;
		if (input_sendMessage != null)
		{
			input_sendMessage.FieldColor = (canSend ? defaultInputColor : Color.red);
		}
	}

	public static void AddMessage(ChatMessage message)
	{
		messages.Add(message);
		if (window_messages != null)
		{
			message.CreateUI();
			ScrollToBottom();
		}
		while (messages.Count > maxMessagesCount)
		{
			messages[0].DestroyUI();
			messages.RemoveAt(0);
		}
	}

	public static void ScrollToBottom()
	{
		if (window_messages == null || window_messages.gameObject == null)
		{
			return;
		}
		ScrollElement component = window_messages.ChildrenHolder.GetComponent<ScrollElement>();
		if (component != null)
		{
			component.PercentPosition = new Vector2(0.5f, 1f);
		}
	}

	public static void OnPlayerColorChange(int id, Color color)
	{
		foreach (ChatMessage message in messages)
		{
			if (message.senderId == id && message.label_playerName != null)
			{
				message.label_playerName.Color = color;
			}
		}
		if (LocalManager.players.TryGetValue(id, out var value) && LocalManager.syncedRockets.TryGetValue(value.controlledRocket.Value, out var value2) && value2.rocket != null)
		{
			new Traverse(value2.rocket.mapIcon).Method("UpdateAlpha").GetValue();
		}
	}
}
}
