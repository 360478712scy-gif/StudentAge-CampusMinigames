"""Import original audio and result-animation frames for private runtime review."""
from pathlib import Path
from PIL import Image,ImageChops,ImageDraw
import json,hashlib,subprocess,concurrent.futures
root=Path(__file__).resolve().parents[2];out=root/'assets/sanguosha';src=root/'research/sanguosha';official=out/'official';manifest=json.loads((official/'sources.json').read_text());records=[]
# The final glyph is cropped from the official logo, with previous glyph/trademark masked out.
im=Image.open(src/'audio-additions/logo.png');mask=Image.new('L',im.size);ImageDraw.Draw(mask).polygon([(180,45),(198,8),(228,3),(238,30),(241,51),(256,71),(253,97),(299,116),(298,137),(198,137),(171,114),(180,80)],fill=255);ImageDraw.Draw(mask).rectangle((237,31,258,49),fill=0);im.putalpha(ImageChops.multiply(im.getchannel('A'),mask));glyph=im.crop((170,3,300,139));glyph.thumbnail((146,148),Image.Resampling.LANCZOS);badge=Image.new('RGBA',(192,192));d=ImageDraw.Draw(badge);d.ellipse((4,4,188,188),fill=(99,28,23,255),outline=(182,132,69,255),width=4);badge.alpha_composite(glyph,((192-glyph.width)//2,(192-glyph.height)//2));dest=official/'social-kill.png';badge.save(dest);records.append(dict(file='social-kill.png',source='https://web.sanguosha.com/10/pc/res/assets/login/login1.png',atlas='https://web.sanguosha.com/10/pc/res/assets/login/login.atlas',region='LoginLogo.png',transform='Masked final traditional 殺 glyph, set inside burgundy seal',sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
meta=json.loads((official/'effects/metadata.json').read_text())
# Text and first-blood sequences belong to import_motion_effects.py.
for name in ['victory-base','defeat-base']:
 folder=src/'official-client/effects'/name;files=sorted(folder.glob('*.png'));mask=Image.new('L',Image.open(files[0]).size)
 for f in files:mask=ImageChops.lighter(mask,Image.open(f).getchannel('A'))
 bounds=mask.getbbox();x,y,r,b=bounds;w,h=r-x,b-y;cols=min(8,4096//(w+2));rows=(len(files)+cols-1)//cols;sheet=Image.new('RGBA',((w+2)*cols,(h+2)*rows))
 for i,f in enumerate(files):sheet.paste(Image.open(f).crop(bounds),(i%cols*(w+2)+1,i//cols*(h+2)+1))
 dest=official/'effects'/(name+'.png');sheet.save(dest);m=json.load(open(folder/'meta.json'));meta[name]=dict(frames=len(files),columns=cols,rows=rows,stageWidth=mask.width,stageHeight=mask.height,width=w,height=h,crop=list(bounds),duration=m['duration']/1000)
 records.append(dict(file='effects/'+name+'.png',source=['https://web.sanguosha.com/10/pc/res/assets/animate/battle/'+m['source']+ext for ext in ['.sk','.png']],animation=m['animation'],scale=.6,render=meta[name],sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
(official/'effects/metadata.json').write_text(json.dumps(meta,indent=2)+'\n')
by={e['file']:e for e in manifest['files']};by.update({e['file']:e for e in records});manifest['files']=list(by.values());(official/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
audio=[('game-start','https://raw.githubusercontent.com/diandian157/decadeUI/30dbc8911ea9d27cf674c9be50e8abd663e8a8f0/audio/game_start.mp3'),('card-place','https://raw.githubusercontent.com/diandian157/decadeUI/30dbc8911ea9d27cf674c9be50e8abd663e8a8f0/audio/GameShowCard.mp3'),('recover-effect','https://web.sanguosha.com/10/pc/res/assets/runtime/voice/spell/armor_baiyinshizi_heal.mp3')]
ffmpeg=Path('/Users/yugonglian/.local/share/after-school-video/venv/lib/python3.12/site-packages/imageio_ffmpeg/binaries/ffmpeg-macos-aarch64-v7.1')
def fetch(job):
 key,url=job;raw=src/'audio-additions'/(key+'.mp3')
 if not raw.exists():subprocess.run(['curl','-fLs','--max-time','25',url,'-o',str(raw)],check=True)
 target=out/'audio'/(key+'.ogg');subprocess.run([str(ffmpeg),'-loglevel','error','-y','-i',str(raw),'-c:a','libvorbis','-q:a','5',str(target)],check=True)
 return dict(file='audio/'+key+'.ogg',source=url,sourceSha256=hashlib.sha256(raw.read_bytes()).hexdigest(),sha256=hashlib.sha256(target.read_bytes()).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as ex:entries=list(ex.map(fetch,audio))
previous=json.loads((out/'audio-additions-sources.json').read_text()) if (out/'audio-additions-sources.json').exists() else {'files':[]}
by_audio={e['file']:e for e in previous['files']};by_audio.update({e['file']:e for e in entries});entries=list(by_audio.values())
(out/'audio-additions-sources.json').write_text(json.dumps({'scope':'Local review of original Sanguosha voices, no synthesized dialogue.','files':entries},ensure_ascii=False,indent=2)+'\n');print('Imported',len(records),'visuals and',len(entries),'sounds')
