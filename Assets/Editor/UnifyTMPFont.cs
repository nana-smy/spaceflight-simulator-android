using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodexLocalization
{
	[InitializeOnLoad]
	public static class UnifyTMPFont
	{
		private const string CJKFontAssetPath = "Assets/MonoBehaviour/NotoSansCJKsc SDF.asset";

		private const string DoneKey = "CodexLocalization.TMPUnified.v2";

		static UnifyTMPFont()
		{
			EditorApplication.delayCall += TryUnify;
		}

		[MenuItem("Codex/重新统一 TMP 字体为思源黑体")]
		public static void RunManually()
		{
			EditorPrefs.DeleteKey(DoneKey);
			TryUnify();
		}

		private static void TryUnify()
		{
			if (EditorPrefs.GetBool(DoneKey, false))
			{
				return;
			}
			TMP_FontAsset cjkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CJKFontAssetPath);
			if (cjkFont == null)
			{
				// 等生成脚本先创建好字体资产
				EditorApplication.delayCall += TryUnify;
				return;
			}
			try
			{
				int replaced = 0;
				replaced += UnifyScenes(cjkFont);
				replaced += UnifyPrefabs(cjkFont);
				SetDefaultFont(cjkFont);
				AssetDatabase.SaveAssets();
				EditorPrefs.SetBool(DoneKey, true);
				Debug.Log("[CJK] TMP 字体统一完成，共替换 " + replaced + " 个文本对象为思源黑体");
			}
			catch (System.Exception exception)
			{
				Debug.LogError("[CJK] 统一 TMP 字体失败: " + exception);
			}
		}

		private static int UnifyScenes(TMP_FontAsset cjkFont)
		{
			int count = 0;
			string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
			foreach (string guid in sceneGuids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.StartsWith("Assets/Scenes"))
				{
					continue;
				}
				Scene scene = SceneManager.GetSceneByPath(path);
				bool loaded = scene.IsValid() && scene.isLoaded;
				if (!loaded)
				{
					scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
				}
				foreach (GameObject root in scene.GetRootGameObjects())
				{
					count += ReplaceInObject(root, cjkFont);
				}
				if (scene.IsValid() && scene.isLoaded)
				{
					EditorSceneManager.SaveScene(scene);
					if (!loaded)
					{
						EditorSceneManager.CloseScene(scene, removeScene: true);
					}
				}
			}
			return count;
		}

		private static int UnifyPrefabs(TMP_FontAsset cjkFont)
		{
			int count = 0;
			string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
			foreach (string guid in prefabGuids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.StartsWith("Assets"))
				{
					continue;
				}
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				int replaced = ReplaceInObject(root, cjkFont);
				if (replaced > 0)
				{
					PrefabUtility.SaveAsPrefabAsset(root, path);
					count += replaced;
				}
				PrefabUtility.UnloadPrefabContents(root);
			}
			return count;
		}

		private static int ReplaceInObject(GameObject root, TMP_FontAsset cjkFont)
		{
			int count = 0;
			TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
			foreach (TMP_Text text in texts)
			{
				if (text.font != cjkFont)
				{
					text.font = cjkFont;
					count++;
				}
			}
			return count;
		}

		private static void SetDefaultFont(TMP_FontAsset cjkFont)
		{
			if (TMP_Settings.instance == null)
			{
				return;
			}
			EditorUtility.SetDirty(TMP_Settings.instance);
		}
	}
}
