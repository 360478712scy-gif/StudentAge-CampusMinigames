#!/usr/bin/env python3
"""从 settings.json 生成：
1. src/MinigameTuning.cs 里的默认值表（代码内兜底）；
2. integration/mod/Cfgs/zh-cn/<游戏>Cfg.json 各游戏关卡表（玩家/作者可复制修改）；
3. integration/mod/Cfgs/zh-cn/CampusMinigameCfg.json 通用项（NDS、Audio、Social）；
4. integration/CONFIGURATION.md 配置说明。
"""
from pathlib import Path
import itertools
import json
import re

root = Path(__file__).resolve().parents[2]
spec = json.loads((Path(__file__).parent / 'settings.json').read_text(encoding='utf-8'))
cfg_dir = root / 'integration/mod/Cfgs/zh-cn'
cfg_dir.mkdir(parents=True, exist_ok=True)

GAME_IDS = {'UNO': 9101, 'Gomoku': 9102, 'Bubble': 9103, 'Box': 9104, 'TwentyFour': 9105, 'Tetris': 9106,
            'Landlord': 9107, 'Snake': 9108, 'Pacman': 9109, 'Mario': 9110, 'Contra': 9111, 'Mahjong': 9112}
GAME_NAMES = {'UNO': '课间 UNO', 'Gomoku': '五子棋', 'Bubble': '课间泡泡', 'Box': '换盒寻物', 'TwentyFour': '算24点',
              'Tetris': '俄罗斯方块', 'Landlord': '斗地主', 'Snake': '贪吃蛇', 'Pacman': '吃豆人', 'Mario': '超级马里奥兄弟',
              'Contra': '魂斗罗', 'Mahjong': '课间麻将'}
COMMON = 'CampusMinigameCfg'
SOCIAL_DEFAULTS = {'GameId': 9101, 'TrustCost': 4, 'NeedRelation': 3}
SOCIAL_DESC = {'GameId': '没有 UP 时 PersonGrowCfg.minigame 的绑定编号（正常情况下请安装 UP）。',
               'TrustCost': '没有 UP 时默认每局消耗的信任。', 'NeedRelation': '没有 UP 时默认需要的关系等级。'}


def split_level(key):
    m = re.match(r'^(.*?)(\d+)$', key)
    return (m.group(1), int(m.group(2))) if m else (key, 0)


def num(v):
    return int(v) if isinstance(v, (int, float)) and float(v).is_integer() else v


# 1. C# 默认值
p = root / 'src/MinigameTuning.cs'
code = p.read_text(encoding='utf-8')
start = code.index(' public static readonly Setting[] Settings={\n') + len(' public static readonly Setting[] Settings={\n')
end = code.index('\n };', start)
lines = []
for s in spec:
    args = [json.dumps(s['section']), json.dumps(s['key']), str(s['default']) + 'f', str(s['min']) + 'f', str(s['max']) + 'f',
            str(s['integer']).lower(), json.dumps(s['desc'], ensure_ascii=False)]
    lines.append('new Setting(' + ','.join(args) + '),')
p.write_text(code[:start] + '\n'.join(lines) + code[end:], encoding='utf-8')

# 2/3. JSON 表
common = {}
for section, items in itertools.groupby(spec, key=lambda s: s['section']):
    items = list(items)
    if section in GAME_IDS:
        gid = GAME_IDS[section]
        rows = {}
        rows[str(gid)] = {'id': gid, 'game': gid, 'level': 0, 'name': GAME_NAMES[section], 'parms': {}}
        for s in items:
            base, level = split_level(s['key'])
            if level == 0:
                rows[str(gid)]['parms'][base] = num(s['default'])
            else:
                rid = gid * 100 + level
                rows.setdefault(str(rid), {'id': rid, 'game': gid, 'level': level, 'name': f'第{level}关', 'parms': {}})
                rows[str(rid)]['parms'][base] = num(s['default'])
        if not rows[str(gid)]['parms']:
            del rows[str(gid)]
        ordered = {k: rows[k] for k in sorted(rows, key=int)}
        (cfg_dir / f'{section}Cfg.json').write_text(json.dumps(ordered, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    else:
        common[section] = {'id': section, 'name': section, 'parms': {s['key']: num(s['default']) for s in items}}
common['Social'] = {'id': 'Social', 'name': '无 UP 时的社交绑定', 'parms': dict(SOCIAL_DEFAULTS)}
(cfg_dir / f'{COMMON}.json').write_text(json.dumps(common, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

# 4. 说明文档
doc = ['# 小游戏配置说明', '',
       '所有可调数值都在本 Mod 的 `Cfgs/zh-cn/` 下的 JSON 表里，**不再使用 BepInEx/config**。',
       '每款游戏一张表（`GomokuCfg.json`、`SnakeCfg.json`……），格式与原版 `FightPlayerCfg.json` 一样：键是 id，值是一行；',
       '`id` 等于游戏编号（如 9102）的行是该游戏的全局参数，`id` 等于 游戏编号×100+关卡号（如 910203）的行是第 3 关的参数，数值都写在 `parms` 里。', '',
       '```json', '{', '  "910201": { "id": 910201, "game": 9102, "level": 1, "name": "第1关", "parms": { "Win": 0.9, "Block": 0.65 } }', '}', '```', '',
       '## 怎么改', '',
       '1. 不要直接改本 Mod 目录里的文件（Steam 更新会覆盖）。',
       '2. 把要改的表复制到你自己的 Mod 的 `Cfgs/zh-cn/` 下，只保留要改的行，`parms` 里只写要改的键。',
       '3. 同一 id 的行按键合并，后加载的 Mod 覆盖前面的（本 Mod 的表是默认值），没写的项保持默认。',
       '4. 也可以加新的关卡行（例如 910206），并用 TalkCfg/OptionCfg 的 `miniGame: [9102, 6]` 打开它；没有配的关卡沿用已定义的最高一关。',
       '5. 超出范围的值会夹到上下限，写错格式的文件会被跳过并在日志里提示。修改后重启游戏生效。', '',
       f'通用项在 `{COMMON}.json`：`NDS`（回合额度、掌机价格）、`Audio`、`Social`（仅无 UP 时使用）。',
       '三国杀对手/奖励在 `SanguoshaCfg.json`，对话气泡在 `SanguoshaDialogueCfg.json`，见《三国杀说明》。', '',
       '## 全部配置项', '']
for section, items in itertools.groupby(spec, key=lambda s: s['section']):
    items = list(items)
    table = f'{section}Cfg.json' if section in GAME_IDS else f'{COMMON}.json'
    doc += [f'### {section}（{table}）', '', '| parms 键 | 关卡 | 默认 | 范围 | 说明 |', '| --- | --- | --- | --- | --- |']
    for s in items:
        base, level = split_level(s['key'])
        doc.append(f"| {base} | {level or '全局'} | {s['default']} | {s['min']}–{s['max']} | {s['desc']} |")
    doc.append('')
doc += ['### Social（' + COMMON + '.json）', '', '| parms 键 | 默认 | 说明 |', '| --- | --- | --- |']
for k, v in SOCIAL_DEFAULTS.items():
    doc.append(f'| {k} | {v} | {SOCIAL_DESC[k]} |')
doc.append('')
(root / 'integration/CONFIGURATION.md').write_text('\n'.join(doc) + '\n', encoding='utf-8')
print('Generated', len(spec), 'settings into', cfg_dir)
