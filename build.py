#!/usr/bin/env python3
"""把全部源码编译成单个 CampusMinigames.dll。

引用本机的游戏程序集、BepInEx 5 核心程序集和 UP（EC2BUnofficialPatch.dll）；
只编译，不修改游戏，不打包。输出 dist/build/CampusMinigames.dll。
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parent
VERSION = re.search(r'const string Value="([0-9.]+)"', (ROOT / 'src/Update/CampusAutoUpdate.cs').read_text()).group(1)
DEFAULT_GAME = Path.home() / 'Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/StudentAge'
ASSEMBLY = 'CampusMinigames'

# 所有运行时代码统一编入同一个 DLL；UNO 与其他游戏一样位于 src/Games。

def find_csc(dotnet):
    sdks = subprocess.check_output([dotnet, '--list-sdks'], text=True).strip().splitlines()
    sdk = sdks[-1]
    return Path(sdk.split('[')[1].rstrip(']')) / sdk.split()[0] / 'Roslyn/bincore/csc.dll'


def sources():
    return sorted((ROOT / 'src').rglob('*.cs'))


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--game', type=Path, default=DEFAULT_GAME, help='游戏根目录（含 StudentAge_Data/Managed）')
    p.add_argument('--bepinex', type=Path, default=None, help='BepInEx/core 目录；默认取游戏自带的')
    p.add_argument('--up', type=Path, required=True, help='用于编译引用的 EC2BUnofficialPatch.dll（不会被打包）')
    p.add_argument('--out', type=Path, default=ROOT / 'dist/build')
    p.add_argument('--helper', type=Path, default=None, help='已编译好的 CampusMinigames.Updater.exe；给出时跳过 dotnet build')
    p.add_argument('--deploy', type=Path, default=None, help='将 DLL 复制进指定目录；工坊填 Mod/plugins，手动安装填 BepInEx/plugins/CampusMinigames')
    args = p.parse_args()

    managed = args.game / 'StudentAge_Data/Managed'
    core = args.bepinex or (args.game / 'BepInEx/core')
    assert (managed / 'Assembly-CSharp.dll').is_file(), 'Game assemblies missing: ' + str(managed)
    assert (core / 'BepInEx.dll').is_file(), 'BepInEx 5 core assemblies missing: ' + str(core)
    assert args.up.is_file(), 'UP dll missing: ' + str(args.up)

    dotnet = shutil.which('dotnet') or '/usr/local/share/dotnet/dotnet'
    helper = args.helper
    if helper is None:
        subprocess.run([dotnet, 'build', str(ROOT / 'tools/updater/Updater.csproj'), '-c', 'Release', '--nologo'], check=True)
        helper = ROOT / 'tools/updater/bin/Release/net472/CampusMinigames.Updater.exe'
    assert helper.is_file(), 'Updater helper missing: ' + str(helper)

    out = args.out.resolve()
    out.mkdir(parents=True, exist_ok=True)
    dll = out / (ASSEMBLY + '.dll')
    refs = list(managed.glob('*.dll')) + [core / 'BepInEx.dll', core / '0Harmony.dll', args.up.resolve()]
    cmd = [dotnet, str(find_csc(dotnet)), '-nologo', '-target:library', '-nostdlib+', '-langversion:9', '-optimize+', '-out:' + str(dll)]
    cmd += ['-r:' + str(r) for r in refs]
    cmd += ['-resource:' + str(ROOT / 'assets/classroom-desk.png') + ',CampusUno.Desk']
    cmd += ['-resource:' + str(helper) + ',CampusMinigames.Updater']
    cmd += [str(f) for f in sources()]
    subprocess.run(cmd, check=True)

    manifest = {
        'pluginVersion': VERSION,
        'assembly': ASSEMBLY,
        'gameAssemblySHA256': hashlib.sha256((managed / 'Assembly-CSharp.dll').read_bytes()).hexdigest(),
        'upReferenceSHA256': hashlib.sha256(args.up.read_bytes()).hexdigest(),
        'pluginSHA256': hashlib.sha256(dll.read_bytes()).hexdigest(),
        'validation': 'Compilation only; see QA report for runtime evidence.',
    }
    (out / 'build-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print('Built', dll)

    if args.deploy is not None:
        target_dir = args.deploy
        target_dir.mkdir(parents=True, exist_ok=True)
        target = target_dir / (ASSEMBLY + '.dll')
        shutil.copy2(dll, target)
        print('Deployed', target)
        manifest['deployedTo'] = str(target)
        manifest['deployedSHA256'] = hashlib.sha256(target.read_bytes()).hexdigest()
        (out / 'build-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
        if manifest['deployedSHA256'] != manifest['pluginSHA256']:
            raise SystemExit('Deploy verification failed: hash mismatch')


if __name__ == '__main__':
    main()
