using System;using System.Linq;using StudentAge.Sanguosha;
static class Test {
 static int checks;static void Check(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static void Main(){checks+=Scenarios.Run();Check(Catalog.Generals.Length==35,"35 generals");Check(Deck.Create().Count==108,"108 card distribution");Check(Deck.Create().GroupBy(c=>c.Id).All(g=>g.Count()==1),"unique physical card IDs");foreach(var pair in new[]{new[]{103,204},new[]{105,202}})Check(Encounters.For(pair[0]).SequenceEqual(Encounters.For(pair[1])),"same obtainable cards across gender routes");
 var p=new Progress();Check(p.Reserve(101,22),"first fight allowed");Check(!p.CanBegin(102,22),"same round different NPC blocked");Check(!p.CanBegin(101,22),"loss cannot retry same round");Check(p.Stage(101)==0,"loss does not advance");Check(p.Reserve(101,23),"retry next round");for(int i=0;i<3;i++)Check(p.CompleteWin(101)==Encounters.For(101)[i],"win gives stage card");Check(!p.CanBegin(101,24),"three wins close NPC");Check(p.CompleteWin(101)==null,"no fourth card");
 for(int i=0;i<35;i++)for(int j=0;j<35;j++){var m=new Match(Catalog.Generals[i].Id,Catalog.Generals[j].Id,(i+j)%2==0,i*67+j);int steps=0;while(m.Winner==-2&&steps++<20000){Check(m.Pending!=null,"match must yield decision "+i+"/"+j);m.Choose(m.AiChoice());var all=m.DrawPile.Concat(m.Discard).Concat(m.Seats.SelectMany(s=>s.Hand.Concat(s.Judgments).Concat(s.Buqu).Concat(s.Equipment.Where(c=>c!=null)))).ToArray();Check(all.GroupBy(c=>c.Id).All(g=>g.Count()==1),"card duplicated "+i+"/"+j+" "+m.Pending?.Text);}
 Check(m.Winner!=-2,"match terminates "+i+"/"+j);}
 Console.WriteLine("SANGUOSHA_TESTS_OK "+checks);
 }
}
