# 官方 1.94 工坊插件接入评估

> 2026-09-16 更新：按 UP 作者示范，Mod 布局已改为 `plugins/CampusMinigames.dll`（单 DLL）+ `EC2BUnofficialPatch/Minigame/`（注册与资源）+ `Cfgs/zh-cn/`（表）+ `readme/`，并且不再随包分发 UP。下文的旧布局与"整合 UP"方案只作历史记录。

状态：2026-09-16，0.11.1 已发布至 [Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3802190178) 与 [GitHub](https://github.com/360478712scy-gif/StudentAge-CampusMinigames/releases/tag/v0.11.1)。实际Steam下载包1100文件校验一致；隔离CrossOver中，仅启用该Mod后重启，官方MultiFolderLoader直接加载下载包及兼容UP，F7/F8和元数据检查通过。未做Windows原生或完整自然周目验收。下文早期评估保留为历史，不代表当前发布状态。

## 官方已经提供的能力

公告：https://store.steampowered.com/news/app/1991040/view/667250326206678138?l=schinese
标题：【测试分支】1.94更新公告：支持代码插件（BepinEx）。原公告 HTML 和 Steam 官方新闻 API 正文保存在 research/workshop-official-20260915。

玩家使用 1.94 测试分支，订阅兼容 Mod，在游戏内启用，再重启即可加载。不是订阅后当前运行进程立即生效。作者必须把 plugins（全小写）直接放在 Mod 根目录，不再套 BepInEx/plugins。插件仍使用 BepInEx 5、BepInPlugin 与 BaseUnityPlugin，引用游戏自带 BepInEx.dll/0Harmony.dll，目标 x64 或 AnyCPU。

## 当前代码需要的修改

1. integration/build.py 增加工坊打包目标，保留手动包。两个 DLL 直接置于 Mod/plugins，资源跟随 CampusMinigames.UP.dll。根目录加入真实工坊 preview.jpg；作者模板留在开发包，不把示例人物配置作为玩家默认数据加载。
2. 统一资源根路径为实际已加载 CampusMinigames.UP 程序集所在目录。需要改 src/ExternalMetadata.cs、src/NativeMinigameAudio.cs、src/MinigamePause.cs 中写死 Paths.PluginPath/StudentAgeCampusMinigames 的位置。NdsConsole、NdsBadges、SanguoshaView 已按程序集位置取资源。基础 DLL 不直接引用扩展 DLL，可通过已加载程序集定位或显式注册资源根。
3. CustomMinigamecfg.json 继续紧邻扩展 DLL，dll 字段保留 CampusMinigames.UP.dll；UpSession.Root 目前来自该 JSON 的目录，移动 JSON 时必须同步考虑音效/图片路径。MinigameCfg/MinigameActionCfg 默认数据仍由 ExternalMetadata 缺项补注册，保持作者 Mod 覆盖优先。
4. src/CampusAutoUpdate.cs 当前只认可手动安装的两个固定目录，因此工坊路径下会跳过更新。应把工坊/手动来源明确区分：工坊版交给 Steam 更新，手动版保留 GitHub 更新；不得从工坊版复制另一份 DLL 到全局 plugins，也不把用户 cfg 写回会被 Steam 替换的资源目录。
5. 用实际 1.94 游戏自带引用重新编译；保留现有稳定 GUID，处理旧手动安装与工坊安装同 GUID 重复存在的升级场景，不靠改 GUID 同时加载两套逻辑。

建议的小游戏内容布局（UP 前置单独处理）：

```text
工坊Mod根目录/
├── preview.jpg
└── plugins/
    ├── CampusUno.dll
    ├── CampusMinigames.UP.dll
    ├── CustomMinigamecfg.json
    ├── Cfgs/zh-cn/...
    ├── Music/...
    ├── Nds/...
    ├── Retro/...
    ├── Sanguosha/...
    └── 其他现有资源目录/...
```

两个 DLL 直接在 plugins 下，避免依赖公告未明确说明的多层扫描行为。此布局不是已构建交付物。

## UP 仍是前置

NdsIntegration.cs 有 sa.EC2B.UnofficialPatch 硬依赖；12 个小游戏入口实现 UP 的 ICustomMinigame，UpSession 使用其 CustomMinigameContext/Begin/Complete/Invalidated。官方加载 DLL 不等于提供这些接口，不能只删除 BepInDependency。

- 最小迁移：UP 也按 1.94 格式发布为工坊前置，玩家订阅并启用 UP 与小游戏后重启，不需要复制文件。工坊依赖关系应配置，但不能把标记必需物品当成玩家必然已经下载并启用的保证。
- 若必须只订阅一个条目：在允许再分发的前提下，同一 Mod 包含兼容 UP 及其必要资源，统一管理版本并处理重复安装；或者将当前实际依赖的小游戏接口/结算接入迁入独立模块，消除 UP 前置，后者工作量更大。

核对的本地 UP 源码：student-age-patch-merged/EC2BUnofficialPatch/Workshop/ContentRootCatalog.cs 当前枚举同 Steam 库下所有已下载工坊目录，CustomMinigameRegistry.cs 递归寻找注册 JSON，未依据官方启用列表筛选。这与官方“启用 Mod 后生效”的语义可能不一致；需对接实际 1.94 启用目录来源，避免禁用 Mod 后仍注册其配置，并检查跨库/本地 Mod。该发现基于本地源码，不代表已核对最新公开 UP 版本。

## 验收范围

在无手动小游戏/UP 副本的隔离 1.94 环境，验证工坊订阅、启用、重启、两插件及前置加载顺序；NDS/F7/社交入口、资源与 cfg、存读档结算、Steam 更新后个人配置保留；禁用后重启不再加载/注册；旧手动版升级时不双加载。官方是否原地加载或使用缓存、启用名单接口及跨工坊 DLL 依赖解析，公告没有展开，需由实际运行日志/代码确认。尚未执行以上验证。

## 当前实现与证据

- Plugin 仅作 BepInEx 引导，CampusRuntime 和 NdsRuntime 使用独立 HideAndDontSave 常驻对象。实际旧安装日志显示游戏销毁 BepInEx_Manager 后旧插件执行 OnDestroy 撤掉补丁，导致 F7/F8/商店入口同时失效；新宿主通过相同清理流程后继续运行。没有修改玩家的 HideManagerGameObject=false 配置。
- CampusResources.Root 定位实际已加载的 CampusMinigames.UP 程序集；音乐、暂停字体/像素、默认关卡数据改为共用该目录。非标准手动目录禁用 GitHub 自动更新任务，交由 Mod 分发渠道管理。
- tools/package_workshop.py 从完整编译 staging 生成扁平 plugins 包；--up 可加入兼容前置生成本地候选。当前 dist/workshop-official-1.94 带已安装相同 UP，保留来源和许可；不是公开发布包。配套 UP 已通过 OfficialModPaths 按官方启用名单与目录快照读取；工坊 UP 的独立更新器停用。纯数据启用 Mod 与禁用 Mod 的过滤已通过定向检查。
- 定向证据：qa/official-194/steamapps/common/StudentAge/CampusUpQA/checks.txt、success.txt、f7.png、f8.png、shop.png。覆盖真实 InputSystem F7/F8 输入、官方工坊路径加载、默认元数据、隔离存档读入、2003 三国杀和 2005 夏季后 NDS 商店列表、生命周期及无 Unity 异常。未全量重跑所有小游戏。
- dist/up-integration-official-1.94/local-install.json 记录两 DLL 安装指纹、备份和检查结果。用户存档未修改。

最终交付：dist/workshop-official-1.94-ready（配套 UP 源码与许可随包）、dist/up-integration-official-1.94-follow/local-install.json。三国杀描述和 2002 年春季上架、满 5 人关注的商店补卡均已并入最终 22 项检查。前文评估/初次检查涉及的旧 staging 仅为历史证据。
