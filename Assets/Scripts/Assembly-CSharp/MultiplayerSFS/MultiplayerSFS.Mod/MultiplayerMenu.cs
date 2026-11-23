using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SFS.IO;
using SFS.Input;
using SFS.UI;
using SFS.UI.ModGUI;
using UnityEngine;

namespace MultiplayerSFS.Mod
{
	public class MultiplayerMenu : BasicMenu
	{
		public static MultiplayerMenu main;

		public static GameObject windowHolder;

		private static readonly int windowID = Builder.GetRandomID();

		private static Window window;

		private static string username;

		private static int difficultyIndex = 0;

		private static int solarSystemIndex = 0;

		private static string[] solarSystems = new string[1] { "" };

		private static string[] worlds = new string[0];

		private static int worldIndex = -1;

		private static readonly string[] DifficultyNames = new string[3] { "简单", "普通", "困难" };

	private static readonly string[] DifficultyNamesEn = new string[3] { "Easy", "Normal", "Hard" };

	private static string GetDifficultyName(int index)
	{
		return MultiplayerLoc.L(DifficultyNames[index], DifficultyNamesEn[index]);
	}

		protected override CloseMode OnEscape => CloseMode.Current;

		public static void OpenMenu()
		{
			if (windowHolder != null)
			{
				UnityEngine.Object.Destroy(windowHolder);
			}
			windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "MultiplayerSFS - Menu Holder");
			main = windowHolder.AddComponent<MultiplayerMenu>();
			main.OnOpen();
		}

		public override void OnOpen()
		{
			if (ScreenManager.main.CurrentScreen != this)
			{
				ScreenManager.main.OpenScreen(() => this);
				windowHolder.SetActive(value: true);
				ClientManager.multiplayerEnabled.Value = true;
				ShowMainMenu();
			}
		}

		public override void Close()
		{
			Debug.Log("[MultiplayerMenu] Close called");
			ClientManager.multiplayerEnabled.Value = false;
			if (ScreenManager.main.CurrentScreen == this)
			{
				ScreenManager.main.CloseCurrent();
			}
			if (windowHolder != null)
			{
				windowHolder.SetActive(value: false);
			}
		}

		private static void CloseWindowForConnect()
		{
			if (ScreenManager.main.CurrentScreen == main)
			{
				ScreenManager.main.CloseCurrent();
			}
			if (windowHolder != null)
			{
				UnityEngine.Object.Destroy(windowHolder);
				windowHolder = null;
			}
		}

		private static void RebuildWindow(string title, int width, int height)
		{
			if (window != null && window.gameObject != null)
			{
				window.gameObject.SetActive(false);
				UnityEngine.Object.Destroy(window.gameObject);
			}
			window = Builder.CreateWindow(windowHolder.transform, windowID, width, height, 0, height / 2, draggable: false, savePosition: false, 1f, title);
			RectTransform mask = window.ChildrenHolder.parent as RectTransform;
			if (mask != null)
			{
				mask.sizeDelta = new Vector2(-10f, -60f);
			}
			window.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.MiddleCenter, 15f, new RectOffset(10, 10, 10, 10));
			ScrollElement scrollElement = window.ChildrenHolder.GetComponent<ScrollElement>();
			if (scrollElement != null)
			{
				scrollElement.enabled = false;
			}
			ButtonPC windowDragButton = window.gameObject.GetComponent<ButtonPC>();
			if (windowDragButton != null)
			{
				windowDragButton.enabled = false;
			}
		}

		private static Container CreateRow(float width, float height, float spacing = 10f)
		{
			Container row = Builder.CreateContainer(window);
			row.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, TextAnchor.MiddleCenter, spacing);
			UnityEngine.UI.ContentSizeFitter fitter = row.gameObject.GetComponent<UnityEngine.UI.ContentSizeFitter>();
			if (fitter != null)
			{
				fitter.enabled = false;
			}
			row.Size = new Vector2(width, height);
			return row;
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

		private static void ShowMainMenu()
		{
			RebuildWindow(MultiplayerLoc.L("联机", "Multiplayer"), 560, 360);
			if (username == null)
			{
				username = MultiplayerLoc.L("玩家", "Player");
			}
			Container userRow = CreateRow(520f, 50f, 10f);
			Builder.CreateLabel(userRow, 90, 50, 0, 0, MultiplayerLoc.L("用户名", "Username")).TextAlignment = TMPro.TextAlignmentOptions.MidlineLeft;
			Builder.CreateTextInput(userRow, 420, 50, 0, 0, username, delegate(string input)
			{
				username = input.Trim();
			});
			Container buttons = CreateRow(500f, 120f, 20f);
			Builder.CreateButton(buttons, 240, 120, 0, 0, ShowHostMenu, MultiplayerLoc.L("创建房间", "Create Room"));
			Builder.CreateButton(buttons, 240, 120, 0, 0, ShowJoinMenu, MultiplayerLoc.L("加入房间", "Join Room"));
			Builder.CreateButton(window, 240, 60, 0, 0, delegate
			{
				Debug.Log("[MultiplayerMenu] 返回按钮点击");
				main.Close();
			}, MultiplayerLoc.L("返回", "Back"));
			Canvas.ForceUpdateCanvases();
		}

		private static void ShowHostMenu()
		{
			LoadSolarSystems();
			LoadWorlds();
			RebuildWindow(MultiplayerLoc.L("创建房间", "Create Room"), 560, 600);
			Builder.CreateLabel(window, 400, 40, 0, 0, MultiplayerLoc.L("房间密码（留空则无密码）", "Room Password (leave empty for none)"));
			TextInput input_password = Builder.CreateTextInput(window, 420, 50, 0, 0, "", delegate
			{
			});
			Container worldRow = CreateRow(430f, 50f, 10f);
			Builder.CreateLabel(worldRow, 90, 50, 0, 0, MultiplayerLoc.L("世界", "World"));
			Label worldLabel = Builder.CreateLabel(worldRow, 200, 50, 0, 0, GetWorldDisplayName());
			Builder.CreateButton(worldRow, 120, 50, 0, 0, delegate
			{
				ShowWorldSelection(worldLabel);
			}, MultiplayerLoc.L("切换", "Switch"));
			Container diffRow = CreateRow(430f, 50f, 10f);
			Builder.CreateLabel(diffRow, 90, 50, 0, 0, MultiplayerLoc.L("难度", "Difficulty"));
			Label diffLabel = Builder.CreateLabel(diffRow, 200, 50, 0, 0, GetDifficultyName(difficultyIndex));
			Builder.CreateButton(diffRow, 120, 50, 0, 0, delegate
			{
				difficultyIndex = (difficultyIndex + 1) % 3;
				diffLabel.Text = GetDifficultyName(difficultyIndex);
			}, MultiplayerLoc.L("切换", "Switch"));
			Container solRow = CreateRow(430f, 50f, 10f);
			Builder.CreateLabel(solRow, 90, 50, 0, 0, MultiplayerLoc.L("扩展包", "Expansion"));
			Label solLabel = Builder.CreateLabel(solRow, 200, 50, 0, 0, GetSolarSystemDisplayName());
			Builder.CreateButton(solRow, 120, 50, 0, 0, delegate
			{
				solarSystemIndex = (solarSystemIndex + 1) % solarSystems.Length;
				solLabel.Text = GetSolarSystemDisplayName();
			}, MultiplayerLoc.L("切换", "Switch"));
			Container buttons = CreateRow(460f, 70f, 20f);
			Builder.CreateButton(buttons, 220, 70, 0, 0, delegate
			{
				string worldName = (worldIndex >= 0 && worldIndex < worlds.Length) ? worlds[worldIndex] : "";
				HostRoom(difficultyIndex, solarSystems[solarSystemIndex], input_password.Text, worldName);
			}, MultiplayerLoc.L("创建房间", "Create Room"));
			Builder.CreateButton(buttons, 220, 70, 0, 0, delegate
			{
				Debug.Log("[MultiplayerMenu] 创建房间-返回点击");
				ShowMainMenu();
			}, MultiplayerLoc.L("返回", "Back"));
			Canvas.ForceUpdateCanvases();
		}

		private static void ShowJoinMenu()
		{
			RebuildWindow(MultiplayerLoc.L("加入房间", "Join Room"), 560, 420);
			Builder.CreateLabel(window, 400, 40, 0, 0, MultiplayerLoc.L("输入房间地址（如 xxx.natfrp.cloud:9806）", "Room Address (e.g. xxx.natfrp.cloud:9806)"));
			TextInput input_address = Builder.CreateTextInput(window, 420, 50, 0, 0, "", delegate
			{
			});
			Builder.CreateLabel(window, 400, 40, 0, 0, MultiplayerLoc.L("密码（无密码留空）", "Password (leave empty for none)"));
			TextInput input_password = Builder.CreateTextInput(window, 420, 50, 0, 0, "", delegate
			{
			});
			Container buttons = CreateRow(420f, 70f, 20f);
			Builder.CreateButton(buttons, 200, 70, 0, 0, delegate
			{
				JoinRoom(input_address.Text, input_password.Text);
			}, MultiplayerLoc.L("确定", "OK"));
			Builder.CreateButton(buttons, 200, 70, 0, 0, delegate
			{
				Debug.Log("[MultiplayerMenu] 加入房间-取消点击");
				ShowMainMenu();
			}, MultiplayerLoc.L("取消", "Cancel"));
			Canvas.ForceUpdateCanvases();
		}

		private static void LoadSolarSystems()
		{
			List<string> list = new List<string> { "" };
			if (FileLocations.SolarSystemsFolder.FolderExists())
			{
				foreach (FolderPath item in FileLocations.SolarSystemsFolder.GetFoldersInFolder(recursively: false))
				{
					if (item.FolderName != "Example")
					{
						list.Add(item.FolderName);
					}
				}
			}
			solarSystems = list.ToArray();
			if (solarSystemIndex >= solarSystems.Length)
			{
				solarSystemIndex = 0;
			}
		}

		private static string GetSolarSystemDisplayName()
		{
			if (solarSystems == null || solarSystems.Length == 0)
			{
				return MultiplayerLoc.L("默认", "Default");
			}
			string name = solarSystems[solarSystemIndex];
			return (name == "" ? MultiplayerLoc.L("默认", "Default") : name);
		}

		private static void LoadWorlds()
		{
			List<string> list = new List<string>();
			string path = FileLocations.WorldsFolder;
			try
			{
				if (System.IO.Directory.Exists(path))
				{
					foreach (string dir in System.IO.Directory.GetDirectories(path))
					{
						if (System.IO.File.Exists(System.IO.Path.Combine(dir, "WorldSettings.txt")))
						{
							list.Add(System.IO.Path.GetFileName(dir));
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Failed to list worlds: " + ex.Message);
			}
			worlds = list.ToArray();
			if (worldIndex >= worlds.Length)
			{
				worldIndex = -1;
			}
		}

		private static string GetWorldDisplayName()
		{
			if (worldIndex < 0 || worlds == null || worldIndex >= worlds.Length)
			{
				return MultiplayerLoc.L("新建世界", "New World");
			}
			return worlds[worldIndex];
		}

		private static void ShowWorldSelection(Label worldLabel)
		{
			int height = Mathf.Min(80 + (worlds.Length + 2) * 45, 560);
			Window selWindow = Builder.CreateWindow(windowHolder.transform, Builder.GetRandomID(), 340, height, 0, height / 2, draggable: false, savePosition: false, 0.98f, MultiplayerLoc.L("选择世界", "Select World"));
			RectTransform selMask = selWindow.ChildrenHolder.parent as RectTransform;
			if (selMask != null)
			{
				selMask.sizeDelta = new Vector2(-10f, -60f);
			}
			selWindow.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, TextAnchor.UpperLeft, 5f, new RectOffset(10, 10, 10, 10));
			selWindow.EnableScrolling(SFS.UI.ModGUI.Type.Vertical);
			ScrollElement selScroll = selWindow.ChildrenHolder.GetComponent<ScrollElement>();
			if (selScroll != null)
			{
				selScroll.enabled = true;
			}
			ButtonPC selDrag = selWindow.gameObject.GetComponent<ButtonPC>();
			if (selDrag != null)
			{
				selDrag.enabled = true;
				selDrag.layoutPriority = 1;
			}
			SetButtonPriority(Builder.CreateButton(selWindow, 300, 40, 0, 0, delegate
			{
				worldIndex = -1;
				worldLabel.Text = GetWorldDisplayName();
				UnityEngine.Object.Destroy(selWindow.gameObject);
			}, MultiplayerLoc.L("新建世界", "New World")), 2);
			for (int i = 0; i < worlds.Length; i++)
			{
				string worldName = worlds[i];
				int idx = i;
				SetButtonPriority(Builder.CreateButton(selWindow, 300, 40, 0, 0, delegate
				{
					worldIndex = idx;
					worldLabel.Text = GetWorldDisplayName();
					UnityEngine.Object.Destroy(selWindow.gameObject);
				}, worldName), 2);
			}
			SetButtonPriority(Builder.CreateButton(selWindow, 300, 40, 0, 0, delegate
			{
				Debug.Log("[MultiplayerMenu] 选择世界-取消点击");
				UnityEngine.Object.Destroy(selWindow.gameObject);
			}, MultiplayerLoc.L("取消", "Cancel")), 2);
		}

		private static async void HostRoom(int difficulty, string solarSystemName, string password, string worldSaveName)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				MsgDrawer.main.Log(MultiplayerLoc.L("用户名不能为空", "Username cannot be empty"));
				return;
			}
			try
			{
				MultiplayerSFS.Server.ServerSettings settings = new MultiplayerSFS.Server.ServerSettings();
				settings.port = 9806;
				settings.serverPassword = password;
				string worldPath;
				if (string.IsNullOrWhiteSpace(worldSaveName))
				{
					worldPath = System.IO.Path.Combine(FileLocations.WorldsFolder, "MultiplayerWorld");
				}
				else if (System.IO.Path.IsPathRooted(worldSaveName))
				{
					worldPath = worldSaveName.Trim();
				}
				else
				{
					worldPath = System.IO.Path.Combine(FileLocations.WorldsFolder, worldSaveName.Trim());
				}
				settings.worldSavePath = worldPath;
				MultiplayerSFS.Server.Server.Initialize(settings);
				MultiplayerSFS.Server.Server.world.difficulty = difficulty;
				MultiplayerSFS.Server.Server.world.solarSystemName = solarSystemName;
				Thread thread = new Thread(MultiplayerSFS.Server.Server.Run);
				thread.IsBackground = true;
				thread.Start();
			}
			catch (Exception ex)
			{
				MsgDrawer.main.Log(MultiplayerLoc.L("服务器启动失败：", "Server failed to start: ") + ex.Message);
				return;
			}
			int waitCounter = 0;
			while (!MultiplayerSFS.Server.Server.isRunning && waitCounter < 300)
			{
				await Task.Delay(10);
				waitCounter++;
			}
			if (!MultiplayerSFS.Server.Server.isRunning)
			{
				MsgDrawer.main.Log(MultiplayerLoc.L("服务器启动超时", "Server start timed out"));
				return;
			}
			CloseWindowForConnect();
			Connect("127.0.0.1", 9806, password);
		}

		private static async void JoinRoom(string addressText, string password)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				MsgDrawer.main.Log(MultiplayerLoc.L("用户名不能为空", "Username cannot be empty"));
				return;
			}
			string host = addressText.Trim();
			int port = 9806;
			int colonIdx = host.LastIndexOf(':');
			if (colonIdx > 0)
			{
				string hostPart = host.Substring(0, colonIdx);
				string portPart = host.Substring(colonIdx + 1);
				if (int.TryParse(portPart, out int p))
				{
					host = hostPart;
					port = p;
				}
			}
			IPAddress ip = await TryParseAddress(host);
			if (ip == null)
			{
				MsgDrawer.main.Log(MultiplayerLoc.L("地址无效", "Invalid address"));
				return;
			}
			CloseWindowForConnect();
			Connect(ip.ToString(), port, password);
		}

		private static async void Connect(string host, int port, string password)
		{
			MsgDrawer.main.Log(MultiplayerLoc.L("正在连接...", "Connecting..."));
			JoinInfo info = new JoinInfo();
			info.address = IPAddress.Parse(host);
			info.port = port;
			info.username = username;
			info.password = password;
			await ClientManager.TryConnect(info);
		}

		private static async Task<IPAddress> TryParseAddress(string input)
		{
			if (IPAddress.TryParse(input, out IPAddress address))
			{
				return address;
			}
			try
			{
				return (await Dns.GetHostAddressesAsync(input))[0];
			}
			catch
			{
				return null;
			}
		}
	}
}
