from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import json,re
out=Path(__file__).resolve().parents[2]/'assets/nds';out.mkdir(parents=True,exist_ok=True)
# Original pixel case: two 4:3 displays, hinge, D-pad, ABXY and power button.
im=Image.new('RGBA',(400,450));d=ImageDraw.Draw(im)
def box(r,c):d.rectangle(r,fill=c)
def bevel(r,base='#dbe5e8',edge='#556877'):
 x,y,x2,y2=r;box((x+4,y,x2-4,y2),edge);box((x,y+4,x2,y2-4),edge);box((x+3,y+4,x2-3,y2-5),base);box((x+5,y+2,x2-5,y+4),'#f3f8f6');box((x+5,y2-5,x2-5,y2-2),'#94aab4')
bevel((8,5,392,211));bevel((8,223,392,440))
for y in (20,234):
 bevel((78,y,322,y+182),'#889ca7','#41535c');box((83,y+5,317,y+177),'#172b3a');box((85,y+7,315,y+175),'#cfe4d9')
for y in (61,70,79):
 for x in (42,48,54,346,352,358):box((x,y,x+2,y+2),'#81959e')
bevel((24,209,376,226),'#a7bec7');box((43,213,354,216),'#eaf3f0');box((46,221,354,223),'#647d8b')
# D-pad left and four circular pixel buttons right.
box((37,295,53,344),'#465c6c');box((20,312,70,328),'#465c6c');box((41,299,49,340),'#6f8492');box((24,316,66,324),'#6f8492')
for x,y in ((357,293),(376,312),(338,312),(357,331)):
 d.ellipse((x-8,y-8,x+8,y+8),fill='#536b7b');d.ellipse((x-5,y-6,x+5,y+4),fill='#91a9b5')
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial.ttf',9)
for label,x,y in [('X',357,293),('A',376,312),('Y',338,312),('B',357,331)]:d.text((x,y),label,font=font,fill='#f9fffa',anchor='mm')
box((338,211,369,219),'#40566a');box((344,212,364,215),'#b3d9e5');box((372,213,376,218),'#89d871')
for x in (29,52):box((x,394,x+13,398),'#738b96')
im.save(out/'shell.png')
# Twelve original, deliberately simple cartridge icons.
for n in range(12):
 a=Image.new('RGBA',(32,32));q=ImageDraw.Draw(a);q.rectangle((1,1,30,30),fill=['#6b9bab','#b48b5a','#75aeb5','#bc876b','#769398','#667f99','#bd8a60','#78a668','#d9b368','#86ae94','#759680','#5b9d91'][n]);q.rectangle((3,3,28,28),outline='#e9ead3')
 if n in (0,4,6):
  for j,c in enumerate(['#e26e4c','#efd98a','#5d94b9']):q.rectangle((5+j*6,8-j,15+j*6,25-j),fill='#fff9dc',outline='#344d57');q.rectangle((7+j*6,12-j,13+j*6,20-j),fill=c)
 elif n==1:
  for p in (9,16,23):q.line((5,p,27,p),fill='#6e513e');q.line((p,5,p,27),fill='#6e513e')
  q.ellipse((6,6,12,12),fill='#243b42');q.ellipse((14,14,21,21),fill='#fffbde');q.ellipse((20,7,26,13),fill='#243b42')
 elif n==2:
  for x,y in ((8,8),(16,15),(9,22)):q.ellipse((x-4,y-4,x+5,y+5),fill='#b8eff1',outline='#436a89');q.rectangle((x-1,y-2,x+1,y),fill='white')
 elif n==3:
  q.rectangle((6,13,26,25),fill='#e9c591',outline='#71503f');q.polygon([(4,12),(11,6),(27,8),(25,14)],fill='#f4dcaa');q.rectangle((14,7,17,24),fill='#729e8e')
 elif n==5:
  for x,y,c in [(6,19,'#ddae6a'),(12,19,'#ddae6a'),(18,19,'#ddae6a'),(18,13,'#ddae6a'),(6,7,'#86c0bc'),(12,7,'#86c0bc'),(6,13,'#86c0bc')]:q.rectangle((x,y,x+5,y+5),fill=c,outline='#f5e8c8')
 elif n==7:
  q.line([(8,9),(23,9),(23,22),(11,22),(11,16)],fill='#d5e5a1',width=5);q.rectangle((7,13,14,18),fill='#d5e5a1');q.point((9,14),fill='#243e39');q.rectangle((17,14,20,17),fill='#d36f58')
 elif n==8:
  q.pieslice((6,6,24,24),35,325,fill='#ffe5a3');q.rectangle((22,14,25,17),fill='#fff9dc');q.rectangle((10,9,12,11),fill='#3c5360')
 elif n==9:
  q.rectangle((10,7,22,11),fill='#e26750');q.rectangle((8,11,24,13),fill='#e26750');q.rectangle((12,14,21,19),fill='#edc699');q.rectangle((10,20,23,25),fill='#4c718f');q.rectangle((8,25,14,27),fill='#5c4b43');q.rectangle((21,25,26,27),fill='#5c4b43')
 elif n==10:
  q.rectangle((12,7,18,13),fill='#f0c593');q.rectangle((10,14,22,21),fill='#708347');q.rectangle((16,16,28,19),fill='#354752');q.rectangle((12,22,16,27),fill='#485d57');q.rectangle((20,22,24,27),fill='#485d57')
 else:
  for x,y in ((5,10),(12,7),(19,10)):
   q.rectangle((x,y,x+8,y+16),fill='#f5efd6',outline='#466a5f');q.rectangle((x+2,y+3,x+3,y+11),fill='#538d6c');q.rectangle((x+5,y+3,x+6,y+11),fill='#538d6c')
 a.save(out/f'icon-{n}.png')
chars=''.join(dict.fromkeys('NDS掌机课间UNO五子棋泡泡换盒寻物算24点俄罗斯方块斗地主贪吃蛇吃豆人超级马里奥魂斗罗麻将选择关卡开始电源第关全部游戏已通关可重玩新挑战下回合开放尚未解锁·返回上一页下一页1234567890/←→-ABXY'))
extra=''.join(p.read_text() for p in (out.parents[1]/'src/Handheld').glob('Nds*.cs'))
chars=''.join(dict.fromkeys(chars+''.join(re.findall(r'"([^"\n]*)"',extra))))
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Songti.ttc',12,index=1)
atlas=Image.new('RGBA',(16*14,((len(chars)+15)//16)*16));ad=ImageDraw.Draw(atlas)
for i,ch in enumerate(chars):
 m=Image.new('L',(14,16));md=ImageDraw.Draw(m);md.text((7,7),ch,font=font,fill=255,anchor='mm');m=m.point(lambda v:255 if v>=100 else 0);glyph=Image.new('RGBA',(14,16),'white');glyph.putalpha(m);atlas.alpha_composite(glyph,((i%16)*14,(i//16)*16))
atlas.save(out/'font.png');(out/'font.json').write_text(json.dumps({'characters':chars,'columns':16,'width':14,'height':16},ensure_ascii=False))
(out/'SOURCE.txt').write_text('Original procedural pixel handheld shell and cartridge icons. Glyph bitmaps rendered locally with system Songti; no font file distributed. No Nintendo logos, BIOS or firmware assets included.\n')
print('NDS_ART_OK')
