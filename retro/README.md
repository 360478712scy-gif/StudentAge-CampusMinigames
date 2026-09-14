# 经典小游戏本地预览 · 0.7.0

这版已经能在学生时代隔离副本中从角色社交入口启动，并用原生Unity运行。当前是可玩原型，尚未完成原作逐帧比对与人工完整通关验收。

## 关卡

马里奥初代精选8关：**1-1、1-2、1-3、2-1、3-1、5-1、6-3、1-4**。依次涵盖入门草原、地下、树顶、管道与藤蔓、夜间、敌人密集区、高空平台、城堡；含支路共17张地图。1-2直接从地下主体开始。八个社交阶段依次推进，小游戏内不提供选关按钮。

魂斗罗先做初代丛林第一关：移动、跳跃、卧倒、游泳、八方向射击、N/M/S/L/F武器与R射速道具、爆破桥和关底防御设施。其他关卡尚未实现。

## 直接试玩

Unity工程：`另行提供的 student-age-minigames-unity 工程`，使用Unity 2020.3.48f1。

打开 `Assets/CampusRetro/Scenes/MarioClassic.unity` 或 `ContraClassic.unity`，点击编辑器播放。马里奥预览关卡可在播放前选中 `Retro Preview`，调整检查器里的 `Mario Level`（仅开发工具，游戏内无此选项）。结束后停止再播放即可重试。

| 操作 | 按键 |
| --- | --- |
| 移动/瞄准 | WASD或方向键 |
| 跳跃 | Z或空格，马里奥按住可跳更高 |
| 马里奥加速/火球 | X或左Shift |
| 魂斗罗连续射击 | X或左Shift |
| 进入水管/蹲下 | 下；藤蔓用上/下 |

游戏插件内另支持手柄。准备页、操作说明、结果窗沿用学生时代；局内保留经典像素画面与HUD。音效默认开启，原生接入保留小游戏BGM8；Unity独立试玩只播放短音效。

## 接入

本地插件目录：`dist/up-integration-0.7.0-preview`。这不是已发布更新包，原0.6.0 ZIP保持原样。
角色绑定马里奥 `minigame=9110`、魂斗罗 `minigame=9111`。阶段分别为911001..911008、911101，仍由UP持有费用与社交进度。不得另建玩家可见选关或重复奖励逻辑。

运行层采用新写的C#规则与256×240像素绘制，无网页内核、不启动外部游戏进程。转换后的图集、地图、短音效必须随插件目录的 `Retro` 子目录保留。编辑器与插件共用相同规则代码。

## 参考来源

- [linhdvu14/SMB-clone](https://github.com/linhdvu14/SMB-clone)：马里奥动画图、短音效与运动实现参考；代码BSD-2-Clause。
- [Stabyourself/mari0](https://github.com/Stabyourself/mari0)：SMB地图、图块、补充精灵；README声明MIT。
- [RanZelong/Contra-Unity](https://github.com/RanZelong/Contra-Unity)：丛林Tilemap、角色动画、关底与短音效数据；代码GPL-3.0。

固定提交及许可副本见 `assets/sources.json`、`assets/licenses`。运行层没有导入第三方Unity项目脚本，地图和动画由转换脚本读取。上述代码许可不等于Nintendo/Konami原作图像与声音授权；这些素材当前仅为本地开发参考，公开发布前需另行确认素材权限或替换。

## 验证与限制

定向证据在 `qa/retro-0.7.0`：纯引擎检查、Unity2020编译与场景生成、隔离CrossOver游戏中的按键输入、八个马里奥阶段及关底结算、魂斗罗实际射击与关底结算。

八关结算采用临近终点局面；魂斗罗采用接近击破的关底局面。检查不能替代八关从头到尾人工通关，也不证明Windows原生或真实用户角色Mod已经验收。部分敌人/机关行为、碰撞参数、动画节拍、武器效果仍需对原作调整；没有完成一比一还原或双人模式。没有复制原作完整背景音乐。

开发命令：`python3 retro/sync_unity.py` 同步编辑器工程；`dotnet run --project retro/tests/RetroTests.csproj` 检查规则。来源转换需要Pillow和PyYAML。
