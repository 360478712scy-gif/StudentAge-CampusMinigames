using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;
using StudentAge.PlayingCards;
namespace StudentAge.CampusPuzzles
{
    public sealed class FallingBlocksGame
    {
        public const int Width=10,Height=20;public readonly int[] Board=new int[Width*Height];public readonly int Level,Target;
        readonly Random rng;readonly Queue<int> bag=new Queue<int>();
        public int Piece{get;private set;}public int Rotation{get;private set;}public int X{get;private set;}public int Y{get;private set;}public int Lines{get;private set;}public int Locked{get;private set;}public bool Lost{get;private set;}public bool Won=>Lines>=Target;public int Revision{get;private set;}public int LastClear{get;private set;}public readonly List<int> ClearedRows=new List<int>();
        float fall,lockTime;int resets;public float Interval=>Tuning.Get("Tetris","FallSeconds",Level);
        static readonly int[][] Forms={new[]{4,5,6,7},new[]{0,4,5,6},new[]{2,4,5,6},new[]{1,2,5,6},new[]{1,2,4,5},new[]{1,4,5,6},new[]{0,1,5,6}};
        public FallingBlocksGame(int level,int seed){Level=Math.Max(1,Math.Min(5,level));Target=Tuning.Int("Tetris","Lines",Level);rng=new Random(seed);FillBag();Spawn();}
        void FillBag(){while(bag.Count<7){int[] values={0,1,2,3,4,5,6};PlayingDeck.Shuffle(values,rng);foreach(int p in values)bag.Enqueue(p);}}
        public int[] Next=>bag.Take(3).ToArray();
        public static int[] Cells(int piece,int rotation){var result=new int[8];for(int i=0;i<4;i++){int x=Forms[piece][i]%4,y=Forms[piece][i]/4,size=piece==0?4:3;if(piece!=3)for(int t=0;t<(rotation%4+4)%4;t++){int old=x;x=size-1-y;y=old;}result[i*2]=x;result[i*2+1]=y;}return result;}
        public bool Fits(int x,int y,int rotation){var cells=Cells(Piece,rotation);for(int n=0;n<4;n++){int cx=x+cells[n*2],cy=y+cells[n*2+1];if(cx<0||cx>=Width||cy>=Height||cy>=0&&Board[cy*Width+cx]!=0)return false;}return true;}
        void Spawn(){FillBag();Piece=bag.Dequeue();Rotation=0;X=3;Y=-1;fall=lockTime=0;resets=0;Revision++;if(!Fits(X,Y,Rotation))Lost=true;}
        public bool Move(int dx,int dy=0){if(Won||Lost||!Fits(X+dx,Y+dy,Rotation))return false;bool grounded=!Fits(X,Y+1,Rotation);X+=dx;Y+=dy;if(grounded&&resets++<15)lockTime=0;Revision++;return true;}
        public bool Rotate(int direction=1){if(Won||Lost)return false;int to=(Rotation+direction+4)%4;int[][] kicks=Piece==0?new[]{new[]{0,0},new[]{-1,0},new[]{1,0},new[]{-2,0},new[]{2,0},new[]{0,-1},new[]{0,-2}}:new[]{new[]{0,0},new[]{-1,0},new[]{1,0},new[]{0,-1},new[]{-1,-1},new[]{1,-1}};foreach(var k in kicks)if(Fits(X+k[0],Y+k[1],to)){X+=k[0];Y+=k[1];Rotation=to;if(resets++<15)lockTime=0;Revision++;return true;}return false;}
        public int GhostY{get{int y=Y;while(Fits(X,y+1,Rotation))y++;return y;}}
        public void HardDrop(){if(Won||Lost)return;Y=GhostY;Lock();}
        public void Tick(float dt){if(Won||Lost)return;dt=Math.Max(0,Math.Min(dt,.1f));fall+=dt;if(fall>=Interval){fall-=Interval;Move(0,1);}if(!Fits(X,Y+1,Rotation)){lockTime+=dt;if(lockTime>=Tuning.Get("Tetris","LockSeconds"))Lock();}else lockTime=0;}
        void Lock(){var cells=Cells(Piece,Rotation);for(int n=0;n<4;n++)if(Y+cells[n*2+1]<0){Lost=true;Revision++;return;}for(int n=0;n<4;n++)Board[(Y+cells[n*2+1])*Width+X+cells[n*2]]=Piece+1;Locked++;ClearedRows.Clear();for(int y=0;y<Height;y++)if(Enumerable.Range(0,Width).All(x=>Board[y*Width+x]!=0))ClearedRows.Add(y);LastClear=ClearedRows.Count;int dest=Height-1;for(int y=Height-1;y>=0;y--){if(ClearedRows.Contains(y))continue;for(int x=0;x<Width;x++)Board[dest*Width+x]=Board[y*Width+x];dest--;}while(dest>=0){for(int x=0;x<Width;x++)Board[dest*Width+x]=0;dest--;}Lines+=LastClear;Revision++;if(!Won)Spawn();}
    }
}
