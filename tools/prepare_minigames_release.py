"""Prepare versioned full install and transactional auto-update assets. Does not publish."""
from pathlib import Path
import argparse,hashlib,json,re,zipfile
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--staging',type=Path,required=True);p.add_argument('--out',type=Path,default=root/'dist/release');a=p.parse_args()
source=(root/'src/CampusAutoUpdate.cs').read_text();version=re.search(r'const string Value="([0-9.]+)"',source).group(1);repo=re.search(r'const string Repository="([^"]+)"',source).group(1);stage=a.staging.resolve();out=a.out.resolve();out.mkdir(parents=True,exist_ok=True)
files={str(f.relative_to(stage)):hashlib.sha256(f.read_bytes()).hexdigest() for f in stage.rglob('*') if f.is_file() and f.name!='manifest.json'};manifest=json.loads((stage/'manifest.json').read_text());manifest.update(version=version,files=files);(stage/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
full=out/('StudentAge-CampusMinigames-'+version+'.zip')
with zipfile.ZipFile(full,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for f in sorted(stage.rglob('*')):
  if f.is_file():z.write(f,str(f.relative_to(stage)))
plugin=stage/'BepInEx/plugins';entries=[]
for f in sorted(plugin.rglob('*')):
 if f.is_file():
  rel=str(f.relative_to(plugin));assert rel.startswith(('CampusUno/','StudentAgeCampusMinigames/'))
  assert not any(x in rel for x in ('CampusUpQA','.save','Assembly-CSharp'))
  entries.append(dict(path=rel,size=f.stat().st_size,sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
payload={'schema':1,'version':version,'files':entries};update=out/('CampusMinigames-update-'+version+'.zip')
with zipfile.ZipFile(update,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 z.writestr('package.json',json.dumps(payload,ensure_ascii=False,separators=(',',':')))
 for f in entries:z.write(plugin/f['path'],f['path'])
feed={'schema':1,'layout':'campus-minigames-v1','version':version,'url':f'https://github.com/{repo}/releases/download/v{version}/{update.name}','sha256':hashlib.sha256(update.read_bytes()).hexdigest(),'size':update.stat().st_size}
(out/'update.json').write_text(json.dumps(feed,ensure_ascii=False,indent=2)+'\n')
for file in (full,update):
 with zipfile.ZipFile(file) as z:assert z.testzip() is None
(out/'SHA256SUMS.txt').write_text(''.join(hashlib.sha256(f.read_bytes()).hexdigest()+'  '+f.name+'\n' for f in (full,update,out/'update.json')))
print(json.dumps({'version':version,'files':len(entries),'full':str(full),'update':str(update),'feed':str(out/'update.json')},ensure_ascii=False,indent=2))
