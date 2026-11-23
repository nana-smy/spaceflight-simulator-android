using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Input;
using SFS.Translations;
using TMPro;
using UnityEngine;

namespace SFS.UI
{
	public class HomeManager : BasicMenu
	{
		public static HomeManager main;

		[Space]
		[Space]
		public MusicPlaylistPlayer menuMusic;

		public TextAdapter version;

		public Material starsMaterial;

		public DevelopmentMenu developmentMenu;

		[Space]
		public GameObject steamBanner;

		public Button settingsButton;

		public GameObject fullVersionButton;

		public BasicMenu fullVersionSalePage;

		private void Awake()
		{
			main = this;
		}

		private void Start()
		{
			fullVersionButton.SetActive(DevSettings.ShowFullVersionButton);
			RemoteSettings.ForceUpdate();
			menuMusic.StartPlaying(5f);
			starsMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
			string versionText = GetVersion();
			version.Text = "v" + versionText;
			LegacyUITheme.ApplyAll();
			Loc.OnChange += new Action(LocalizeModsTexts);
			LocalizeModsTexts();
			if (!string.IsNullOrEmpty(Base.sceneLoader.sceneSettings.openShop))
			{
				if (Base.sceneLoader.sceneSettings.openShop == "Full Version")
				{
					fullVersionSalePage.Open();
				}
				return;
			}
			LanguageSettings.main.Initialize(delegate
			{
				ShowUpgradeVersionMenu(versionText, delegate
				{
					ShowIsNewPlayerMenu(delegate
					{
					});
				});
			});
		}

		private void OnDestroy()
		{
			Loc.OnChange -= new Action(LocalizeModsTexts);
		}

		// 首页模组入口是场景静态英文，按原文映射到本地化字段
		private void LocalizeModsTexts()
		{
			Transform root = (transform.root != null) ? transform.root : transform;
			TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
			for (int i = 0; i < tmpTexts.Length; i++)
			{
				string current = tmpTexts[i].text?.Trim();
				if (current == "modding")
				{
					tmpTexts[i].text = Loc.main.Modding_Button;
				}
				else if (current == "Mod Loader")
				{
					tmpTexts[i].text = Loc.main.Mod_Loader_Title;
				}
				else if (current == "Open Mods Folder")
				{
					tmpTexts[i].text = Loc.main.Open_Mods_Folder;
				}
				else if (current == "Mod loader:")
				{
					tmpTexts[i].text = Loc.main.Mod_Loader_Label;
				}
				else if (current == "Download Mods")
				{
					tmpTexts[i].text = Loc.main.Download_Mods;
				}
				else if (current == "Assets Pack")
				{
					tmpTexts[i].text = Loc.main.AssetPack_Name;
				}
			}
			UnityEngine.UI.Text[] legacyTexts = root.GetComponentsInChildren<UnityEngine.UI.Text>(includeInactive: true);
			for (int j = 0; j < legacyTexts.Length; j++)
			{
				string current2 = legacyTexts[j].text?.Trim();
				if (current2 == "modding")
				{
					legacyTexts[j].text = Loc.main.Modding_Button;
				}
				else if (current2 == "Mod Loader")
				{
					legacyTexts[j].text = Loc.main.Mod_Loader_Title;
				}
				else if (current2 == "Open Mods Folder")
				{
					legacyTexts[j].text = Loc.main.Open_Mods_Folder;
				}
				else if (current2 == "Mod loader:")
				{
					legacyTexts[j].text = Loc.main.Mod_Loader_Label;
				}
				else if (current2 == "Download Mods")
				{
					legacyTexts[j].text = Loc.main.Download_Mods;
				}
				else if (current2 == "Assets Pack")
				{
					legacyTexts[j].text = Loc.main.AssetPack_Name;
				}
			}
		}

		// TODO: intentionally disabled? Re-enable the original new-player/upgrade menus if needed.
		private static void ShowIsNewPlayerMenu(Action callback)
		{
		}

		private static void ShowUpgradeVersionMenu(string versionText, Action callback)
		{
			callback();
		}

		private static string GetVersion()
		{
			string text = Application.version.Replace("1.59.11", "1.5.10.1");
			string text2 = ((text[3] != '.') ? text.Insert(3, ".") : text);
			int num = 313;
			string text3 = ((Application.platform == RuntimePlatform.Android) ? "android" : ((Application.platform == RuntimePlatform.IPhonePlayer) ? "ios" : "other"));
			string[] array = (from a in RemoteSettings.GetString("Version_Display_Data", "").Replace(" ", "").Split(';')
				where a != ""
				select a).ToArray();
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				string[] array2 = array[num2].Split(',');
				string text4 = array2[0];
				int num3 = int.Parse(array2[1]);
				string text5 = array2[2];
				string text6 = array2[3];
				if (text4 == text2 && num3 == num && (text5 == "any" || text5 == text3))
				{
					return text6.Replace("_", " ");
				}
			}
			return text2;
		}

		public void OpenFullVersion()
		{
			fullVersionSalePage.Open();
		}

		public void OpenCommunity()
		{
			SizeSyncerBuilder.Carrier carrier;
			List<MenuElement> list = new List<MenuElement>
			{
				new SizeSyncerBuilder(out carrier).HorizontalMode(SizeMode.MaxChildSize),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Youtube, delegate
				{
					Application.OpenURL("https://www.youtube.com/channel/UCOpgvpnGyZw4IRT_kuebiWA");
				}, CloseMode.Current).MinSize(300f, 60f),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Discord, delegate
				{
					Application.OpenURL("https://discordapp.com/invite/hwfWm2d");
				}, CloseMode.Current).MinSize(300f, 60f),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Reddit, delegate
				{
					Application.OpenURL("https://www.reddit.com/r/SpaceflightSimulator/");
				}, CloseMode.Current).MinSize(300f, 60f),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Forums, delegate
				{
					Application.OpenURL("https://jmnet.one/sfs/forum/index.php");
				}, CloseMode.Current).MinSize(300f, 60f)
			};
			if (LanguageSettings.main.settings.name == "Russian")
			{
				list.Add(ButtonBuilder.CreateButton(carrier, () => "ВКонтакте", delegate
				{
					Application.OpenURL("https://vk.com/public194508161");
				}, CloseMode.Current).MinSize(300f, 60f));
			}
			ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.Close, CloseMode.Current, delegate
			{
			}, delegate
			{
			}, list.ToArray()));
		}

		public void OpenSettings()
		{
			Menu.settings.Open();
		}

		public void OpenCredits()
		{
			Menu.read.Open(() => Loc.main.Credits_Text, CloseMode.Current, background: false);
		}

		public void OpenPC()
		{
			Application.OpenURL("https://spaceflight-simulator-steam.azurewebsites.net/");
		}

		public static void OpenTutorials_Static()
		{
			string link_Orbit = RemoteSettings.GetString("Video_Tutorial_Orbit");
			string link_Moon = RemoteSettings.GetString("Video_Tutorial_Moon");
			string link_Dock = RemoteSettings.GetString("Video_Tutorial_Dock");
			SizeSyncerBuilder.Carrier carrier;
			List<MenuElement> list = new List<MenuElement>
			{
				new SizeSyncerBuilder(out carrier).HorizontalMode(SizeMode.MaxChildSize),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Video_Orbit, delegate
				{
					Application.OpenURL(link_Orbit);
				}, CloseMode.Current).MinSize(300f, 60f),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Video_Moon, delegate
				{
					Application.OpenURL(link_Moon);
				}, CloseMode.Current).MinSize(300f, 60f),
				ButtonBuilder.CreateButton(carrier, () => Loc.main.Video_Dock, delegate
				{
					Application.OpenURL(link_Dock);
				}, CloseMode.Current).MinSize(300f, 60f)
			};
			ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.Close, CloseMode.Current, delegate
			{
			}, delegate
			{
			}, list.ToArray()));
		}

		public override void Close()
		{
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Close_Game, () => Loc.main.Close, Application.Quit);
		}
	}
}
