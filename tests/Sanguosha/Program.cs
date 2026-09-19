using System;using System.Linq;using StudentAge.Sanguosha;
static class Test {
 static int checks;static void Check(bool b,string m){checks++;if(!b)throw new Exception(m);}
 static void Main(){checks+=EquipmentScenarios.Run();checks+=Scenarios.Run();checks+=DeckScenarios.Run();checks+=JudgmentScenarios.Run();checks+=TableSelectionTests.Run();Check(Catalog.Generals.Length==35,"35 generals");Check(Deck.Create().Count==108,"108 card distribution");Check(Deck.Create().GroupBy(c=>c.Id).All(g=>g.Count()==1),"unique physical card IDs");Check(Encounters.RewardPool.Length==30&&Encounters.RewardPool.Distinct().Count()==30,"ten NPCs each reserve three unique generals");Check(Encounters.For(201).Contains("sunquan"),"Sun Quan belongs to Meng Huaian");
 foreach(bool male in new[]{true,false})foreach(bool dlc in new[]{true,false}){
  var available=Encounters.Npcs.Where(n=>(dlc||n!=103&&n!=204)&&(male?n!=202&&n!=204:n!=103&&n!=105)).ToArray();
  var rewards=available.SelectMany(Encounters.For);var shop=Catalog.Generals.Where(g=>Encounters.ShopEligible(g.Id,available,Array.Empty<int>(),new Progress())).Select(g=>g.Id);
  Check(rewards.Concat(shop).Append("zhaoyun").Distinct().Count()==35,"both routes and DLC states can collect all 35 generals");
  Check(!rewards.Intersect(shop).Any(),"available NPC rewards remain earned through battles");
  foreach(int npc in Encounters.Npcs.Except(available))Check(Encounters.For(npc).All(shop.Contains),"unavailable route NPC cards go directly to shop");
 }
 Check(Encounters.OnRoute(3,true)&&!Encounters.OnRoute(3,false)&&Encounters.OnRoute(4,false)&&!Encounters.OnRoute(4,true)&&Encounters.OnRoute(2,true)&&Encounters.OnRoute(2,false),"native route filters");
 var migrated=new Progress();migrated.Wins[201]=3;Check(Encounters.ShopEligible("sunquan",Encounters.Npcs,Array.Empty<int>(),migrated),"old cleared stage can buy newly mapped missing Sun Quan");Check(!Encounters.ShopEligible("sunquan",Encounters.Npcs,Array.Empty<int>(),new Progress()),"uncleared stage not sold early");Check(Encounters.ShopEligible("sunquan",Encounters.Npcs,new[]{201},new Progress()),"full follow list makes unfollowed reward purchasable");

 var p=new Progress();Check(p.Reserve(101,22),"first fight allowed");Check(!p.CanBegin(102,22),"same round different NPC blocked");Check(!p.CanBegin(101,22),"loss cannot retry same round");Check(p.Stage(101)==0,"loss does not advance");Check(p.Reserve(101,23),"retry next round");for(int i=0;i<3;i++)Check(p.CompleteWin(101)==Encounters.For(101)[i],"win gives stage card");Check(!p.CanBegin(101,24),"three wins close NPC");Check(p.CompleteWin(101)==null,"no fourth card");
 for(int i=0;i<35;i++)for(int j=0;j<35;j++){var m=new Match(Catalog.Generals[i].Id,Catalog.Generals[j].Id,(i+j)%2==0,i*67+j);int steps=0;while(m.Winner==-2&&steps++<20000){Check(m.Pending!=null,"match must yield decision "+i+"/"+j);m.Choose(m.AiChoice());var all=m.DrawPile.Concat(m.Discard).Concat(m.Seats.SelectMany(s=>s.Hand.Concat(s.Judgments).Concat(s.Buqu).Concat(s.Equipment.Where(c=>c!=null)))).ToArray();Check(all.GroupBy(c=>c.Id).All(g=>g.Count()==1),"card duplicated "+i+"/"+j+" "+m.Pending?.Text);}
 Check(m.Winner!=-2,"match terminates "+i+"/"+j);}
 Console.WriteLine("SANGUOSHA_TESTS_OK "+checks);
 }
}
