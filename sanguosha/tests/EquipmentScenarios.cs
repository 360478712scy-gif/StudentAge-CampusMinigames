using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using StudentAge.Sanguosha;
static class EquipmentScenarios {
 static int n;static void Check(bool v,string s){n++;if(!v)throw new Exception(s);}
 static object Call(Match m,string name,params object[] a)=>typeof(Match).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,a);
 static Match Blank(bool easy=false){var m=new Match("zhaoyun","liubei",easy,3);m.DrawPile.Clear();m.Discard.Clear();m.Events.Clear();foreach(var p in m.Seats){p.Hand.Clear();p.Judgments.Clear();Array.Clear(p.Equipment,0,4);}((Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m)).Clear();typeof(Match).GetProperty("Pending").SetValue(m,null);return m;}
 static void Start(Match m,string name,params object[] a){((Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m)).Push((IEnumerator)Call(m,name,a));Call(m,"Pump");}
 static Card C(int id,string k="slash",Suit s=Suit.Spade)=>new Card(id,k,s,7);
 public static int Run(){
 foreach(bool destroy in new[]{false,true})foreach(int actor in new[]{0,1})foreach(string zone in new[]{"weapon","armor","judgment","hand"}){
  var m=Blank();int other=1-actor;var w=C(800,"crossbow");var armor=C(801,"eight_diagram");var j=C(802,"indulgence");var h=C(803,"jink");m.Seats[other].Equipment[0]=w;m.Seats[other].Equipment[1]=armor;m.Seats[other].Judgments.Add(j);m.Seats[other].Hand.Add(h);
  Start(m,"Steal",actor,other,destroy,true);Check(m.Pending.Reveal.Count==3&&!m.Pending.Reveal.Contains(h)&&m.Pending.Options.All(o=>o.CardId!=h.Id),"enemy hand remains private during zone selection");var selected=zone=="weapon"?w:zone=="armor"?armor:zone=="judgment"?j:h;m.Choose(m.Pending.Options.FindIndex(o=>o.CardId==(zone=="hand"?-5:selected.Id)));
  Check(!m.Seats[other].Cards.Concat(m.Seats[other].Judgments).Contains(selected),"selected card leaves correct enemy zone");Check(destroy?m.Discard.Contains(selected):m.Seats[actor].Hand.Contains(selected),"discard versus acquisition destination");Check(m.Seats[actor].Equipment.All(c=>c==null),"snatch never auto-equips");var ev=m.Events.Single(e=>e.Movement!=null);Check(ev.Card.Id==selected.Id&&ev.Actor==other&&ev.Target==(destroy?-1:actor)&&ev.EquipmentSlot==(zone=="weapon"?0:zone=="armor"?1:-1),"public movement identifies exact card and source zone");
 }
 foreach(int actor in new[]{0,1}){
  var m=Blank();m.Seats[actor].Equipment[0]=C(810,"crossbow");m.Seats[actor].Equipment[1]=C(811,"eight_diagram");Start(m,"Drowning",1-actor,actor,C(812,"drowning"));m.Choose(0);Check(m.Seats[actor].Equipment.All(c=>c==null)&&m.Discard.Count==2,"drowning removes all equipment");Check(m.Events.Count(e=>e.Movement=="discard"&&e.EquipmentSlot>=0)==2,"each removed equipment has visible motion event");
 }
 foreach(bool easy in new[]{false,true})foreach(int actor in new[]{0,1}){var m=Blank(easy);m.DrawPile.AddRange(Enumerable.Range(900,8).Select(i=>C(i)));Start(m,"Round",actor);Check(m.Seats[actor].Hand.Count==(easy?3:2),"same base draw count for both actors");Check(m.Events.Single(e=>e.DrawCount>0).DrawCount==(easy?3:2),"animation payload equals actual drawn count");}
 foreach(int actor in new[]{0,1})foreach(bool red in new[]{false,true}){var m=Blank();m.Seats[actor].Equipment[1]=C(820,"eight_diagram");var judge=C(821,"slash",red?Suit.Heart:Suit.Spade);m.DrawPile.Add(judge);Start(m,"Respond",actor,"jink",1-actor,true,C(822));m.Choose(m.Pending.Options.FindIndex(o=>o.CardId==-4));var es=m.Events.Where(e=>e.JudgmentStage!=null).ToArray();Check(es.Length==2&&es[0].JudgmentStage=="reveal"&&es[1].JudgmentStage=="result"&&es.All(e=>e.Card==judge&&e.Actor==actor),"both armor users publicly reveal judgment card");Check(es[1].JudgmentSuccess==red&&m.Discard.Contains(judge),"armor red succeeds black fails, judgment discarded");}
 return n;
 }
}
