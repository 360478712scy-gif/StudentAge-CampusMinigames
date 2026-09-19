"""Install only the verified local Sanguosha audio/icon/result payload; never publish."""
from pathlib import Path
import hashlib,json,shutil,subprocess,time
root=Path(__file__).resolve().parents[2];qa=root/'qa/up-runtime/steamapps/common/StudentAge/CampusUpQA';out=root/'qa/sanguosha-audio';src=root/'dist/up-integration-sanguosha-audio/BepInEx/plugins/StudentAgeCampusMinigames';target=Path.home()/'Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/StudentAge/BepInEx/plugins/StudentAgeCampusMinigames'
assert (qa/'success.txt').exists();checks=(qa/'checks.txt').read_text().splitlines();assert len(checks)==57 and 'surrender skips first blood 1' in checks
processes=subprocess.check_output(['ps','-axo','command'],text=True).splitlines();assert not any('StudentAge.exe' in s and '/bin/zsh' not in s and 'rg ' not in s and 'python' not in s for s in processes),'Game still running'
out.mkdir(exist_ok=True);backup=out/'install-backup'/str(int(time.time()));backup.mkdir(parents=True)
for f in qa.iterdir():
 if f.is_file() and f.suffix in ['.png','.txt']:shutil.copy2(f,out/f.name)
shutil.copytree(qa/'outcome-frames',out/'outcome-frames',dirs_exist_ok=True)
for name in ['build.log','runtime.log','rules.log']:shutil.copy2(root/'research/sanguosha/audio-additions'/name,out/name)
for name in ['up-unity.log','up-bepinex.log']:shutil.copy2(root/'qa'/name,out/name)
files=[src/'CampusMinigames.UP.dll']+sorted(f for f in (src/'Sanguosha').rglob('*') if f.is_file());records=[]
for f in files:
 rel=f.relative_to(src);dest=target/rel
 if dest.exists():old=backup/rel;old.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dest,old)
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest);sha=hashlib.sha256(f.read_bytes()).hexdigest();assert hashlib.sha256(dest.read_bytes()).hexdigest()==sha;records.append(dict(file=str(rel),sha256=sha))
(out/'delivery.json').write_text(json.dumps(dict(installed=str(target),build=str(src),backup=str(backup),checks=len(checks),files=records,publicDistribution=False,limits=['Isolated CrossOver fixture, not native Windows or complete natural-story acceptance','35-general audio, existing 1v1 rules; not every original client skin or multiplayer voice']),ensure_ascii=False,indent=2)+'\n')
print('INSTALLED',len(records),'FILES;',len(checks),'RUNTIME CHECKS; NO PUBLIC UPLOAD')
