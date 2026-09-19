using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;using System.Linq;using System.Collections.Generic;
namespace StudentAge.CampusPuzzles
{
    public sealed class MazeGame
    {
        public static readonly string[] Map={
            "###################",
            "#........#........#",
            "#.##.###.#.###.##.#",
            "#.................#",
            "#.##.#.#####.#.##.#",
            "#....#...#...#....#",
            "####.###.#.###.####",
            "#.................#",
            "#.##.#.#####.#.##.#",
            "#....#...#...#....#",
            "#.##.###.#.###.##.#",
            "#.................#",
            "#.##.###.#.###.##.#",
            "#.................#",
            "###################"};
        public const int Width=19,Height=15,Start=13*19+1;
        public readonly int Level,Target;public readonly HashSet<int> Beans=new HashSet<int>(),Powers=new HashSet<int>();public readonly List<int> Guards=new List<int>();readonly Random rng;readonly int[] homes={1*19+17,1*19+1,7*19+17};
        public int Player{get;private set;}=Start;public int Direction{get;private set;}=1;int wanted=1;
        public int Collected{get;private set;}public int Lives{get;private set;}=Tuning.Int("Pacman","Lives");public int Moves{get;private set;}public int PowerSteps{get;private set;}public int Grace{get;private set;}=24;public bool Won=>Collected>=Target;public bool Lost=>Lives==0;public float Interval=>Tuning.Get("Pacman","StepSeconds");
        public MazeGame(int level,int seed){Level=Math.Max(1,Math.Min(5,level));rng=new Random(seed);Target=Tuning.Int("Pacman","Beans",Level);int count=Tuning.Int("Pacman","Guards",Level);for(int i=0;i<count;i++)Guards.Add(homes[i]);var paths=Distances(Start).Keys.Where(p=>p!=Start).OrderBy(p=>p).ToArray();for(int i=0;i<Target;i++)Beans.Add(paths[i*paths.Length/Target]);foreach(int cell in new[]{11*19+1,7*19+9,3*19+17,13*19+17})Powers.Add(cell);}
        public static bool Open(int cell)=>cell>=0&&cell<Width*Height&&Map[cell/Width][cell%Width]!='#';
        public static int Next(int cell,int direction){int x=cell%Width,y=cell/Width;x+=direction==1?1:direction==3?-1:0;y+=direction==2?1:direction==0?-1:0;int n=y*Width+x;return x>=0&&x<Width&&y>=0&&y<Height&&Open(n)?n:cell;}
        public void Turn(int direction){if(direction>=0&&direction<4&&!Won&&!Lost)wanted=direction;}
        public Dictionary<int,int> Distances(int from){var d=new Dictionary<int,int>{{from,0}};var q=new Queue<int>();q.Enqueue(from);while(q.Count>0){int p=q.Dequeue();for(int i=0;i<4;i++){int n=Next(p,i);if(n!=p&&!d.ContainsKey(n)){d[n]=d[p]+1;q.Enqueue(n);}}}return d;}
        public void Step(){if(Won||Lost)return;Moves++;if(Grace>0)Grace--;if(PowerSteps>0)PowerSteps--;if(Next(Player,wanted)!=Player)Direction=wanted;Player=Next(Player,Direction);if(Beans.Remove(Player))Collected++;if(Powers.Remove(Player))PowerSteps=Tuning.Int("Pacman","PowerSteps");if(Won)return;
            if(Touch())return;int period=Tuning.Int("Pacman","GuardPeriod",Level);if(Moves%period!=0||Moves<24)return;var distance=Distances(Player);for(int i=0;i<Guards.Count;i++){int guard=Guards[i];var options=Enumerable.Range(0,4).Select(d=>Next(guard,d)).Where(n=>n!=guard).Distinct().ToArray();if(options.Length==0)continue;int n;if(PowerSteps>0)n=options.OrderByDescending(c=>distance[c]).First();else if(rng.NextDouble()<(Level<=2?.6:.18))n=options[rng.Next(options.Length)];else n=options.OrderBy(c=>distance[c]).First();Guards[i]=n;}Touch();}
        bool Touch(){for(int i=0;i<Guards.Count;i++){if(Guards[i]!=Player)continue;if(PowerSteps>0){Guards[i]=homes[i];continue;}if(Grace>0)continue;Lives--;Player=Start;Direction=wanted=1;Grace=24;for(int j=0;j<Guards.Count;j++)Guards[j]=homes[j];return true;}return false;}
    }
}
