#!/usr/bin/env python3
"""把编译好的 CampusMinigames.dll 和资源组装成官方 Mod / 工坊目录布局（lfw 示范结构）。

Mod根/
├── preview.jpg
├── manifest.json
├── plugins/CampusMinigames.dll                 ← BepInEx 插件（官方 1.94 加载）
├── EC2BUnofficialPatch/Minigame/               ← UP 注册与全部小游戏资源
│   ├── CustomMinigamecfg.json                  （dll 指向 ../../plugins/CampusMinigames.dll）
│   └── Music/ Nds/ Retro/ Sanguosha/ ...
├── Cfgs/zh-cn/                                 ← 原版注册表 + 玩家可自定义的关卡表/剧情
└── readme/                                     ← 说明文档与许可

不附带 UP：玩家需要另外订阅 EC2BUnofficialPatch。
用法：python tools/assemble_mod.py --dll dist/build/CampusMinigames.dll [--out dist/mod-<版本>]
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
VERSION = re.search(r'const string Value="([0-9.]+)"', (ROOT / 'src/Update/CampusAutoUpdate.cs').read_text()).group(1)

# 资源目录：仓库路径 -> EC2BUnofficialPatch/Minigame/ 下的目录名
ASSET_DIRS = {
    'assets/music': 'Music',
    'assets/card-audio': 'CardAudio',
    'assets/bubble/audio': 'BubbleAudio',
    'assets/playing-cards': 'PlayingCards',
    'assets/arcade': 'Arcade',
    'assets/retro': 'Retro',
    'assets/mahjong': 'Mahjong',
    'assets/nds': 'Nds',
    'assets/sanguosha': 'Sanguosha',
}

# 说明文档：仓库路径 -> readme/ 下的文件名
README_FILES = {
    'docs/INSTALL.md': '安装与角色绑定.md',
    'docs/CONFIGURATION.md': '小游戏配置说明.md',
    'docs/games/mahjong/README.md': '课间麻将说明.md',
    'docs/games/nds/README.md': 'NDS掌机说明.md',
    'docs/games/sanguosha/README.md': '三国杀说明.md',
    'docs/games/sanguosha/角色武将分配.md': '角色武将分配.md',
    'docs/games/sanguosha/人物对话配置说明.md': '人物对话配置说明.md',
    'docs/games/sanguosha/第一关胜利剧情说明.md': '第一关胜利剧情说明.md',
    'THIRD_PARTY_NOTICES.md': 'THIRD_PARTY_NOTICES.md',
}


def copy_tree(src: Path, dst: Path):
    shutil.copytree(src, dst, dirs_exist_ok=True, ignore=shutil.ignore_patterns('.DS_Store', '__pycache__', '*.pyc'))


def write_manifest(out: Path):
    files = {
        str(f.relative_to(out)).replace('\\', '/'): hashlib.sha256(f.read_bytes()).hexdigest()
        for f in sorted(out.rglob('*')) if f.is_file() and f.name != 'manifest.json'
    }
    manifest = {
        'name': 'NDS小游戏拓展',
        'id': 'studio.studentage.campusminigames',
        'version': VERSION,
        'author': '360478712scy-gif',
        'game': 'StudentAge 1.94',
        'requires': ['sa.EC2B.UnofficialPatch'],
        'plugins': ['plugins/CampusMinigames.dll'],
        'files': files,
    }
    (out / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def assemble(dll: Path, out: Path, preview: Path):
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)

    # 1. 插件
    (out / 'plugins').mkdir()
    shutil.copy2(dll, out / 'plugins' / 'CampusMinigames.dll')

    # 2. UP 注册 + 资源
    content = out / 'EC2BUnofficialPatch' / 'Minigame'
    content.mkdir(parents=True)
    shutil.copy2(ROOT / 'mod/EC2BUnofficialPatch/Minigame/CustomMinigamecfg.json', content / 'CustomMinigamecfg.json')
    for src, name in ASSET_DIRS.items():
        copy_tree(ROOT / src, content / name)
    audio = content / 'Audio'
    audio.mkdir()
    for f in (ROOT / 'assets/gomoku/audio').iterdir():
        if f.is_file():
            shutil.copy2(f, audio / f.name)

    # 3. 原版配置表与玩家可自定义的表
    copy_tree(ROOT / 'mod/Cfgs', out / 'Cfgs')

    # 4. 说明与许可
    readme = out / 'readme'
    readme.mkdir()
    for src, name in README_FILES.items():
        shutil.copy2(ROOT / src, readme / name)
    copy_tree(ROOT / 'licenses', readme / 'licenses')
    (readme / '使用说明.txt').write_text(
        '适用《学生时代》1.94 测试分支。\n'
        '需要先订阅并启用 EC2BUnofficialPatch（UP），再订阅本 Mod，在游戏内启用后重启。\n'
        '本 Mod 不包含 UP，也不要把旧版手动安装的 CampusUno.dll / CampusMinigames.UP.dll 留在 BepInEx/plugins 下。\n'
        '玩家可修改的内容都在 Cfgs/zh-cn 下，把对应 json 复制到自己的 Mod 里再改；不要改 BepInEx/config。\n'
        'F7：主界面三国杀测试目录。F8：主界面 NDS。\n'
        '三国杀 2006 年年初起出售；NDS 2005 年夏起出售，已有物品不重复出售。\n',
        encoding='utf-8')

    # 5. 预览图与清单
    from PIL import Image
    Image.open(preview).convert('RGB').save(out / 'preview.jpg', quality=90)
    write_manifest(out)
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--dll', type=Path, default=ROOT / 'dist/build/CampusMinigames.dll', help='build.py 的产物')
    p.add_argument('--out', type=Path, default=ROOT / f'dist/mod-{VERSION}')
    p.add_argument('--preview', type=Path, default=ROOT / 'distribution/workshop/preview.png')
    args = p.parse_args()
    assert args.dll.is_file(), 'Run build.py first: ' + str(args.dll)
    out = assemble(args.dll.resolve(), args.out.resolve(), args.preview)
    print(out)


if __name__ == '__main__':
    main()
