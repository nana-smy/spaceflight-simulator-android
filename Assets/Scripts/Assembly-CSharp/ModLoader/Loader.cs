using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ModLoader.UI;
using SFS.IO;
using UnityEngine;

namespace ModLoader
{
	public class Loader : MonoBehaviour
	{
		public static Loader main;

		private List<Mod> mods;

		private List<Mod> loadedMods = new List<Mod>();

		private static readonly Regex Version_Regex = new Regex("\\b([0-9]+\\.)+[0-9]+\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public static FolderPath ModsFolder => FileLocations.BaseFolder.Extend("/../Mods");

		public Mod[] GetAllMods()
		{
			return mods.ToArray();
		}

		public Mod[] GetLoadedMods()
		{
			return loadedMods.ToArray();
		}

		public void Initialize_EarlyLoad()
		{
			ModsFolder.CreateFolder();
			MoveIndividualDLLs();
			LoadModList();
			foreach (Mod mod in mods)
			{
				try
				{
					LoadMod(mod);
				}
				catch (Exception exception)
				{
					Debug.Log("Failed to load " + mod.DisplayName);
					Debug.LogException(exception);
				}
			}
		}

		public void Initialize_Load()
		{
			foreach (Mod loadedMod in loadedMods)
			{
				try
				{
					loadedMod.LoadKeybindings?.Invoke();
					loadedMod.Load();
					Debug.Log("Loaded " + loadedMod.DisplayName);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}

		private void MoveIndividualDLLs()
		{
			try
			{
				foreach (FilePath item in ModsFolder.GetFilesInFolder(recursively: false))
				{
					if (item.Extension == "dll")
					{
						item.Move(ModsFolder.Extend(item.CleanFileName).CreateFolder().ExtendToFile(item.FileName));
					}
				}
			}
			catch (Exception exception)
			{
				Debug.Log("Failed to move Individual DLLs due to:");
				Debug.LogException(exception);
			}
		}

		private void LoadModList()
		{
			mods = new List<Mod>();
			// IL2CPP 模式：不支持运行时加载外部 DLL（Assembly.Load 在 AOT 下不可用）。
			// 所有 mod 以源码编译进游戏，通过 BuiltinMods 静态注册。
			foreach (Mod builtinMod in BuiltinMods)
			{
				if (!mods.Any((Mod x) => x.ModNameID == builtinMod.ModNameID))
				{
					builtinMod.ModFolder = ModsFolder.FolderName; // 无独立文件夹，使用 Mods 根目录
					mods.Add(builtinMod);
				}
			}
		}

		/// <summary>
		/// 编译进游戏的 mod 清单（IL2CPP 下替代外部 DLL 加载）。
		/// 新增源码 mod 时在此注册。
		/// </summary>
		private static IEnumerable<Mod> BuiltinMods
		{
			get
			{
				yield return new MultiplayerSFS.Mod.Main();
			}
		}

		private void LoadMod(Mod mod)
		{
			if (loadedMods.Any((Mod x) => x.ModNameID == mod.ModNameID))
			{
				return;
			}
			if (!VerifyVersion(mod.MinimumGameVersionNecessary, Application.version))
			{
				Debug.Log("This game version is too low for " + mod.DisplayName);
			}
			else if (LoadDependencies(mod))
			{
				ModsMenu.AddElement(new ModsListElement.ModData
				{
					author = mod.Author,
					description = mod.Description,
					icon = null,
					name = mod.DisplayName,
					type = ModsListElement.ModType.Mod,
					version = mod.ModVersion,
					saveName = mod.ModNameID,
					loadingTexture = TextureUtility.GetRemoteTexture(mod.IconLink)
				});
				if (ModsSettings.main == null || ModsSettings.main.settings == null || ModsSettings.main.settings.modsActive.GetValueOrDefault(mod.ModNameID, defaultValue: true))
				{
					mod.Early_Load();
					loadedMods.Add(mod);
				}
			}
		}

		private static bool VerifyVersion(string targetVersion, string currentVersion)
		{
			if (!Version_Regex.IsMatch(targetVersion) || !Version_Regex.IsMatch(currentVersion))
			{
				return false;
			}
			int[] array = (from x in targetVersion.Split(".")
				select int.Parse(x)).ToArray();
			int[] array2 = (from x in currentVersion.Split(".")
				select int.Parse(x)).ToArray();
			foreach (int item in Enumerable.Range(0, Math.Min(array.Length, array2.Length)))
			{
				if (array2[item] > array[item])
				{
					return true;
				}
				if (array2[item] < array[item])
				{
					return false;
				}
			}
			return true;
		}

		private bool LoadDependencies(Mod mod)
		{
			if (mod.Dependencies == null)
			{
				return true;
			}
			foreach (KeyValuePair<string, string> item in mod.Dependencies)
			{
				if (mods.Any((Mod x) => x.ModNameID == item.Key))
				{
					Mod mod2 = mods.Find((Mod x) => x.ModNameID == item.Key);
					if (VerifyVersion(item.Value, mod2.ModVersion))
					{
						if (ModsSettings.main == null || ModsSettings.main.settings == null || !ModsSettings.main.settings.modsActive.GetValueOrDefault(mod2.ModNameID, defaultValue: true))
						{
							Debug.Log("Dependency mod of " + mod.DisplayName + " is disabled: " + mod2.DisplayName);
							return false;
						}
						LoadMod(mod2);
						continue;
					}
					Debug.Log(mod2.DisplayName + " version is too low for " + mod.DisplayName);
					return false;
				}
				Debug.Log(mod.DisplayName + " need dependency: " + item.Key);
				return false;
			}
			return true;
		}
	}
}
