#!/usr/bin/env python3
"""检查源码引用、单 DLL 注册和组装包；可对比已有 Steam 包。只读。"""
import argparse
import hashlib
import importlib.util
import json
import re
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
REGISTRY = 'EC2BUnofficialPatch/Minigame/CustomMinigamecfg.json'
DLL = 'plugins/CampusMinigames.dll'
FORBIDDEN = {'CampusUno.dll', 'CampusMinigames.UP.dll', 'EC2BUnofficialPatch.dll', 'UP-source.zip'}


def require(value, message):
    if not value:
        raise ValueError(message)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_tool(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def inventory(root):
    return {p.relative_to(root).as_posix(): digest(p) for p in root.rglob('*')
            if p.is_file() and p.name != '.DS_Store'}


def check_registry(root):
    registry = root / REGISTRY
    rows = json.loads(registry.read_text(encoding='utf-8'))['minigames']
    require({r['id'] for r in rows} == set(range(9101, 9113)), '十二款小游戏注册编号变化')
    require(len(rows) == 12, '小游戏注册重复或遗漏')
    for row in rows:
        require(row['dll'] == '../../' + DLL, f"游戏 {row['id']} 未使用统一 DLL")
        require((registry.parent / row['dll']).resolve() == (root / DLL).resolve(), 'DLL 相对路径错误')
    return rows


def check_repository():
    rows = check_registry(ROOT / 'mod')
    build = load_tool('campus_build', ROOT / 'build.py')
    source = build.sources()
    require(source and len(source) == len(set(source)), '编译输入为空或重复')
    require(all(p.is_relative_to(ROOT / 'src') for p in source), '编译混入非运行时代码')
    require(ROOT / 'src/Games/Uno/UnoGame.cs' in source, 'UNO 未纳入统一编译')
    declarations = '\n'.join(p.read_text(encoding='utf-8') for p in source)
    for row in rows:
        require(re.search(r'\bclass\s+' + re.escape(row['class'].split('.')[-1]) + r'\b', declarations),
                '注册类型未进入编译：' + row['class'])
    for project in [*ROOT.glob('tests/*/*.csproj'), ROOT / 'tools/updater/Updater.csproj']:
        for item in ET.parse(project).iter('Compile'):
            require(list(project.parent.glob(item.attrib['Include'])), f'{project}: 源文件引用不存在 {item.attrib}')
    assembly = load_tool('campus_assemble', ROOT / 'tools/assemble_mod.py')
    for path in [*assembly.ASSET_DIRS, *assembly.README_FILES, 'assets/gomoku/audio']:
        require((ROOT / path).exists(), '打包输入不存在：' + path)
    return {'compiledSources': len(source), 'registeredGames': len(rows)}


def check_mod(mod):
    require(mod.is_dir(), '组装包不存在：' + str(mod))
    check_registry(mod)
    actual = inventory(mod)
    manifest = json.loads((mod / 'manifest.json').read_text(encoding='utf-8'))
    require(manifest['plugins'] == [DLL], 'manifest 未使用单 DLL')
    require(manifest['requires'] == ['sa.EC2B.UnofficialPatch'], '缺少 UP 前置')
    require(manifest['files'] == {k: v for k, v in actual.items() if k != 'manifest.json'}, 'manifest 文件清单或摘要不符')
    require(set(p.name for p in mod.iterdir()) == {'plugins', 'EC2BUnofficialPatch', 'Cfgs', 'readme', 'preview.jpg', 'manifest.json'}, '工坊根目录格式变化')
    require({k for k in actual if k.endswith('.dll')} == {DLL}, '存在多余插件 DLL')
    require(not any(Path(k).name in FORBIDDEN for k in actual), '包含旧 DLL 或 UP')
    return actual


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mod', type=Path)
    parser.add_argument('--reference', type=Path, help='已有 Steam 包；允许 DLL、manifest 和说明文字变化')
    args = parser.parse_args()
    result = check_repository()
    if args.mod:
        actual = check_mod(args.mod.resolve())
        result['packageFiles'] = len(actual)
        if args.reference:
            before = check_mod(args.reference.resolve())
            require(actual.keys() == before.keys(), 'Steam 包文件路径有增删')
            changed = sorted(k for k in actual if actual[k] != before[k])
            require(all(k in {DLL, 'manifest.json'} or k.startswith('readme/') for k in changed),
                    'Steam 配置或资源内容变化：' + str(changed))
            result['changedFromReference'] = changed
    elif args.reference:
        parser.error('--reference 需要同时提供 --mod')
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
