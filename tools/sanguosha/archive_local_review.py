"""Archive passing local review evidence and install only the reviewed Sanguosha payload."""
from pathlib import Path
import subprocess,shutil,json,hashlib,os,time
root=Path(__file__).resolve().parents[2];qa=root/'qa/up-runtime/steamapps/common/StudentAge/CampusUpQA';out=root/'qa/sanguosha-official';source=root/'dist/up-integration-sanguosha-official/BepInEx/plugins/StudentAgeCampusMinigames';target=Path.home()/'Library/Application Support/CrossOver/Bottles/Steam/drive_c/Program Files (x86)/Steam/steamapps/common/StudentAge/BepInEx/plugins/StudentAgeCampusMinigames'
assert (qa/'success.txt').exists();checks=(qa/'checks.txt').read_text();assert 'converted cards retain their printed art, rank and suit' in checks
processes=subprocess.check_output(['ps','-axo','command'],text=True).splitlines()
assert not any('StudentAge.exe' in s and 'qa/up-runtime' not in s and 'qa\\up-runtime' not in s and '/bin/zsh' not in s and 'rg ' not in s for s in processes),'A game process is still running; do not modify its loaded plugin'
out.mkdir(exist_ok=True);backup=out/'install-backup'/str(int(time.time()));backup.mkdir(parents=True)
for f in list(qa.glob('sanguosha-*.png'))+[qa/'checks.txt',qa/'success.txt']:
 shutil.copy2(f,out/f.name)
for name in ['hand-motion','selection-motion','target-motion','table-motion']:
 d=out/name;d.mkdir(exist_ok=True)
 for f in (qa/name).iterdir():
  dest=d/f.name
  if dest.exists():dest.unlink()
  os.link(f,dest)
for name in ['build.log','runtime.log']:shutil.copy2(root/'research/sanguosha/official-client'/name,out/name)
for name in ['up-unity.log','up-bepinex.log']:shutil.copy2(root/'qa'/name,out/name)
files=[source/'CampusMinigames.UP.dll']+sorted(f for f in (source/'Sanguosha').rglob('*') if f.is_file());records=[]
for f in files:
 relative=f.relative_to(source);dest=target/relative
 if dest.exists():
  old=backup/relative;old.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dest,old)
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest);sha=hashlib.sha256(f.read_bytes()).hexdigest();assert hashlib.sha256(dest.read_bytes()).hexdigest()==sha;records.append({'file':str(relative),'sha256':sha})
(out/'delivery.json').write_text(json.dumps({'installed':str(target),'build':str(source),'backup':str(backup),'checks':len(checks.splitlines()),'files':records,'publicDistribution':False,'limits':['CrossOver isolated fixture, not native Windows or full natural-story acceptance','Reference background texture not an exact match','1v1 seating and limited skill animations']},ensure_ascii=False,indent=2)+'\n')
public=root.parent/'student-age-minigames-github'
for f in list((root/'integration/src').glob('Sanguosha*.cs'))+[root/'THIRD_PARTY_NOTICES.md',root/'sanguosha/README.md']+list((root/'tools/sanguosha').glob('import_*.py')):
 dest=public/f.relative_to(root);dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,dest)
shutil.copytree(root/'assets/sanguosha/official',public/'assets/sanguosha/official',dirs_exist_ok=True)
print('INSTALLED_VERIFIED',len(records),'CHECKS',len(checks.splitlines()),'PUBLIC_PUSH_NONE')
