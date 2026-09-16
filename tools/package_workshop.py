#!/usr/bin/env python3
"""把 integration/build.py 组装好的 Mod 目录打成创意工坊 zip。不发布，不附带 UP。

用法：python tools/package_workshop.py --mod dist/mod-<版本> [--out dist/release]
"""
from pathlib import Path
import argparse
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = ['preview.jpg', 'manifest.json', 'plugins/CampusMinigames.dll',
            'EC2BUnofficialPatch/Minigame/CustomMinigamecfg.json', 'Cfgs/zh-cn/MinigameCfg.json']
FORBIDDEN_NAMES = {'EC2BUnofficialPatch.dll', 'CampusUno.dll', 'CampusMinigames.UP.dll', 'UP-source.zip'}


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--mod', type=Path, required=True, help='integration/build.py 的输出目录')
    p.add_argument('--out', type=Path, default=ROOT / 'dist/release')
    a = p.parse_args()
    mod = a.mod.resolve()
    for rel in REQUIRED:
        assert (mod / rel).is_file(), 'Mod 目录缺少 ' + rel
    for f in mod.rglob('*'):
        assert f.name not in FORBIDDEN_NAMES, '工坊包不得包含 ' + f.name
    manifest = json.loads((mod / 'manifest.json').read_text(encoding='utf-8'))
    version = manifest['version']
    a.out.mkdir(parents=True, exist_ok=True)
    target = a.out / f'StudentAge-CampusMinigames-Workshop-{version}.zip'
    with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for f in sorted(mod.rglob('*')):
            if f.is_file() and f.name != '.DS_Store':
                z.write(f, str(f.relative_to(mod)).replace('\\', '/'))
    with zipfile.ZipFile(target) as z:
        assert z.testzip() is None
    print(json.dumps({'version': version, 'zip': str(target), 'sha256': hashlib.sha256(target.read_bytes()).hexdigest()}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
