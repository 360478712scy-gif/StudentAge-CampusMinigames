from pathlib import Path
import argparse,subprocess,hashlib,json,shutil,zipfile
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--bepinex',type=Path,default=root/'lib/BepInEx-5.4.23.5/BepInEx/core');p.add_argument('--up',type=Path,required=True);p.add_argument('--game',type=Path,default=root/'qa-game');p.add_argument('--out',type=Path,default=root/'dist/up-integration-0.9.3');args=p.parse_args()
subprocess.run(['python3',str(root/'build.py'),'--game',str(args.game),'--bepinex',str(args.bepinex)],check=True)
managed=args.game/'StudentAge_Data/Managed';core=args.bepinex
dotnet=shutil.which('dotnet') or '/usr/local/share/dotnet/dotnet';sdk=subprocess.check_output([dotnet,'--list-sdks'],text=True).strip().splitlines()[-1];compiler=Path(sdk.split('[')[1].rstrip(']'))/sdk.split()[0]/'Roslyn/bincore/csc.dll'
out=args.out.resolve();
assert out.parent == (root/'dist').resolve() and out.name not in {'up-integration','up-integration-0.3.1'}, 'Use a new staging directly under dist; preserve released packages'
if out.exists():shutil.rmtree(out)
plugin=out/'BepInEx/plugins/CampusUno';mod=out/'ModAuthorTemplate/CampusMinigames';ext=out/'BepInEx/plugins/StudentAgeCampusMinigames';ext.mkdir(parents=True,exist_ok=True);plugin.mkdir(parents=True,exist_ok=True)
shutil.copy2(root/'dist/BepInEx/plugins/CampusUno/CampusUno.dll',plugin/'CampusUno.dll')
refs=list(managed.glob('*.dll'))+[core/'BepInEx.dll',core/'0Harmony.dll',args.up,plugin/'CampusUno.dll']
cmd=[dotnet,str(compiler),'-nologo','-target:library','-nostdlib+','-langversion:9','-optimize+','-out:'+str(ext/'CampusMinigames.UP.dll')]+['-r:'+str(r) for r in refs]+[str(r) for r in (root/'integration/src').glob('*.cs')]+[str(r) for r in (root/'bubble/Runtime').glob('*.cs')]+[str(r) for r in (root/'shared/Runtime').glob('*.cs')]+[str(r) for r in (root/'shared/Unity').glob('*.cs')]+[str(r) for r in (root/'retro/Runtime').glob('*.cs')]+[str(root/'retro/Unity/PixelCanvas.cs')]+[str(r) for r in (root/'mahjong/Runtime').glob('*.cs')]+[str(r) for r in (root/'mahjong/Unity').glob('*.cs')]
subprocess.run(cmd,check=True)
shutil.copytree(root/'integration/mod/Cfgs',mod/'Cfgs',dirs_exist_ok=True)
shutil.copytree(root/'integration/mod/Cfgs',ext/'Cfgs',dirs_exist_ok=True)
shutil.copy2(root/'integration/mod/EC2BUnofficialPatch/CustomMinigamecfg.json',ext/'CustomMinigamecfg.json')
shutil.copytree(root/'assets/music',ext/'Music',dirs_exist_ok=True)
shutil.copytree(root/'assets/card-audio',ext/'CardAudio',dirs_exist_ok=True)
audio=ext/'Audio';audio.mkdir(exist_ok=True)
for f in (root/'h5/gomoku/assets/audio').iterdir():shutil.copy2(f,audio/f.name)
shutil.copytree(root/'assets/bubble/audio',ext/'BubbleAudio',dirs_exist_ok=True)
shutil.copytree(root/'assets/playing-cards',ext/'PlayingCards',dirs_exist_ok=True)
shutil.copytree(root/'assets/arcade',ext/'Arcade',dirs_exist_ok=True)
shutil.copytree(root/'retro/assets',ext/'Retro',dirs_exist_ok=True)
shutil.copytree(root/'assets/mahjong',ext/'Mahjong',dirs_exist_ok=True)
shutil.copytree(root/'assets/nds',ext/'Nds',dirs_exist_ok=True)
shutil.copy2(root/'integration/INSTALL.md',out/'安装与角色绑定.md')
shutil.copy2(root/'integration/CONFIGURATION.md',out/'小游戏配置说明.md')
shutil.copy2(root/'updater/AUTO_UPDATE.md',out/'自动更新说明.md')
shutil.copytree(root/'integration/config',out/'配置示例',dirs_exist_ok=True)
shutil.copy2(root/'mahjong/README.md',out/'课间麻将说明.md')
shutil.copy2(root/'nds/README.md',out/'NDS掌机说明.md')
shutil.copy2(root/'THIRD_PARTY_NOTICES.md',out/'THIRD_PARTY_NOTICES.md')
manifest={'version':'0.9.3','upReferenceSHA256':hashlib.sha256(args.up.read_bytes()).hexdigest(),'gameAssemblySHA256':hashlib.sha256((managed/'Assembly-CSharp.dll').read_bytes()).hexdigest(),'files':{str(f.relative_to(out)):hashlib.sha256(f.read_bytes()).hexdigest() for f in out.rglob('*') if f.is_file() and f.name!='manifest.json'}}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2))
print(ext/'CampusMinigames.UP.dll')
