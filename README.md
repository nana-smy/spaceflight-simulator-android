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

## 云构建（GitHub Actions）

`Android Build` 工作流在 push 到 `main`、提 PR 或手动触发时，用 GameCI 在 Ubuntu runner 上直接出 APK（IL2CPP、arm64、debug 签名）：

- 产物：Actions 运行页 → Artifacts → `SpaceflightSimulator-Android`
- 推 `v*` 标签会自动创建 Release 并附上 APK（需先把 Settings → Actions → General 的 Workflow permissions 设为 **Read and write**）
- 没配 `UNITY_LICENSE` 时，工作流会在第二步明确报错，不会白跑
- `Library/` 走 Actions 缓存，二次构建明显更快

首次使用需在仓库 Settings → Secrets and variables → Actions 里配置：

| Secret | 说明 |
|---|---|
| `UNITY_EMAIL` | Unity ID 邮箱 |
| `UNITY_PASSWORD` | Unity ID 密码 |
| `UNITY_LICENSE` | `.ulf` 许可证文件全文 |

获取 `.ulf`（Personal 许可证）：

1. 生成 `.alf`。本地装了 Unity 2021.3.25f1c1 就在终端跑
   `Unity -batchmode -nographics -createManualActivationFile -logFile`；
   没装的话用 Docker：
   `docker run --rm -v "$PWD":/project unityci/editor:ubuntu-2021.3.25f1-android-3 unity-editor -batchmode -nographics -createManualActivationFile -logFile -`
2. 把 `.alf` 传到 https://license.unity3d.com/manual ，选 Unity Personal 走完流程，下载 `.ulf`
3. 把 `.ulf` 内容整段贴进 `UNITY_LICENSE` secret（`UNITY_EMAIL` / `UNITY_PASSWORD` 可选，主要给 Pro/Plus 的 `UNITY_SERIAL` 流程用）

> 工程由中国版编辑器 `2021.3.25f1c1` 导出，CI 用的是同版本号国际版镜像 `unityci/editor:ubuntu-2021.3.25f1-android-3`。若构建时报版本或中国区服务不兼容，把工作流里的 `unityVersion` 换成你们可用的 c1 镜像即可（GameCI 暂未提供 c1 镜像）。

## 说明

`Library/`、`Logs/`、`UserSettings/` 等构建缓存已通过 `.gitignore` 排除；原始导出压缩包（1.22 GB）不包含在本仓库。
