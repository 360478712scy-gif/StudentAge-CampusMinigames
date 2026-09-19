// Generated defaults from tools/tuning/settings.json. No Unity or BepInEx dependency.
using System;using System.Collections.Generic;
namespace StudentAge.CampusUno {
public static class MinigameTuning {
 public sealed class Setting {
  public readonly string Section,Key,Description;public readonly float Default,Min,Max;public readonly bool Integer;
  public Setting(string section,string key,float value,float min,float max,bool integer,string description){Section=section;Key=key;Default=value;Min=min;Max=max;Integer=integer;Description=description;}
  public float Clamp(float value){if(float.IsNaN(value)||float.IsInfinity(value))return Default;value=Math.Max(Min,Math.Min(Max,value));return Integer?(float)Math.Round(value):value;}
 }
 public static readonly Setting[] Settings={
new Setting("NDS","RoundLimit",3f,1f,30f,true,"所有游戏共享的每回合新通关额度。失败和已通关重玩不扣额度；F8主界面试玩不受限。"),
new Setting("NDS","MarioHalfUnits",1f,1f,6f,true,"马里奥首次通关消耗的半关单位数。1为半关，2为一关。其他游戏固定消耗2单位。"),
new Setting("NDS","Price",100f,0f,10000f,false,"商店NDS掌机价格；不会退补已经购买的金额。"),
new Setting("UNO","InitialCards",7f,3f,15f,true,"每位玩家起始手牌数。规则和108张牌库不变。"),
new Setting("UNO","AiDelay",1.2f,0.2f,5f,false,"电脑每次出牌前等待的秒数；越小越快。"),
new Setting("Gomoku","Win1",0.9f,0f,1f,false,"第1关：发现立即获胜落点的概率。"),
new Setting("Gomoku","Win2",0.94f,0f,1f,false,"第2关：发现立即获胜落点的概率。"),
new Setting("Gomoku","Win3",0.97f,0f,1f,false,"第3关：发现立即获胜落点的概率。"),
new Setting("Gomoku","Win4",0.99f,0f,1f,false,"第4关：发现立即获胜落点的概率。"),
new Setting("Gomoku","Win5",1f,0f,1f,false,"第5关：发现立即获胜落点的概率。"),
new Setting("Gomoku","Block1",0.65f,0f,1f,false,"第1关：堵住对方立即获胜落点的概率。"),
new Setting("Gomoku","Block2",0.72f,0f,1f,false,"第2关：堵住对方立即获胜落点的概率。"),
new Setting("Gomoku","Block3",0.79f,0f,1f,false,"第3关：堵住对方立即获胜落点的概率。"),
new Setting("Gomoku","Block4",0.85f,0f,1f,false,"第4关：堵住对方立即获胜落点的概率。"),
new Setting("Gomoku","Block5",0.9f,0f,1f,false,"第5关：堵住对方立即获胜落点的概率。"),
new Setting("Gomoku","Lapse1",0.35f,0f,1f,false,"第1关：普通局面忽略更优选择的概率；仍会沿合理棋形落子。"),
new Setting("Gomoku","Lapse2",0.3f,0f,1f,false,"第2关：普通局面忽略更优选择的概率；仍会沿合理棋形落子。"),
new Setting("Gomoku","Lapse3",0.25f,0f,1f,false,"第3关：普通局面忽略更优选择的概率；仍会沿合理棋形落子。"),
new Setting("Gomoku","Lapse4",0.2f,0f,1f,false,"第4关：普通局面忽略更优选择的概率；仍会沿合理棋形落子。"),
new Setting("Gomoku","Lapse5",0.15f,0f,1f,false,"第5关：普通局面忽略更优选择的概率；仍会沿合理棋形落子。"),
new Setting("Gomoku","Defense1",0.65f,0f,1f,false,"第1关：防守在落点评分中的权重；越大越重视防守。"),
new Setting("Gomoku","Defense2",0.74f,0f,1f,false,"第2关：防守在落点评分中的权重；越大越重视防守。"),
new Setting("Gomoku","Defense3",0.82f,0f,1f,false,"第3关：防守在落点评分中的权重；越大越重视防守。"),
new Setting("Gomoku","Defense4",0.9f,0f,1f,false,"第4关：防守在落点评分中的权重；越大越重视防守。"),
new Setting("Gomoku","Defense5",0.98f,0f,1f,false,"第5关：防守在落点评分中的权重；越大越重视防守。"),
new Setting("Box","CloseSeconds1",0.34f,0.04f,3f,false,"第1关：盒盖合拢耗时，秒；越小越难。"),
new Setting("Box","CloseSeconds2",0.29f,0.04f,3f,false,"第2关：盒盖合拢耗时，秒；越小越难。"),
new Setting("Box","CloseSeconds3",0.24f,0.04f,3f,false,"第3关：盒盖合拢耗时，秒；越小越难。"),
new Setting("Box","CloseSeconds4",0.16f,0.04f,3f,false,"第4关：盒盖合拢耗时，秒；越小越难。"),
new Setting("Box","CloseSeconds5",0.08f,0.04f,3f,false,"第5关：盒盖合拢耗时，秒；越小越难。"),
new Setting("Box","SwapSeconds1",0.96f,0.1f,3f,false,"第1关：每次换位耗时，秒；越小越难。"),
new Setting("Box","SwapSeconds2",0.81f,0.1f,3f,false,"第2关：每次换位耗时，秒；越小越难。"),
new Setting("Box","SwapSeconds3",0.66f,0.1f,3f,false,"第3关：每次换位耗时，秒；越小越难。"),
new Setting("Box","SwapSeconds4",0.44f,0.1f,3f,false,"第4关：每次换位耗时，秒；越小越难。"),
new Setting("Box","SwapSeconds5",0.22f,0.1f,3f,false,"第5关：每次换位耗时，秒；越小越难。"),
new Setting("Box","MemorySeconds1",3f,0.3f,15f,false,"第1关：记忆展示时间，秒。"),
new Setting("Box","MemorySeconds2",2.8f,0.3f,15f,false,"第2关：记忆展示时间，秒。"),
new Setting("Box","MemorySeconds3",2.6f,0.3f,15f,false,"第3关：记忆展示时间，秒。"),
new Setting("Box","MemorySeconds4",2.1f,0.3f,15f,false,"第4关：记忆展示时间，秒。"),
new Setting("Box","MemorySeconds5",1.6f,0.3f,15f,false,"第5关：记忆展示时间，秒。"),
new Setting("Box","GapSeconds1",0.15f,0f,2f,false,"第1关：两次换位之间的间隔，秒。"),
new Setting("Box","GapSeconds2",0.13f,0f,2f,false,"第2关：两次换位之间的间隔，秒。"),
new Setting("Box","GapSeconds3",0.1f,0f,2f,false,"第3关：两次换位之间的间隔，秒。"),
new Setting("Box","GapSeconds4",0.07f,0f,2f,false,"第4关：两次换位之间的间隔，秒。"),
new Setting("Box","GapSeconds5",0.04f,0f,2f,false,"第5关：两次换位之间的间隔，秒。"),
new Setting("Box","Swaps1",3f,1f,30f,true,"第1关：每轮交换次数。"),
new Setting("Box","Swaps2",4f,1f,30f,true,"第2关：每轮交换次数。"),
new Setting("Box","Swaps3",5f,1f,30f,true,"第3关：每轮交换次数。"),
new Setting("Box","Swaps4",7f,1f,30f,true,"第4关：每轮交换次数。"),
new Setting("Box","Swaps5",9f,1f,30f,true,"第5关：每轮交换次数。"),
new Setting("Box","Rounds",3f,1f,10f,true,"一局总轮数。"),
new Setting("Box","Wins",2f,1f,10f,true,"获胜所需正确轮数；超过总轮数时按总轮数计算。"),
new Setting("TwentyFour","Seconds1",120f,10f,600f,false,"第1关：每题限时秒数；暂停菜单暂停计时。"),
new Setting("TwentyFour","Seconds2",120f,10f,600f,false,"第2关：每题限时秒数；暂停菜单暂停计时。"),
new Setting("TwentyFour","Seconds3",120f,10f,600f,false,"第3关：每题限时秒数；暂停菜单暂停计时。"),
new Setting("TwentyFour","Seconds4",120f,10f,600f,false,"第4关：每题限时秒数；暂停菜单暂停计时。"),
new Setting("TwentyFour","Seconds5",120f,10f,600f,false,"第5关：每题限时秒数；暂停菜单暂停计时。"),
new Setting("TwentyFour","Answers1",3f,1f,10f,true,"第1关：本关答对题数。保持原题库与无限撤销。"),
new Setting("TwentyFour","Answers2",3f,1f,10f,true,"第2关：本关答对题数。保持原题库与无限撤销。"),
new Setting("TwentyFour","Answers3",3f,1f,10f,true,"第3关：本关答对题数。保持原题库与无限撤销。"),
new Setting("TwentyFour","Answers4",3f,1f,10f,true,"第4关：本关答对题数。保持原题库与无限撤销。"),
new Setting("TwentyFour","Answers5",3f,1f,10f,true,"第5关：本关答对题数。保持原题库与无限撤销。"),
new Setting("Tetris","FallSeconds1",1.05f,0.08f,3f,false,"第1关：自然下落一格的秒数；越小越快。"),
new Setting("Tetris","FallSeconds2",0.85f,0.08f,3f,false,"第2关：自然下落一格的秒数；越小越快。"),
new Setting("Tetris","FallSeconds3",0.68f,0.08f,3f,false,"第3关：自然下落一格的秒数；越小越快。"),
new Setting("Tetris","FallSeconds4",0.53f,0.08f,3f,false,"第4关：自然下落一格的秒数；越小越快。"),
new Setting("Tetris","FallSeconds5",0.42f,0.08f,3f,false,"第5关：自然下落一格的秒数；越小越快。"),
new Setting("Tetris","Lines1",4f,1f,60f,true,"第1关：过关需要消除的行数。"),
new Setting("Tetris","Lines2",6f,1f,60f,true,"第2关：过关需要消除的行数。"),
new Setting("Tetris","Lines3",8f,1f,60f,true,"第3关：过关需要消除的行数。"),
new Setting("Tetris","Lines4",10f,1f,60f,true,"第4关：过关需要消除的行数。"),
new Setting("Tetris","Lines5",12f,1f,60f,true,"第5关：过关需要消除的行数。"),
new Setting("Tetris","LockSeconds",0.5f,0.1f,2f,false,"方块接触底部后的锁定等待秒数。"),
new Setting("Snake","StepSeconds1",0.31f,0.08f,1f,false,"第1关：移动一格的秒数；越小越快。"),
new Setting("Snake","StepSeconds2",0.29f,0.08f,1f,false,"第2关：移动一格的秒数；越小越快。"),
new Setting("Snake","StepSeconds3",0.27f,0.08f,1f,false,"第3关：移动一格的秒数；越小越快。"),
new Setting("Snake","StepSeconds4",0.25f,0.08f,1f,false,"第4关：移动一格的秒数；越小越快。"),
new Setting("Snake","StepSeconds5",0.2f,0.08f,1f,false,"第5关：移动一格的秒数；越小越快。"),
new Setting("Snake","Food1",5f,1f,60f,true,"第1关：需要吃到的食物数量。"),
new Setting("Snake","Food2",6f,1f,60f,true,"第2关：需要吃到的食物数量。"),
new Setting("Snake","Food3",7f,1f,60f,true,"第3关：需要吃到的食物数量。"),
new Setting("Snake","Food4",9f,1f,60f,true,"第4关：需要吃到的食物数量。"),
new Setting("Snake","Food5",10f,1f,60f,true,"第5关：需要吃到的食物数量。"),
new Setting("Snake","Obstacles1",0f,0f,10f,true,"第1关：启用预设安全障碍位置的数量，上限10。"),
new Setting("Snake","Obstacles2",2f,0f,10f,true,"第2关：启用预设安全障碍位置的数量，上限10。"),
new Setting("Snake","Obstacles3",4f,0f,10f,true,"第3关：启用预设安全障碍位置的数量，上限10。"),
new Setting("Snake","Obstacles4",7f,0f,10f,true,"第4关：启用预设安全障碍位置的数量，上限10。"),
new Setting("Snake","Obstacles5",10f,0f,10f,true,"第5关：启用预设安全障碍位置的数量，上限10。"),
new Setting("Pacman","Beans1",24f,1f,80f,true,"第1关：需要吃掉的普通豆子数量。"),
new Setting("Pacman","Beans2",30f,1f,80f,true,"第2关：需要吃掉的普通豆子数量。"),
new Setting("Pacman","Beans3",36f,1f,80f,true,"第3关：需要吃掉的普通豆子数量。"),
new Setting("Pacman","Beans4",42f,1f,80f,true,"第4关：需要吃掉的普通豆子数量。"),
new Setting("Pacman","Beans5",48f,1f,80f,true,"第5关：需要吃掉的普通豆子数量。"),
new Setting("Pacman","Guards1",1f,1f,3f,true,"第1关：幽灵数量。"),
new Setting("Pacman","Guards2",1f,1f,3f,true,"第2关：幽灵数量。"),
new Setting("Pacman","Guards3",2f,1f,3f,true,"第3关：幽灵数量。"),
new Setting("Pacman","Guards4",2f,1f,3f,true,"第4关：幽灵数量。"),
new Setting("Pacman","Guards5",3f,1f,3f,true,"第5关：幽灵数量。"),
new Setting("Pacman","GuardPeriod1",4f,1f,10f,true,"第1关：玩家每走多少步幽灵走一步；越大越容易。"),
new Setting("Pacman","GuardPeriod2",3f,1f,10f,true,"第2关：玩家每走多少步幽灵走一步；越大越容易。"),
new Setting("Pacman","GuardPeriod3",3f,1f,10f,true,"第3关：玩家每走多少步幽灵走一步；越大越容易。"),
new Setting("Pacman","GuardPeriod4",2f,1f,10f,true,"第4关：玩家每走多少步幽灵走一步；越大越容易。"),
new Setting("Pacman","GuardPeriod5",2f,1f,10f,true,"第5关：玩家每走多少步幽灵走一步；越大越容易。"),
new Setting("Pacman","Lives",3f,1f,9f,true,"初始生命数。"),
new Setting("Pacman","StepSeconds",0.17f,0.08f,0.8f,false,"玩家走一格的秒数。"),
new Setting("Pacman","PowerSteps",42f,5f,180f,true,"能量豆效果持续的移动步数。"),
new Setting("Bubble","AiSeconds1",0.7f,0.1f,2f,false,"第1关：电脑行动间隔秒数；越小越快。"),
new Setting("Bubble","AiSeconds2",0.53f,0.1f,2f,false,"第2关：电脑行动间隔秒数；越小越快。"),
new Setting("Bubble","AiSeconds3",0.39f,0.1f,2f,false,"第3关：电脑行动间隔秒数；越小越快。"),
new Setting("Bubble","AiSeconds4",0.29f,0.1f,2f,false,"第4关：电脑行动间隔秒数；越小越快。"),
new Setting("Bubble","AiSeconds5",0.23f,0.1f,2f,false,"第5关：电脑行动间隔秒数；越小越快。"),
new Setting("Bubble","AiLives1",1f,1f,9f,true,"第1关：电脑初始生命数。"),
new Setting("Bubble","AiLives2",2f,1f,9f,true,"第2关：电脑初始生命数。"),
new Setting("Bubble","AiLives3",2f,1f,9f,true,"第3关：电脑初始生命数。"),
new Setting("Bubble","AiLives4",2f,1f,9f,true,"第4关：电脑初始生命数。"),
new Setting("Bubble","AiLives5",2f,1f,9f,true,"第5关：电脑初始生命数。"),
new Setting("Bubble","Lives",2f,1f,9f,true,"玩家初始生命数。"),
new Setting("Bubble","Range",2f,1f,5f,true,"玩家初始泡泡范围，格数。"),
new Setting("Bubble","Capacity",1f,1f,3f,true,"玩家初始可同时放置的泡泡数。"),
new Setting("Landlord","AiLevel1",1f,1f,5f,true,"第1关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Landlord","AiLevel2",2f,1f,5f,true,"第2关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Landlord","AiLevel3",3f,1f,5f,true,"第3关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Landlord","AiLevel4",4f,1f,5f,true,"第4关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Landlord","AiLevel5",5f,1f,5f,true,"第5关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Mahjong","AiLevel1",1f,1f,5f,true,"第1关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Mahjong","AiLevel2",2f,1f,5f,true,"第2关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Mahjong","AiLevel3",3f,1f,5f,true,"第3关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Mahjong","AiLevel4",4f,1f,5f,true,"第4关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Mahjong","AiLevel5",5f,1f,5f,true,"第5关：电脑策略等级；1较弱，5较强。提示按钮仍使用最强策略。"),
new Setting("Mario","Lives",3f,1f,9f,true,"初始生命数。"),
new Setting("Contra","Lives",3f,1f,9f,true,"初始生命数。"),
new Setting("Mario","TimeScale",1f,0.25f,4f,false,"关卡原始计时额度的倍率；越大时间越充裕。"),
new Setting("Mario","StarSeconds",10f,1f,60f,false,"无敌星持续秒数。"),
new Setting("Mario","HurtGraceSeconds",2f,0.2f,10f,false,"大马里奥受伤缩小后的无敌秒数。"),
new Setting("Contra","BossHP",32f,1f,150f,true,"丛林关底设施生命值。"),
new Setting("Contra","BatteryHP",5f,1f,30f,true,"固定炮台生命值。"),
new Setting("Contra","RespawnGraceSeconds",3f,0.2f,15f,false,"复活后的无敌秒数。"),
new Setting("Audio","TableBgmVolume",0.38f,0f,1f,false,"UNO、五子棋、泡泡、寻物、24点、方块、斗地主、贪吃蛇、吃豆人、麻将及NDS菜单的本地背景音乐音量。"),
new Setting("Audio","MarioBgmVolume",0.75f,0f,1f,false,"马里奥背景音乐音量。"),
new Setting("Audio","ContraBgmVolume",1f,0f,1f,false,"魂斗罗背景音乐音量。"),
 };
 static Dictionary<string,float> values=Defaults();
 static Dictionary<string,float> Defaults(){var result=new Dictionary<string,float>();foreach(var s in Settings)result.Add(s.Section+"."+s.Key,s.Default);return result;}
 // Publish a complete startup snapshot; game/AI threads never see a partially loaded config.
 public static void Configure(Func<Setting,float> read){var result=new Dictionary<string,float>();foreach(var s in Settings)result.Add(s.Section+"."+s.Key,s.Clamp(read(s)));values=result;}
 // One active round owns overrides; disposing restores the user's BepInEx settings.
 static Scope activeScope;
 public static IDisposable Push(IReadOnlyDictionary<string,float> overrides){
  if(activeScope!=null)throw new InvalidOperationException("Another minigame tuning scope is active");
  var saved=values;var next=new Dictionary<string,float>(saved);
  foreach(var item in overrides){var setting=Array.Find(Settings,s=>s.Section+"."+s.Key==item.Key);if(setting==null)throw new ArgumentException("Unknown tuning parameter: "+item.Key);next[item.Key]=setting.Clamp(item.Value);}
  var scope=new Scope(saved);values=next;activeScope=scope;return scope;
 }
 sealed class Scope:IDisposable{readonly Dictionary<string,float> saved;bool disposed;internal Scope(Dictionary<string,float> value){saved=value;}public void Dispose(){if(disposed)return;disposed=true;if(activeScope==this){values=saved;activeScope=null;}}}
 /// <summary>表里定义了、但 Settings 没有列出的关卡（例如作者加的第 6 关）从这里补读；返回 null 表示没有。参数：分组、键、关卡、同键的已知设定（可为 null）。</summary>
 public static Func<string,string,int,Setting,float?> Fallback;
 public static float Get(string section,string key,int level=0){
  string suffix=level>0?level.ToString(System.Globalization.CultureInfo.InvariantCulture):"";
  if(values.TryGetValue(section+"."+key+suffix,out float v))return v;
  if(level>0){
   var known=Array.Find(Settings,s=>s.Section==section&&s.Key==key+"1");
   float? extra=Fallback?.Invoke(section,key,level,known);
   if(extra.HasValue)return extra.Value;
   // 作者没有为这一关配数值：沿用已定义的最高一关。
   for(int l=level-1;l>=1;l--)if(values.TryGetValue(section+"."+key+l,out v))return v;
  }
  throw new KeyNotFoundException("Unknown tuning parameter: "+section+"."+key+suffix);
 }
 public static int Int(string section,string key,int level=0)=>(int)Get(section,key,level);
}
}
