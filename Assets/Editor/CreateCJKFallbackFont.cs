using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CodexLocalization
{
	[InitializeOnLoad]
	public static class CreateCJKFallbackFont
	{
		private const string CJKFontAssetPath = "Assets/MonoBehaviour/NotoSansCJKsc SDF.asset";

		private const string CJKFallbackFontAssetPath = "Assets/MonoBehaviour/NotoSansCJKsc SDF Fallback.asset";

		private const string SourceFontPath = "Assets/Font/normal.ttf";

		static CreateCJKFallbackFont()
		{
			EditorApplication.delayCall += TryCreate;
		}

		[MenuItem("Codex/重新预写入中文 TMP 字体")]
		public static void RunManually()
		{
			TryCreate();
		}

		private static void TryCreate()
		{
			try
			{
				TMP_FontAsset cjkFont = LoadOrCreateFont(CJKFontAssetPath, 90);
				if (cjkFont == null)
				{
					Debug.LogError("[CJK] 主中文 TMP 字体不可用");
					return;
				}

				AddFallbackToSettings(cjkFont);
				AddFallbackToKnownFonts(cjkFont);

				TMP_FontAsset cjkFallbackFont = LoadOrCreateFallbackFont();
				if (cjkFallbackFont != null)
				{
					AddFallbackToPrimary(cjkFont, cjkFallbackFont);
					AddFallbackToSettings(cjkFallbackFont);
					AddFallbackToKnownFonts(cjkFallbackFont);
				}

				AssetDatabase.SaveAssets();
				Debug.Log("[CJK] 中文 TMP 字体已就绪: " + CJKFontAssetPath + " + " + CJKFallbackFontAssetPath);
			}
			catch (System.Exception exception)
			{
				Debug.LogError("[CJK] 生成中文 TMP 字体失败: " + exception);
			}
		}

		private static TMP_FontAsset LoadOrCreateFont(string path, int samplingPointSize)
		{
			TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
			if (fontAsset != null)
			{
				return fontAsset;
			}

			Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
			if (sourceFont == null)
			{
				Debug.LogError("[CJK] 找不到源字体: " + SourceFontPath);
				return null;
			}

			// 关闭多图集（旧版 TMP 多图集会产生空图集槽导致运行时崩溃），改用大图集
			fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, samplingPointSize, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
			if (fontAsset == null)
			{
				Debug.LogError("[CJK] TMP 字体生成失败");
				return null;
			}

			AssetDatabase.CreateAsset(fontAsset, path);
			if (fontAsset.atlasTexture != null)
			{
				AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
			}
			if (fontAsset.material != null)
			{
				AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
			}
			AssetDatabase.SaveAssets();
			return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
		}

		private static TMP_FontAsset LoadOrCreateFallbackFont()
		{
			TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CJKFallbackFontAssetPath);
			if (fallback == null)
			{
				Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
				if (sourceFont == null)
				{
					Debug.LogError("[CJK] 找不到源字体: " + SourceFontPath);
					return null;
				}

				// 回退字体用更小的采样字号，保证 650+ 个中文字符能全部装进单张 2048 图集。
				fallback = TMP_FontAsset.CreateFontAsset(sourceFont, 36, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
				if (fallback == null)
				{
					Debug.LogError("[CJK] 生成 TMP 中文回退字体失败");
					return null;
				}

				AssetDatabase.CreateAsset(fallback, CJKFallbackFontAssetPath);
				if (fallback.atlasTexture != null)
				{
					AssetDatabase.AddObjectToAsset(fallback.atlasTexture, fallback);
				}
				if (fallback.material != null)
				{
					AssetDatabase.AddObjectToAsset(fallback.material, fallback);
				}
				AssetDatabase.SaveAssets();
				fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CJKFallbackFontAssetPath);
			}

			PrepopulateFallback(fallback);
			return fallback;
		}

		private static void PrepopulateFallback(TMP_FontAsset fallback)
		{
			string characters = CollectNeededCharacters();
			if (string.IsNullOrEmpty(characters))
			{
				return;
			}

			if (fallback.TryAddCharacters(characters, out string missing))
			{
				Debug.Log("[CJK] 已向回退字体预写入 " + characters.Length + " 个字符。");
			}
			else if (!string.IsNullOrEmpty(missing))
			{
				// 图集预写入放不下的字符运行时会由 TMP 动态补充，无需告警刷屏
				Debug.Log("[CJK] 有 " + missing.Length + " 个字符未能预写入回退字体（运行时会按需动态补充）");
			}

			EditorUtility.SetDirty(fallback);
			if (fallback.atlasTexture != null)
			{
				EditorUtility.SetDirty(fallback.atlasTexture);
			}
		}

		private static string CollectNeededCharacters()
		{
			HashSet<uint> set = new HashSet<uint>();

			// 常用 ASCII，保证主字体图集满时英文仍可显示
			for (uint c = 32; c <= 126; c++)
			{
				set.Add(c);
			}

			// 中文翻译文件里出现的所有非 ASCII 字符
			string translationPath = Path.Combine(Application.dataPath, "Editor", "Translations", "简体中文.txt");
			if (File.Exists(translationPath))
			{
				foreach (char c in File.ReadAllText(translationPath, Encoding.UTF8))
				{
					if (c > 127)
					{
						set.Add(c);
					}
				}
			}

			// 源码字符串字面量里的中文、全角标点和 CJK 符号（覆盖硬编码中文）
			string scriptsPath = Path.Combine(Application.dataPath, "Scripts", "Assembly-CSharp");
			if (Directory.Exists(scriptsPath))
			{
				foreach (string file in Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories))
				{
					string content;
					try
					{
						content = File.ReadAllText(file, Encoding.UTF8);
					}
					catch
					{
						continue;
					}
					bool inString = false;
					for (int i = 0; i < content.Length; i++)
					{
						char c = content[i];
						if (c == '"')
						{
							inString = !inString;
							continue;
						}
						if (!inString)
						{
							continue;
						}
						if ((c >= 0x2E80 && c <= 0x9FFF) || (c >= 0x3000 && c <= 0x303F) || (c >= 0xFF00 && c <= 0xFFEF))
						{
							set.Add(c);
						}
					}
				}
			}

			StringBuilder builder = new StringBuilder();
			foreach (uint c in set)
			{
				if (c <= 0xFFFF)
				{
					builder.Append((char)c);
				}
				else
				{
					builder.Append(char.ConvertFromUtf32((int)c));
				}
			}
			return builder.ToString();
		}

		private static void AddFallbackToPrimary(TMP_FontAsset primary, TMP_FontAsset fallback)
		{
			if (primary == null || fallback == null || primary == fallback)
			{
				return;
			}
			if (primary.fallbackFontAssetTable == null)
			{
				primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
			}
			primary.fallbackFontAssetTable.RemoveAll((TMP_FontAsset font) => font == null);
			if (!primary.fallbackFontAssetTable.Contains(fallback))
			{
				primary.fallbackFontAssetTable.Add(fallback);
				EditorUtility.SetDirty(primary);
			}
		}

		private static void AddFallbackToSettings(TMP_FontAsset cjkFont)
		{
			if (TMP_Settings.instance != null && TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(cjkFont))
			{
				TMP_Settings.fallbackFontAssets.RemoveAll((TMP_FontAsset font) => font == null);
				TMP_Settings.fallbackFontAssets.Add(cjkFont);
				EditorUtility.SetDirty(TMP_Settings.instance);
			}
		}

		private static void AddFallbackToKnownFonts(TMP_FontAsset cjkFont)
		{
			string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new string[] { "Assets/MonoBehaviour", "Assets/Resources", "Assets/TextMesh Pro" });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (path == CJKFontAssetPath || path == CJKFallbackFontAssetPath)
				{
					continue;
				}
				TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
				if (fontAsset == null || fontAsset.fallbackFontAssetTable == null || fontAsset.fallbackFontAssetTable.Contains(cjkFont))
				{
					continue;
				}
				fontAsset.fallbackFontAssetTable.RemoveAll((TMP_FontAsset font) => font == null);
				fontAsset.fallbackFontAssetTable.Add(cjkFont);
				EditorUtility.SetDirty(fontAsset);
			}
		}
	}
}
