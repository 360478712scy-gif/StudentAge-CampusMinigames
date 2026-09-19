#!/usr/bin/env python3
"""Bounded, repeatable relevance and latency checks for the current knowledge set."""
import argparse
import json
import statistics
import subprocess
import sys
import time
from pathlib import Path
import project_memory as memory

CASES = [
    ('UNO 是否独立 DLL 安装目录', 'UNO 与其他小游戏统一'),
    ('Steam 发布包目录 UP 前置', 'Steam 单 DLL 布局与 UP 前置'),
    ('JSON 参数表 作者模组覆盖', 'JSON 配置与作者覆盖'),
    ('完工审计 UI 玩法 稳定性 三个子任务', '三个并行子任务'),
    ('审计复核停止 避免重复整轮审查', '复核与停止'),
    ('UNO 摸牌摸光 洗牌 牌数', '姓名头像、剩余牌数与摸光洗牌'),
    ('原生 Windows CrossOver 验收区别', '不能混淆的证据'),
    ('源码导航 游戏规则与画面在哪', '游戏规则与画面'),
    ('防止重复扣费 奖励 会话结算', '防止重复扣费与奖励'),
    ('暂停音频 退出释放恢复输入', '暂停、音频与释放'),
]


def main():
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', choices=('auto', 'hybrid', 'lexical'), default='auto')
    args = parser.parse_args()
    db = memory.connect()
    try:
        state = memory.status(db, check=True)
        if state['stale_paths'] or state['model_code_changed']:
            raise RuntimeError('Index stale; run project_memory.py index first.')
        db.execute('DELETE FROM cache')
        db.commit()
    finally:
        db.close()
    rows = []
    for query, expected in CASES:
        timings = []
        for repeat in range(2):
            start = time.perf_counter()
            raw = subprocess.check_output([sys.executable, '-B', str(Path(__file__).with_name('project_memory.py')),
                                           'search', query, '--mode', args.mode], text=True, encoding='utf-8')
            result = json.loads(raw)
            timings.append({'wall_ms': round((time.perf_counter()-start)*1000, 2),
                            'search_ms': result['search_ms'], 'cache_hit': result['cache_hit']})
            assert result['source_files_read'] == 0 and len(result['results']) <= 3
            assert sum(len(x['text']) for x in result['results']) <= 1650
            assert result['cache_hit'] == bool(repeat)
        titles = [r['title'] for r in result['results']]
        rows.append({'query': query, 'expected': expected, 'top3': titles,
                     'hit': expected in titles, 'timings': timings})
    report = {'model': state['model'], 'dimensions': int(state['dimensions']),
              'mode': args.mode, 'files': state['files'], 'chunks': state['chunks'],
              'text_characters': state['text_characters'], 'queries': len(rows),
              'top3_hits': sum(r['hit'] for r in rows),
              'cold_median_wall_ms': statistics.median(r['timings'][0]['wall_ms'] for r in rows),
              'cached_median_wall_ms': statistics.median(r['timings'][1]['wall_ms'] for r in rows),
              'cold_median_search_ms': statistics.median(r['timings'][0]['search_ms'] for r in rows),
              'cached_median_search_ms': statistics.median(r['timings'][1]['search_ms'] for r in rows),
              'source_files_read_per_search': 0, 'rows': rows}
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if not all(r['hit'] for r in rows):
        raise SystemExit(1)


if __name__ == '__main__':
    main()
