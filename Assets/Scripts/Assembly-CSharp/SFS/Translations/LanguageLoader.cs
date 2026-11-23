using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using SFS.Core;
using SFS.IO;
using SFS.UI;
using UnityEngine;

namespace SFS.Translations
{
	public class LanguageLoader : MonoBehaviour
	{
		[Serializable]
		public class LanguageReference
		{
			public string name;

			public bool custom;

			[NonSerialized]
			public string displayName;

			public LanguageReference()
			{
			}

			public LanguageReference(string name, string displayName, bool custom)
			{
				this.name = name;
				this.custom = custom;
				this.displayName = displayName;
			}
		}

		private class CustomWebClient : WebClient
		{
			public int timeoutMs;

			protected override WebRequest GetWebRequest(Uri address)
			{
				WebRequest webRequest = base.GetWebRequest(address);
				webRequest.Timeout = timeoutMs;
				return webRequest;
			}
		}

		public List<Font> fonts = new List<Font>();

		private const string LinksFileUrl = "https://raw.githubusercontent.com/Stef-Moroyna/Spaceflight-Simulator-Translations/master/1.5.3/links.txt";

		private int onlineTranslationVersion = -1;

		private Dictionary<string, (string link, string localizedName)> translationLinks = new Dictionary<string, (string, string)>();

		private Action onInitialized;

		public bool IsInitialized { get; private set; }

		private void Awake()
		{
			string version = Application.version;
			FilePath examplePath = FileLocations.PublicTranslationFolder.ExtendToFile("Example.txt");
			FilePath exampleVersionPath = FileLocations.GetNotificationsPath("Translation_Example_Version");
			new Thread((ParameterizedThreadStart)delegate
			{
				if (!exampleVersionPath.FileExists() || !(exampleVersionPath.ReadText() == version) || !examplePath.FileExists())
				{
					examplePath.WriteText(TranslationSerialization.Serialize(TranslationSerialization.CreateTranslation<SFS_Translation>()));
					exampleVersionPath.WriteText(version);
				}
			}).Start();
			Loc.OnChange += new Action(Canvas.ForceUpdateCanvases);
			StartCoroutine(WaitForInitialization());
			DownloadLinks();
		}

		private void DownloadLinks()
		{
			// 本构建只使用 English 和本地自定义翻译，不再联网拉取语言列表，
			// 避免启动时 Menu.loading 卡在 GitHub 请求上导致“Loading Languages”进不去。
			IsInitialized = true;
		}

		private void LoadLinks(string linksFileContent)
		{
			string[] array = linksFileContent.Replace("\r\n", "\n").Split('\n');
			foreach (string text in array)
			{
				if (text.StartsWith("ver:"))
				{
					onlineTranslationVersion = int.Parse(text.Substring(4));
					continue;
				}
				string text2 = text.Split(';')[1];
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
				string item = text.Split(';')[0];
				translationLinks[fileNameWithoutExtension] = (text2, item);
			}
		}

		public void RunAfterInitialization(Action a)
		{
			if (IsInitialized)
			{
				a();
			}
			else
			{
				onInitialized = (Action)Delegate.Combine(onInitialized, a);
			}
		}

		public string GetDisplayName(LanguageReference a)
		{
			if (a.custom)
			{
				return a.displayName;
			}
			foreach (LanguageReference item in from language in GetAvailableLanguages()
				where language != null
				select language)
			{
				if (a.name == item.name)
				{
					return item.displayName;
				}
			}
			if (a.name == null)
			{
				return "English";
			}
			return "-";
		}

		public LanguageReference[] GetAvailableLanguages()
		{
			List<LanguageReference> list = new List<LanguageReference>();
			list.Add(new LanguageReference("English", "English", custom: false));
			// 只保留 English 和本地自定义翻译（官方在线语言列表不再显示）
			FilePath[] array = FileLocations.PublicTranslationFolder.GetFilesInFolder(recursively: false).ToArray();
			if (array.Length != 0)
			{
				list.Add(null);
			}
			FilePath[] array2 = array;
			foreach (FilePath filePath in array2)
			{
				if (filePath.Extension == "txt" && filePath.FileName != "Example.txt" && filePath.FileName != "READ_ME.txt")
				{
					list.Add(new LanguageReference(filePath.CleanFileName, filePath.CleanFileName, custom: true));
				}
			}
			return list.ToArray();
		}

		public void LoadLanguage(LanguageReference language, I_MsgLogger logger, Action<bool> callback)
		{
			try
			{
				if (language.name == null || language.name == "English")
				{
					LoadDefaultEnglish();
					return;
				}
				if (!language.custom)
				{
					// 在线语言列表已移除，旧设置里如果存有在线语言，直接回退 English，避免联网下载卡住
					LoadDefaultEnglish();
					return;
				}
				if (language.custom)
				{
					FilePath filePath = FileLocations.PublicTranslationFolder.ExtendToFile(language.name + ".txt");
					if (filePath.FileExists())
					{
						Loc.SetLanguage("Non-Default", TranslationSerialization.Deserialize(filePath.ReadText(), out var unused, out var missing, out var _));
						ShowDeserializationReport(missing, unused, delegate
						{
							callback(obj: true);
						});
					}
					else
					{
						LoadDefaultEnglish(success: false);
					}
					return;
				}
				DownloadLanguage(language.name, delegate(bool success, string content)
				{
					if (!success)
					{
						LoadDefaultEnglish();
						Menu.read.Open(() => "Loading language failed: No internet", delegate
						{
							callback(obj: false);
						});
					}
					else if (content == null)
					{
						LoadDefaultEnglish(success: false);
					}
					else
					{
						Loc.SetLanguage("Non-Default", TranslationSerialization.Deserialize(content, out var unused2, out var missing2, out var _));
						if (Application.isEditor)
						{
							ShowDeserializationReport(missing2, unused2, delegate
							{
								callback(obj: true);
							});
						}
						else
						{
							callback(obj: true);
						}
					}
				});
			}
			catch (Exception exception)
			{
				logger.Log(Loc.main.Could_Not_Load_Language);
				Debug.LogException(exception);
				LoadDefaultEnglish();
			}
			void LoadDefaultEnglish(bool success = true)
			{
				Loc.SetLanguage("Default", TranslationSerialization.CreateTranslation<SFS_Translation>());
				callback(success);
			}
		}

		private void DownloadLanguage(string name, Action<bool, string> onLoaded)
		{
			FilePath cacheFile = FileLocations.TranslationCacheFolder.ExtendToFile(name + ".txt");
			if (cacheFile.FileExists())
			{
				string[] array = cacheFile.ReadText().Replace("\r\n", "\n").Split('\n');
				int num = int.Parse(array[0]);
				if (onlineTranslationVersion != -1 && onlineTranslationVersion != num)
				{
					UpdateCache();
					return;
				}
				string[] array2 = new string[array.Length - 1];
				for (int i = 1; i < array.Length; i++)
				{
					array2[i - 1] = array[i];
				}
				string arg = string.Join("\n", array2);
				onLoaded(arg1: true, arg);
			}
			else
			{
				UpdateCache();
			}
			void UpdateCache()
			{
				if (Application.internetReachability == NetworkReachability.NotReachable)
				{
					onLoaded(arg1: false, null);
				}
				else if (!translationLinks.ContainsKey(name))
				{
					onLoaded(arg1: true, null);
				}
				else
				{
					CustomWebClient customWebClient = new CustomWebClient();
					customWebClient.timeoutMs = 5000;
					customWebClient.DownloadStringAsync(new Uri(translationLinks[name].link));
					customWebClient.DownloadStringCompleted += delegate(object _, DownloadStringCompletedEventArgs eventData)
					{
						if (eventData.Error != null)
						{
							onLoaded(arg1: false, null);
						}
						else
						{
							cacheFile.WriteText(onlineTranslationVersion + "\n" + eventData.Result);
							onLoaded(arg1: true, eventData.Result);
						}
					};
				}
			}
		}

		private static void ShowDeserializationReport(List<FieldReference> missing, List<FieldReference> unused, Action callback)
		{
			if (unused.Count + missing.Count == 0)
			{
				callback();
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(Loc.main.Translation_Out_Of_Date);
			stringBuilder.AppendLine(Loc.main.Translation_English_For_Missing);
			stringBuilder.AppendLine("");
			if (missing.Count > 0)
			{
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine(Loc.main.Translation_Missing);
				foreach (FieldReference item in missing)
				{
					stringBuilder.AppendLine(item.group + " / " + item.name);
				}
			}
			if (unused.Count > 0)
			{
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine(Loc.main.Translation_Unnecessary);
				foreach (FieldReference item2 in unused)
				{
					stringBuilder.AppendLine(item2.group + " / " + item2.name);
				}
			}
			Menu.read.ShowReport(stringBuilder, callback);
		}

		private IEnumerator WaitForInitialization()
		{
			while (!IsInitialized)
			{
				yield return new WaitForEndOfFrame();
			}
			onInitialized?.Invoke();
		}
	}
}
