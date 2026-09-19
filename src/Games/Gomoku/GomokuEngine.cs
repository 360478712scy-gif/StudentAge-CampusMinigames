using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StudentAge.CampusMinigames
{
    public sealed class GomokuGame
    {
        public readonly int[] Board=new int[225];
        public readonly List<int> History=new List<int>();
        public int Turn=1,Winner;public int[] Line;public bool Draw;
        public bool Play(int index){if(index<0||index>=225||Board[index]!=0||Winner!=0||Draw)return false;Board[index]=Turn;History.Add(index);Line=FindLine(Board,index);if(Line!=null)Winner=Turn;else if(History.Count==225)Draw=true;Turn=3-Turn;return true;}
        public bool Undo(){if(History.Count==0)return false;int index=History[History.Count-1];History.RemoveAt(History.Count-1);Turn=Board[index];Board[index]=0;Winner=0;Draw=false;Line=null;return true;}
        internal static readonly int[,] Dirs={{1,0},{0,1},{1,1},{1,-1}};
        internal static bool Inside(int x,int y){return x>=0&&x<15&&y>=0&&y<15;}
        public static int[] FindLine(int[] b,int index){int p=b[index];if(p==0)return null;int x=index%15,y=index/15;for(int dir=0;dir<4;dir++){var line=new List<int>{index};foreach(int sign in new[]{-1,1})for(int d=1;d<15;d++){int xx=x+sign*Dirs[dir,0]*d,yy=y+sign*Dirs[dir,1]*d;if(!Inside(xx,yy)||b[yy*15+xx]!=p)break;if(sign<0)line.Insert(0,yy*15+xx);else line.Add(yy*15+xx);}if(line.Count>=5)return line.ToArray();}return null;}
    }
    // Shape-based casual opponent. Difficulty controls missed opportunities, never remote random moves.
    public static class GomokuAI
    {
        const double Five=1e8,Four=1e6,Three=18000;
        sealed class Move{public int I;public double Attack,Defend,Score;}
        static bool Has(string s,string patterns){foreach(string pattern in patterns.Split('|'))for(int at=s.IndexOf(pattern,StringComparison.Ordinal);at>=0;at=s.IndexOf(pattern,at+1,StringComparison.Ordinal))if(at<=5&&at+pattern.Length>5)return true;return false;}
        static double Threat(int[] b,int index,int p){int x=index%15,y=index/15,fours=0,threes=0;double score=0;for(int dir=0;dir<4;dir++){char[] line=new char[11];for(int d=-5;d<=5;d++){int xx=x+GomokuGame.Dirs[dir,0]*d,yy=y+GomokuGame.Dirs[dir,1]*d;line[d+5]=d==0?'1':!GomokuGame.Inside(xx,yy)?'2':b[yy*15+xx]==p?'1':b[yy*15+xx]==0?'0':'2';}string s=new string(line);if(Has(s,"11111"))return Five;if(Has(s,"011110")){score+=Four;fours++;}else if(Has(s,"01111|11110|11011|10111|11101")){score+=80000;fours++;}else if(Has(s,"01110|010110|011010")){score+=Three;threes++;}else if(Has(s,"001110|011100|01011|11010|10110|01101"))score+=1800;else if(Has(s,"001100|0010100|010010"))score+=550;else if(Has(s,"0110|01010"))score+=170;else if(Has(s,"01|10"))score+=15;}if(fours>=2)score+=Four*2;if(fours>0&&threes>0)score+=Four;if(threes>=2)score+=150000;return score;}
        static List<Move> Rank(int[] b,int p){var cells=new HashSet<int>();bool empty=true;for(int i=0;i<225;i++)if(b[i]!=0){empty=false;int x=i%15,y=i/15;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)if(GomokuGame.Inside(x+dx,y+dy)){int k=(y+dy)*15+x+dx;if(b[k]==0)cells.Add(k);}}if(empty)cells.Add(112);var result=new List<Move>();foreach(int i in cells){double a=Threat(b,i,p),d=Threat(b,i,3-p);result.Add(new Move{I=i,Attack=a,Defend=d,Score=Math.Max(a,d*1.04)+Math.Min(a,d)*.2+14-Math.Abs(i%15-7)-Math.Abs(i/15-7)});}return result.OrderByDescending(m=>m.Score).ThenBy(m=>m.I).ToList();}
        // A lapse means pursuing a credible local attacking line while overlooking another threat.
        // Exclude the overlooked urgent cells so the ordinary score cannot silently undo the lapse.
        public static int Choose(int[] input,int player,int level,CancellationToken token=default(CancellationToken),Func<double> rng=null,int budgetOverride=0)
        {
            if(input==null||input.Length!=225||player<1||player>2||level<1||level>5)throw new ArgumentException("Invalid Gomoku position or level");
            token.ThrowIfCancellationRequested();var all=Rank((int[])input.Clone(),player);if(all.Count==0)return -1;
            var random=new Random();rng=rng??random.NextDouble;
            var wins=all.Where(m=>m.Attack>=Five).ToList();
            var blocks=all.Where(m=>m.Defend>=Five).ToList();
            if(wins.Count>0&&rng()<Tuning.Get("Gomoku","Win",level))return wins[0].I;
            if(blocks.Count>0&&rng()<Tuning.Get("Gomoku","Block",level))return blocks[0].I;
            var choices=all.Where(m=>m.Attack<Five&&m.Defend<Five).ToList();
            if(choices.Count==0)return all[0].I;
            bool lapse=wins.Count>0||blocks.Count>0||rng()<Tuning.Get("Gomoku","Lapse",level);
            // No multi-ply fork solving. Every level still extends its own lines and sees nearby danger.
            foreach(var m in choices)m.Score=m.Attack+m.Defend*Tuning.Get("Gomoku","Defense",level)+Math.Min(m.Attack,m.Defend)*.15;
            choices=choices.OrderByDescending(m=>m.Score).ThenBy(m=>m.I).ToList();
            if(lapse){
                int best=choices[0].I;
                var plans=choices.Where(m=>m.Attack>=170&&m.I!=best).OrderByDescending(m=>m.Attack).ThenByDescending(m=>m.Score).ToList();
                if(plans.Count==0)plans=choices.Where(m=>m.I!=best).ToList();
                if(plans.Count>0){double threshold=plans[0].Attack*.45;var pool=plans.Where(m=>m.Attack>=threshold).Take(3).ToList();token.ThrowIfCancellationRequested();return pool[Math.Min(pool.Count-1,(int)(Math.Max(0,rng())*pool.Count))].I;}
            }
            // Tie-like alternatives keep openings varied without abandoning the shape being played.
            var close=choices.Where(m=>m.Score>=choices[0].Score*.85).Take(level<=2?2:1).ToList();
            token.ThrowIfCancellationRequested();return close[Math.Min(close.Count-1,(int)(Math.Max(0,rng())*close.Count))].I;
        }
    }
}
