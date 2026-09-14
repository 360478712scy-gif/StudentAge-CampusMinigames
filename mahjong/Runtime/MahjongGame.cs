using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;
namespace StudentAge.Mahjong
{
    public enum Phase { Discard, Draw, Claim, Finished }
    public enum Call { Pass, Chi, Peng, Gang, Hu }
    public sealed class Meld { public Call Kind; public List<int> Tiles=new List<int>(); public int From; public bool Concealed; }
    public sealed class RiverTile { public int Tile,Seat; public bool Taken; }
    public sealed class Move { public string Kind; public int Seat,Tile,From=-1; }
    // One 136-tile hand. No flowers, betting, fan threshold, or hidden information in bot decisions.
    public sealed class MahjongGame
    {
        public readonly List<int>[] Hands=Enumerable.Range(0,4).Select(_=>new List<int>()).ToArray();
        public readonly List<Meld>[] Melds=Enumerable.Range(0,4).Select(_=>new List<Meld>()).ToArray();
        public readonly List<RiverTile>[] Rivers=Enumerable.Range(0,4).Select(_=>new List<RiverTile>()).ToArray();
        public readonly Queue<Move> Events=new Queue<Move>();
        public readonly int[] Wall; public int Head{get;private set;} public int Tail{get;private set;}=135;
        public int Remaining=>Math.Max(0,Tail-Head+1); public int Turn{get;private set;}
        public Phase State{get;private set;} public int Winner{get;private set;}=-1;public bool SelfDraw{get;private set;}
        public bool DrawnGame=>State==Phase.Finished&&Winner<0;public int LastDraw{get;private set;}=-1;
        public RiverTile LastDiscard{get;private set;} public int Level{get;}
        readonly Random random; readonly bool[] answered=new bool[4];readonly Call[] replies=new Call[4];readonly int[] chiStarts=new int[4];
        int robSeat=-1,robTile=-1;Meld robMeld;public bool RobbingKong=>robSeat>=0;
        public MahjongGame(int level,int seed)
        {
            Level=Math.Max(1,Math.Min(5,level));random=new Random(seed);Wall=Enumerable.Range(0,136).ToArray();
            for(int n=135;n>0;n--){int j=random.Next(n+1),a=Wall[n];Wall[n]=Wall[j];Wall[j]=a;}
            for(int round=0;round<13;round++)for(int seat=0;seat<4;seat++)Hands[seat].Add(Wall[Head++]);
            foreach(var hand in Hands)hand.Sort();Turn=0;State=Phase.Draw;Draw();Events.Clear();
        }
        public static int Kind(int id)=>id/4;
        public static string Name(int kind)=>kind<9?(kind+1)+"万":kind<18?(kind-8)+"条":kind<27?(kind-17)+"筒":new[]{"东","南","西","北","红中","发财","白板"}[kind-27];
        public static bool Winning(IEnumerable<int> tiles,int open=0)
        {
            if(open<0||open>4)return false;var ids=tiles.ToArray();if(ids.Length!=14-open*3||ids.Any(t=>t<0||t>=136))return false;
            var c=new int[34];foreach(int id in ids)if(++c[Kind(id)]>4)return false;
            if(open==0&&c.Count(v=>v==2)==7)return true;
            for(int n=0;n<34;n++)if(c[n]>=2){c[n]-=2;if(Sets(c,4-open)){c[n]+=2;return true;}c[n]+=2;}return false;
        }
        static bool Sets(int[] c,int needed)
        {
            int i=Array.FindIndex(c,x=>x>0);if(i<0)return needed==0;if(needed<=0)return false;
            if(c[i]>=3){c[i]-=3;bool ok=Sets(c,needed-1);c[i]+=3;if(ok)return true;}
            if(i<27&&i%9<=6&&c[i+1]>0&&c[i+2]>0){c[i]--;c[i+1]--;c[i+2]--;bool ok=Sets(c,needed-1);c[i]++;c[i+1]++;c[i+2]++;if(ok)return true;}return false;
        }
        void Emit(string kind,int seat,int tile=-1,int from=-1)=>Events.Enqueue(new Move{Kind=kind,Seat=seat,Tile=tile,From=from});
        public bool Draw(bool replacement=false)
        {
            if(State!=Phase.Draw)return false;if(Remaining==0){State=Phase.Finished;Emit("流局",Turn);return true;}
            int tile=replacement?Wall[Tail--]:Wall[Head++];Hands[Turn].Add(tile);Hands[Turn].Sort();LastDraw=tile;State=Phase.Discard;Emit("摸牌",Turn,tile);return true;
        }
        public bool Discard(int seat,int tile)
        {
            if(State!=Phase.Discard||seat!=Turn||!Hands[seat].Remove(tile))return false;
            LastDiscard=new RiverTile{Seat=seat,Tile=tile};Rivers[seat].Add(LastDiscard);LastDraw=-1;Emit("出牌",seat,tile);OpenClaims();return true;
        }
        public bool CanHu(int seat)
        {
            if(seat<0||seat>3)return false;
            if(State==Phase.Discard)return Turn==seat&&Winning(Hands[seat],Melds[seat].Count);
            if(State!=Phase.Claim||seat==Discarder)return false;
            return Winning(Hands[seat].Concat(new[]{ClaimTile}),Melds[seat].Count);
        }
        int Discarder=>RobbingKong?robSeat:LastDiscard.Seat;int ClaimTile=>RobbingKong?robTile:LastDiscard.Tile;
        public List<int> ChiOptions(int seat)
        {
            var result=new List<int>();if(State!=Phase.Claim||RobbingKong||seat!=(Discarder+1)%4)return result;int k=Kind(ClaimTile);if(k>=27)return result;
            for(int start=Math.Max(k-k%9,k-2);start<=Math.Min(k,k-k%9+6);start++)
            {bool ok=true;for(int n=start;n<start+3;n++)if(n!=k&&!Hands[seat].Any(t=>Kind(t)==n))ok=false;if(ok)result.Add(start);}return result;
        }
        public List<Call> Options(int seat)
        {
            var r=new List<Call>();if(State!=Phase.Claim||seat==Discarder||answered[seat])return r;
            if(CanHu(seat))r.Add(Call.Hu);if(!RobbingKong){int count=Hands[seat].Count(t=>Kind(t)==Kind(ClaimTile));if(count>=3&&Remaining>0)r.Add(Call.Gang);if(count>=2)r.Add(Call.Peng);if(ChiOptions(seat).Count>0)r.Add(Call.Chi);}return r;
        }
        void OpenClaims()
        {
            State=Phase.Claim;for(int n=0;n<4;n++){answered[n]=false;replies[n]=Call.Pass;chiStarts[n]=-1;}
            for(int n=0;n<4;n++)if(n==Discarder||Options(n).Count==0)answered[n]=true;
            Resolve();
        }
        public bool Respond(int seat,Call call,int chiStart=-1)
        {
            if(State!=Phase.Claim||seat<0||seat>3||answered[seat]||seat==Discarder)return false;
            if(call!=Call.Pass&&!Options(seat).Contains(call))return false;
            if(call==Call.Chi&&!ChiOptions(seat).Contains(chiStart))return false;
            answered[seat]=true;replies[seat]=call;chiStarts[seat]=chiStart;Resolve();return true;
        }
        void Resolve()
        {
            if(State!=Phase.Claim||answered.Any(x=>!x))return;int from=Discarder,tile=ClaimTile;
            for(int step=1;step<=3;step++){int seat=(from+step)%4;if(replies[seat]==Call.Hu){if(RobbingKong)Hands[from].Remove(tile);else LastDiscard.Taken=true;Hands[seat].Add(tile);Hands[seat].Sort();Winner=seat;SelfDraw=false;State=Phase.Finished;Emit(RobbingKong?"抢杠胡":"胡",seat,tile,from);robSeat=-1;return;}}
            if(RobbingKong){Hands[robSeat].Remove(robTile);robMeld.Tiles.Add(robTile);robMeld.Kind=Call.Gang;Turn=robSeat;robSeat=-1;Emit("杠",Turn,tile);State=Phase.Draw;Draw(true);return;}
            int chosen=-1;for(int step=1;step<=3;step++){int n=(from+step)%4;if(replies[n]==Call.Gang||replies[n]==Call.Peng){chosen=n;break;}}
            if(chosen<0){int n=(from+1)%4;if(replies[n]==Call.Chi)chosen=n;}
            if(chosen>=0)
            {
                var call=replies[chosen];var meld=new Meld{Kind=call,From=from};int k=Kind(tile);meld.Tiles.Add(tile);
                if(call==Call.Chi){for(int n=chiStarts[chosen];n<chiStarts[chosen]+3;n++)if(n!=k)TakeKind(chosen,n,meld);}
                else for(int n=0;n<(call==Call.Gang?3:2);n++)TakeKind(chosen,k,meld);
                meld.Tiles.Sort();Melds[chosen].Add(meld);LastDiscard.Taken=true;Turn=chosen;State=call==Call.Gang?Phase.Draw:Phase.Discard;Emit(call==Call.Chi?"吃":call==Call.Peng?"碰":"杠",chosen,tile,from);if(call==Call.Gang)Draw(true);return;
            }
            Turn=(from+1)%4;State=Phase.Draw;
        }
        void TakeKind(int seat,int kind,Meld meld){int id=Hands[seat].First(t=>Kind(t)==kind);Hands[seat].Remove(id);meld.Tiles.Add(id);}
        public List<int> OwnKongs(int seat)
        {
            var result=new List<int>();if(State!=Phase.Discard||seat!=Turn||Remaining==0)return result;
            result.AddRange(Hands[seat].GroupBy(Kind).Where(g=>g.Count()==4).Select(g=>g.Key));
            result.AddRange(Melds[seat].Where(m=>m.Kind==Call.Peng&&Hands[seat].Any(t=>Kind(t)==Kind(m.Tiles[0]))).Select(m=>Kind(m.Tiles[0])));return result;
        }
        public bool Kong(int seat,int kind)
        {
            if(!OwnKongs(seat).Contains(kind))return false;var pung=Melds[seat].FirstOrDefault(m=>m.Kind==Call.Peng&&Kind(m.Tiles[0])==kind);
            if(pung!=null){robSeat=seat;robTile=Hands[seat].First(t=>Kind(t)==kind);robMeld=pung;OpenClaims();return true;}
            var meld=new Meld{Kind=Call.Gang,From=seat,Concealed=true};for(int n=0;n<4;n++)TakeKind(seat,kind,meld);Melds[seat].Add(meld);Emit("暗杠",seat,meld.Tiles[0]);State=Phase.Draw;Draw(true);return true;
        }
        public bool Hu(int seat)
        {
            if(State!=Phase.Discard||!CanHu(seat))return false;Winner=seat;SelfDraw=true;State=Phase.Finished;Emit("自摸",seat,LastDraw);return true;
        }
        public bool Awaiting(int seat)=>State==Phase.Claim&&!answered[seat];
        // Bot evaluation uses only its own concealed hand and publicly exposed tiles.
        public int ChooseDiscard(int seat,int strength=0)
        {
            var hand=Hands[seat];if(hand.Count==0)return -1;int skill=strength>0?strength:Tuning.Int("Mahjong","AiLevel",Level);
            if(skill<5&&random.NextDouble()<(6-skill)*.085)return hand[random.Next(hand.Count)];
            double best=double.NegativeInfinity;int chosen=hand[0];var c=new int[34];foreach(int id in hand)c[Kind(id)]++;
            foreach(int id in hand.GroupBy(Kind).Select(g=>g.First()))
            {
                int k=Kind(id);c[k]--;double value=Shape(c,Melds[seat].Count);
                if(skill>=3){int visible=Rivers.Sum(r=>r.Count(t=>!t.Taken&&Kind(t.Tile)==k))+Melds.Sum(ms=>ms.Sum(m=>m.Tiles.Count(t=>Kind(t)==k)));value+=visible*.25;}
                if(value>best){best=value;chosen=id;}c[k]++;
            }return chosen;
        }
        static double Shape(int[] c,int open)
        {
            // Greedy structural score with suited connections; full winning predicate remains exact.
            var a=(int[])c.Clone();double v=open*18;int pairs=0;
            for(int n=0;n<34;n++){if(a[n]>=3){v+=18;a[n]-=3;}if(a[n]==2){v+=8;pairs++;}}
            for(int n=0;n<27;n++)if(n%9<=6)while(a[n]>0&&a[n+1]>0&&a[n+2]>0){v+=17;a[n]--;a[n+1]--;a[n+2]--;}
            for(int n=0;n<27;n++)if(a[n]>0){if(n%9<8&&a[n+1]>0)v+=4;if(n%9<7&&a[n+2]>0)v+=2;if(n%9>0&&n%9<8)v+=.35;}
            return v+(pairs>0?3:0);
        }
        public void BotRespond(int seat)
        {
            if(!Awaiting(seat))return;var opts=Options(seat);if(opts.Contains(Call.Hu)){Respond(seat,Call.Hu);return;}
            if(opts.Contains(Call.Gang)){Respond(seat,Call.Gang);return;}
            if(opts.Contains(Call.Peng)){Respond(seat,Call.Peng);return;}
            var chi=ChiOptions(seat);if(chi.Count>0){Respond(seat,Call.Chi,chi[0]);return;}Respond(seat,Call.Pass);
        }
        public void BotTurn()
        {if(State==Phase.Draw){Draw();return;}if(State==Phase.Claim){for(int n=0;n<4;n++)if(Awaiting(n))BotRespond(n);return;}if(State!=Phase.Discard)return;if(CanHu(Turn)){Hu(Turn);return;}var kong=OwnKongs(Turn);if(kong.Count>0){Kong(Turn,kong[0]);return;}Discard(Turn,ChooseDiscard(Turn));}
    }
}
