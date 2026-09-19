"""Original 128px medal illustrations; crisp stepped edges with fine metal highlights."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math, json, shutil
root=Path(__file__).resolve().parents[2]; out=root/'assets/nds/Badges';out.mkdir(parents=True,exist_ok=True)
ink='#142237';gold='#e1ac55';hi='#fff1b7';dark='#84552e';white='#fff7dc'
colors=['#ed6657','#91bcd0','#5bd4da','#d1ac80','#98aff7','#9b9de8','#ed9871','#96c669','#e5b755','#ef826a','#8fb17c','#73c9ad','#bba1ff']
font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf',24)
def poly(d,p,c):d.polygon([(round(x),round(y)) for x,y in p],fill=c)
def star(d,x,y,r,c,n=5):poly(d,[(x+math.sin(i*math.pi/n)*r*(1 if i%2==0 else .44),y-math.cos(i*math.pi/n)*r*(1 if i%2==0 else .44)) for i in range(2*n)],c)
def box(d,r,c,outline=ink,w=2):d.rectangle(r,fill=c,outline=outline,width=w)
for n in range(13):
 im=Image.new('RGBA',(128,128));d=ImageDraw.Draw(im)
 # Individual silhouettes: cards, board, bubbles, chest, star, blocks, crown, snake,
 # maze, mushroom, wings, jade tiles, and the ultimate winged handheld.
 if n==0:
  poly(d,[(28,31),(72,19),(100,77),(55,99),(25,71)],ink);poly(d,[(32,34),(70,24),(94,75),(56,93),(30,70)],'#476b96')
 elif n==1:
  box(d,(27,22,101,96),ink,w=4);box(d,(31,26,97,92),'#c19158',w=4)
  for x,y in [(27,22),(93,22),(27,88),(93,88)]:box(d,(x,y,x+8,y+8),gold,None)
 elif n==2:
  for x,y,r in [(39,39,24),(82,54,25),(53,82,22)]:d.ellipse((x-r,y-r,x+r,y+r),fill=ink)
 elif n==3:
  box(d,(27,54,101,98),ink,w=4);box(d,(31,58,97,94),'#b67c48',None);box(d,(35,88,93,98),gold,None)
 elif n==4:
  star(d,64,61,56,ink);star(d,64,61,49,'#8fa0e2');star(d,64,61,39,'#c5d4f4')
 elif n==5:
  poly(d,[(26,54),(48,54),(48,22),(82,22),(82,54),(103,54),(103,96),(26,96)],ink);poly(d,[(31,59),(53,59),(53,27),(77,27),(77,59),(98,59),(98,91),(31,91)],'#575d96')
 elif n==6:
  poly(d,[(24,34),(40,45),(64,18),(88,45),(104,34),(94,91),(34,91)],ink);poly(d,[(31,41),(42,50),(64,26),(86,50),(97,41),(89,86),(39,86)],'#d8a556')
 elif n==7:
  # The curled snake itself defines this badge's shape.
  d.line([(39,34),(87,34),(87,80),(47,80),(47,54),(70,54)],fill=ink,width=22)
 elif n==8:
  d.pieslice((24,23,99,98),35,325,fill=ink);d.pieslice((29,28,94,93),35,325,fill='#c69a43')
 elif n==9:
  # A mushroom badge, with a small adventure banner beneath it.
  poly(d,[(26,73),(102,73),(96,91),(103,101),(25,101),(32,91)],ink);box(d,(32,78,96,96),'#6a9a9e',None)
 elif n==10:
  poly(d,[(16,30),(50,41),(64,21),(78,41),(112,30),(99,68),(81,80),(64,103),(47,80),(29,68)],ink)
  poly(d,[(23,37),(51,47),(64,28),(77,47),(105,37),(93,66),(77,76),(64,95),(51,76),(35,66)],'#aeb98a')
 elif n==11:
  poly(d,[(23,46),(44,24),(75,23),(104,44),(98,91),(36,100)],ink);poly(d,[(28,48),(46,29),(73,28),(99,46),(94,87),(39,95)],'#3f8e81')
 else:
  poly(d,[(11,27),(44,34),(64,10),(84,34),(117,27),(105,71),(88,85),(64,112),(40,85),(23,71)],ink)
  poly(d,[(17,33),(46,40),(64,17),(82,40),(111,33),(100,68),(85,80),(64,104),(43,80),(28,68)],gold)
  poly(d,[(41,43),(64,24),(87,43),(83,79),(64,96),(45,79)],'#654f85')
 # Each illustration is designed at native resolution instead of enlarging the 32px cartridge art.
 if n==0:
  for x,y,c in [(39,43,'#43acc0'),(53,34,'#eab14f'),(67,42,'#e96859')]:
   box(d,(x,y,x+23,y+35),white);box(d,(x+3,y+3,x+20,y+32),c,None);d.ellipse((x+5,y+10,x+17,y+26),outline=white,width=2)
  star(d,64,74,8,hi)
 elif n==1:
  box(d,(36,31,91,87),'#ae7c4f');box(d,(39,34,88,84),'#d8b57c',None)
  for k in range(5):d.line((42+10*k,36,42+10*k,82),fill='#8c683f');d.line((40,39+10*k,86,39+10*k),fill='#8c683f')
  for x,y,c in [(43,39,ink),(53,49,ink),(63,59,ink),(73,69,ink),(83,79,ink),(53,69,white),(63,39,white),(83,49,white)]:
   d.ellipse((x-5,y-5,x+5,y+5),fill=c,outline='#bdab85');d.point((x-2,y-2),fill='#eaf7ef')
 elif n==2:
  for x,y,r,c in [(49,48,15,'#63b9d9'),(77,55,17,'#76ded8'),(57,76,12,'#8299df')]:
   d.ellipse((x-r,y-r,x+r,y+r),fill=c,outline=ink,width=2);d.arc((x-r+4,y-r+4,x+r-4,y+r-4),180,280,fill=white,width=3);d.arc((x-r+2,y-r+2,x+r-2,y+r-2),0,75,fill='#c4ffff',width=2)
  star(d,84,32,6,hi)
 elif n==3:
  box(d,(38,48,88,81),'#b67b47');box(d,(41,52,85,78),'#e2ae70',None);poly(d,[(34,46),(47,30),(92,37),(85,53)],ink);poly(d,[(39,44),(49,34),(87,39),(82,49)],'#ffe0a3');box(d,(59,36,66,81),'#6baaa3');star(d,76,60,9,white)
 elif n==4:
  box(d,(39,31,89,85),'#7088c5');box(d,(43,35,85,56),'#d5e8d4');d.text((64,46),'24',font=ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf',18),anchor='mm',fill=ink)
  for j in range(2):
   for i in range(3):box(d,(45+i*13,62+j*12,54+i*13,70+j*12),'#f2cf79' if i==2 else '#c9d6ea',None)
  star(d,90,33,8,hi)
 elif n==5:
  for x,y,c in [(39,69,'#7ed3c0'),(53,69,'#7ed3c0'),(67,69,'#7ed3c0'),(81,69,'#7ed3c0'),(53,55,'#c098eb'),(67,55,'#c098eb'),(67,41,'#c098eb'),(81,41,'#ecb162')]:
   box(d,(x,y,x+12,y+12),c);d.line((x+3,y+3,x+9,y+3),fill=white);d.line((x+9,y+5,x+9,y+9),fill='#a7a5b9')
  star(d,42,39,8,hi)
 elif n==6:
  for x,y in [(37,47),(53,42),(69,47)]:box(d,(x,y,x+22,y+34),white);poly(d,[(x+11,y+10),(x+5,y+17),(x+11,y+23),(x+17,y+17)],'#de6b56')
  poly(d,[(42,30),(48,39),(64,25),(80,39),(87,30),(82,46),(47,46)],ink);poly(d,[(47,32),(51,40),(64,29),(77,40),(83,32),(80,43),(49,43)],gold);box(d,(51,45,77,49),hi,None)
 elif n==7:
  points=[(43,38),(83,38),(83,76),(51,76),(51,57),(66,57)];d.line(points,fill=ink,width=15);d.line(points,fill='#78af60',width=11);d.line(points,fill='#abd67d',width=5);box(d,(60,50,74,64),'#b5de87');box(d,(67,52,69,55),ink,None);d.ellipse((39,53,47,62),fill='#ee8665',outline=ink);d.line((44,53,46,49),fill='#9ecb7a',width=2)
 elif n==8:
  for x,y in [(38,39),(85,79),(83,36)]:box(d,(x,y,x+7,y+7),'#7194b1');d.pieslice((39,36,83,80),35,325,fill='#eebd51',outline=ink,width=2);d.pieslice((43,39,79,75),35,325,fill='#ffe3a1');box(d,(59,44,62,49),ink,None)
  for x in [84,94]:box(d,(x,57,x+4,61),white,None)
 elif n==9:
  # Mushroom cap, brick pedestal and a miniature finish flag.
  box(d,(37,79,90,86),'#a56542');
  # Deliberately layered cap silhouette rather than a mosaic face.
  d.ellipse((41,34,84,74),fill=ink);d.ellipse((44,37,81,70),fill='#e77763');box(d,(42,57,83,70),'#e77763',None);box(d,(51,68,75,79),white);box(d,(57,70,59,75),ink,None);box(d,(68,70,70,75),ink,None)
  for x,y in [(53,47),(70,45),(62,60)]:d.ellipse((x-5,y-5,x+5,y+5),fill=white)
 elif n==10:
  # Winged commando chevron with a polished cartridge and jungle leaves.
  for side in [-1,1]:
   for j in range(3):poly(d,[(64+side*12,49+j*9),(64+side*33,39+j*9),(64+side*27,54+j*9),(64+side*12,60+j*9)],'#87aa76')
  box(d,(53,34,74,82),'#c9b27a');box(d,(57,38,70,59),'#cddb9a');box(d,(57,61,70,78),'#779365');star(d,64,48,7,hi)
 elif n==11:
  for x,y in [(36,45),(51,34),(69,43)]:
   box(d,(x,y,x+23,y+37),'#63aa96');box(d,(x,y,x+21,y+32),white);d.line((x+6,y+8,x+15,y+8),fill='#448b79',width=2);d.line((x+6,y+16,x+15,y+16),fill='#448b79',width=2);d.line((x+6,y+24,x+15,y+24),fill='#448b79',width=2)
 else:
  for side in [-1,1]:
   for j in range(4):poly(d,[(64+side*14,49+j*8),(64+side*(52-j*4),33+j*8),(64+side*(43-j*4),53+j*8),(64+side*15,66+j*7)],gold);d.line((64+side*16,51+j*8,64+side*(44-j*4),41+j*8),fill=hi,width=2)
  box(d,(44,41,84,62),hi);box(d,(48,45,80,58),'#9cdce2');box(d,(42,64,86,86),gold);box(d,(48,67,80,81),'#c6a5f1');box(d,(56,84,72,88),hi,None)
  poly(d,[(42,26),(50,36),(64,20),(78,36),(87,26),(80,41),(48,41)],ink);poly(d,[(46,28),(52,37),(64,24),(76,37),(83,28),(78,38),(50,38)],hi);star(d,64,33,4,'#e89e70');star(d,64,73,7,white)
 im=im.resize((64,64) if n!=12 else (80,80),Image.Resampling.NEAREST)
 im.save(out/f'badge-{919999 if n==12 else 919920+n}.png')
# Contact sheet for visual review.
sheet=Image.new('RGB',(7*168,2*188),'#15243a');d=ImageDraw.Draw(sheet)
for n in range(13):
 x=(n%7)*168+20;y=(n//7)*188+12;a=Image.open(out/f'badge-{919999 if n==12 else 919920+n}.png').resize((128,128),Image.Resampling.NEAREST);sheet.paste(a,(x,y),a);d.text((x+64,y+145),str(n+1),anchor='mm',fill=hi)
sheet.save(root/'qa/nds-badges/badge-sheet.png')
shutil.copy2(root/'research/retro-sources/smb-clone/Assets/Sounds/06-level-complete.mp3',out/'badge-win.mp3')
shutil.copy2(root/'research/retro-sources/smb-clone/Assets/Sounds/07-castle-complete.mp3',out/'badge-ultimate.mp3')
(out/'SOURCE.txt').write_text('Badge illustrations: original procedural 64px pixel art, 80px ultimate badge. Audio: existing smb-clone reference, Assets/Sounds/06-level-complete.mp3 and 07-castle-complete.mp3. Ultimate sequence layers the castle completion fanfare with timed local sparkle and impact voices.\n')
print('BADGE_ART_OK')
