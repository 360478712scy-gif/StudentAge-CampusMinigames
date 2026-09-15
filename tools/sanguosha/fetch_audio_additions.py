"""Fetch missing original voices/equipment effects for the 35-general local review."""
from pathlib import Path
import json,subprocess,concurrent.futures,hashlib
root=Path(__file__).resolve().parents[2];dst=root/'assets/sanguosha';tree=json.loads((root/'research/sanguosha/qs-tree.json').read_text());paths={e['path'] for e in tree['tree']};manifest=json.loads((dst/'sources.json').read_text());jobs=[]
for e in manifest['files']:
 if e['file'].startswith('generals/'):
  g=Path(e['file']).stem; candidates=['audio/death/nos_'+g+'.ogg','audio/death/'+g+'.ogg'];source=next((p for p in candidates if p in paths),None)
  if source is None:raise ValueError(g)
  jobs.append((source,'audio/death-'+g+'.ogg'))
for kind in ['weapon','armor','horse']:jobs.append(('audio/card/common/'+kind+'.ogg','audio/'+kind+'.ogg'))
for kind in ['injure2','injure3','standoff']:jobs.append(('audio/system/'+kind+'.ogg','audio/'+kind+'.ogg'))
for e in list(manifest['files']):
 if e['source'].startswith('audio/skill/'):
  key=Path(e['file']).stem
  p='audio/skill/'+key+'2.ogg'
  if p in paths:jobs.append((p,'audio/'+key+'-2.ogg'))
def fetch(job):
 p,rel=job;f=dst/rel
 if not f.exists():subprocess.run(['curl','-fLs','--retry','1','--max-time','25','https://raw.githubusercontent.com/Mogara/QSanguosha-v2/'+tree['sha']+'/'+p,'-o',str(f)],check=True)
 return dict(source=p,file=rel,sha256=hashlib.sha256(f.read_bytes()).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as ex:records=list(ex.map(fetch,jobs))
byfile={e['file']:e for e in manifest['files']};byfile.update({e['file']:e for e in records});manifest['files']=list(byfile.values());(dst/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n');print('Added',len(records),'audio assets')
