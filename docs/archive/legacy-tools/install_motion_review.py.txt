"""Install the runtime-verified local Sanguosha presentation only; do not publish."""
from pathlib import Path
import hashlib,json,shutil,subprocess,time
root=Path(__file__).resolve().parents[2];game=root/'qa/up-runtime/steamapps/common/StudentAge';qa=game/'CampusUpQA';out=root/'qa/sanguosha-motion';src=root/'dist/up-integration-sanguosha-motion/BepInEx/plugins/StudentAgeCampusMinigames';target=Path.home()/'Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/StudentAge/BepInEx/plugins/StudentAgeCampusMinigames'
assert (qa/'success.txt').exists() and not (qa/'failed.txt').exists(),'Runtime verification did not pass'
checks=(qa/'checks.txt').read_text().splitlines()
for required in ['new general unlock follows victory without a text result dialog','public-card selection is visible','continuous play settles shenguanyu','no Unity exceptions: ']:assert required in checks,required
metrics=json.loads((qa/'motion-summary.json').read_text());assert metrics['maxHandSpeed']<4200 and metrics['maxTableSpeed']<4200 and metrics['maxExitJump']<12
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
files=[src/'CampusMinigames.UP.dll']+sorted(f for f in (src/'Sanguosha').rglob('*') if f.is_file())
for f in files:assert sha(f)==sha(game/'BepInEx/plugins/StudentAgeCampusMinigames'/f.relative_to(src)),f
commands=subprocess.check_output(['ps','-axo','command'],text=True).splitlines();real=str(target.parents[2]/'StudentAge.exe');win=r'C:\Program Files (x86)\Steam\steamapps\common\StudentAge\StudentAge.exe'
assert not any((real.lower() in c.lower() or win.lower() in c.lower()) and not any(x in c for x in ['/bin/zsh','python','rg ']) for c in commands),'The installed user game is still running'
out.mkdir(exist_ok=True);backup=out/'install-backup'/str(int(time.time()));backup.mkdir(parents=True)
for f in qa.iterdir():
 if f.is_file() and f.suffix in ['.png','.txt','.json','.csv']:shutil.copy2(f,out/f.name)
for name in ['outcome-frames','surrender-0','surrender-1','reward-hold','deal-motion','hover-motion']:shutil.copytree(qa/name,out/name,dirs_exist_ok=True)
for name in ['build.log','runtime.log']:shutil.copy2(root/'research/sanguosha/motion-fixes'/name,out/name)
for name in ['up-unity.log','up-bepinex.log']:shutil.copy2(root/'qa'/name,out/name)
records=[]
for f in files:
 rel=f.relative_to(src);dest=target/rel
 if dest.exists():old=backup/rel;old.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dest,old)
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest);assert sha(dest)==sha(f);records.append(dict(file=str(rel),sha256=sha(f)))
# Keep resource attribution beside the installed payload.
notice=target/'THIRD_PARTY_NOTICES.md'
if notice.exists():shutil.copy2(notice,backup/notice.name)
shutil.copy2(root/'THIRD_PARTY_NOTICES.md',notice)
(out/'delivery.json').write_text(json.dumps(dict(installed=str(target),build=str(src),backup=str(backup),checks=len(checks),motion=metrics,files=records,publicDistribution=False,limits=['Isolated CrossOver fixtures, not native Windows or complete natural-story acceptance','Projected card hit tests inject GUI pointer coordinates; not an unattended physical mouse test','Original recruitment effect and popup assets, custom 1v1 layout; not every original client screen']),ensure_ascii=False,indent=2)+'\n');print('INSTALLED',len(records),'FILES;',len(checks),'RUNTIME CHECKS; NO PUBLIC UPLOAD')
