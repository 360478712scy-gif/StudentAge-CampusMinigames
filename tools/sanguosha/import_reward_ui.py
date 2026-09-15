"""Import original selection-window artwork and general recruitment effect, without its placeholder portrait."""
from pathlib import Path
from PIL import Image,ImageChops
import json,hashlib,math,shutil
root=Path(__file__).resolve().parents[2];res=root/'research/sanguosha/official-client';out=root/'assets/sanguosha/official';manifest=json.loads((out/'sources.json').read_text());records={e['file']:e for e in manifest['files']};meta=json.loads((out/'effects/metadata.json').read_text())
def record(dest,source,**extra):
 records[str(dest.relative_to(out))]=dict(file=str(dest.relative_to(out)),source=source,sha256=hashlib.sha256(dest.read_bytes()).hexdigest(),**extra)
p='assets/game/skill_window/newSkillWindow_bg.png';d=out/'dialogs/selection-bg.png';d.parent.mkdir(exist_ok=True);shutil.copy2(res/'res'/p,d);record(d,'https://web.sanguosha.com/10/pc/res/'+p)
a=json.loads((res/'res/assets/window/getGeneral.atlas').read_text());sheet=Image.open(res/'res/assets/window/getGeneral.png')
for name in ['getGeneralTitle.png','collect_general_frame.png','collect_general_name_bg.png']:
 v=a['frames'][name];f=v['frame'];im=sheet.crop((f['x'],f['y'],f['x']+f['w'],f['y']+f['h']));d=out/'dialogs'/name;im.save(d);record(d,'https://web.sanguosha.com/10/pc/res/assets/window/getGeneral.atlas',region=name)
for name in ['general-unlock-start','general-unlock-loop']:
 folder=res/'effects'/name;frames=[Image.open(f).resize((768,768),Image.Resampling.LANCZOS) for f in sorted(folder.glob('*.png'))];m=json.loads((folder/'meta.json').read_text());mask=Image.new('L',frames[0].size)
 for im in frames:mask=ImageChops.lighter(mask,im.getchannel('A'))
 box=mask.getbbox();x,y,r,b=box;w,h=r-x,b-y;cols=math.ceil(math.sqrt(len(frames)*(h+2)/(w+2)));rows=math.ceil(len(frames)/cols);atlas=Image.new('RGBA',((w+2)*cols,(h+2)*rows));assert max(atlas.size)<=8192
 for i,im in enumerate(frames):atlas.paste(im.crop(box),(i%cols*(w+2)+1,i//cols*(h+2)+1))
 d=out/'effects'/(name+'.png');atlas.save(d);meta[name]=dict(frames=len(frames),columns=cols,rows=rows,width=w,height=h,crop=list(box),stageWidth=768,stageHeight=768,duration=m['duration']/1000);record(d,'https://web.sanguosha.com/10/pc/res/assets/animate/generalWindow/sf_dhua_zhaomuwj_wujiang.sk',animation=m['animation'],transform='Laya original timeline; wujiang placeholder slot hidden for runtime portrait; 1024 canvas at 0.6 then 768 export',render=meta[name]);print(name,atlas.size)
manifest['files']=list(records.values());(out/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n');(out/'effects/metadata.json').write_text(json.dumps(meta,indent=2)+'\n')
