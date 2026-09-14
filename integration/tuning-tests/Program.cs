using StudentAge.CampusUno;using StudentAge.CampusPuzzles;using StudentAge.CampusMinigames;using StudentAge.CampusBubble;using StudentAge.Retro;using StudentAge.Mahjong;using System.Text.Json;
int checks=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
void Configure(params (string key,float value)[] changes){var dict=changes.ToDictionary(v=>v.key,v=>v.value);MinigameTuning.Configure(s=>dict.TryGetValue(s.Section+"."+s.Key,out float v)?v:s.Default);}
Check(MinigameTuning.Settings.Select(s=>s.Section+"."+s.Key).Distinct().Count()==140,"all setting keys unique");
foreach(var s in MinigameTuning.Settings){Check(s.Clamp(float.NaN)==s.Default&&s.Clamp(float.PositiveInfinity)==s.Default,"non-finite input "+s.Key);Check(s.Clamp(s.Min-10)==s.Min&&s.Clamp(s.Max+10)==s.Max,"range protection "+s.Key);}
Configure(("Box.Rounds",2),("Box.Wins",9),("Box.Swaps5",15),("Box.CloseSeconds5",.12f));var box=new BoxGame(5,12);Check(box.RequiredWins==2&&box.SwapCount==15&&box.CloseDuration==.12f,"box coupled options");for(int n=0;n<2;n++){box.NextRound();foreach(var swap in box.Swaps)box.Swap(swap[0],swap[1]);box.Pick(Array.IndexOf(box.Items,box.Target));}Check(box.Finished&&box.Won,"custom box match finishes after two rounds");
Configure(("TwentyFour.Seconds1",20),("TwentyFour.Answers1",1));var calc=new TwentyFourGame(1,23);Check(calc.Remaining==20&&calc.RequiredAnswers==1,"calculation options apply on creation");calc.Tick(19);Check(!calc.Lost,"time before configured deadline");calc.Tick(1);Check(calc.Lost&&!calc.Submit(),"deadline enforces failure");
Configure(("Tetris.FallSeconds2",.1f),("Tetris.Lines2",1));var blocks=new FallingBlocksGame(2,1);int y=blocks.Y;blocks.Tick(.1f);Check(blocks.Y==y+1&&blocks.Target==1,"configured gravity changes simulation");
Configure(("Snake.Food5",3),("Snake.Obstacles5",4),("Snake.StepSeconds5",.12f));var snake=new SnakeGame(5,1);Check(snake.Target==3&&snake.Walls.Count==4&&snake.Interval==.12f,"snake fifth stage options");Check(new SnakeGame(1,1).Target==5,"other stages unchanged");
Configure(("Pacman.Beans1",80),("Pacman.Guards1",3),("Pacman.Lives",5));var maze=new MazeGame(1,2);Check(maze.Target==80&&maze.Beans.Count==80&&maze.Guards.Count==3&&maze.Lives==5,"maze custom target unique reachable beans");Check(maze.Beans.All(p=>maze.Distances(MazeGame.Start).ContainsKey(p)),"custom beans reachable");
Configure(("Bubble.Lives",5),("Bubble.Range",4),("Bubble.Capacity",3),("Bubble.AiLives1",4));var bubble=new BubbleGame(2,1);Check(bubble.Players[0].Lives==5&&bubble.Players[0].Range==4&&bubble.Players[0].Capacity==3&&bubble.Players[1].Lives==4,"bubble starting stats consumed");
int[] board=new int[225];foreach(int x in new[]{3,4,6,7})board[105+x]=1;
Configure(("Gomoku.Block1",1));Check(GomokuAI.Choose(board,2,1,rng:()=>.99)==110,"configured guaranteed block");Configure(("Gomoku.Block1",0));Check(GomokuAI.Choose(board,2,1,rng:()=>.01)!=110,"configured missed block still legal");
Configure(("UNO.InitialCards",12),("UNO.AiDelay",.4f));var uno=new UnoGame(3,4,false);Check(Enumerable.Range(0,4).All(s=>uno.Hand(s).Count==12),"UNO custom deal");
Configure(("Landlord.AiLevel5",1),("Mahjong.AiLevel5",1));for(int seed=1;seed<12;seed++){var a=new LandlordGame(5,seed);var b=new LandlordGame(5,seed);a.Bid(a.Turn,3);b.Bid(b.Turn,3);Check(a.Choose(a.Turn).SequenceEqual(b.Choose(b.Turn,1)),"landlord configured strategy "+seed);var ma=new MahjongGame(5,seed);var mb=new MahjongGame(5,seed);Check(ma.ChooseDiscard(0)==mb.ChooseDiscard(0,1),"mahjong configured strategy "+seed);}
Configure(("Mario.Lives",7),("Mario.TimeScale",2),("Contra.Lives",6),("Contra.BossHP",12),("Contra.BatteryHP",2));var json=new JsonSerializerOptions{IncludeFields=true};var marioData=JsonSerializer.Deserialize<MarioData>(File.ReadAllText("retro/assets/mario.json"),json);var mario=new MarioGame(marioData,0);var clock=(float)typeof(MarioGame).GetField("clock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(mario)!;Check(mario.Lives==7&&clock==marioData.maps.First(m=>m.name==mario.MapName).time*2,"Mario life and timer configuration");var contra=new ContraGame(JsonSerializer.Deserialize<ContraData>(File.ReadAllText("retro/assets/contra.json"),json));Check(contra.Lives==6&&contra.Actors.First(a=>a.Kind=="Boss").HP==12&&contra.Actors.Where(a=>a.Kind.StartsWith("Battery")).All(a=>a.HP==2),"Contra lives and enemy health");
Configure();Check(MinigameTuning.Get("NDS","RoundLimit")==3&&MinigameTuning.Get("NDS","MarioHalfUnits")==1,"default quota");
Configure(("UNO.InitialCards",7));
using(var scope=MinigameTuning.Push(new Dictionary<string,float>{{"UNO.InitialCards",10}})){
 Check(MinigameTuning.Int("UNO","InitialCards")==10,"per-mod tuning applies");
 try{MinigameTuning.Push(new Dictionary<string,float>{{"UNO.InitialCards",4}});throw new Exception("overlapping scope allowed");}catch(InvalidOperationException){}
 Check(MinigameTuning.Int("UNO","InitialCards")==10,"failed overlapping scope preserves active round");
}
Check(MinigameTuning.Int("UNO","InitialCards")==7,"session disposal restores user cfg");
try{using(var scope=MinigameTuning.Push(new Dictionary<string,float>{{"UNO.InitialCards",11}})){throw new ArgumentException();}}catch(ArgumentException){}
Check(MinigameTuning.Int("UNO","InitialCards")==7,"exception restores original tuning");
try{MinigameTuning.Push(new Dictionary<string,float>{{"not.a.setting",1}});throw new Exception("invalid setting accepted");}catch(ArgumentException){}
Check(MinigameTuning.Int("UNO","InitialCards")==7,"invalid cfg does not mutate settings");
using(var scope=MinigameTuning.Push(new Dictionary<string,float>{{"UNO.InitialCards",9}}))Check(MinigameTuning.Int("UNO","InitialCards")==9,"next NPC gets independent tuning");
Console.WriteLine("TUNING_BEHAVIOR_OK "+checks);
