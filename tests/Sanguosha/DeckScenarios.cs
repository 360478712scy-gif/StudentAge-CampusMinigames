using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StudentAge.Sanguosha;

static class DeckScenarios {
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static object Call(Match m,string name,params object[] args)=>typeof(Match).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,args);
 static Match Blank(string actor="zhaoyun"){
  var m=new Match(actor,"liubei",false,91);m.DrawPile.Clear();m.Discard.Clear();
  foreach(var s in m.Seats){s.Hand.Clear();s.Judgments.Clear();s.Buqu.Clear();Array.Clear(s.Equipment,0,4);}
  ((Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m)).Clear();
  typeof(Match).GetProperty("Pending").SetValue(m,null);return m;
 }
 static void Start(Match m,string name,params object[] args){
  ((Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m)).Push((IEnumerator)Call(m,name,args));Call(m,"Pump");
 }
 public static int Run(){
  var deck=Deck.CreateDuel();
  Check(deck.Count==52&&deck.Select(c=>c.Id).Distinct().Count()==52,"52 physical cards with unique IDs");
  Check(deck.Select(c=>(c.Suit,c.Rank)).Distinct().Count()==52,"one card for every suit/rank in published duel table");
  foreach(Suit suit in Enum.GetValues(typeof(Suit)))Check(deck.Count(c=>c.Suit==suit)==13,"13 cards per suit");
  var counts=new Dictionary<string,int>{{"slash",16},{"jink",8},{"peach",4},{"dismantlement",3},{"snatch",3},{"duel",2},{"nullification",2},{"ex_nihilo",2},{"drowning",1},{"archery_attack",1},{"savage_assault",1},{"supply_shortage",1},{"indulgence",1},{"crossbow",1},{"spear",1},{"axe",1},{"ice_sword",1},{"qinggang_sword",1},{"eight_diagram",1},{"renwang_shield",1}};
  Check(deck.GroupBy(c=>c.Kind).Count()==counts.Count&&counts.All(p=>deck.Count(c=>c.Kind==p.Key)==p.Value),"all official per-kind counts, no extra military cards");
  // Independent suit columns transcribed from the official card-table image.
  string[][] columns={
   new[]{"duel","eight_diagram","dismantlement","snatch","slash","qinggang_sword","slash","slash","ice_sword","slash","snatch","spear","savage_assault"},
   new[]{"archery_attack","jink","peach","peach","jink","indulgence","ex_nihilo","ex_nihilo","peach","slash","slash","dismantlement","nullification"},
   new[]{"duel","renwang_shield","dismantlement","slash","slash","slash","drowning","slash","slash","slash","slash","supply_shortage","nullification"},
   new[]{"crossbow","jink","jink","snatch","axe","slash","jink","jink","slash","jink","jink","peach","slash"}};
  var suits=new[]{Suit.Spade,Suit.Heart,Suit.Club,Suit.Diamond};
  for(int s=0;s<4;s++)for(int r=1;r<=13;r++)Check(deck.Single(c=>c.Suit==suits[s]&&c.Rank==r).Kind==columns[s][r-1],"official suit/rank entry "+s+"/"+r);
  var m=Blank();m.DrawPile.AddRange(deck);var drawn=new List<Card>();
  for(int i=0;i<52;i++)drawn.Add((Card)Call(m,"Top"));
  Check(drawn.SequenceEqual(deck),"drawing consumes shuffled order from top, not independent random choices");
  Check(Call(m,"Top")==null&&m.DrawPile.Count==0,"empty deck and discard cannot create a card");
  m=Blank();m.DrawPile.Add(deck[0]);m.Discard.AddRange(deck.Skip(1).Take(4));
  m.Seats[0].Hand.Add(deck[5]);m.Seats[1].Equipment[0]=deck[45];m.Seats[1].Judgments.Add(deck[44]);m.Seats[1].Buqu.Add(deck[6]);
  Call(m,"Draw",0,8);
  Check(m.Seats[0].Hand.Select(c=>c.Id).OrderBy(x=>x).SequenceEqual(deck.Take(6).Select(c=>c.Id)),"cross-exhaustion draw recycles only discard once");
  Check(m.DrawPile.Count==0&&m.Discard.Count==0&&m.Seats[1].Equipment[0]==deck[45]&&m.Seats[1].Judgments.Single()==deck[44]&&m.Seats[1].Buqu.Single()==deck[6],"held/equipped/delayed/special cards never join shuffle");
  Check(m.Log.Last().Contains("摸5张牌"),"partial draw reports actual amount");
  m=Blank();var trick=deck.First(c=>c.Kind=="ex_nihilo");m.Seats[0].Hand.Add(trick);Start(m,"Use",0,trick,"ex_nihilo",false);
  Check(m.Seats[0].Hand.Count==0&&m.Discard.Single()==trick,"resolving ex nihilo cannot draw itself from an empty pile");
  Check(Call(m,"Top")==trick&&Call(m,"Top")==null,"resolved trick becomes recyclable exactly once");
  m=Blank("luxun");var response=deck.First(c=>c.Kind=="jink");m.Seats[0].Hand.Add(response);Start(m,"Respond",0,"jink",1,false,null);
  m.Choose(m.Pending.Options.FindIndex(o=>o.CardId==response.Id));
  Check(m.Pending.Text.StartsWith("连营"),"response loss triggers lianying while response still resolves");m.Choose(0);
  Check(m.Seats[0].Hand.Count==0&&m.Discard.Single()==response,"lianying cannot redraw still-resolving response");
  Check(Call(m,"Top")==response,"completed response is released for the next shuffle");
  m=Blank("yuji");m.Seats[0].Hand.Add(deck[0]);
  Check(!((List<Option>)Call(m,"PlayOptions",0)).Any(o=>o.Label.Contains("酒")),"guhuo cannot declare a card excluded from this mode");
  var one=new Match("zhaoyun","liubei",false,17);var two=new Match("zhaoyun","liubei",false,17);var other=new Match("zhaoyun","liubei",false,18);
  Check(one.DrawPile.Select(c=>c.Id).SequenceEqual(two.DrawPile.Select(c=>c.Id)),"same seed reproduces initial shuffle");
  Check(!one.DrawPile.Select(c=>c.Id).SequenceEqual(other.DrawPile.Select(c=>c.Id)),"different seeds shuffle differently");
  return checks;
 }
}
