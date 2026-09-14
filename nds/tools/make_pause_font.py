"""Build only caption bitmaps; uses the same binary-alpha pixel treatment as the NDS menu."""
from pathlib import Path
from PIL import Image, ImageFont, ImageDraw
import re, math
root=Path(__file__).resolve().parents[2]
text=''.join(re.findall(r'"([^"\n]*)"',(root/'src/MinigamePause.cs').read_text()))
chars=''.join(dict.fromkeys(text))
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Songti.ttc',12,index=1)
atlas=Image.new('RGBA',(224,math.ceil(len(chars)/16)*16))
for i,c in enumerate(chars):
 mask=Image.new('L',(14,16));d=ImageDraw.Draw(mask);d.text((7,7),c,font=font,fill=255,anchor='mm');mask=mask.point(lambda v:255 if v>=100 else 0)
 glyph=Image.new('RGBA',(14,16),'white');glyph.putalpha(mask);atlas.alpha_composite(glyph,((i%16)*14,(i//16)*16))
atlas.save(root/'assets/nds/pause-font.png');(root/'assets/nds/pause-font.txt').write_text(chars)
print('PAUSE_FONT_OK',len(chars))
