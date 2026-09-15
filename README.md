# NDS小游戏拓展（仅测试版支持）

《学生时代》的 BepInEx / EC2BUnofficialPatch 小游戏扩展，包含 NDS 双屏菜单、12 款小游戏、徽章收藏、可编辑配置与自动更新。

## 下载与安装

最新试玩版：[0.11.1](https://github.com/360478712scy-gif/StudentAge-CampusMinigames/releases/tag/v0.11.1)，适用于《学生时代》**1.94 测试分支**。官方已内置 BepInEx，无需另装加载器。此版本作为预发布提供，不替换旧版 0.9.4 的自动更新入口。

- `StudentAge-CampusMinigames-Workshop-0.11.1.zip`：官方 Mod 目录格式，根目录为 `plugins/`，包含兼容 UP 与对应源码、许可，供官方 Mod 加载器使用或上传创意工坊。在游戏内启用后重启；不要再套一层 `BepInEx/plugins`。GitHub 发布不等于已上传 Steam 创意工坊。
- `StudentAge-CampusMinigames-0.11.1.zip`：已有兼容 UP 的手动安装包，将 `BepInEx` 合并到游戏目录，保留两个插件目录与所有资源。此包不附 UP；如需兼容 UP，可从上面的整合包取得 `plugins/EC2BUnofficialPatch.dll` 及对应 `Source/`、许可。不要同时保留另一份同名插件或 UP。

`ModAuthorTemplate` 仅供剧情 Mod 作者参考。包内不包含游戏本体、游戏程序集或玩家存档。

特别感谢**响空山老师**提供的角色剧情！剧情尚未更新完毕，敬请期待。

0.11.1 增加 Mod 作者独立剧情绑定：在自己的 EvtCfg 中用类型920093/920094关联角色第一关胜利/失败，保留原生对白和表情。参考[作者剧情接入](sanguosha/第一关胜利剧情说明.md)。

本版包含三国杀人物第一关胜利/失败剧情（十位角色，各一段），补齐林嘉宇剧情和赠卡，保留原生表情与男女主分支。孟怀安暂只采用尚未赢过他的版本。失败不推进关卡，每段剧情只播放一次。包含近期牌桌、拖牌、公开判定、装备选择与展示修订。

三国杀从 **2006 年年初** 起售，35名标风与神武将、人物三关、胜利集卡。十位角色武将奖励互不重复；路线不可遇角色的卡转入商店，两条路线都能收齐。每游戏回合限一局，失败下回合重试，输赢均增加10好感。详见 [三国杀说明](sanguosha/README.md)、[角色武将分配](sanguosha/角色武将分配.md) 与 [第一关剧情说明](sanguosha/第一关胜利剧情说明.md)。

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

**0.9.2及更早版本先手动安装一次最新完整包。** 手动目录安装从0.9.3起默认后台检查并下载最新正式版，正常退出游戏后自动替换，下次启动生效。更新包同时包含两个插件和资源，带大小与SHA256校验、备份及替换失败回滚，保留存档和BepInEx配置。

官方 Mod/工坊目录安装由分发渠道负责更新，不执行 GitHub 自更新。0.11.1 为预发布，需要主动下载。

开关位于 `studio.studentage.campusuno.cfg` 的 `[Updates]`，设 `Enabled = false` 可关闭后续检查。网络不可用时继续使用已安装版本。详情与维护者发布流程见 [自动更新说明](updater/AUTO_UPDATE.md)。

## 从源码构建

需要 Python 3、.NET 10 SDK、自己的《学生时代》Windows游戏程序集、BepInEx 5核心程序集和兼容UP DLL。游戏程序集只从本机引用，不上传仓库。

```sh
python build.py --game "游戏目录" --bepinex "BepInEx/core目录"
python integration/build.py --game "游戏目录" --up "EC2BUnofficialPatch.dll" --bepinex "BepInEx/core目录" --out dist/up-integration-0.10.0
python tools/prepare_minigames_release.py --staging dist/up-integration-0.10.0
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
