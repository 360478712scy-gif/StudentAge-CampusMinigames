#!/usr/bin/env python3
"""从组装好的 Mod 目录生成手动安装包和自动更新包。不发布。

手动安装布局（合并到游戏根目录）：
  BepInEx/plugins/CampusMinigames/
    CampusMinigames.dll
    CustomMinigamecfg.json      （dll 字段改为同目录）
    Cfgs/zh-cn/ Music/ Nds/ ...  （与工坊包 EC2BUnofficialPatch/Minigame 内容相同）
    readme/

用法：python tools/prepare_minigames_release.py --mod dist/mod-<版本> [--out dist/release]
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PLUGIN_FOLDER = 'CampusMinigames'
LAYOUT = 'campus-minigames-v2'


def build_manual_tree(mod: Path, target: Path):
    plugin = target / 'BepInEx' / 'plugins' / PLUGIN_FOLDER
    plugin.mkdir(parents=True)
    shutil.copy2(mod / 'plugins/CampusMinigames.dll', plugin / 'CampusMinigames.dll')
    content = mod / 'EC2BUnofficialPatch' / 'Minigame'
    for item in content.iterdir():
        if item.is_dir():
            shutil.copytree(item, plugin / item.name)
        elif item.name != 'CustomMinigamecfg.json':
            shutil.copy2(item, plugin / item.name)
    registry = json.loads((content / 'CustomMinigamecfg.json').read_text(encoding='utf-8'))
    for entry in registry.get('minigames', []):
        if 'dll' in entry:
            entry['dll'] = Path(entry['dll']).name
    (plugin / 'CustomMinigamecfg.json').write_text(json.dumps(registry, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    shutil.copytree(mod / 'Cfgs', plugin / 'Cfgs')
    shutil.copytree(mod / 'readme', plugin / 'readme')
    shutil.copy2(ROOT / 'updater/AUTO_UPDATE.md', plugin / 'readme' / '自动更新说明.md')
    return plugin


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--mod', type=Path, required=True)
    p.add_argument('--out', type=Path, default=ROOT / 'dist/release')
    a = p.parse_args()
    mod = a.mod.resolve()
    out = a.out.resolve()
    out.mkdir(parents=True, exist_ok=True)
    source = (ROOT / 'src/CampusAutoUpdate.cs').read_text()
    version = re.search(r'const string Value="([0-9.]+)"', source).group(1)
    repo = re.search(r'const string Repository="([^"]+)"', source).group(1)
    assert json.loads((mod / 'manifest.json').read_text(encoding='utf-8'))['version'] == version, 'Mod 目录版本与源码不一致'

    with tempfile.TemporaryDirectory() as tmp:
        stage = Path(tmp) / 'manual'
        plugin = build_manual_tree(mod, stage)
        full = out / f'StudentAge-CampusMinigames-{version}.zip'
        with zipfile.ZipFile(full, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
            for f in sorted(stage.rglob('*')):
                if f.is_file():
                    z.write(f, str(f.relative_to(stage)).replace('\\', '/'))

        plugins_root = plugin.parent
        entries = []
        for f in sorted(plugin.rglob('*')):
            if f.is_file():
                rel = str(f.relative_to(plugins_root)).replace('\\', '/')
                assert rel.startswith(PLUGIN_FOLDER + '/')
                assert not any(x in rel for x in ('CampusUpQA', '.save', 'Assembly-CSharp'))
                entries.append(dict(path=rel, size=f.stat().st_size, sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
        payload = {'schema': 1, 'version': version, 'files': entries}
        update = out / f'CampusMinigames-update-{version}.zip'
        with zipfile.ZipFile(update, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
            z.writestr('package.json', json.dumps(payload, ensure_ascii=False, separators=(',', ':')))
            for f in entries:
                z.write(plugins_root / f['path'], f['path'])

    feed = {'schema': 1, 'layout': LAYOUT, 'version': version,
            'url': f'https://github.com/{repo}/releases/download/v{version}/{update.name}',
            'sha256': hashlib.sha256(update.read_bytes()).hexdigest(), 'size': update.stat().st_size}
    (out / 'update.json').write_text(json.dumps(feed, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for file in (full, update):
        with zipfile.ZipFile(file) as z:
            assert z.testzip() is None
    (out / 'SHA256SUMS.txt').write_text(''.join(hashlib.sha256(f.read_bytes()).hexdigest() + '  ' + f.name + '\n' for f in (full, update, out / 'update.json')))
    print(json.dumps({'version': version, 'files': len(entries), 'full': str(full), 'update': str(update), 'feed': str(out / 'update.json')}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
