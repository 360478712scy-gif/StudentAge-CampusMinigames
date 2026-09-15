"""Import original decadeUI judgment animations rendered from the pinned 3.6 skeleton."""
from pathlib import Path
from PIL import Image,ImageChops
import json,hashlib,math
root=Path(__file__).resolve().parents[2];src=root/'research/sanguosha/judgment';out=root/'assets/sanguosha/official';manifest=json.loads((out/'sources.json').read_text());meta=json.loads((out/'effects/metadata.json').read_text());records={e['file']:e for e in manifest['files'] if not e['file'].startswith('judgment/')}
for f in (out/'judgment').glob('skill_judge_*.png'):f.unlink()
for key,anim in [('judgment-reveal','play'),('judgment-success','play4'),('judgment-failure','play5')]:
 frames=[Image.open(f).convert('RGBA') for f in sorted((src/'frames'/anim).glob('*.png'))];mask=Image.new('L',(512,512))
 for im in frames:mask=ImageChops.lighter(mask,im.getchannel('A'))
 box=mask.getbbox();x,y,r,b=box;w,h=r-x,b-y;cols=8;rows=math.ceil(len(frames)/cols);sheet=Image.new('RGBA',((w+2)*cols,(h+2)*rows))
 for i,im in enumerate(frames):sheet.paste(im.crop(box),(i%cols*(w+2)+1,i//cols*(h+2)+1))
 p=out/'effects'/(key+'.png');sheet.save(p);meta[key]=dict(frames=len(frames),columns=cols,rows=rows,width=w,height=h,crop=list(box),duration=2,stageWidth=512,stageHeight=512)
 rel=str(p.relative_to(out));records[rel]={'file':rel,'repository':'https://github.com/diandian157/decadeUI','revision':'30dbc8911ea9d27cf674c9be50e8abd663e8a8f0','source':'assets/animation/effect_panding.skel','sourceSHA256':hashlib.sha256((src/'effect_panding.skel').read_bytes()).hexdigest(),'animation':anim,'render':meta[key],'sha256':hashlib.sha256(p.read_bytes()).hexdigest()};print(key,box)
manifest['files']=list(records.values());(out/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n');(out/'effects/metadata.json').write_text(json.dumps(meta,indent=2)+'\n')
