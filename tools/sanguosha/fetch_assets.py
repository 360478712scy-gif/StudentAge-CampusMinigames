from pathlib import Path
import json,subprocess,concurrent.futures,hashlib
root=Path(__file__).resolve().parents[2];tree=json.loads((root/'research/sanguosha/qs-tree.json').read_text());sha=tree['sha'];paths={x['path'] for x in tree['tree']};dst=root/'assets/sanguosha';dst.mkdir(exist_ok=True)
generals='caocao simayi xiahoudun zhangliao xuchu guojia zhenji liubei guanyu zhangfei zhugeliang zhaoyun machao huangyueying sunquan ganning lvmeng huanggai zhouyu daqiao luxun sunshangxiang huatuo lvbu diaochan xiahouyuan caoren huangzhong weiyan xiaoqiao zhoutai zhangjiao yuji shenguanyu shenlvmeng'.split()
cards='slash jink peach crossbow double_sword qinggang_sword blade spear axe halberd kylin_bow eight_diagram renwang_shield ice_sword jueying dilu zhuahuangfeidian chitu dayuan zixing amazing_grace god_salvation savage_assault archery_attack duel ex_nihilo snatch dismantlement collateral nullification indulgence lightning'.split()
requests={}
for g in generals:
 p='image/generals/card/'+('nos_'+g if 'image/generals/card/nos_'+g+'.jpg' in paths else g)+'.jpg'
 requests[p]='generals/'+g+'.jpg'
 old=dst/('generals/'+g+'.jpg')
 if old.exists() and '/nos_' in p:old.unlink()
for c in cards:
 requests['image/big-card/'+c+'.png']='cards/'+c+'.png'
 for sex in ['male','female']:
  p='audio/card/'+sex+'/'+c+'.ogg'
  if p in paths:requests[p]='audio/'+sex+'-'+c+'.ogg'
for p in ['background','win','lose','injure1','button-down','choose-item','hplost']:requests['audio/system/'+p+'.ogg']='audio/'+p+'.ogg'
requests['image/system/backdrop/default.jpg']='board.jpg';requests['image/system/card-back.png']='back.png'
for i in range(24):requests['image/system/emotion/slash_red/'+str(i)+'.png']='fx/slash-'+str(i)+'.png'
requests['LICENSE']='QSanguosha-LICENSE.txt';requests['GPLv3']='QSanguosha-GPLv3.txt';requests['MCFR']='QSanguosha-MCFR.txt'
for skill in 'jianxiong fankui guicai ganglie tuxi luoyi tiandu yiji qingguo luoshen rende wusheng paoxiao guanxing kongcheng longdan mashu tieji jizhi qicai zhiheng qixi keji kurou yingzi fanjian guose liuli qianxun lianying jieyin xiaoji jijiu qingnang wushuang lijian biyue shensu jushou liegong kuanggu tianxiang hongyan buqu leiji guidao guhuo wushen wuhun shelie gongxin'.split():
 p='audio/skill/'+skill+'1.ogg'
 if p not in paths:p='audio/skill/'+skill+'.ogg'
 if p in paths:requests[p]='audio/'+skill+'.ogg'
def fetch(pair):
 p,rel=pair;f=dst/rel;f.parent.mkdir(parents=True,exist_ok=True)
 if not f.exists():subprocess.run(['curl','--fail','--retry','2','-Ls','https://raw.githubusercontent.com/Mogara/QSanguosha-v2/'+sha+'/'+p,'-o',str(f)],check=True)
 return {'source':p,'file':rel,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()}
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as ex:records=list(ex.map(fetch,requests.items()))
(dst/'sources.json').write_text(json.dumps({'repository':'https://github.com/Mogara/QSanguosha-v2','revision':sha,'note':'Original Sanguosha artwork/audio; upstream attribution and licenses accompany these assets. No upstream C++ engine code is included.','files':records},ensure_ascii=False,indent=2))
print('Fetched',len(records),'assets')
