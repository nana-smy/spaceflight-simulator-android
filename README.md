# Spaceflight Simulator (Unity / Android)

Unity 工程，自 Android 导出包整理而来。

- Unity 版本：`2021.3.25f1c1`
- 产品名：Spaceflight Simulator
- 版本号：1.5.10.2
- 目标平台：Android / IL2CPP / arm64-v8a（minSdk 22）

## 打开工程

1. 用 Unity Hub 安装 `2021.3.25f1c1`
2. 打开本目录（`Assets`、`Packages`、`ProjectSettings` 三件套）
3. Build Settings 选 Android，切换 IL2CPP 并勾选 arm64-v8a

## 云构建 A：Gradle 打包（不需要 Unity 许可证）

`.github/workflows/android-package.yml`，push 到 `main`（改动 `android-export/**` 时）、手动触发、或推 `v*` 标签时运行：

- 环境：JDK 11 + Gradle 6.1.1 + Android SDK 30 / build-tools 30.0.2
- 输入：`android-export/` —— Unity 导出的 Gradle 工程，已带预编译好的 `libil2cpp.so`、`libunity.so`、`data.unity3d` 等产物（115 MB）
- 产物：`gradle assembleRelease` 出来的 APK → Actions Artifacts `SpaceflightSimulator-APK`；推标签时自动建 Release 挂 APK

能改什么：包名、版本号、签名、`AndroidManifest.xml`、launcher/java 代码、mipmap 等资源。

不能改什么：Unity 场景、脚本、贴图、字体等任何 Unity 侧内容 —— 改完必须本地用 Unity 重新导出一次，把新的 Gradle 工程覆盖到 `android-export/`（导出的 IL2CPP 库是编译产物，无法在 CI 里重新生成）。

> tag 自动发 Release 需要把 Settings → Actions → General 的 Workflow permissions 设为 **Read and write**（默认是 read）。

## 云构建 B：Unity 构建（需要许可证）

`.github/workflows/android-build-unity.yml`，手动触发，用 GameCI 跑完整 Unity 构建（含 IL2CPP 重编）。触发时可以在对话框里直接填许可证，不填就读仓库 secret：

- `unity_license_base64`：`.ulf` 的 base64（推荐，`base64 -w0 Unity_v.ulf`）或单行原文
- `unity_email` / `unity_password`：可选
- `unity_version`：默认 `2021.3.25f1`

也可以改成长期用 secret：

| Secret | 说明 |
|---|---|
| `UNITY_LICENSE` | `.ulf` 许可证文件全文（Unity Personal 免费），对话框不填时使用 |
| `UNITY_EMAIL` / `UNITY_PASSWORD` | 可选，Pro/Plus 的 `UNITY_SERIAL` 流程用 |

获取 `.ulf`：

1. 生成 `.alf`：本地装了 Unity 2021.3.25f1c1 就在终端跑
   `Unity -batchmode -nographics -createManualActivationFile -logFile`；
   没装的话用 Docker：
   `docker run --rm -v "$PWD":/project unityci/editor:ubuntu-2021.3.25f1-android-3 unity-editor -batchmode -nographics -createManualActivationFile -logFile -`
2. 把 `.alf` 传到 https://license.unity3d.com/manual ，选 Unity Personal 走完流程，下载 `.ulf`
3. `.ulf` 内容整段贴进 `UNITY_LICENSE`

> 工程由中国版编辑器 `2021.3.25f1c1` 导出，GameCI 没有 c1 镜像，工作流用同版本号的国际版镜像 `ubuntu-2021.3.25f1-android-3`；报版本不匹配就换 `customImage`。

## 说明

`Library/`、`Logs/`、`UserSettings/` 等构建缓存已通过 `.gitignore` 排除；`android-export/` 是刻意入库的导出产物（115 MB），原始导出压缩包（1.22 GB）不包含在本仓库。
