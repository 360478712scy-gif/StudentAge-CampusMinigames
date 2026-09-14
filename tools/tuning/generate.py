"""Generate runtime defaults and human-readable configuration from settings.json."""
from pathlib import Path
import json,itertools
root=Path(__file__).resolve().parents[2]
spec=json.loads((Path(__file__).parent/'settings.json').read_text())
p=root/'src/MinigameTuning.cs';code=p.read_text();start=code.index(' public static readonly Setting[] Settings={\n')+len(' public static readonly Setting[] Settings={\n');end=code.index('\n };',start)
lines=[]
for s in spec:
 args=[json.dumps(s['section']),json.dumps(s['key']),str(s['default'])+'f',str(s['min'])+'f',str(s['max'])+'f',str(s['integer']).lower(),json.dumps(s['desc'],ensure_ascii=False)]
 lines.append('new Setting('+','.join(args)+'),')
p.write_text(code[:start]+'\n'.join(lines)+code[end:])
out=root/'integration/config';out.mkdir(exist_ok=True)
example=['# 小游戏配置示例：复制为 BepInEx/config/studio.studentage.minigames.cfg。', '# 游戏会自动生成同名配置；已有配置只修改所需项，不必覆盖。', '# 修改前退出游戏，保存 UTF-8 后重启生效；小数使用英文点。','']
doc=['# 小游戏配置说明','', '实际配置文件：`游戏目录/BepInEx/config/studio.studentage.minigames.cfg`。首次启动新版插件会自动生成；也可以在首次启动前，将包内“配置示例”中的同名文件复制过去。两个插件目录及资源仍然需要正常安装。','', '先退出游戏，用文本编辑器修改 `键 = 值` 右边的数字，再保存为 UTF-8 并重新启动。无需重新编译。整数项不要填小数，小数用英文点；不要删除方括号中的分组名或修改键名。超出范围会夹到上下限，无效值回退默认值。已有配置文件会保留已有选项并补齐缺项。','', '这些设置对本机的所有存档、NDS正式游玩、标题试玩和对应社交小游戏生效；回合额度只约束存档内NDS的新关推进，F8标题试玩与角色社交各自保持原有规则。所有通关进度、已用额度、徽章与物品仍随原生存档保存。改配置不会重置它们。','', '恢复默认：退出游戏，把该 cfg 改名备份，重启自动重新生成。不要删除存档。升级包中的配置示例不会自动覆盖你的配置。其他已有文件 `studio.studentage.campusuno.cfg` 与角色 Mod 的 `Cfgs` 保持原用途，本文件不替代角色剧情或社交奖励配置。','', '## 默认共享额度','', '- 每回合所有游戏合计可首次通关 3 关。马里奥每关消耗 0.5，其余每关消耗 1。','- 例如：2 关普通游戏加 2 关马里奥，或者 6 关马里奥。允许同一游戏连续推进。','- 失败、认输、重开和已经通关的关卡不扣额度。剩半关时，只能继续挑战马里奥新关；旧关始终可重玩。','- 游戏内进入下一回合才补满额度，重新打开NDS或读回同一个存档不会刷新。跨回合完成的旧局不记新通关。','- 旧版存档本回合各游戏的推进记录会计入额度；若旧记录已超新上限，则本回合只可重玩。','- 增减 RoundLimit 只改变上限，已花费单位不变；MarioHalfUnits 影响之后的新通关成本。旧版记录第一次迁移时按当前成本折算。','', '## 常用修改示例','', '```ini','[NDS]','RoundLimit = 3','MarioHalfUnits = 1','','[TwentyFour]','Seconds1 = 150','','[Gomoku]','Block1 = 0.55','Lapse1 = 0.45','','[Snake]','StepSeconds5 = 0.18','Obstacles5 = 10','','[Contra]','Lives = 5','BossHP = 24','```','', '只把需要的行改进对应分组，不要把不完整示例覆盖整份 cfg。键名末尾的 1–5 对应关卡；五子棋 Win/Block 越高越强，Lapse 越高越易失误，速度类秒数越小越快。原有题库、关卡地图、操作键位、徽章效果与动画在本版保持既定规则，不属于下面这些数值选项。','', '## 全部配置项','']
for section,items in itertools.groupby(spec,key=lambda s:s['section']):
 items=list(items);example+=['['+section+']',''];doc+=['### '+section,'','| 键 | 默认 | 范围 | 说明 |','| --- | --- | --- | --- |']
 for s in items:
  example += ['## '+s['desc'],'## '+('整数' if s['integer'] else '数值')+'，范围 '+str(s['min'])+'–'+str(s['max'])+'；修改后重启游戏生效。',s['key']+' = '+str(s['default']),'']
  doc += ['| '+s['key']+' | '+str(s['default'])+' | '+str(s['min'])+'–'+str(s['max'])+' | '+s['desc']+' |']
 doc+=['']
(out/'studio.studentage.minigames.cfg').write_text('\n'.join(example)+'\n');(root/'integration/CONFIGURATION.md').write_text('\n'.join(doc)+'\n')
print('Generated',len(spec),'documented settings')
