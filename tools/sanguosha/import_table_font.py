"""Bake the public desktop client's calligraphy font into demand-loaded glyph pages."""
from pathlib import Path
from PIL import Image,ImageFont,ImageDraw
import json,hashlib,math
root=Path(__file__).resolve().parents[2];source=root/'research/sanguosha/official-client/fzkt.ttf';out=root/'assets/sanguosha/official';font=ImageFont.truetype(str(source),48)
texts=''.join(p.read_text() for p in (root/'src/Games/Sanguosha').glob('Sanguosha*.cs'))+''.join(p.read_text() for p in (root/'src/Games/Sanguosha').glob('*.cs'))
people=json.loads((root/'research/sanguosha/npcs.json').read_text())['persons'];texts+=''.join(str(p.get('name','')) for p in people)+''.join(chr(i) for i in range(32,127))+'♠♣♥♦‹›−—…'
common=''.join(bytes((a,b)).decode('gb2312',errors='ignore') for a in range(0xA1,0xF8) for b in range(0xA1,0xFF))
missing=bytes(font.getmask('\U0010ffff'))
def supported(c):return c==' ' or c>=' ' and bytes(font.getmask(c))!=missing
primary=sorted(c for c in set(texts) if supported(c));extra=sorted(c for c in set(common)-set(primary) if supported(c));chars=primary+extra
cols=32;rows=64;page_size=cols*rows;glyphs={};names=[];(out/'font').mkdir(exist_ok=True)
for old in (out/'font').glob('fzkt*.png'):old.unlink()
for page in range(math.ceil(len(chars)/page_size)):
 sheet=Image.new('RGBA',(cols*64,rows*64));draw=ImageDraw.Draw(sheet)
 for j,c in enumerate(chars[page*page_size:(page+1)*page_size]):
  x=j%cols*64;y=j//cols*64;draw.text((x+8,y+52),c,font=font,anchor='ls',fill='white');glyphs[c]={'index':page*page_size+j,'advance':font.getlength(c)}
 name='fzkt'+('' if page==0 else '-'+str(page))+'.png';sheet.save(out/'font'/name);names.append(name)
meta={'size':48,'cell':64,'columns':cols,'rows':rows,'pageSize':page_size,'baseline':52,'bearing':8,'glyphs':glyphs};(out/'font/fzkt.json').write_text(json.dumps(meta,ensure_ascii=False,separators=(',',':'))+'\n');names.append('fzkt.json')
manifest=json.loads((out/'sources.json').read_text());manifest['files']=[r for r in manifest['files'] if not r['file'].startswith('font/')]
for n in names:
 f=out/'font'/n;manifest['files'].append({'file':'font/'+n,'source':'https://web.sanguosha.com/10/pc/res/assets/font/fzkt.ttf','sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'render':'48px original glyphs; common UI first; GB2312 dialogue extensions on demand-loaded 2048-glyph pages','sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
(out/'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n');print('FONT',font.getname(),len(chars),'glyphs;',len(primary),'primary;',len(names)-1,'pages')
