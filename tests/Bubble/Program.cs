using StudentAge.CampusBubble;
int checks=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
BubbleGame Empty(){var g=new BubbleGame(7,1);for(int y=1;y<10;y++)for(int x=1;x<12;x++)g.Tiles[y*13+x]=Tile.Floor;g.Players[0].Cell=70;g.Players[1].Cell=126;return g;}
void Tick(BubbleGame g,float seconds,bool ai=false){for(int n=0;n<(int)Math.Ceiling(seconds/.025);n++)g.Tick(.025f,ai);}
var game=Empty();game.Tiles[71]=Tile.Desk;Check(!game.Move(0,1,0),"desks block movement");game.Tiles[71]=Tile.Floor;Check(game.Drop(0),"drop on current tile");Check(!game.Drop(0),"one bubble per tile and capacity");Check(game.Move(0,1,0),"can leave own bubble");Tick(game,.2f);Check(!game.Move(0,-1,0),"cannot reenter bubble");game.Players[0].Cell=14;Tick(game,2.3f);Check(game.Bubbles.Count==0&&game.WetUntil[70]>game.Clock,"bubble bursts after fuse");
game=Empty();game.Tiles[71]=Tile.Box;game.Players[0].Range=4;game.Drop(0);game.Players[0].Cell=14;Tick(game,2.4f);Check(game.Tiles[71]==Tile.Floor&&game.WetUntil[71]>game.Clock,"wave destroys box");Check(game.WetUntil[72]==0,"same wave stops at destroyed box");
game=Empty();game.Tiles[71]=Tile.Desk;game.Drop(0);game.Players[0].Cell=14;Tick(game,2.4f);Check(game.WetUntil[71]==0&&game.WetUntil[72]==0,"desks stop wave");
game=Empty();game.Drop(0);Tick(game,.6f);game.Players[1].Cell=71;game.Drop(1);game.Players[0].Cell=14;game.Players[1].Cell=126;Tick(game,1.8f);Check(game.Bubbles.Count==0,"chain burst does not wait for second fuse");
game=Empty();game.Players[0].Lives=game.Players[1].Lives=1;game.Players[1].Cell=71;game.Drop(0);Tick(game,2.4f);Check(game.Result==MatchResult.Draw,"simultaneous hit draws");Check(!game.Drop(0)&&!game.Move(0,1,0),"terminal state blocks input");
game=Empty();game.Players[0].Lives=2;game.Drop(0);Tick(game,2.6f);Check(game.Players[0].Lives==1,"one wave does not remove both lives");
game=Empty();game.Items[71]=1;game.Move(0,1,0);Check(game.Players[0].Range==3&&game.Items[71]==0,"ruler pickup extends wave");
for(int level=1;level<=5;level++){game=new BubbleGame(19,level);Check(game.Level==level&&!game.Solid(game.Players[0].Cell)&&!game.Solid(game.Players[1].Cell),"hidden level and spawn "+level);var start=game.Players[1].Cell;Tick(game,4,true);Check(game.Players[1].Cell!=start||game.Bubbles.Count>0,"AI actively moves or places bubbles "+level);}
Console.WriteLine("BUBBLE_RULES_OK "+checks);
