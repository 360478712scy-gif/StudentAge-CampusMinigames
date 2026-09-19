# 仓库与发布结构

本结构承接 Fable 5.1 对 Steam 0.12.0 发布包的整合：UNO 与其他小游戏共用一个插件，UP 单独作为前置，配置使用原版风格的 JSON 表。本次仅整理源文件位置和构建引用，不修改游戏逻辑、注册编号或存档格式。

## 仓库

| 目录 | 内容 |
| --- | --- |
| `src/Games/` | UNO、五子棋、泡泡、麻将、三国杀等游戏的规则与画面；UNO 没有独立打包流程 |
| `src/Core/`、`src/Integration/` | 插件生命周期与 UP 社交/剧情接口 |
| `src/Common/`、`src/Handheld/` | 共用牌桌、音频、资源、NDS 菜单及暂停界面 |
| `src/Configuration/`、`src/Update/` | JSON 表加载和更新逻辑 |
| `src/Diagnostics/` | 随插件保留的诊断入口 |
| `mod/` | 工坊原始配置结构：`Cfgs/zh-cn/` 与 UP 注册 JSON |
| `assets/` | 所有随包资源，包括原来分散在 retro、h5 目录的资源 |
| `tests/` | 按规则领域分类的独立测试，含 UNO 和更新器；`Runtime/` 为历史游戏内定向检查源码 |
| `tools/` | 配置生成、资源制作、组装与打包；`updater/` 是退出游戏后执行的更新助手 |
| `docs/` | 安装、配置、游戏说明、更新说明；废弃流程明确放在 `archive/` |
| `distribution/workshop/` | 工坊页面封面、截图、简介及发布记录 |
| `licenses/` | 第三方许可证原文 |

`build.py` 只收集 `src/` 中的运行时代码，测试程序、Unity 独立预览和更新助手主程序不会混入 DLL。旧顶层的 `integration/`、`shared/`、`bubble/`、`retro/`、`mahjong/`、`sanguosha/`、`nds/`、`h5/`、`updater/` 不再作为源码入口。

## Steam 发布包（保持不变）

```text
Mod根目录/
├── plugins/CampusMinigames.dll
├── EC2BUnofficialPatch/Minigame/
│   ├── CustomMinigamecfg.json
│   └── Audio/ Music/ CardAudio/ BubbleAudio/ PlayingCards/ Arcade/ Retro/ Mahjong/ Nds/ Sanguosha/
├── Cfgs/zh-cn/
├── readme/
├── preview.jpg
└── manifest.json
```

全部 UP 注册项的 `dll` 都是 `../../plugins/CampusMinigames.dll`。UNO 的 9101 编号和注册类型保留；历史命名空间 `StudentAge.CampusUno`、GUID 和资源名同样保留兼容，不能据此再拆出另一个 DLL。工坊包不包含 `CampusUno.dll`、`CampusMinigames.UP.dll` 或 `EC2BUnofficialPatch.dll`。

```sh
python3 build.py --game "游戏目录" --up "EC2BUnofficialPatch.dll"
python3 tools/assemble_mod.py
python3 tools/check_layout.py --mod dist/mod-0.12.0
python3 tools/package_workshop.py --mod dist/mod-0.12.0
```

以上只生成本地产物，不部署、不上传 Steam。`build.py --deploy` 只在显式指定时复制 DLL，工坊目标应为 Mod 的 `plugins/` 目录。

如需手动安装/更新包，再执行 `python3 tools/prepare_minigames_release.py --mod dist/mod-0.12.0`。该渠道继续使用 `BepInEx/plugins/CampusMinigames/` 单目录和 `campus-minigames-v2` 更新协议，不等于工坊布局；Steam 安装仍由 Steam 更新。旧双 DLL 客户端需手动迁移，不能发布新清单强行替换其布局。

## 整理验收

- 比较整理前后编译输入：所有 82 个运行时 C# 文件内容一致，只有位置变化。
- 比较 Steam 包与新组装包：配置和资源必须逐文件相同，运行时路径不得增删；允许重新编译的 DLL、相应清单和更新后的说明文档不同。
- 执行全部 `tests/*/*.csproj`、组装验证和配置生成检查。编译与规则检查不能替代原生 Windows 实机游戏验收。
- `tests/Runtime/` 含旧版本宿主检查，使用前需核对当前游戏 API 和隔离存档路径；不参与普通 CI 或正式 DLL 编译。
