using System;
using System.Collections.Generic;
using System.Linq;
namespace StudentAge.Sanguosha {
 public sealed class General {
  public readonly string Id,Name,Kingdom,Skills,Description;public readonly int Hp;public readonly bool Female;
  public General(string id,string name,string kingdom,int hp,bool female,string skills,string description){Id=id;Name=name;Kingdom=kingdom;Hp=hp;Female=female;Skills=skills;Description=description;}
  public bool Has(string skill)=>Skills.Split(' ').Contains(skill);
 }
 public enum Suit { Spade,Club,Heart,Diamond }
 public sealed class Card {
  public readonly int Id,Rank;public readonly Suit Suit;public readonly string Kind,PrintedKind;
  public Card(int id,string kind,Suit suit,int rank,string printedKind=null){Id=id;Kind=kind;PrintedKind=printedKind??kind;Suit=suit;Rank=rank;}
  public Card Physical=>Kind==PrintedKind?this:new Card(Id,PrintedKind,Suit,Rank);
  public bool Red=>Suit==Suit.Heart||Suit==Suit.Diamond;
  public string Name=>Catalog.CardName(Kind);
  public string Face=>(new[]{"♠","♣","♥","♦"})[(int)Suit]+(Rank==1?"A":Rank==11?"J":Rank==12?"Q":Rank==13?"K":Rank.ToString());
  public string Text=>Face+" "+Name;
  public int Slot=>Catalog.Slot(Kind);
  public bool Equip=>Slot>=0;public bool Trick=>!Equip&&Kind!="slash"&&Kind!="jink"&&Kind!="peach"&&Kind!="wine";
  public bool Delayed=>Kind=="indulgence"||Kind=="lightning"||Kind=="supply_shortage";
 }
 public static class Catalog {
  public static readonly General[] Generals={
   new General("caocao","曹操","魏",4,false,"jianxiong","奸雄：受到伤害后，可获得造成此次伤害的牌。"),
   new General("simayi","司马懿","魏",3,false,"fankui guicai","反馈：受到伤害后，可获得伤害来源的一张牌。鬼才：判定生效前，可打出一张手牌替换判定牌。"),
   new General("xiahoudun","夏侯惇","魏",4,false,"ganglie","刚烈：每次受到伤害后，可判定；非红桃时，伤害来源弃两张手牌或受到1点伤害。"),
   new General("zhangliao","张辽","魏",4,false,"tuxi","突袭：可放弃摸牌，获得至多两名其他角色各一张手牌。单挑中只能获得对手一张。"),
   new General("xuchu","许褚","魏",4,false,"luoyi","裸衣：摸牌阶段可少摸一张牌，本回合杀和决斗造成的伤害+1。"),
   new General("guojia","郭嘉","魏",3,false,"tiandu yiji","天妒：可获得自己的判定牌。遗计：每受到1点伤害，可摸两张牌并分配给任意角色。"),
   new General("zhenji","甄姬","魏",3,true,"qingguo luoshen","倾国：黑色手牌可当闪。洛神：准备阶段可反复判定，获得黑色判定牌，直到红色或主动停止。"),
   new General("liubei","刘备","蜀",4,false,"rende","仁德：可将手牌交给其他角色；本阶段累计交出至少两张时回复1点体力。"),
   new General("guanyu","关羽","蜀",4,false,"wusheng","武圣：红色牌可当杀使用或打出。"),
   new General("zhangfei","张飞","蜀",4,false,"paoxiao","咆哮：出牌阶段使用杀没有次数限制。"),
   new General("zhugeliang","诸葛亮","蜀",3,false,"guanxing kongcheng","观星：准备阶段观看牌堆顶X张牌，调整牌堆顶或底的顺序（X为存活人数，最多5）。空城：没有手牌时不能成为杀或决斗的目标。"),
   new General("zhaoyun","赵云","蜀",4,false,"longdan","龙胆：杀可当闪，闪可当杀。"),
   new General("machao","马超","蜀",4,false,"mashu tieji","马术：计算与其他角色的距离-1。铁骑：使用杀时可判定，红色则对方不能出闪。"),
   new General("huangyueying","黄月英","蜀",3,true,"jizhi qicai","集智：使用非延时锦囊时可摸一张牌。奇才：使用锦囊不受距离限制。"),
   new General("sunquan","孙权","吴",4,false,"zhiheng","制衡：出牌阶段限一次，弃置任意张牌，摸等量的牌。"),
   new General("ganning","甘宁","吴",4,false,"qixi","奇袭：黑色牌可当过河拆桥使用。"),
   new General("lvmeng","吕蒙","吴",4,false,"keji","克己：本回合出牌阶段没有使用或打出杀，可跳过弃牌阶段。"),
   new General("huanggai","黄盖","吴",4,false,"kurou","苦肉：出牌阶段可失去1点体力，然后摸两张牌。"),
   new General("zhouyu","周瑜","吴",3,false,"yingzi fanjian","英姿：摸牌阶段可额外摸一张。反间：阶段限一次，对手猜花色并获得你的一张随机手牌，猜错受到1点伤害。"),
   new General("daqiao","大乔","吴",3,true,"guose liuli","国色：方块牌可当乐不思蜀。流离：弃一张牌把杀转移给攻击范围内另一名角色；单挑中没有合法的第三方目标。"),
   new General("luxun","陆逊","吴",3,false,"qianxun lianying","谦逊：不能成为顺手牵羊或乐不思蜀的目标。连营：失去最后一张手牌后可摸一张。"),
   new General("sunshangxiang","孙尚香","吴",3,true,"jieyin xiaoji","结姻：阶段限一次，弃两张手牌，自己和一名受伤男性各回复1点体力。枭姬：每失去一张装备区的牌，可摸两张。"),
   new General("huatuo","华佗","群",3,false,"jijiu qingnang","急救：回合外红色牌可当桃。青囊：阶段限一次，弃一张手牌，令一名角色回复1点体力。"),
   new General("lvbu","吕布","群",4,false,"wushuang","无双：杀需要两张闪抵消；决斗中对方每次须打出两张杀。"),
   new General("diaochan","貂蝉","群",3,true,"lijian biyue","离间：弃一张牌，令两名其他男性决斗；单挑中没有两个合法目标。闭月：结束阶段可摸一张牌。"),
   new General("xiahouyuan","夏侯渊","魏",4,false,"shensu","神速：可跳过判定和摸牌，视为使用一张无距离限制的杀；也可弃一张装备牌并跳过出牌阶段，再视为使用一张无距离限制的杀。"),
   new General("caoren","曹仁","魏",4,false,"jushou","据守：结束阶段可摸三张牌，然后翻面；背面朝上的武将在下次回合翻回并跳过该回合。"),
   new General("huangzhong","黄忠","蜀",4,false,"liegong","烈弓：出牌阶段使用杀时，若对手手牌数≥你的体力或≤你的攻击范围，可令其不能出闪。"),
   new General("weiyan","魏延","蜀",4,false,"kuanggu","狂骨：对距离1以内角色每造成1点伤害，回复1点体力。"),
   new General("xiaoqiao","小乔","吴",3,true,"hongyan tianxiang","红颜：你的黑桃牌视为红桃。天香：受到伤害前，可弃一张红桃手牌转移伤害；受伤者随后摸等于其已损失体力值的牌。"),
   new General("zhoutai","周泰","吴",4,false,"buqu","不屈：体力降至0或以下，可翻开对应数量的不屈牌；点数不重复则仍存活，回复体力时移去相应不屈牌。"),
   new General("zhangjiao","张角","群",3,false,"leiji guidao","雷击：使用或打出闪时可令对手判定，黑桃则造成2点雷电伤害。鬼道：可用黑色手牌或装备替换判定牌，并获得原判定牌。"),
   new General("yuji","于吉","群",3,false,"guhuo","蛊惑：用一张手牌暗置，声明基本牌或非延时锦囊。无人质疑则生效；有人质疑，真牌令质疑者失去1体力，假牌令其摸1张；被质疑后仅真实的红桃牌生效。"),
   new General("shenguanyu","神关羽","神",5,false,"wushen wuhun","武神：红桃手牌均视为杀，使用红桃杀无距离限制。武魂：伤害来源获得等量梦魇；你死亡时其判定，非桃或桃园结义则死亡。"),
   new General("shenlvmeng","神吕蒙","神",3,false,"shelie gongxin","涉猎：可替代摸牌，亮出五张牌，每种花色取一张，其余弃置。攻心：阶段限一次，观看对手手牌，可将其中一张红桃牌弃置或置于牌堆顶。")
  };
  static readonly Dictionary<string,string> skillNames="jianxiong|奸雄 simayi|司马懿 fankui|反馈 guicai|鬼才 ganglie|刚烈 tuxi|突袭 luoyi|裸衣 tiandu|天妒 yiji|遗计 qingguo|倾国 luoshen|洛神 rende|仁德 wusheng|武圣 paoxiao|咆哮 guanxing|观星 kongcheng|空城 longdan|龙胆 mashu|马术 tieji|铁骑 jizhi|集智 qicai|奇才 zhiheng|制衡 qixi|奇袭 keji|克己 kurou|苦肉 yingzi|英姿 fanjian|反间 guose|国色 liuli|流离 qianxun|谦逊 lianying|连营 jieyin|结姻 xiaoji|枭姬 jijiu|急救 qingnang|青囊 wushuang|无双 lijian|离间 biyue|闭月 shensu|神速 jushou|据守 liegong|烈弓 kuanggu|狂骨 tianxiang|天香 hongyan|红颜 buqu|不屈 leiji|雷击 guidao|鬼道 guhuo|蛊惑 wushen|武神 wuhun|武魂 shelie|涉猎 gongxin|攻心".Split(' ').ToDictionary(x=>x.Split('|')[0],x=>x.Split('|')[1]);
  public static string SkillName(string key)=>skillNames.TryGetValue(key,out var name)?name:key;
  public static General Get(string id)=>Generals.First(g=>g.Id==id);
  static readonly string[] names={"slash|杀","jink|闪","peach|桃","wine|酒","supply_shortage|兵粮寸断","drowning|水淹七军","crossbow|诸葛连弩","double_sword|雌雄双股剑","qinggang_sword|青釭剑","blade|青龙偃月刀","spear|丈八蛇矛","axe|贯石斧","halberd|方天画戟","kylin_bow|麒麟弓","ice_sword|寒冰剑","eight_diagram|八卦阵","renwang_shield|仁王盾","jueying|绝影","dilu|的卢","zhuahuangfeidian|爪黄飞电","chitu|赤兔","dayuan|大宛","zixing|紫骍","amazing_grace|五谷丰登","god_salvation|桃园结义","savage_assault|南蛮入侵","archery_attack|万箭齐发","duel|决斗","ex_nihilo|无中生有","snatch|顺手牵羊","dismantlement|过河拆桥","collateral|借刀杀人","nullification|无懈可击","indulgence|乐不思蜀","lightning|闪电"};
  static readonly Dictionary<string,string> cardNames=names.ToDictionary(v=>v.Split('|')[0],v=>v.Split('|')[1]);
  public static string CardName(string kind)=>cardNames.ContainsKey(kind)?cardNames[kind]:kind;
  public static int Slot(string k){if(new[]{"crossbow","double_sword","qinggang_sword","blade","spear","axe","halberd","kylin_bow","ice_sword"}.Contains(k))return 0;if(k=="eight_diagram"||k=="renwang_shield")return 1;if(new[]{"jueying","dilu","zhuahuangfeidian"}.Contains(k))return 2;if(new[]{"chitu","dayuan","zixing"}.Contains(k))return 3;return -1;}
  public static int Range(string k)=>k=="kylin_bow"?5:k=="halberd"?4:new[]{"blade","spear","axe"}.Contains(k)?3:new[]{"double_sword","qinggang_sword","ice_sword"}.Contains(k)?2:1;
 }
}
