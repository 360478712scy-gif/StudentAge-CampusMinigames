"""Import the user's selected Noname decadeUI caise skin at native resolution.
Only art assets; no upstream game code. Rebuild physical ranks/suits at native size.
"""
from pathlib import Path
import json, re, subprocess, hashlib, concurrent.futures
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[2]
out=root/'assets/sanguosha/hd';out.mkdir(exist_ok=True)
ref=json.loads((root/'research/sanguosha/decade-reference/tree.json').read_text());paths={e['path'] for e in ref['tree']}
mapping='slash:sha jink:shan peach:tao wine:jiu supply_shortage:bingliang drowning:shuiyanqijun crossbow:zhuge double_sword:cixiong qinggang_sword:qinggang blade:qinglong spear:zhangba axe:guanshi halberd:fangtian kylin_bow:qilin eight_diagram:bagua renwang_shield:renwang ice_sword:hanbing jueying:jueying dilu:dilu zhuahuangfeidian:zhuahuang chitu:chitu dayuan:dawan zixing:zixin amazing_grace:wugu god_salvation:taoyuan savage_assault:nanman archery_attack:wanjian duel:juedou ex_nihilo:wuzhong snatch:shunshou dismantlement:guohe collateral:jiedao nullification:wuxie indulgence:lebu lightning:shandian'
def fetch(pair):
 key,name=pair.split(':');source='image/card-skins/caise/'+name+'.webp';assert source in paths,source
 dest=out/'art'/f'{key}.webp';dest.parent.mkdir(exist_ok=True)
 if not dest.exists():subprocess.run(['curl','-fsSL','--retry','2','--max-time','40','https://raw.githubusercontent.com/diandian157/decadeUI/'+ref['sha']+'/'+source,'-o',str(dest)],check=True)
 im=Image.open(dest).convert('RGBA');(out/'cards').mkdir(exist_ok=True);im.save(out/'cards'/f'{key}.png')
 return dict(file=str(dest.relative_to(out)),repository='https://github.com/diandian157/decadeUI',revision=ref['sha'],source=source,nativeSize=im.size,sha256=hashlib.sha256(dest.read_bytes()).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:records=list(pool.map(fetch,mapping.split()))
font='/System/Library/Fonts/Supplemental/Times New Roman Bold.ttf'
assert Path(font).exists()
faces=set(re.findall(r'new Card\(\d+,"(\w+)",Suit\.(\w+),(\d+)\)',(root/'src/Games/Sanguosha/Deck.cs').read_text()))
(out/'faces').mkdir(exist_ok=True)
for kind,suit,rank in sorted(faces):
 im=Image.open(out/'cards'/f'{kind}.png').convert('RGBA');w,h=im.size;d=ImageDraw.Draw(im);ink=(142,20,15,255) if suit in ('Heart','Diamond') else (35,28,23,255)
 size=round(w*.115);f=ImageFont.truetype(font,size);num={'1':'A','11':'J','12':'Q','13':'K'}.get(rank,rank)
 d.text((w*.068,h*.019),num,font=f,fill=ink,anchor='mt',stroke_width=0)
 # Vector suit silhouettes keep these tiny marks crisp without stretching 13px atlas glyphs.
 cx=w*.068;cy=h*.127;r=w*.026
 if suit=='Diamond':d.polygon([(cx,cy-r*1.3),(cx+r,cy),(cx,cy+r*1.3),(cx-r,cy)],fill=ink)
 elif suit=='Heart':
  d.ellipse((cx-r,cy-r,cx,cy),fill=ink);d.ellipse((cx,cy-r,cx+r,cy),fill=ink);d.polygon([(cx-r,cy-r*.45),(cx+r,cy-r*.45),(cx,cy+r*1.15)],fill=ink)
 elif suit=='Spade':
  d.polygon([(cx,cy-r*1.3),(cx-r,cy),(cx+r,cy)],fill=ink);d.ellipse((cx-r,cy-r*.4,cx,cy+r*.65),fill=ink);d.ellipse((cx,cy-r*.4,cx+r,cy+r*.65),fill=ink);d.polygon([(cx,cy),(cx-r*.5,cy+r*1.1),(cx+r*.5,cy+r*1.1)],fill=ink)
 else:
  for dx,dy in [(0,-.55),(-.6,.3),(.6,.3)]:d.ellipse((cx+(dx-.6)*r,cy+(dy-.6)*r,cx+(dx+.6)*r,cy+(dy+.6)*r),fill=ink)
  d.polygon([(cx,cy),(cx-r*.5,cy+r*1.3),(cx+r*.5,cy+r*1.3)],fill=ink)
 dest=out/'faces'/f'{kind}-{suit.lower()}-{rank}.png';im.save(dest)
 records.append(dict(file=str(dest.relative_to(out)),composition=f'art/{kind}.webp + native vector suit + Times New Roman rank',size=im.size,sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
(out/'sources.json').write_text(json.dumps(dict(note='User-selected caise art. Original Sanguosha artwork rights retained; decadeUI upstream license in ../decade/decadeUI-LICENSE.txt. No upscaling, no upstream JS.',files=records),ensure_ascii=False,indent=2)+'\n')
print('HD_CARDS_OK',len(faces),'physical faces',len(mapping.split()),'kinds')
