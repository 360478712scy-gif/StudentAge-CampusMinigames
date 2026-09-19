using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentAge.CampusBubble
{
    public enum Tile { Floor, Desk, Box, Bag }
    public enum MatchResult { Playing, Win, Lose, Draw }
    public sealed class Student
    {
        public int Cell, Lives=2, Range=2, Capacity=1;
        public float NextMove, InvulnerableUntil;
    }
    public sealed class Bubble
    {
        public int Id, Cell, Owner, Range;
        public float BurstAt;
    }
    public struct FieldEvent
    {
        public string Kind;public int Cell, Actor;
        public FieldEvent(string kind,int cell,int actor=-1){Kind=kind;Cell=cell;Actor=actor;}
    }
    // Pure deterministic grid simulation. Unity only supplies input, time and presentation.
    public sealed class BubbleGame
    {
        public const int Width=13,Height=11,Count=Width*Height;
        public const float Fuse=2.35f,WaveTime=.42f,HumanStep=.16f;
        public readonly Tile[] Tiles=new Tile[Count];
        public readonly int[] Items=new int[Count];
        public readonly float[] WetUntil=new float[Count];
        public readonly Student[] Players={new Student(),new Student()};
        public readonly List<Bubble> Bubbles=new List<Bubble>();
        public readonly Queue<FieldEvent> Events=new Queue<FieldEvent>();
        public readonly int Level;
        public float Clock{get;private set;}
        public MatchResult Result{get;private set;}
        readonly Random random;int nextId;float nextAI,aiBombAt;
        static readonly int[] Steps={-Width,1,Width,-1};
        public BubbleGame(int seed,int level)
        {
            if(level<1||level>5)throw new ArgumentOutOfRangeException(nameof(level));
            Level=level;random=new Random(seed);Players[0].Cell=Width+1;Players[1].Cell=(Height-2)*Width+Width-2;
            Players[0].Lives=Tuning.Int("Bubble","Lives");Players[0].Range=Tuning.Int("Bubble","Range");Players[0].Capacity=Tuning.Int("Bubble","Capacity");Players[1].Lives=Tuning.Int("Bubble","AiLives",Level);Players[1].Range=level>=4?3:2;Players[1].Capacity=level>=3?2:1;
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
            {
                int i=y*Width+x;
                if(x==0||y==0||x==Width-1||y==Height-1||(x%2==0&&y%2==0))Tiles[i]=Tile.Desk;
                else if(i<Count/2&&random.NextDouble()<.44)
                {Tiles[i]=Tiles[Count-1-i]=random.Next(3)==0?Tile.Bag:Tile.Box;}
            }
            foreach(var p in Players)for(int i=0;i<Count;i++)if(Distance(i,p.Cell)<=2&&Tiles[i]!=Tile.Desk)Tiles[i]=Tile.Floor;
            nextAI=1.2f;aiBombAt=2.0f;
        }
        public static int Distance(int a,int b){return Math.Abs(a%Width-b%Width)+Math.Abs(a/Width-b/Width);}
        public static bool Neighbor(int from,int to){return to>=0&&to<Count&&Distance(from,to)==1;}
        public bool Solid(int cell){return cell<0||cell>=Count||Tiles[cell]!=Tile.Floor;}
        public Bubble At(int cell){return Bubbles.FirstOrDefault(b=>b.Cell==cell);}
        public bool Move(int actor,int dx,int dy)
        {
            if(Result!=MatchResult.Playing||actor<0||actor>1||Math.Abs(dx)+Math.Abs(dy)!=1)return false;
            var p=Players[actor];int dest=p.Cell+dx+dy*Width;
            if(Clock+.0001f<p.NextMove||!Neighbor(p.Cell,dest)||Solid(dest)||At(dest)!=null||Players[1-actor].Cell==dest)return false;
            p.Cell=dest;p.NextMove=Clock+(actor==0?HumanStep:Tuning.Get("Bubble","AiSeconds",Level));
            if(Items[dest]!=0){if(Items[dest]==1)p.Range=Math.Min(5,p.Range+1);else p.Capacity=Math.Min(3,p.Capacity+1);Items[dest]=0;Events.Enqueue(new FieldEvent("pickup",dest,actor));}
            Events.Enqueue(new FieldEvent("step",dest,actor));ResolveHits();return true;
        }
        public bool Drop(int actor)
        {
            if(Result!=MatchResult.Playing||actor<0||actor>1)return false;
            var p=Players[actor];if(At(p.Cell)!=null||Bubbles.Count(b=>b.Owner==actor)>=p.Capacity)return false;
            Bubbles.Add(new Bubble{Id=++nextId,Cell=p.Cell,Owner=actor,Range=p.Range,BurstAt=Clock+Fuse});
            Events.Enqueue(new FieldEvent("drop",p.Cell,actor));return true;
        }
        public List<int> RayCells(int cell,int range)
        {
            var cells=new List<int>{cell};foreach(int step in Steps){int last=cell;for(int n=1;n<=range;n++){int i=cell+step*n;if(!Neighbor(last,i)||Tiles[i]==Tile.Desk)break;cells.Add(i);if(Tiles[i]!=Tile.Floor)break;last=i;}}return cells;
        }
        void Burst(Bubble first)
        {
            var pending=new Queue<Bubble>();pending.Enqueue(first);
            while(pending.Count>0)
            {
                var b=pending.Dequeue();if(!Bubbles.Remove(b))continue;
                Events.Enqueue(new FieldEvent("burst",b.Cell,b.Owner));
                // Snapshot rays before changing boxes so this wave cannot travel through its own destruction.
                foreach(int i in RayCells(b.Cell,b.Range))
                {
                    WetUntil[i]=Math.Max(WetUntil[i],Clock+WaveTime);
                    var chain=At(i);if(chain!=null)pending.Enqueue(chain);
                    if(Tiles[i]==Tile.Box||Tiles[i]==Tile.Bag)
                    {Tiles[i]=Tile.Floor;Items[i]=random.NextDouble()<.38?random.Next(1,3):0;Events.Enqueue(new FieldEvent("break",i));}
                }
            }
        }
        void ResolveHits()
        {
            if(Result!=MatchResult.Playing)return;
            for(int n=0;n<2;n++){var p=Players[n];if(WetUntil[p.Cell]>Clock&&Clock>=p.InvulnerableUntil){p.Lives--;p.InvulnerableUntil=Clock+1.25f;Events.Enqueue(new FieldEvent("hit",p.Cell,n));}}
            bool a=Players[0].Lives<=0,b=Players[1].Lives<=0;if(a||b){Result=a&&b?MatchResult.Draw:a?MatchResult.Lose:MatchResult.Win;Events.Enqueue(new FieldEvent("finish",Players[a?0:1].Cell));}
        }
        public void Tick(float dt,bool computer=true)
        {
            if(Result!=MatchResult.Playing||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            // Bound each simulation step, including long frames, so a splash is never skipped.
            float left=Math.Min(dt,.25f);while(left>0&&Result==MatchResult.Playing){float step=Math.Min(.025f,left);left-=step;Clock+=step;
                foreach(var b in Bubbles.Where(b=>b.BurstAt<=Clock).ToArray())if(Bubbles.Contains(b))Burst(b);
                ResolveHits();if(computer&&Result==MatchResult.Playing&&Clock>=nextAI){nextAI=Clock+Tuning.Get("Bubble","AiSeconds",Level);ComputerMove();}
            }
        }
        float[] Danger(Bubble extra=null)
        {
            var all=new List<Bubble>(Bubbles);if(extra!=null)all.Add(extra);
            var times=all.Select(b=>b.BurstAt).ToArray();var rays=all.Select(b=>RayCells(b.Cell,b.Range)).ToArray();
            for(int pass=0;pass<all.Count;pass++)for(int a=0;a<all.Count;a++)for(int b=0;b<all.Count;b++)if(times[a]<times[b]&&rays[a].Contains(all[b].Cell))times[b]=times[a];
            var danger=Enumerable.Repeat(float.PositiveInfinity,Count).ToArray();for(int i=0;i<Count;i++)if(WetUntil[i]>Clock)danger[i]=Clock;
            for(int a=0;a<all.Count;a++)foreach(int i in rays[a])danger[i]=Math.Min(danger[i],times[a]);return danger;
        }
        int Escape(float[] danger,Bubble extra=null)
        {
            int start=Players[1].Cell;var first=Enumerable.Repeat(-1,Count).ToArray();var depth=new int[Count];var queue=new Queue<int>();queue.Enqueue(start);first[start]=start;
            while(queue.Count>0){int cell=queue.Dequeue();if(cell!=start&&float.IsPositiveInfinity(danger[cell]))return first[cell];
                foreach(int step in Steps){int n=cell+step;if(!Neighbor(cell,n)||Solid(n)||first[n]!=-1||At(n)!=null||(extra!=null&&extra.Cell==n)||n==Players[0].Cell)continue;
                    float arrival=Clock+(depth[cell]+1)*Tuning.Get("Bubble","AiSeconds",Level);if(danger[n]<=arrival+.22f)continue;
                    first[n]=cell==start?n:first[cell];depth[n]=depth[cell]+1;queue.Enqueue(n);
                }
            }return -1;
        }
        void StepAI(int dest){if(dest>=0&&dest!=Players[1].Cell)Move(1,dest%Width-Players[1].Cell%Width,dest/Width-Players[1].Cell/Width);}
        void ComputerMove()
        {
            var p=Players[1];var danger=Danger();
            if(!float.IsPositiveInfinity(danger[p.Cell])){StepAI(Escape(danger));return;}
            var ray=RayCells(p.Cell,p.Range);bool target=ray.Contains(Players[0].Cell)||ray.Any(i=>Tiles[i]==Tile.Box||Tiles[i]==Tile.Bag);
            if(target&&Clock>=aiBombAt&&Bubbles.Count(b=>b.Owner==1)<p.Capacity)
            {
                var pretend=new Bubble{Cell=p.Cell,Owner=1,Range=p.Range,BurstAt=Clock+Fuse};int safe=Escape(Danger(pretend),pretend);
                if(safe>=0&&Drop(1)){aiBombAt=Clock+(Level==1?4.5f:2.5f-Level*.22f);StepAI(safe);return;}
            }
            // Breadth-first routing around desks, with preference for the opponent and useful pickups.
            var first=Enumerable.Repeat(-1,Count).ToArray();var q=new Queue<int>();first[p.Cell]=p.Cell;q.Enqueue(p.Cell);int best=p.Cell;double bestScore=double.MaxValue;
            while(q.Count>0){int cell=q.Dequeue();double score=Distance(cell,Players[0].Cell)*3+(Items[cell]>0?-6:0)+(cell==p.Cell?1:0)+random.NextDouble()*.35;
                if(score<bestScore){bestScore=score;best=cell;}
                foreach(int step in Steps){int n=cell+step;if(!Neighbor(cell,n)||Solid(n)||first[n]!=-1||At(n)!=null||n==Players[0].Cell||!float.IsPositiveInfinity(danger[n]))continue;first[n]=cell==p.Cell?n:first[cell];q.Enqueue(n);}
            }StepAI(first[best]);
        }
    }
}
