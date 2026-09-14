using System;using System.Linq;using System.Collections.Generic;using StudentAge.PlayingCards;
namespace StudentAge.CampusPuzzles
{
    public enum LandlordKind{Invalid,Single,Pair,Triple,TripleSingle,TriplePair,Straight,PairRun,TripleRun,PlaneSingles,PlanePairs,FourSingles,FourPairs,Bomb,Rocket}
    public readonly struct LandlordPattern
    {
        public readonly LandlordKind Kind;public readonly int High,Count,Units;
        public LandlordPattern(LandlordKind kind,int high,int count,int units=1){Kind=kind;High=high;Count=count;Units=units;}
        public bool Valid=>Kind!=LandlordKind.Invalid;
        public bool Beats(LandlordPattern other){if(!Valid)return false;if(!other.Valid)return true;if(Kind==LandlordKind.Rocket)return other.Kind!=LandlordKind.Rocket;if(other.Kind==LandlordKind.Rocket)return false;if(Kind==LandlordKind.Bomb&&other.Kind!=LandlordKind.Bomb)return true;return Kind==other.Kind&&Count==other.Count&&Units==other.Units&&High>other.High;}
    }
    public static class LandlordRules
    {
        public static int Rank(PlayingCard c)=>c.Suit==CardSuit.Joker?15+c.Rank:c.Rank==1?14:c.Rank==2?15:c.Rank;
        public static LandlordPattern Classify(IEnumerable<PlayingCard> cards){var array=cards.ToArray();if(array.Select(c=>c.Id).Distinct().Count()!=array.Length)return default;return ClassifyRanks(array.Select(Rank).ToArray());}
        public static LandlordPattern ClassifyRanks(int[] ranks)
        {
            int n=ranks.Length;if(n==0||ranks.Any(r=>r<3||r>17))return default;int[] c=new int[18];foreach(int r in ranks)c[r]++;if(c.Take(16).Any(x=>x>4)||c[16]>1||c[17]>1)return default;var keys=Enumerable.Range(3,15).Where(r=>c[r]>0).ToArray();int high=keys.Last();
            if(n==1)return new LandlordPattern(LandlordKind.Single,high,n);if(n==2&&c[16]==1&&c[17]==1)return new LandlordPattern(LandlordKind.Rocket,17,n);
            if(keys.Length==1)return new LandlordPattern(n==2?LandlordKind.Pair:n==3?LandlordKind.Triple:n==4?LandlordKind.Bomb:LandlordKind.Invalid,high,n);
            int triple=Array.FindIndex(c,x=>x==3),quad=Array.FindIndex(c,x=>x==4);
            if(n==4&&triple>=3)return new LandlordPattern(LandlordKind.TripleSingle,triple,n);
            if(n==5&&triple>=3&&keys.Length==2)return new LandlordPattern(LandlordKind.TriplePair,triple,n);
            if(n==6&&quad>=3&&!(c[16]>0&&c[17]>0))return new LandlordPattern(LandlordKind.FourSingles,quad,n);
            if(n==8&&quad>=3&&keys.Length==3&&keys.Where(r=>r!=quad).All(r=>c[r]==2))return new LandlordPattern(LandlordKind.FourPairs,quad,n);
            bool consecutive=high<=14&&keys.Last()-keys.First()+1==keys.Length;
            if(consecutive){if(n>=5&&keys.All(r=>c[r]==1))return new LandlordPattern(LandlordKind.Straight,high,n,keys.Length);if(keys.Length>=3&&keys.All(r=>c[r]==2))return new LandlordPattern(LandlordKind.PairRun,high,n,keys.Length);if(keys.Length>=2&&keys.All(r=>c[r]==3))return new LandlordPattern(LandlordKind.TripleRun,high,n,keys.Length);}
            foreach(int wing in new[]{1,2}){if(n%(3+wing)!=0)continue;int length=n/(3+wing);if(length<2)continue;for(int start=3;start+length-1<=14;start++){bool ok=true;for(int r=start;r<start+length;r++)if(c[r]!=3)ok=false;if(!ok)continue;var rest=keys.Where(r=>r<start||r>=start+length).ToArray();if(wing==1&&rest.All(r=>c[r]<=2)&&!(c[16]>0&&c[17]>0)||wing==2&&rest.Length==length&&rest.All(r=>c[r]==2))return new LandlordPattern(wing==1?LandlordKind.PlaneSingles:LandlordKind.PlanePairs,start+length-1,n,length);}}
            return default;
        }
        public static List<PlayingCard[]> Moves(IReadOnlyList<PlayingCard> hand,LandlordPattern previous=default)
        {
            var groups=hand.GroupBy(Rank).ToDictionary(g=>g.Key,g=>g.OrderBy(c=>c.Id).ToArray());var result=new List<PlayingCard[]>();var seen=new HashSet<string>();
            Action<List<int>> add=ranks=>{var type=ClassifyRanks(ranks.ToArray());if(!type.Beats(previous))return;string key=string.Join(",",ranks.OrderBy(r=>r));if(!seen.Add(key))return;var take=new Dictionary<int,int>();var cards=new List<PlayingCard>();foreach(int r in ranks){int index;take.TryGetValue(r,out index);if(!groups.ContainsKey(r)||index>=groups[r].Length)return;cards.Add(groups[r][index]);take[r]=index+1;}result.Add(cards.ToArray());};
            foreach(var g in groups){for(int n=1;n<=g.Value.Length;n++)add(Enumerable.Repeat(g.Key,n).ToList());if(g.Value.Length>=3){foreach(var h in groups.Where(x=>x.Key!=g.Key)){add(new List<int>{g.Key,g.Key,g.Key,h.Key});if(h.Value.Length>=2)add(new List<int>{g.Key,g.Key,g.Key,h.Key,h.Key});}}if(g.Value.Length==4){var core=Enumerable.Repeat(g.Key,4).ToList();foreach(var wing in Wings(groups,new[]{g.Key},2,false))add(core.Concat(wing).ToList());foreach(var wing in Wings(groups,new[]{g.Key},2,true))add(core.Concat(wing).ToList());}}
            if(groups.ContainsKey(16)&&groups.ContainsKey(17))add(new List<int>{16,17});
            for(int copies=1;copies<=3;copies++)for(int start=3;start<=14;start++){var core=new List<int>();for(int end=start;end<=14&&groups.ContainsKey(end)&&groups[end].Length>=copies;end++){core.AddRange(Enumerable.Repeat(end,copies));int length=end-start+1;if(length<(copies==1?5:copies==2?3:2))continue;add(new List<int>(core));if(copies==3){var excluded=Enumerable.Range(start,length).ToArray();foreach(var wing in Wings(groups,excluded,length,false))add(core.Concat(wing).ToList());foreach(var wing in Wings(groups,excluded,length,true))add(core.Concat(wing).ToList());}}}
            return result;
        }
        static IEnumerable<int[]> Wings(Dictionary<int,PlayingCard[]> groups,int[] exclude,int count,bool pairs)
        {
            var keys=groups.Keys.Where(r=>!exclude.Contains(r)&&(!pairs||groups[r].Length>=2)).OrderBy(r=>r).ToArray();var chosen=new List<int>();return WingRecurse(keys,groups,0,count,pairs,chosen);
        }
        static IEnumerable<int[]> WingRecurse(int[] keys,Dictionary<int,PlayingCard[]> groups,int at,int remaining,bool pairs,List<int> chosen)
        {
            if(remaining==0){yield return chosen.ToArray();yield break;}if(at>=keys.Length)yield break;int rank=keys[at],max=pairs?1:Math.Min(2,groups[rank].Length);for(int n=0;n<=Math.Min(max,remaining);n++){int add=n*(pairs?2:1);for(int k=0;k<add;k++)chosen.Add(rank);foreach(var wing in WingRecurse(keys,groups,at+1,remaining-n,pairs,chosen))yield return wing;if(add>0)chosen.RemoveRange(chosen.Count-add,add);}
        }
        public static string Name(LandlordKind k){switch(k){case LandlordKind.Single:return "单张";case LandlordKind.Pair:return "对子";case LandlordKind.Triple:return "三张";case LandlordKind.TripleSingle:return "三带一";case LandlordKind.TriplePair:return "三带二";case LandlordKind.Straight:return "顺子";case LandlordKind.PairRun:return "连对";case LandlordKind.TripleRun:case LandlordKind.PlaneSingles:case LandlordKind.PlanePairs:return "飞机";case LandlordKind.FourSingles:case LandlordKind.FourPairs:return "四带二";case LandlordKind.Bomb:return "炸弹";case LandlordKind.Rocket:return "王炸";default:return "";}}
    }
}
