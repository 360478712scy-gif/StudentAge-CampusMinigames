using System;
using System.Collections.Generic;
using System.Linq;
namespace StudentAge.Sanguosha {
 public static class Encounters {
  // 默认对手/奖励表；SanguoshaCfg.json 里的同名行会在启动时通过 Configure 覆盖。
  static readonly Dictionary<int,string[]> defaults=new Dictionary<int,string[]> {
   {3,new[]{"ganning","zhangliao","huanggai"}},
   {101,new[]{"liubei","sunshangxiang","huatuo"}},
   {102,new[]{"zhouyu","simayi","zhugeliang"}},
   {103,new[]{"huangzhong","guanyu","zhoutai"}},
   {104,new[]{"xiaoqiao","daqiao","zhenji"}},
   {105,new[]{"luxun","huangyueying","guojia"}},
   {201,new[]{"machao","zhangfei","sunquan"}},
   {202,new[]{"caocao","diaochan","zhangjiao"}},
   {203,new[]{"lvmeng","caoren","xiahoudun"}},
   {204,new[]{"xuchu","xiahouyuan","lvbu"}}
  };
  static readonly string[] defaultFallback={"weiyan","yuji","zhaoyun"};
  static Dictionary<int,string[]> rewards=new Dictionary<int,string[]>(defaults);
  static string[] fallback=defaultFallback;
  public static string[] RewardPool{get;private set;}=defaults.Values.SelectMany(s=>s).ToArray();
  public static int[] Npcs=>rewards.Keys.ToArray();
  public static IReadOnlyDictionary<int,string[]> Defaults=>defaults;
  public static string[] DefaultFallback=>(string[])defaultFallback.Clone();
  /// <summary>用 SanguoshaCfg.json 覆盖对手表：table 的键是 npc id，值是三关对手武将 id；fallbackGenerals 给未列出的角色用。传 null 表示保留默认。</summary>
  public static void Configure(IReadOnlyDictionary<int,string[]> table,string[] fallbackGenerals=null){
   var next=new Dictionary<int,string[]>(defaults);
   if(table!=null)foreach(var pair in table){if(pair.Value!=null&&pair.Value.Length>=3)next[pair.Key]=(string[])pair.Value.Clone();}
   rewards=next;fallback=fallbackGenerals!=null&&fallbackGenerals.Length>=3?(string[])fallbackGenerals.Clone():defaultFallback;
   RewardPool=rewards.Values.SelectMany(s=>s).Distinct().ToArray();
  }
  public static string[] For(int npcId){if(rewards.TryGetValue(npcId,out var cards))return (string[])cards.Clone();
   // Extra mod characters retain playable opponents; they do not reserve any shop cards.
   return (string[])fallback.Clone();}
  public static bool OnRoute(int init,bool male)=>init==2||init==(male?3:4);
  public static bool Sold(string general)=>general!="zhaoyun"&&!RewardPool.Contains(general);
  public static bool ShopEligible(string general,IEnumerable<int> available,IEnumerable<int> unfollowed,Progress progress){
   if(general=="zhaoyun")return false;if(Sold(general))return true;
   var owner=rewards.First(p=>p.Value.Contains(general));
   if(!available.Contains(owner.Key)||unfollowed.Contains(owner.Key))return true;
   // A remapped reward behind an already cleared stage must remain obtainable in old saves.
   return progress!=null&&Array.IndexOf(owner.Value,general)<progress.Stage(owner.Key);
  }
  public static int Price(string general)=>general=="shenguanyu"||general=="shenlvmeng"?100:20;
 }
 public sealed class Progress {
  public int Schema=1;public int LastRound=-1;public Dictionary<int,int> Wins=new Dictionary<int,int>();
  // Export IDs of first-stage stories already shown in this save (victory 12221xx / defeat 12221xx); absent in older records.
  public List<int> Stories=new List<int>();
  public bool StoryTold(int evt)=>evt>0&&Stories!=null&&Stories.Contains(evt);
  public bool MarkStory(int evt){if(evt<=0)return false;if(Stories==null)Stories=new List<int>();if(Stories.Contains(evt))return false;Stories.Add(evt);return true;}
  public int Stage(int npc)=>Wins.TryGetValue(npc,out int n)?Math.Max(0,Math.Min(3,n)):0;
  public bool CanBegin(int npc,int round)=>npc>0&&round!=LastRound&&Stage(npc)<3;
  public bool Reserve(int npc,int round){if(!CanBegin(npc,round))return false;LastRound=round;return true;}
  public string CompleteWin(int npc){int n=Stage(npc);if(n>=3)return null;string reward=Encounters.For(npc)[n];Wins[npc]=n+1;return reward;}
 }
}
