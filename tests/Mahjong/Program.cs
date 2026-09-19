using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using StudentAge.Mahjong;
class Tests
{
 static int checks;static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static int[] Tiles(params int[] kinds){var counts=new int[34];return kinds.Select(k=>k*4+counts[k]++).ToArray();}
 static void Set(MahjongGame g,string name,object value)=>typeof(MahjongGame).GetProperty(name).SetValue(g,value);
 static void Hand(MahjongGame g,int s,params int[] kinds){g.Hands[s].Clear();g.Hands[s].AddRange(Tiles(kinds));}
 static void Invariant(MahjongGame g)
 {
  var tiles=g.Wall.Skip(g.Head).Take(g.Remaining).Concat(g.Hands.SelectMany(h=>h)).Concat(g.Melds.SelectMany(m=>m.SelectMany(t=>t.Tiles))).Concat(g.Rivers.SelectMany(r=>r.Where(t=>!t.Taken).Select(t=>t.Tile))).ToArray();
  Check(tiles.Length==136&&tiles.Distinct().Count()==136,"136 unique tiles conserved");
  for(int s=0;s<4;s++){int expected=13-3*g.Melds[s].Count;if(g.State==Phase.Discard&&g.Turn==s||g.State==Phase.Finished&&g.Winner==s||g.RobbingKong&&g.Turn==s)expected++;Check(g.Hands[s].Count==expected,"hand arity "+g.State+" seat="+s);}
 }
 static void Main()
 {
  Check(MahjongGame.Winning(Tiles(0,1,2,3,4,5,9,10,11,27,27,27,31,31)),"four melds and eyes");
  Check(MahjongGame.Winning(Tiles(0,0,2,2,9,9,18,18,27,27,31,31,33,33)),"seven distinct pairs");
  Check(!MahjongGame.Winning(Tiles(0,0,0,0,9,9,18,18,27,27,31,31,33,33)),"quad is not two seven-pairs pairs");
  Check(!MahjongGame.Winning(Tiles(7,8,9,0,1,2,18,19,20,27,27,27,31,31)),"sequences cannot cross suits");
  Check(!MahjongGame.Winning(Tiles(27,28,29,0,1,2,18,19,20,9,9,9,31,31)),"honors are not a sequence");
  Check(MahjongGame.Winning(Tiles(0,1,2,9,10,11,27,27),2),"open hand winning shape");
  Check(!MahjongGame.Winning(Tiles(0,1,2,9,10,11,27,27),1),"wrong concealed size rejected");
  // One discard gives player 1 chi, player 2 pung, player 3 ron. Resolution must await all decisions.
  var f=new MahjongGame(1,41);Hand(f,0,0,1,2,3,4,5,6,7,8,9,10,11,20,20);Hand(f,1,18,19,1,3,5,7,9,11,13,15,27,30,32);Hand(f,2,20,20,1,3,5,7,9,11,13,15,27,30,32);Hand(f,3,0,1,2,3,4,5,9,10,11,27,27,27,20);
  Check(f.Discard(0,80),"fixture discard");Check(f.ChiOptions(1).Contains(18)&&f.ChiOptions(2).Count==0,"only next seat can chi");Check(f.Respond(1,Call.Chi,18),"chi response");Check(f.Respond(2,Call.Peng),"pung response");Check(f.State==Phase.Claim,"waits for ron response");Check(f.Respond(3,Call.Hu)&&f.Winner==3,"ron beats pung and chi");
  // Concealed kong consumes four tiles, creates one meld, and supplements from the tail.
  f=new MahjongGame(2,51);Hand(f,0,0,0,0,0,1,2,3,4,5,9,10,11,31,31);int tail=f.Tail;Check(f.Kong(0,0)&&f.Melds[0][0].Concealed&&f.Tail==tail-1&&f.Hands[0].Count==11,"concealed kong replacement");
  // No replacement tile => no legal kong.
  Set(f,"Head",f.Tail+1);Check(f.OwnKongs(0).Count==0,"no kong with empty wall");Set(f,"State",Phase.Draw);Check(f.Draw()&&f.DrawnGame,"empty wall drawn game");
  f=new MahjongGame(3,31);f.Melds[0].Add(new Meld{Kind=Call.Peng,From=2,Tiles=new List<int>{80,81,82}});Hand(f,0,0,2,4,6,9,11,13,15,27,31,20);f.Hands[0].Remove(80);f.Hands[0].Add(83);
  Hand(f,1,0,1,2,3,4,5,9,10,11,18,19,27,27);
  Check(f.Kong(0,20)&&f.RobbingKong&&f.CanHu(1),"added kong offers robbing ron");
  Check(f.Respond(1,Call.Hu),"rob kong response accepted");for(int n=2;n<4;n++)if(f.Awaiting(n))f.Respond(n,Call.Pass);
  Check(f.Winner==1&&f.Melds[0][0].Kind==Call.Peng&&f.Melds[0][0].Tiles.Count==3&&!f.Hands[0].Contains(83)&&f.Hands[1].Contains(83),"robbed kong preserves pung and transfers fourth tile");
  int wins=0,draws=0,kongs=0,chis=0,pengs=0;for(int seed=0;seed<250;seed++)
  {
   var game=new MahjongGame(seed%5+1,seed+93);Invariant(game);int moves=0;
   while(game.State!=Phase.Finished&&moves++<1500){game.BotTurn();Invariant(game);while(game.Events.Count>0){var e=game.Events.Dequeue();if(e.Kind.Contains("杠"))kongs++;if(e.Kind=="吃")chis++;if(e.Kind=="碰")pengs++;}}
   Check(game.State==Phase.Finished,"game terminates seed "+seed);if(game.Winner>=0){wins++;Check(MahjongGame.Winning(game.Hands[game.Winner],game.Melds[game.Winner].Count),"actual winning hand");}else draws++;
  }
  Check(kongs>0&&chis>0&&pengs>0,"simulation covers calls");Console.WriteLine($"MAHJONG_RULES_OK checks={checks} games=250 wins={wins} draws={draws} gangs={kongs} chi={chis} peng={pengs}");
 }
}
