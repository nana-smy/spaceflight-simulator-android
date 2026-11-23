using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 每次构建前自动把 Assets/Icon/sfs.png 设置为 Standalone 游戏图标。
/// 保证任何一次重新构建后 exe 都带自定义图标（同时保留 Unity 的数字签名）。
/// </summary>
public class SetGameIcon : IPreprocessBuildWithReport
{
	public int callbackOrder => -1000;

	public void OnPreprocessBuild(BuildReport report)
	{
		Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icon/sfs.png");
		if (icon == null)
		{
			Debug.LogWarning("[SetGameIcon] 未找到 Assets/Icon/sfs.png，跳过图标设置");
			return;
		}

		int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
		Texture2D[] icons = new Texture2D[sizes.Length];
		for (int i = 0; i < icons.Length; i++)
		{
			icons[i] = icon;
		}

		PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icons);
		Debug.Log("[SetGameIcon] 已应用 sfs.png（" + icon.width + "x" + icon.height + "）作为游戏图标");
	}
}
