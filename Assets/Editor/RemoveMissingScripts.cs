using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键清理 AssetRipper 反编译遗留的 missing script（空 MonoBehaviour 占位组件）。
/// 用法：Unity 编辑器菜单 → Tools → Remove Missing Scripts In PC Scenes。
/// </summary>
public static class RemoveMissingScripts
{
	[MenuItem("Tools/Remove Missing Scripts In PC Scenes")]
	public static void RemoveInAllPCScenes()
	{
		string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/Scenes/PC" });
		int totalRemoved = 0;
		foreach (string guid in sceneGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
			int removed = 0;
			GameObject[] roots = scene.GetRootGameObjects();
			foreach (GameObject root in roots)
			{
				removed += RemoveMissingRecursive(root.transform);
			}
			if (removed > 0)
			{
				EditorSceneManager.SaveScene(scene);
			}
			totalRemoved += removed;
			Debug.Log("Scene " + scene.name + ": removed " + removed + " missing scripts");
		}
		Debug.Log("Total removed " + totalRemoved + " missing scripts across all PC scenes");
	}

	private static int RemoveMissingRecursive(Transform t)
	{
		int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
		for (int i = 0; i < t.childCount; i++)
		{
			removed += RemoveMissingRecursive(t.GetChild(i));
		}
		return removed;
	}

	/// <summary>
	/// 清理所有 Prefab 上的 missing script（零件预制体是缺失脚本警告的主要来源）。
	/// 用法：Unity 编辑器菜单 → Tools → Remove Missing Scripts In All Prefabs。
	/// </summary>
	[MenuItem("Tools/Remove Missing Scripts In All Prefabs")]
	public static void RemoveInAllPrefabs()
	{
		string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets" });
		int totalRemoved = 0;
		int touchedPrefabs = 0;
		foreach (string guid in prefabGuids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			GameObject root = PrefabUtility.LoadPrefabContents(path);
			int removed = 0;
			foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
			{
				removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
			}
			if (removed > 0)
			{
				PrefabUtility.SaveAsPrefabAsset(root, path);
				touchedPrefabs++;
			}
			PrefabUtility.UnloadPrefabContents(root);
			totalRemoved += removed;
		}
		Debug.Log("Removed " + totalRemoved + " missing scripts from " + touchedPrefabs + " prefabs");
	}
}
