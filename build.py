#!/usr/bin/env python3
"""Compile against the user's local Unity/Mono assemblies. Does not modify the game."""
from pathlib import Path
import argparse
import subprocess
import shutil
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parent
VERSION = re.search(r'const string Value="([0-9.]+)"', (ROOT / 'src/CampusAutoUpdate.cs').read_text()).group(1)
DEFAULT_GAME = Path.home() / 'Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/StudentAge'
p = argparse.ArgumentParser()
p.add_argument('--game', type=Path, default=DEFAULT_GAME)
p.add_argument('--bepinex', type=Path, default=ROOT / 'lib/BepInEx-5.4.23.5/BepInEx/core')
args = p.parse_args()
managed = args.game / 'StudentAge_Data/Managed'
dotnet = shutil.which('dotnet') or '/usr/local/share/dotnet/dotnet'
sdks = subprocess.check_output([dotnet, '--list-sdks'], text=True).strip().splitlines()
sdk = sdks[-1].split()[0]
sdk_root = Path(sdks[-1].split('[')[1].rstrip(']'))
compiler = sdk_root / sdk / 'Roslyn/bincore/csc.dll'
out = ROOT / 'dist/BepInEx/plugins/CampusUno'
out.mkdir(parents=True, exist_ok=True)
assert (managed / 'Assembly-CSharp.dll').is_file(), 'Game assemblies missing'
assert (args.bepinex / 'BepInEx.dll').is_file(), 'BepInEx 5 core assemblies missing'
refs = list(managed.glob('*.dll')) + [args.bepinex / 'BepInEx.dll', args.bepinex / '0Harmony.dll']
subprocess.run([dotnet,'build',str(ROOT/'updater/Helper/Updater.csproj'),'-c','Release','--nologo'],check=True)
cmd = [dotnet, str(compiler), '-nologo', '-target:library', '-nostdlib+', '-langversion:9', '-optimize+', '-out:' + str(out / 'CampusUno.dll')]
cmd += ['-r:' + str(f) for f in refs]
cmd += ['-resource:' + str(ROOT / 'assets/classroom-desk.png') + ',CampusUno.Desk']
cmd += ['-resource:'+str(ROOT/'updater/Helper/bin/Release/net472/CampusMinigames.Updater.exe')+',CampusMinigames.Updater']
cmd += [str(f) for f in sorted((ROOT / 'src').glob('*.cs'))]+[str(f) for f in (ROOT/'updater/Core').glob('*.cs')]
subprocess.run(cmd, check=True)
manifest = {'pluginVersion': VERSION, 'gameAssemblySHA256': hashlib.sha256((managed / 'Assembly-CSharp.dll').read_bytes()).hexdigest(), 'pluginSHA256': hashlib.sha256((out / 'CampusUno.dll').read_bytes()).hexdigest(), 'bepinex': '5.4.23.5', 'validation': 'Compilation only; see QA report for runtime evidence.'}
(ROOT / 'dist/build-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print('Built', out / 'CampusUno.dll')
