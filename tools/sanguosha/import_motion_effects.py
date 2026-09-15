"""Preserve oversized opening frames and import the original first-blood green-screen sequence."""
from pathlib import Path
from PIL import Image,ImageChops
import json,hashlib,subprocess,math
root=Path(__file__).resolve().parents[2];out=root/'assets/sanguosha/official';res=root/'research/sanguosha';manifest=json.loads((out/'sources.json').read_text());records={e['file']:e for e in manifest['files']};meta=json.loads((out/'effects/metadata.json').read_text())
def sheet(name,frames,canvas,offset,duration,source):
 mask=Image.new('L',frames[0].size)
 for im in frames:mask=ImageChops.lighter(mask,im.getchannel('A'))
 box=mask.getbbox();x,y,r,b=box;w,h=r-x,b-y;cols=min(8192//(w+2),max(1,math.ceil(math.sqrt(len(frames)*(h+2)/(w+2)))));rows=(len(frames)+cols-1)//cols
 atlas=Image.new('RGBA',((w+2)*cols,(h+2)*rows));assert max(atlas.size)<=8192,atlas.size
 for i,im in enumerate(frames):atlas.paste(im.crop(box),(i%cols*(w+2)+1,i//cols*(h+2)+1))
 f=out/'effects'/(name+'.png');atlas.save(f);meta[name]=dict(frames=len(frames),columns=cols,rows=rows,width=w,height=h,crop=[x+offset[0],y+offset[1],r+offset[0],b+offset[1]],duration=duration,stageWidth=canvas[0],stageHeight=canvas[1]);records['effects/'+name+'.png']=dict(file='effects/'+name+'.png',source=source,render=meta[name],sha256=hashlib.sha256(f.read_bytes()).hexdigest());print(name,'atlas',atlas.size,'content',box)
for name in ['victory-text','defeat-text']:
 folder=res/'official-client/effects'/name;frames=[Image.open(f).resize((768,768),Image.Resampling.LANCZOS) for f in sorted(folder.glob('*.png'))];m=json.load(open(folder/'meta.json'))
 # The actual glyph's opening silhouette must fit before upload; transparent glow can extend off-screen.
 b=frames[0].getchannel('A').point(lambda a:255 if a>32 else 0).getbbox();assert name!="victory-text" or (b and min(b[:2])>0 and max(b[2:])<768),b
 sheet(name,frames,(768,768),(0,0),m['duration']/1000,dict(skeleton='https://web.sanguosha.com/10/pc/res/assets/animate/battle/gameover/shenglishibaiwenzi.sk',animation=m['animation'],renderCanvas=1024,renderScale=.6,downsample=768))
frames=[Image.open(f) for f in sorted((res/'motion-fixes/first-blood-keyed').glob('*.png'))]
sheet('first-blood',frames,(896,504),(0,42),3.5,dict(video='https://www.bilibili.com/video/BV1ag411p7nB/',uploader='Notify_',segment=[0,3.5],transform='Chroma keyed original 720p sequence; crop only upper 60px outside effect, scale 0.7',sourceSha256=hashlib.sha256((res/'motion-fixes/first-blood-reference.mp4').read_bytes()).hexdigest()))
manifest['files']=list(records.values());(out/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n');(out/'effects/metadata.json').write_text(json.dumps(meta,indent=2)+'\n')
# Use the matching first-blood audio segment, including its natural tail before the usage example.
ff='/Users/yugonglian/.local/share/after-school-video/venv/lib/python3.12/site-packages/imageio_ffmpeg/binaries/ffmpeg-macos-aarch64-v7.1';dest=out.parent/'audio/first-blood.ogg';subprocess.run([ff,'-loglevel','error','-y','-i',str(res/'motion-fixes/first-blood-reference.mp4'),'-t','4','-vn','-c:a','libvorbis','-q:a','5',str(dest)],check=True)
p=out.parent/'audio-additions-sources.json';m=json.loads(p.read_text());m['files']=[e for e in m['files'] if e['file']!='audio/first-blood.ogg']+[dict(file='audio/first-blood.ogg',source='https://www.bilibili.com/video/BV1ag411p7nB/',uploader='Notify_',segment=[0,4],sha256=hashlib.sha256(dest.read_bytes()).hexdigest())];p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
