# 学生时代 · NDS掌机小游戏

《学生时代》的 BepInEx / EC2BUnofficialPatch 小游戏扩展，包含 NDS 双屏菜单、12 款小游戏、徽章收藏、可编辑配置与自动更新。

## 下载与安装

从 [Releases](https://github.com/360478712scy-gif/StudentAge-CampusMinigames/releases/latest) 下载 `StudentAge-CampusMinigames-版本.zip`，将包内 `BepInEx` 合并到自己的游戏目录。需要预先安装 **BepInEx 5** 和兼容的 **EC2BUnofficialPatch**；本包不包含游戏本体、游戏程序集、玩家存档或基础补丁。

必须保留 `CampusUno` 和 `StudentAgeCampusMinigames` 两个插件目录及资源。`ModAuthorTemplate` 仅供剧情 Mod 作者接入，不是普通玩家必须安装的部分。

## 内容

UNO、五子棋、课间泡泡、换盒寻物、算24点、俄罗斯方块、斗地主、贪吃蛇、吃豆人、超级马里奥、魂斗罗、麻将。马里奥提供八关，魂斗罗目前提供丛林一关，其余各五阶段。

- 游戏内2005年夏起可购买NDS掌机，默认100元，通过对应行动打开。
- 每回合所有游戏共用3关首次通关额度；马里奥每关计半关，旧关重玩与失败重试不扣额度。
- 标题主界面F8可打开全关卡试玩；游戏内F8保留原版快速读档用途。
- 小游戏局内Esc打开暂停菜单。UNO已移除独立F10入口。
- 十二款游戏全部通关各得一枚徽章，集齐后获得“NDS大玩家”；徽章及效果随原生存档保存。
- 140个带中文注释的数值设置：`BepInEx/config/studio.studentage.minigames.cfg`，退出游戏修改后重启生效。

详见 [掌机说明](nds/README.md)、[完整配置说明](integration/CONFIGURATION.md)、[角色Mod接入](integration/INSTALL.md)。

## 自动更新

**0.9.2及更早版本先手动安装一次0.9.3。** 从0.9.3起默认后台检查并下载新版，正常退出游戏后自动替换，下次启动生效。更新包同时包含两个插件和资源，带大小与SHA256校验、备份及替换失败回滚，保留存档和BepInEx配置。

开关位于 `studio.studentage.campusuno.cfg` 的 `[Updates]`，设 `Enabled = false` 可关闭后续检查。网络不可用时继续使用已安装版本。详情与维护者发布流程见 [自动更新说明](updater/AUTO_UPDATE.md)。

## 从源码构建

需要 Python 3、.NET 10 SDK、自己的《学生时代》Windows游戏程序集、BepInEx 5核心程序集和兼容UP DLL。游戏程序集只从本机引用，不上传仓库。

```sh
python build.py --game "游戏目录" --bepinex "BepInEx/core目录"
python integration/build.py --game "游戏目录" --up "EC2BUnofficialPatch.dll" --bepinex "BepInEx/core目录" --out dist/up-integration-0.9.3
python tools/prepare_minigames_release.py --staging dist/up-integration-0.9.3
```

更新助手针对 .NET Framework 4.7.2 构建，通过NuGet引用程序集即可在macOS或Linux编译。Windows需对应.NET Framework运行时；CrossOver需可运行.NET Framework程序的Bottle。

规则验证：

```sh
dotnet run --project tests/EngineTests.csproj
dotnet run --project integration/tuning-tests/TuningTests.csproj
dotnet run --project updater/tests/UpdateTests.csproj
```

其余规则测试位于各子目录 `tests`，CI运行全部独立规则测试。跨平台源码编译与规则检查不能替代原生Windows游戏验收。

## 资源与贡献说明

运行逻辑主要为本项目C#实现；部分UI通过游戏自己的资源加载器读取，因此必须有游戏本体。外部素材的来源、作者及对应许可见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) 和各资源目录；第三方代码许可证不等于原作图像、地图或音乐的再授权。本仓库不对第三方内容作统一许可证声明。

报告问题时请附游戏版本、插件版本、复现步骤及相关错误日志；不要公开个人存档或账号信息。
