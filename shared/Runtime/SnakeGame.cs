using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;using System.Collections.Generic;using System.Linq;
namespace StudentAge.CampusPuzzles
{
    public sealed class SnakeGame
    {
        public const int Width=22,Height=15;public readonly int Level,Target;public readonly List<int> Body=new List<int>();public readonly HashSet<int> Walls=new HashSet<int>();readonly Random rng;readonly Queue<int> input=new Queue<int>();
        public int Direction{get;private set;}=1;public int Food{get;private set;}public int Eaten{get;private set;}public int Moves{get;private set;}public bool Lost{get;private set;}public bool Won=>Eaten>=Target;public float Interval=>Tuning.Get("Snake","StepSeconds",Level);
        public SnakeGame(int level,int seed){Level=Math.Max(1,Math.Min(5,level));Target=Tuning.Int("Snake","Food",Level);rng=new Random(seed);int head=7*Width+7;Body.Add(head);Body.Add(head-1);Body.Add(head-2);int[] obstacles={3*Width+9,11*Width+12,3*Width+10,11*Width+11,5*Width+3,9*Width+18,6*Width+3,8*Width+18,3*Width+11,11*Width+10};int count=Tuning.Int("Snake","Obstacles",Level);for(int i=0;i<count;i++)Walls.Add(obstacles[i]);PlaceFood();}
        public bool Turn(int direction){if(Lost||Won||direction<0||direction>3||input.Count>=2)return false;int previous=input.Count==0?Direction:input.Last();if(direction==previous||(direction+2)%4==previous)return false;input.Enqueue(direction);return true;}
        public void Step(){if(Lost||Won)return;if(input.Count>0)Direction=input.Dequeue();int head=Body[0],x=head%Width,y=head/Width;x+=Direction==1?1:Direction==3?-1:0;y+=Direction==2?1:Direction==0?-1:0;int next=y*Width+x;bool grow=next==Food;int collisionLength=Body.Count-(grow?0:1);if(x<0||x>=Width||y<0||y>=Height||Walls.Contains(next)||Body.Take(collisionLength).Contains(next)){Lost=true;return;}Body.Insert(0,next);if(grow){Eaten++;if(!Won)PlaceFood();}else Body.RemoveAt(Body.Count-1);Moves++;}
        void PlaceFood(){var free=Enumerable.Range(0,Width*Height).Where(i=>!Body.Contains(i)&&!Walls.Contains(i)).ToArray();if(free.Length==0){Eaten=Target;Food=-1;}else Food=free[rng.Next(free.Length)];}
    }
}
