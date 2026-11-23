using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildAutoCopyTranslations : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        string src = Path.Combine(Application.dataPath, "Editor", "Translations", "简体中文.txt");

        // Windows 构建时 outputPath 是 exe 的完整路径（含文件名），需取其目录；
        // 其他平台/导出工程时可能是文件夹路径，按存在性判断。
        string outputPath = report.summary.outputPath;
        if (!string.IsNullOrEmpty(outputPath) && File.Exists(outputPath))
        {
            outputPath = Path.GetDirectoryName(outputPath);
        }

        string destDir = Path.Combine(outputPath, "Spaceflight Simulator_Data", "Custom Translations");
        if (File.Exists(src))
        {
            Directory.CreateDirectory(destDir);
            File.Copy(src, Path.Combine(destDir, "简体中文.txt"), true);
            Debug.Log("已自动复制简体中文翻译到构建输出：" + destDir);
        }
        else
        {
            Debug.LogWarning("未找到简体中文翻译文件：" + src);
        }
    }
}
