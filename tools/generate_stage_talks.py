#!/usr/bin/env python3
"""为每个社交阶段生成 startTalk 对话，并把小游戏的打开写进对话（TalkCfg.miniGame），
而不是靠社交事件末尾自动打开。

生成/更新：
- integration/mod/Cfgs/zh-cn/TalkCfg.json   每阶段三句：开局（带 miniGame）、赢、输
- integration/mod/Cfgs/zh-cn/MinigameActionCfg.json  的 startTalk 指向开局句

对话 id = 阶段 id × 1000 + 序号，例如 910201 → 910201001/910201002/910201003。
roleIds 里的 -1 表示当前社交对象。想换台词时直接改 TalkCfg.json 里的 content，
或者在自己的 Mod 里提供同 id 的 TalkCfg 行（原版机制，作者 Mod 优先）。
"""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
cfg = root / 'integration/mod/Cfgs/zh-cn'
games = json.loads((cfg / 'MinigameCfg.json').read_text(encoding='utf-8'))
actions = json.loads((cfg / 'MinigameActionCfg.json').read_text(encoding='utf-8'))

OPEN = {
    9101: '课间还有点时间，来一局 UNO 吧？',
    9102: '五子棋，敢不敢来一盘？',
    9103: '玩泡泡吗？看谁先把对方困住。',
    9104: '看好了，文具在哪个盒子里？',
    9105: '来算 24 点，看谁算得快。',
    9106: '俄罗斯方块，比谁消的行多？',
    9107: '三缺一，来打斗地主吧。',
    9108: '贪吃蛇，来比比看谁吃得多。',
    9109: '吃豆人，你敢挑战吗？',
    9110: '马里奥借你玩一关，别死太多次哦。',
    9111: '魂斗罗，一起闯一关？',
    9112: '来搓一局麻将吧，就我们几个。',
}
WIN = '……居然输了。下次可没这么容易。'
LOSE = '哼哼，这局是我赢了。要不要再练练？'


def talk(tid, content, mini=None, next_win=None, next_lose=None):
    return {
        'audio': 0, 'bg': 0, 'check': [], 'content': content, 'effect': [], 'effect2': [], 'highlights': [],
        'id': tid, 'maxoptions': 0, 'miniGame': mini or [], 'nextTalk': next_win or [], 'nextTalk2': next_lose or [],
        'option': [], 'replace': [], 'roleIds': [-1], 'roleName': None, 'roles': [], 'screenEffect': [],
        'showTxt': None, 'time': 0, 'vocals': [],
    }


talks = {}
for key, action in actions.items():
    stage = int(key)
    game = stage // 100
    level = stage % 100
    name = games[str(game)]['name']
    start, win, lose = stage * 1000 + 1, stage * 1000 + 2, stage * 1000 + 3
    opener = OPEN.get(game, f'来一局{name}吧？')
    if level > 1:
        opener = f'第 {level} 关了。' + opener
    talks[str(start)] = talk(start, opener, [game, level], [win], [lose])
    talks[str(win)] = talk(win, WIN)
    talks[str(lose)] = talk(lose, LOSE)
    action['startTalk'] = start
    action['winTalk'] = 0
    action['loseTalk'] = 0

(cfg / 'TalkCfg.json').write_text(json.dumps(talks, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(cfg / 'MinigameActionCfg.json').write_text(json.dumps(actions, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('stages', len(actions), 'talks', len(talks))
