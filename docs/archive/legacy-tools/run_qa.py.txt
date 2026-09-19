from pathlib import Path
import subprocess,shutil,time,argparse
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--up',type=Path,required=True);p.add_argument('--nographics',action='store_true');p.add_argument('--reuse-build',action='store_true');p.add_argument('--plugin-only',action='store_true');p.add_argument('--staging',type=Path,default=root/'dist/up-integration-0.4.1');p.add_argument('--qa-source',type=Path,default=root/'integration/RuntimeQA.cs');p.add_argument('--timeout',type=float,default=200);a=p.parse_args()
if a.staging.resolve()!=(root/'dist/up-integration').resolve() and not a.reuse_build:p.error('Custom staging requires --reuse-build to preserve verified packages')
game=root/'qa/up-runtime/steamapps/common/StudentAge';assert game.is_dir() and str(game).startswith(str(root/'qa'))
if not a.reuse_build:subprocess.run(['python3',str(root/'integration/build.py'),'--up',str(a.up),'--out',str(a.staging)],check=True)
plugins=game/'BepInEx/plugins';shutil.rmtree(plugins);shutil.copytree(a.staging/'BepInEx/plugins',plugins);shutil.copy2(a.up,plugins/'EC2BUnofficialPatch.dll')
if (a.up.parent/'LFBetterAudio.dll').exists():shutil.copy2(a.up.parent/'LFBetterAudio.dll',plugins/'LFBetterAudio.dll')
# Also supply an isolated Workshop registration root for pre-plugin-root UP revisions.
workshop=game.parents[1]/'workshop/content/1991040/91020001';
if a.plugin_only:
 if workshop.exists():shutil.rmtree(workshop)
else:
 workshop.mkdir(parents=True,exist_ok=True);shutil.copytree(plugins/'StudentAgeCampusMinigames',workshop/'EC2BUnofficialPatch',dirs_exist_ok=True)
config=game/'BepInEx/config/studio.studentage.campusuno.cfg'
if config.exists():config.write_text(config.read_text().replace('SmokeTest = true','SmokeTest = false'))
bep=game/'BepInEx/config/BepInEx.cfg';bep.write_text(bep.read_text().replace('HideManagerGameObject = false','HideManagerGameObject = true'))
qa=game/'CampusUpQA'
if qa.exists():
 previous=root/'qa'/('up-previous-'+str(int(time.time())));shutil.move(qa,previous)
 for name in ['up-unity.log','up-bepinex.log','up-run.log']:
  source=root/'qa'/name
  if source.exists():shutil.copy2(source,previous/name)
(qa/'Saves').mkdir(parents=True);shutil.copy2(root/'qa-game/CampusUnoQA/save-fixture/fixture.save',qa/'Saves/fixture.save')
dotnet=shutil.which('dotnet');sdk=subprocess.check_output([dotnet,'--list-sdks'],text=True).strip().splitlines()[-1];csc=Path(sdk.split('[')[1].rstrip(']'))/sdk.split()[0]/'Roslyn/bincore/csc.dll'
refs=list((game/'StudentAge_Data/Managed').glob('*.dll'))+[game/'BepInEx/core/BepInEx.dll',game/'BepInEx/core/0Harmony.dll']+[plugins/'EC2BUnofficialPatch.dll',plugins/'CampusUno/CampusUno.dll',plugins/'StudentAgeCampusMinigames/CampusMinigames.UP.dll']
subprocess.run([dotnet,str(csc),'-nologo','-target:library','-nostdlib+','-langversion:9','-out:'+str(plugins/'CampusUpQA.dll')]+['-r:'+str(f) for f in refs]+[str(a.qa_source)],check=True)
wine=Path.home()/'Applications/CrossOver 1.25.27.app/Contents/SharedSupport/CrossOver/bin/wine';log=root/'qa/up-unity.log'
cmd=[str(wine),'--bottle','Steam','--dll','winhttp=n,b',str(game/'StudentAge.exe'),'-screen-fullscreen','0','-screen-width','1440','-screen-height','900','-logFile','Z:'+str(log).replace('/','\\')]
if a.nographics:cmd+=['-batchmode','-nographics']
with (root/'qa/up-launch.log').open('w') as f:
 proc=subprocess.Popen(cmd,cwd=game,stdout=f,stderr=subprocess.STDOUT);(root/'qa/up-process.txt').write_text(str(proc.pid))
 deadline=time.monotonic()+a.timeout
 # Wine may hand off to the actual game and exit its launcher first.
 while time.monotonic()<deadline and not (qa/'success.txt').exists() and not (qa/'failed.txt').exists():time.sleep(1)
 # Some Wine/Steam wrappers survive Application.Quit. Stop only this QA executable.
 processes=subprocess.check_output(['ps','-axo','pid,command'],text=True).splitlines()
 needle=str(game/'StudentAge.exe');wine_needle='Y:'+str(game/'StudentAge.exe').removeprefix(str(Path.home())).replace('/','\\')
 import os,signal
 for line in processes:
  fields=line.strip().split(None,1)
  if len(fields)==2 and (needle in fields[1] or wine_needle in fields[1]) and not any(x in fields[1] for x in ['python','/bin/zsh']):
   try:os.kill(int(fields[0]),signal.SIGTERM)
   except ProcessLookupError:pass
 try:proc.wait(timeout=10)
 except subprocess.TimeoutExpired:proc.terminate()

shutil.copy2(game/'BepInEx/LogOutput.log',root/'qa/up-bepinex.log')
assert (qa/'success.txt').exists(),'Runtime did not pass; inspect '+str(qa)
print((qa/'checks.txt').read_text())
