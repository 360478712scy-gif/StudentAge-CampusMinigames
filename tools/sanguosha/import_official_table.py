"""Extract unmodified UI regions and compose rank/suit layers from public official atlases for local review."""
from pathlib import Path
from PIL import Image, ImageChops
import hashlib,json,re,shutil
root=Path(__file__).resolve().parents[2];source=root/'research/sanguosha/official-client';out=root/'assets/sanguosha/official';out.mkdir(exist_ok=True)
version=json.loads((source/'version.json').read_text());records=[]
mapping=dict(p.split(':') for p in 'slash:Sha jink:Shan peach:Tao crossbow:ZhuGeLianNu double_sword:CiXiongShuangGuJian qinggang_sword:QingGangJian blade:QingLongYanYueDao spear:ZhangBaSheMao axe:GuanShiFu halberd:FangTianHuaJi kylin_bow:QiLinGong eight_diagram:BaGuaZhen renwang_shield:RenWangDun ice_sword:HanBingJian jueying:JueYing dilu:DiLu zhuahuangfeidian:ZhuaHuangFeiDian chitu:ChiTu dayuan:DaWan zixing:ZiXing amazing_grace:WuGuFengDeng god_salvation:TaoYuanJieYi savage_assault:NanManRuQin archery_attack:WanJianQiFa duel:JueDou ex_nihilo:WuZhongShengYou snatch:ShunShouQianYang dismantlement:GuoHeChaiQiao collateral:JieDaoShaRen nullification:WuXieKeJi indulgence:LeBuSiShu lightning:ShanDian'.split())
def region(group,name,target=None):
 atlas='res/assets/cards/normal/card.atlas' if group=='card' else 'res/assets/game/'+('nsOptBar/nsOptBar' if group=='nsOptBar' else group)+'.atlas';a=json.loads((source/atlas).read_text());d=a['frames'][name+'.png'];f=source/'extracted'/group/(name+'.png');dest=out/(target or group+'/'+name+'.png');dest.parent.mkdir(exist_ok=True,parents=True);shutil.copy2(f,dest)
 records.append(dict(file=str(dest.relative_to(out)),atlas='https://web.sanguosha.com/10/pc/'+atlas,atlasVersion=version.get(atlas),region=name+'.png',bounds=d,sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
for kind,name in mapping.items():
 region('card',name+'_png','cards/'+kind+'.png')
 if ('Equip_'+name+'_S.png') in json.loads((source/'res/assets/cards/normal/card.atlas').read_text())['frames']:region('card','Equip_'+name+'_S','equipment/'+kind+'.png')
for name in ['spade','club','heart','diamond','normalcard_selected_new','CardDescBg']+[c+'_'+r for c in ['red','black'] for r in ['A','2','3','4','5','6','7','8','9','10','J','Q','K']]:region('card',name)
for name in ['DecideLeBuSiShu','DecideBingLiangCunDuan','handCardsBg','gradeCountryBg1','gradeCountryBg3','gradeCountryBg5','gradeHpBg','greenBlood','yellowBlood','redBlood','empBlood','seatCanSelect','Figure_Self','Figure_Enemy','cardNumBg1','seatui_directline_new0','seatui_directline_new1','TurnOverMask']+[k+s for k in ['wei','shu','wu','qun','shen'] for s in ['', 'Bg']]:region('seat',name)
for name in ['selfSeatBg','selfSeatEquipBg','seat_bottom_projection','seat_bottom_select_general','hurt_Bg']+['hurt_'+str(i) for i in range(10)]+['hurt_-']:region('seat_bottom',name)
for name in ['selfSeatCountdownBg','selfSeatCountdownBar','selfSeatCountdownLight','selfSeatCancelBtn_normal','selfSeatCancelBtn_over','selfSeatCancelBtn_disable','skill_common_up','skill_common_over','skill_comon_disabled','game_chat_btn_normal','game_chat_btn_close_normal','game_chat_btn_over','surrender_btn_normal','game_audio_btn_normal']+['SeatRoundState_'+str(i) for i in range(1,7)]:region('selfseat',name)
for name in ['gameActionBg','operate_tips_bg','gameBaseLine']:region('gameBase',name)
for name in ['nsOptBarBg','nsOptBarChatBtnNormal','nsOptBarChatBtnOver','nsOptBarConditonNormal','nsOptBarConditionOver','nsOptBarTouXiangBtnNormal','nsOptBarTouXiangBtnOver']:region('nsOptBar',name)
# The printed card bitmap stays intact; only official rank and suit overlays are added.
for kind,suit,rank in re.findall(r'new Card\(\d+,"([^"]+)",Suit\.(\w+),(\d+)\)',(root/'sanguosha/Runtime/Deck.cs').read_text()):
 color='red' if suit in ['Heart','Diamond'] else 'black';face={1:'A',11:'J',12:'Q',13:'K'}.get(int(rank),rank)
 card=Image.open(out/'cards'/f'{kind}.png').convert('RGBA');rankim=Image.open(out/'card'/f'{color}_{face}.png');suitim=Image.open(out/'card'/f'{suit.lower()}.png');card.alpha_composite(rankim,(8+(22-rankim.width)//2,8));card.alpha_composite(suitim,(8+(22-suitim.width)//2,29))
 dest=out/'faces'/f'{kind}-{suit.lower()}-{rank}.png';dest.parent.mkdir(exist_ok=True);card.save(dest)
 record=dict(file=str(dest.relative_to(out)),composition=[f'cards/{kind}.png',f'card/{color}_{face}.png',f'card/{suit.lower()}.png'],rankOrigin=[8+(22-rankim.width)//2,8],suitOrigin=[8+(22-suitim.width)//2,29],sha256=hashlib.sha256(dest.read_bytes()).hexdigest());
 if not any(r['file']==record['file'] for r in records):records.append(record)
# Rendered from original public Laya skeletons; preserve the fixed 512px stage origin.
effects={}
for name in ['cardSkill/cardSkillHeiSha','cardSkill/cardSkillHongSha','cardSkill/cardSkillShan','seatStateRed','seatStateYellow']:
 files=sorted((source/'effects'/name).glob('*.png'));mask=Image.new('L',(512,512))
 for f in files:mask=ImageChops.lighter(mask,Image.open(f).getchannel('A'))
 bounds=mask.getbbox();x,y,right,bottom=bounds;w,h=right-x,bottom-y;cols=8;rows=(len(files)+cols-1)//cols
 sheet=Image.new('RGBA',((w+2)*cols,(h+2)*rows))
 for i,f in enumerate(files):sheet.paste(Image.open(f).crop(bounds),(i%cols*(w+2)+1,i//cols*(h+2)+1))
 key=name.split('/')[-1];dest=out/'effects'/f'{key}.png';dest.parent.mkdir(exist_ok=True);sheet.save(dest)
 meta=json.loads((source/'effects'/name/'meta.json').read_text());effects[key]=dict(frames=len(files),fps=30,duration=meta['duration']/1000,columns=cols,rows=rows,crop=list(bounds),width=w,height=h)
 records.append(dict(file=str(dest.relative_to(out)),source=['https://web.sanguosha.com/10/pc/res/assets/animate/battle/'+name+ext for ext in ['.sk','.png']],render=effects[key],sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
(out/'effects/metadata.json').write_text(json.dumps(effects,indent=2)+'\n')
(out/'sources.json').write_text(json.dumps({'scope':'Local visual comparison only. Original Sanguosha assets remain the property of their rights holders. No client JavaScript, account data or server code is included.','manifest':'https://web.sanguosha.com/10/pc/version.json','manifestSha256':hashlib.sha256((source/'version.json').read_bytes()).hexdigest(),'files':records},ensure_ascii=False,indent=2)+'\n');print('Imported',len(records),'files')
