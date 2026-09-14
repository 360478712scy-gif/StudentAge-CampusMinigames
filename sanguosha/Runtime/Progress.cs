using System;
using System.Collections.Generic;
using System.Linq;
namespace StudentAge.Sanguosha {
 // The explicitly paired routes share the SAME three generals, not merely similar difficulty.
 public static class Encounters {
  static readonly string[][] sets={
   new[]{"ganning","zhangliao","sunquan"},
   new[]{"liubei","sunshangxiang","huatuo"},
   new[]{"zhouyu","simayi","zhugeliang"},
   new[]{"huangzhong","guanyu","zhoutai"},
   new[]{"xiaoqiao","daqiao","zhenji"},
   new[]{"luxun","huangyueying","guojia"},
   new[]{"machao","zhangfei","weiyan"},
   new[]{"lvmeng","caoren","xiahoudun"}};
  static readonly Dictionary<int,int> pairs=new Dictionary<int,int>{{3,0},{101,1},{102,2},{103,3},{204,3},{104,4},{105,5},{202,5},{201,6},{203,7}};
  public static readonly string[] RewardPool=sets.SelectMany(s=>s).Distinct().ToArray();
  public static string[] For(int npcId){if(pairs.TryGetValue(npcId,out int n))return (string[])sets[n].Clone();
   // Fallback uses only the common reward pool. New NPCs cannot create route-exclusive cards.
   int start=(int)((uint)npcId%sets.Length);return (string[])sets[start].Clone();}
  public static bool Sold(string general)=>general!="zhaoyun"&&!RewardPool.Contains(general);
  public static int Price(string general)=>general=="shenguanyu"||general=="shenlvmeng"?100:20;
 }
 public sealed class Progress {
  public int Schema=1;public int LastRound=-1;public Dictionary<int,int> Wins=new Dictionary<int,int>();
  public int Stage(int npc)=>Wins.TryGetValue(npc,out int n)?Math.Max(0,Math.Min(3,n)):0;
  public bool CanBegin(int npc,int round)=>npc>0&&round!=LastRound&&Stage(npc)<3;
  public bool Reserve(int npc,int round){if(!CanBegin(npc,round))return false;LastRound=round;return true;}
  public string CompleteWin(int npc){int n=Stage(npc);if(n>=3)return null;string reward=Encounters.For(npc)[n];Wins[npc]=n+1;return reward;}
 }
}
