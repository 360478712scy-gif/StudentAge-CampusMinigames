# 小游戏插件维护约定

- 当前布局以 Steam 工坊 3802190178 的 0.12.0 发布包为基准，详见 `docs/REPOSITORY_LAYOUT.md`。Fable 5.1 完成了单 DLL、JSON 配置和剧情启动整合；后续整理不能退回旧双 DLL 结构。
- UNO 与其他小游戏同属 `src/Games/`，共同编译为 `CampusMinigames.dll`。不要给 UNO 恢复单独插件、安装目录、发布包或独立启动入口。
- 工坊包固定使用 `plugins/`、`EC2BUnofficialPatch/Minigame/`、`Cfgs/zh-cn/`、`readme/`、`preview.jpg`、`manifest.json`。UP 是外部前置，不附带 UP DLL。
- 游戏参数放在 `mod/Cfgs/zh-cn/`；修改默认值时检查 `tools/tuning/settings.json` 与生成结果。作者自建 Mod 的表覆盖默认配置，不能写回覆盖作者文件。
- 稳定的 GUID、命名空间、注册类型名、游戏编号、存档标识、嵌入资源名不是文件夹名称；不能为了目录美观直接修改它们。
- 源码、资源、配置、测试、工具、文档分别放入 `src/`、`assets/`、`mod/`、`tests/`、`tools/`、`docs/`。构建产物留在忽略的 `dist/`，历史材料放 `docs/archive/`。
- `docs/archive/legacy-tools/` 是旧双 DLL 验证和部署脚本的文本存档，不是当前发布入口。不得对真实游戏运行旧部署流程。
- 目录变更必须同步更新编译、项目引用、生成器、打包和 CI；运行规则测试及 `python3 tools/check_layout.py`，有游戏引用时编译并检查组装包。源码/规则检查与原生游戏验收要分别报告。
- 保留贡献归属、素材来源和许可证；不要上传游戏程序集、玩家模组、存档、日志、账号数据或 agent 临时文件。
