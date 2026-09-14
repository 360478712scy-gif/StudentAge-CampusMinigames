# 自动更新

从 0.9.3 起支持自动更新。旧的 0.9.2 用户需先手动安装一次 0.9.3 完整插件包，之后可后台更新。

启动游戏约5秒后检查 GitHub 最新正式 Release 的 `update.json`，默认检查间隔6小时。发现更高版本时，后台下载更新包并核对文件大小、整包 SHA256、文件清单和每个文件的 SHA256。下载完成后等待玩家正常退出游戏，再由独立助手安装；不强制退出、不自动重启。下次打开游戏使用新版本。网络不可用时继续使用原版本。

完整安装仍需要已有的 BepInEx 5 和兼容的 EC2BUnofficialPatch。此更新器只更新 `BepInEx/plugins/CampusUno` 和 `BepInEx/plugins/StudentAgeCampusMinigames`，包括自带注册 JSON 和资源；不会更新 UP、其他 Mod、玩家存档或 `BepInEx/config` 中的自定义配置。

## 开关

在 `BepInEx/config/studio.studentage.campusuno.cfg` 中：

```ini
[Updates]
Enabled = true
CheckIntervalHours = 6
```

修改后重启游戏。设为 `false` 后不检查、不下载；已下载且正在等待退出的安装任务不会因此取消。关闭前若已安排安装，可在退出安装完成后恢复备份，或手动安装需要的版本。

## 备份与失败处理

备份位于 `BepInEx/cache/CampusMinigamesUpdates/backups/日期-版本/`，保留原路径下被替换的文件。正常替换失败会回滚；如果断电或系统终止助手，检查备份中的 `status.txt`，必要时在游戏完全退出后，把备份里的两个插件目录合并回 `BepInEx/plugins`。`originals.txt` 记录原有文件，`package.json` 记录此次更新文件；没有原文件的新增项可按清单移除。`rollback-needs-attention` 表示回滚被文件权限或占用阻挡，原文件仍保存在备份。

每版下载、`plan.json`、`install.log` 和安装完成标记位于缓存对应版本目录。下载与备份不会混入存档。保留最近一次备份，确认新版本运行正常后可手动清理更早的缓存。

## 发布流程

1. 将 `src/CampusAutoUpdate.cs` 中 `CampusVersion.Value` 升为更高的数字版本，例如 `0.9.4`。所有插件入口使用该常量。
2. 用对应版本编译，先做规则及宿主验证。
3. 执行 `python tools/prepare_minigames_release.py --staging dist/up-integration-0.9.4`。
4. 创建标签 `v0.9.4` 对应的草稿 Release，将完整包、自动更新ZIP、`update.json`、`SHA256SUMS.txt` 全部上传。
5. 确认附件齐全且大小与摘要正确，再将草稿发布为最新正式 Release。更新器不会读取预发布版本。不要先发布指向尚未上传文件的清单。

完整包名 `StudentAge-CampusMinigames-版本.zip`；自动更新包名 `CampusMinigames-update-版本.zip`。安装整个包时使用前者。后者供更新助手使用，不包含加载器、测试存档或账号信息。

自动更新契约为 schema 1、layout `campus-minigames-v1`。下载 URL 限定本项目 GitHub Release 路径，HTTPS 下载；文件仅允许上述两个插件目录，相对路径穿越、大小超限、重复文件、校验失败、目标目录链接会被拒绝。不做远程程序集执行或热替换当前游戏中的程序集。

## 验证

`dotnet run --project updater/tests/UpdateTests.csproj` 检查版本、清单、路径边界、摘要、双插件替换、保留其他Mod、备份与失败回滚。独立助手在 .NET Framework 4.7.2 构建，并已在 CrossOver 中验证等待父进程退出后再安装。并已在隔离Unity游戏中使用公开Release验证HTTPS下载、清单与摘要、等待游戏退出、191个插件及资源文件安装和备份。首次发布的模拟升级只在QA插件中将相等版本视为新版，发行版仍只接受更高版本。Windows 原生环境仍需独立验收。

协议参考：[GitHub Release 附件](https://docs.github.com/en/rest/releases/assets)、[最新 Release 下载链接](https://docs.github.com/en/repositories/releasing-projects-on-github/linking-to-releases)。
