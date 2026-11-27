using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 一键构建 Android APK。
/// 关键是先把 PlayerSettings 的默认方向从 PortraitUpsideDown(倒竖屏) 改成 Portrait，
/// 这是整个画面 180° 翻转的根源（manifest 里会生成 reversePortrait）。
/// </summary>
public static class BuildAndroidApk
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/PC/Home_PC.unity",
        "Assets/Scenes/PC/Hub_PC.unity",
        "Assets/Scenes/PC/Build_PC.unity",
        "Assets/Scenes/PC/World_PC.unity",
        "Assets/Scenes/PC/Base_PC.unity",
    };

    [MenuItem("SFS/Build Android APK")]
    public static void Build()
    {
        FixOrientation();

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;

        // 优先使用云构建机传入的 -customBuildPath（build/Android/...），回退本地 Builds/
        string outputPath = GetOutputPath("Builds/SpaceflightSimulator.apk");
        string outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir)) Directory.CreateDirectory(outputDir);

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;
        Debug.Log($"[SFS] Build {s.result}  size={s.totalSize / 1024 / 1024}MB  out={s.outputPath}");
        if (s.result != BuildResult.Succeeded)
            throw new System.Exception("[SFS] Build failed: " + s.result);
    }

    [MenuItem("SFS/Fix Orientation Only")]
    public static void FixOrientation()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        Debug.Log("[SFS] defaultInterfaceOrientation = " + PlayerSettings.defaultInterfaceOrientation);
    }

    static string GetOutputPath(string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-customBuildPath") return args[i + 1];
        }
        return fallback;
    }
}
