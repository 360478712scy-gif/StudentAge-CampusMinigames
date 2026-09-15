"""Pinned visual assets for local Steam/decade-style table review; no upstream code."""
from pathlib import Path
import json,subprocess,hashlib,concurrent.futures
root=Path(__file__).resolve().parents[2];dst=root/'assets/sanguosha/decade';dst.mkdir(exist_ok=True)
ref=root/'research/sanguosha/decade-reference'
a=json.loads((ref/'tree.json').read_text());b=json.loads((ref/'noname-tree.json').read_text());paths={e['path'] for e in b['tree']};requests=[]
def add(repo,sha,source,out):requests.append((repo,sha,source,out))
for name in ['border_hp','border_hp1','border_camp','border_camp1','border_diwen','button','button_normal','button_disable','card_count','equip1','equip2','equip3','equip4','equipHand']+[f'name_{k}' for k in ['wei','shu','wu','qun','shen']]:add('diandian157/decadeUI',a['sha'],'image/styles/decade/'+name+'.png',name+'.png')
for i in range(1,5):
 add('diandian157/decadeUI',a['sha'],f'ui/assets/skill/shizhounian/btnn{i}.png',f'skill{i}.png')
 add('libnoname/noname',b['sha'],f'apps/core/theme/style/hp/image/round{i}.png',f'jade{i}.png')
add('libnoname/noname',b['sha'],'apps/core/image/background/ol_bg.jpg','table.jpg')
add('libnoname/noname',b['sha'],'apps/core/theme/style/card/image/ol.png','paper.png')
mapping='slash:sha jink:shan peach:tao crossbow:zhuge double_sword:cixiong qinggang_sword:qinggang blade:qinglong spear:zhangba axe:guanshi halberd:fangtian kylin_bow:qilin eight_diagram:bagua renwang_shield:renwang ice_sword:hanbing jueying:jueying dilu:dilu zhuahuangfeidian:zhuahuang chitu:chitu dayuan:dawan zixing:zixin amazing_grace:wugu god_salvation:taoyuan savage_assault:nanman archery_attack:wanjian duel:juedou ex_nihilo:wuzhong snatch:shunshou dismantlement:guohe collateral:jiedao nullification:wuxie indulgence:lebu lightning:shandian'
for pair in mapping.split():
 key,name=pair.split(':');s='apps/core/image/card/'+name+'.png'
 assert s in paths,s
 add('libnoname/noname',b['sha'],s,'cards/'+key+'.png')
add('diandian157/decadeUI',a['sha'],'LICENSE','decadeUI-LICENSE.txt')
lic=next(e['path'] for e in b['tree'] if e['path'] in ['LICENSE','LICENSE.txt','LICENSE.md'])
add('libnoname/noname',b['sha'],lic,'noname-LICENSE.txt')
def fetch(args):
 repo,sha,source,out=args;f=dst/out;f.parent.mkdir(exist_ok=True,parents=True)
 if not f.exists():subprocess.run(['curl','-fsSL','--retry','2','https://raw.githubusercontent.com/'+repo+'/'+sha+'/'+source,'-o',str(f)],check=True)
 return dict(repository='https://github.com/'+repo,revision=sha,source=source,file=out,sha256=hashlib.sha256(f.read_bytes()).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:records=list(pool.map(fetch,requests))
(dst/'sources.json').write_text(json.dumps({'note':'Art remains attributed to original Sanguosha rights holders. Upstream resource licenses retained. No upstream JavaScript/CSS is included.','files':records},ensure_ascii=False,indent=2)+'\n')
print('Imported',len(records),'assets')
