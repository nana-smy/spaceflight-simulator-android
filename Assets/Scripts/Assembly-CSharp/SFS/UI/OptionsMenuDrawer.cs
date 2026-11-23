using System;
using System.Collections.Generic;
using SFS.Input;
using SFS.Translations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI
{
	public class OptionsMenuDrawer : Screen_Menu
	{
		public RectTransform holder;

		public Button closeButton;

		public TextAdapter closeButtonText;

		public RectTransform holderPrefab;

		public RectTransform contentHolder;

		private RectTransform currentHolder;

		private Action onOpen;

		private Action onClose;

		private CloseMode onEscape;

		// 设置窗口内场景静态文本 → 本地化字段映射（场景里的英文写死文本没有绑定 Loc）
		private static readonly Dictionary<string, Func<string>> settingsTextToLoc = new Dictionary<string, Func<string>>
		{
			{ "General Settings", () => Loc.main.General_Title },
			{ "General settings", () => Loc.main.General_Title },
			{ "Video Settings", () => Loc.main.Video_Title },
			{ "Video settings", () => Loc.main.Video_Title },
			{ "Audio Settings", () => Loc.main.Audio_Title },
			{ "Audio settings", () => Loc.main.Audio_Title },
			{ "Keybind", () => Loc.main.Keybindings_Title },
			{ "Keybindings", () => Loc.main.Keybindings_Title },
			{ "Resolution", () => Loc.main.Video_Resolution_Name },
			{ "Window mode", () => Loc.main.Video_WindowMode_Name },
			{ "Window Mode", () => Loc.main.Video_WindowMode_Name },
			{ "Vertical Sync", () => Loc.main.Video_VerticalSync_Name },
			{ "Vertical sync", () => Loc.main.Video_VerticalSync_Name },
			{ "Fullscreen", () => Loc.main.Fullscreen_Exclusive },
			{ "Anti-Aliasing", () => Loc.main.Anti_Aliasing },
			{ "Anti Aliasing", () => Loc.main.Anti_Aliasing },
			{ "Camera Shake", () => Loc.main.Shakes_Name },
			{ "Orbit Line Count", () => Loc.main.Orbit_Line_Count },
			{ "Conic Sections", () => Loc.main.Orbit_Line_Count },
			{ "Reset To Default", () => Loc.main.Reset_To_Default },
			{ "Mass", () => Loc.main.Mass_Title },
			{ "Thrust", () => Loc.main.Thrust_Title },
			{ "Height", () => Loc.main.Height_Title },
			{ "Thrust/Weight", () => Loc.main.Thrust_To_Weight_Ratio_Title },
			{ "Thrust / Weight", () => Loc.main.Thrust_To_Weight_Ratio_Title },
			{ "Parts", () => Loc.main.Part_Count_Title },
			{ "Part Count", () => Loc.main.Part_Count_Title },
			{ "Music", () => Loc.main.Music_Name },
			{ "Sound", () => Loc.main.Sound_Name },
			{ "FPS", () => Loc.main.FPS_Name },
			{ "Fps", () => Loc.main.FPS_Name },
			{ "Language", () => Loc.main.Language_Name },
			{ "Scale UI", () => Loc.main.Menu_Scale },
			{ "Interface Scale", () => Loc.main.Menu_Scale },
			{ "Opacity UI", () => Loc.main.Menu_Opacity },
			{ "Interface Opacity", () => Loc.main.Menu_Opacity },
			{ "Close", () => Loc.main.Close },
			{ "Cancel", () => Loc.main.Cancel },
			{ "Cheats", () => Loc.main.Open_Cheats_Button },
			{ "Mod Loader", () => Loc.main.Mod_Loader_Title },
			{ "Mod loader:", () => Loc.main.Mod_Loader_Label },
			{ "Open Mods Folder", () => Loc.main.Open_Mods_Folder },
			{ "Download Mods", () => Loc.main.Download_Mods },
			{ "modding", () => Loc.main.Modding_Button },
			{ "Assets Pack", () => Loc.main.AssetPack_Name },
		};

		private static readonly Dictionary<TMP_Text, Func<string>> localizedTMPTexts = new Dictionary<TMP_Text, Func<string>>();

		private static readonly Dictionary<Text, Func<string>> localizedLegacyTexts = new Dictionary<Text, Func<string>>();

		private static bool languageRefreshSubscribed;

		protected override CloseMode OnEscape => onEscape;

		public override void OnOpen()
		{
			holder.gameObject.SetActive(value: true);
			LocalizeSettingsTexts(holder);
			onOpen?.Invoke();
		}

		public override void OnClose()
		{
			if (currentHolder != null)
			{
				UnityEngine.Object.Destroy(currentHolder.gameObject);
			}
			if (holder != null)
			{
				holder.gameObject.SetActive(value: false);
			}
			onClose?.Invoke();
		}

		public Func<Screen_Base> CreateDelegate(CloseMode onEscape, CancelButton cancelButton, Action onOpen, Action onClose, params MenuElement[] elements)
		{
			return delegate
			{
				currentHolder = UnityEngine.Object.Instantiate(holderPrefab, contentHolder);
				currentHolder.gameObject.SetActive(value: true);
				MenuElement[] array = elements;
				for (int i = 0; i < array.Length; i++)
				{
					array[i]?.Invoke(currentHolder.gameObject);
				}
				LocalizeSettingsTexts(currentHolder);
				this.onOpen = onOpen;
				this.onClose = onClose;
				this.onEscape = onEscape;
				string text = ((cancelButton == CancelButton.Cancel) ? ((string)Loc.main.Cancel) : ((cancelButton == CancelButton.Close) ? ((string)Loc.main.Close) : ""));
				if (closeButtonText != null)
				{
					closeButtonText.Text = text;
				}
				return this;
			};
		}

		public static void LocalizeSettingsTexts(Transform root)
		{
			if (root == null)
			{
				return;
			}
			if (!languageRefreshSubscribed)
			{
				languageRefreshSubscribed = true;
				Loc.OnChange += new Action(RefreshLocalizedTexts);
			}
			TMP_Text[] tmpTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
			for (int i = 0; i < tmpTexts.Length; i++)
			{
				string current = tmpTexts[i].text?.Trim();
				if (current != null && current.Length > 0 && settingsTextToLoc.TryGetValue(current, out Func<string> field))
				{
					if (!localizedTMPTexts.ContainsKey(tmpTexts[i]))
					{
						localizedTMPTexts[tmpTexts[i]] = field;
					}
					tmpTexts[i].text = field();
				}
			}
			Text[] legacyTexts = root.GetComponentsInChildren<Text>(includeInactive: true);
			for (int j = 0; j < legacyTexts.Length; j++)
			{
				string current2 = legacyTexts[j].text?.Trim();
				if (current2 != null && current2.Length > 0 && settingsTextToLoc.TryGetValue(current2, out Func<string> field2))
				{
					if (!localizedLegacyTexts.ContainsKey(legacyTexts[j]))
					{
						localizedLegacyTexts[legacyTexts[j]] = field2;
					}
					legacyTexts[j].text = field2();
				}
			}
		}

		private static void RefreshLocalizedTexts()
		{
			List<TMP_Text> deadTMP = null;
			foreach (KeyValuePair<TMP_Text, Func<string>> pair in localizedTMPTexts)
			{
				if (pair.Key == null)
				{
					(deadTMP ?? (deadTMP = new List<TMP_Text>())).Add(pair.Key);
				}
				else
				{
					pair.Key.text = pair.Value();
				}
			}
			if (deadTMP != null)
			{
				foreach (TMP_Text dead in deadTMP)
				{
					localizedTMPTexts.Remove(dead);
				}
			}
			List<Text> deadLegacy = null;
			foreach (KeyValuePair<Text, Func<string>> pair2 in localizedLegacyTexts)
			{
				if (pair2.Key == null)
				{
					(deadLegacy ?? (deadLegacy = new List<Text>())).Add(pair2.Key);
				}
				else
				{
					pair2.Key.text = pair2.Value();
				}
			}
			if (deadLegacy != null)
			{
				foreach (Text dead in deadLegacy)
				{
					localizedLegacyTexts.Remove(dead);
				}
			}
		}
	}
}
