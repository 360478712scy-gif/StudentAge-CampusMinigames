"""Render the captured Unity frames at their recorded wall-clock timing."""
from pathlib import Path
import json,subprocess,argparse
p=argparse.ArgumentParser();p.add_argument('--qa',type=Path,required=True);p.add_argument('--out',type=Path,required=True);p.add_argument('--ffmpeg',required=True);a=p.parse_args();parts=[]
def frame(f,seconds):parts.extend(["file '"+str(f.resolve()).replace("'","'\\''")+"'",'duration '+str(seconds)])
frame(a.qa/'sanguosha-board.png',1.2)
for n in ['hand-motion','selection-motion','target-motion','table-motion']:
 path=a.qa/n;times=json.loads((path/'timing.json').read_text());files=sorted(path.glob('*.png'));assert len(times)==len(files)
 for i,f in enumerate(files):frame(f,times[i+1]-times[i] if i+1<len(times) else .05)
frame(a.qa/'sanguosha-response.png',1.3);frame(a.qa/'sanguosha-card-detail.png',2)
parts.append(parts[-2]);a.out.parent.mkdir(parents=True,exist_ok=True);concat=a.out.with_suffix('.ffconcat');concat.write_text('ffconcat version 1.0\n'+'\n'.join(parts)+'\n')
subprocess.run([a.ffmpeg,'-hide_banner','-loglevel','warning','-y','-f','concat','-safe','0','-i',str(concat),'-vf','crop=1440:810:0:45','-r','30','-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(a.out)],check=True)
print(a.out)
