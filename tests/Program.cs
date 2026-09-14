using System;
using System.Linq;
using StudentAge.CampusUno;

class Program
{
    static int checks;
    static Card C(Suit c, Face f) { return new Card(c, f); }
    static void Check(bool ok, string name) { checks++; if (!ok) throw new Exception(name); }
    static void Main()
    {
        var recycle=new UnoGame(491,4,false);var deck=(System.Collections.Generic.List<Card>)typeof(UnoGame).GetField("deck",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(recycle);var discarded=(System.Collections.Generic.List<Card>)typeof(UnoGame).GetField("discards",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(recycle);var kept=recycle.Top;var keptColor=recycle.Color;
        discarded.Clear();discarded.AddRange(deck.Skip(1));discarded.Add(kept);deck.RemoveRange(1,deck.Count-1);int recycled=discarded.Count-1;
        Check(recycle.DrawForPlayer(),"last draw is accepted");var timeline=recycle.Moves.ToArray();Check(timeline.Length==2&&timeline[0].Draw&&timeline[0].DrawCountAfter==0&&timeline[1].Reshuffle&&timeline[1].DrawCountAfter==recycled,"empty deck event precedes automatic reshuffle");Check(recycle.DrawCount==recycled&&recycle.DiscardCount==1&&recycle.Top.ToString()==kept.ToString()&&recycle.Color==keptColor&&recycle.TotalCards==108,"reshuffle preserves the top card, chosen color and all 108 cards");
        recycle.Moves.Clear();typeof(UnoGame).GetMethod("Draw",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(recycle,new object[]{1,200});Check(recycle.DrawCount==0&&recycle.DiscardCount==1&&recycle.Top.ToString()==kept.ToString()&&recycle.TotalCards==108,"empty stock with only the top discard never invents cards");Check(!recycle.Moves.Any(m=>m.Reshuffle),"top discard alone is never recycled");
        int choices=0, passes=0, wilds=0, impossible=0;
        for(int seed=0;seed<120;seed++){
            var manual=new UnoGame(seed,4,false);int count=manual.Player.Count;var top=manual.Top;var total=manual.TotalCards;
            Check(manual.DrawForPlayer(),"manual draw accepted");Check(manual.Player.Count==count+1 && manual.TotalCards==total,"manual draw conserves cards without auto play");
            if(manual.PendingDrawIndex>=0){
                choices++;int index=manual.PendingDrawIndex;Check(manual.Turn==0 && manual.Top.ToString()==top.ToString(),"drawn playable card waits for player");
                Check(!manual.DrawForPlayer() && !manual.DrawAndPass(0),"cannot repeatedly draw while choosing");
                Check(Enumerable.Range(0,index).All(i=>!manual.CanPlay(0,i)),"only newly drawn card may be played");
                manual.Tick(600,false);Check(manual.PendingDrawIndex==index && manual.Player.Count==count+1,"manual decision never times out");
                if(seed%2==0){Check(manual.PassDrawn() && manual.Turn==1 && manual.Player.Count==count+1,"pass keeps drawn card");passes++;}
                else {if(manual.Player[index].IsWild)wilds++;Check(manual.Play(0,index,Suit.Blue) && manual.PendingDrawIndex<0 && manual.Player.Count==count,"manual play consumes drawn card");}
            }else{impossible++;Check(manual.Turn==1 && !manual.PassDrawn(),"unplayable draw ends turn");}
        }
        Check(choices>0 && passes>0 && wilds>0 && impossible>0,"manual fixtures cover play pass wild and unplayable");
        Console.WriteLine("MANUAL_DRAW_OK | 120 seeds");
        var game = new UnoGame(1);
        Check(game.Player.Count == 7 && game.Opponent.Count == 7 && game.TotalCards == 108, "108-card deck and seven-card opening");
        game.Fixture(new[] { C(Suit.Blue, Face.Three), C(Suit.Red, Face.One), C(Suit.Wild, Face.DrawFour), C(Suit.Green, Face.Skip) }, new[] { C(Suit.Red, Face.Two) }, C(Suit.Red, Face.Three), Suit.Red);
        Check(game.CanPlay(0,0) && game.CanPlay(0,1) && !game.CanPlay(0,2) && !game.CanPlay(0,3), "matching and legal +4");
        Check(!game.Play(1,0,Suit.Red) && !game.Play(0,99,Suit.Red), "wrong turn and bounds");
        game.Fixture(new[] { C(Suit.Red, Face.DrawTwo), C(Suit.Blue, Face.Zero) }, new[] { C(Suit.Red, Face.DrawTwo) }, C(Suit.Red, Face.Three), Suit.Red);
        game.Play(0,0,Suit.Red);
        Check(game.Opponent.Count == 3 && game.Turn == 0, "+2 draws immediately, no stacking, skips opponent");
        game.Tick(2.1f); Check(game.Player.Count == 3, "missed UNO adds two");
        game.Fixture(new[] { C(Suit.Red, Face.Reverse), C(Suit.Blue, Face.One) }, new[] { C(Suit.Red, Face.Two) }, C(Suit.Red, Face.Three), Suit.Red);
        game.CallUno(); game.Play(0,0,Suit.Red); game.Tick(.2f);
        Check(game.Turn == 0 && !game.NeedsUno && game.Player.Count == 1, "two-player reverse and predeclared UNO");
        game.Fixture(new[] { C(Suit.Wild, Face.DrawFour) }, new[] { C(Suit.Red, Face.Two) }, C(Suit.Red, Face.Three), Suit.Red);
        Check(!game.Play(0,0,Suit.Wild), "wild color must be selected");
        game.Play(0,0,Suit.Blue); Check(game.Result == Outcome.Win && game.Opponent.Count == 5 && game.Color == Suit.Blue, "last +4 applies penalty before victory");
        game.Fixture(new[] { C(Suit.Red, Face.One) }, new[] { C(Suit.Red, Face.Two) }, C(Suit.Blue, Face.Three), Suit.Blue);
        game.Tick(60); Check(game.Result == Outcome.Win, "timeout points tiebreak");
        game.Fixture(new[] { C(Suit.Red, Face.One) }, new[] { C(Suit.Blue, Face.One) }, C(Suit.Green, Face.Three), Suit.Green);
        game.Tick(60); Check(game.Result == Outcome.Draw, "equal scores are a draw");
        game.Fixture(new[] { C(Suit.Red, Face.One), C(Suit.Red, Face.Two) }, new[] { C(Suit.Wild, Face.Wild) }, C(Suit.Blue, Face.Three), Suit.Blue);
        game.Tick(60); Check(game.Result == Outcome.Lose, "count takes priority over points");
        game = new UnoGame(2); game.Tick(float.NaN); Check(game.Remaining == 60, "invalid clock ignored");
        game.Surrender(); Check(game.Result == Outcome.Lose, "surrender loses");
        var four = new UnoGame(42,4);
        Check(four.TotalCards == 108 && Enumerable.Range(0,4).All(i=>four.Hand(i).Count==7), "four seat deal");
        four.Fixture(new[]{C(Suit.Red,Face.Reverse),C(Suit.Blue,Face.One)},new[]{C(Suit.Red,Face.Two)},C(Suit.Red,Face.Three),Suit.Red);
        four.CallUno();four.Play(0,0,Suit.Red);
        Check(four.Direction == -1 && four.Turn == 3, "four seat reverse changes direction");
        four.Fixture(new[]{C(Suit.Red,Face.DrawTwo),C(Suit.Blue,Face.One)},new[]{C(Suit.Red,Face.Two)},C(Suit.Red,Face.Three),Suit.Red);
        four.CallUno();four.Play(0,0,Suit.Red);
        Check(four.Turn == 2 && four.Hand(1).Count == 3, "four seat penalty skips exactly next seat");
        Check(four.Moves.Count == 3 && !four.Moves.Dequeue().Draw && four.Moves.Dequeue().Draw, "animation queue follows play then penalty draws");
        four.Tick(2,false);Check(four.Turn == 2 && four.Remaining == 58, "animation lock defers AI but not deadline");
        float fourDuration=0;
        for(int seed=0;seed<1000;seed++){
            four=new UnoGame(seed,4);
            for(int tick=0;tick<601 && four.Result==Outcome.Playing;tick++){
                four.Tick(.1f);if(four.Turn==0 && four.TurnRemaining<3.3f)four.AutoMove(0);
                Check(four.TotalCards==108 && four.Turn>=0 && four.Turn<4,"four player conservation and turn");four.Moves.Clear();
            }
            Check(four.Result!=Outcome.Playing,"four player hard deadline");fourDuration+=60-four.Remaining;
        }
        Console.WriteLine("FOUR_PLAYER_OK | 1000 simulations | meanSeconds="+(fourDuration/1000).ToString("0.0"));
        var casual=new UnoGame(17,4,false);casual.Tick(600,false);
        Check(casual.Result==Outcome.Playing && casual.Player.Count==7 && casual.Turn==0,"untimed match never expires or auto draws for player");
        Check(casual.Elapsed==600 && float.IsPositiveInfinity(casual.Remaining),"untimed clock semantics");
        casual.Fixture(new[]{C(Suit.Red,Face.One)},new[]{C(Suit.Blue,Face.Two)},C(Suit.Red,Face.Three),Suit.Red);
        casual.Play(0,0,Suit.Red);Check(casual.Result==Outcome.Win,"untimed first empty hand wins");
        float casualTime=0;
        for(int seed=0;seed<500;seed++){
            casual=new UnoGame(seed,4,false);
            for(int tick=0;tick<20000 && casual.Result==Outcome.Playing;tick++){
                casual.Tick(.1f);if(casual.Turn==0 && casual.TurnRemaining<3.3f)casual.AutoMove(0);
                Check(casual.TotalCards==108,"untimed conservation");casual.Moves.Clear();
            }
            Check(casual.Result!=Outcome.Playing,"untimed AI games terminate by empty hand");casualTime+=casual.Elapsed;
        }
        Console.WriteLine("UNTIMED_OK | 500 simulations | meanSeconds="+(casualTime/500).ToString("0.0"));
        int wins = 0, losses = 0, draws = 0; float duration = 0;
        for (int seed = 0; seed < 2000; seed++)
        {
            game = new UnoGame(seed);
            float decision = 1.7f;
            for (int tick = 0; tick < 601 && game.Result == Outcome.Playing; tick++)
            {
                game.Tick(.1f); decision -= .1f;
                if (game.Turn == 0 && decision <= 0) { game.AutoMove(0); decision = 1.7f; }
                Check(game.TotalCards == 108, "card conservation seed " + seed);
                Check((int)game.Color < 4 && game.Player.Count >= 0 && game.Opponent.Count >= 0, "valid state seed " + seed);
            }
            Check(game.Result != Outcome.Playing, "all matches finish in 60s");
            if (game.Result == Outcome.Win) wins++; else if (game.Result == Outcome.Lose) losses++; else draws++;
            duration += 60 - game.Remaining;
        }
        Console.WriteLine("ENGINE_OK | checks=" + checks + " | 2000 simulations | wins=" + wins + " losses=" + losses + " draws=" + draws + " meanSeconds=" + (duration/2000).ToString("0.0"));
    }
}
