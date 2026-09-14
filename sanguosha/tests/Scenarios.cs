using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using StudentAge.Sanguosha;
static class Scenarios {
 static int count,id=500;static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static Match Blank(string p="zhaoyun",string n="liubei"){var m=new Match(p,n,false,5);m.DrawPile.Clear();m.Discard.Clear();foreach(var s in m.Seats){s.Hand.Clear();s.Judgments.Clear();s.Buqu.Clear();Array.Clear(s.Equipment,0,4);}return m;}
 static Card C(string k,Suit s=Suit.Club,int r=7)=>new Card(++id,k,s,r);
 static void Invoke(Match m,string method,params object[] args){var stack=(Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m);stack.Clear();typeof(Match).GetProperty("Pending").SetValue(m,null);stack.Push((IEnumerator)typeof(Match).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m,args));typeof(Match).GetMethod("Pump",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m,null);}
 static void Finish(Match m,Func<Prompt,int> choose=null){int cap=0;while(m.Pending!=null&&cap++<300){int c=choose==null?m.AiChoice():choose(m.Pending);m.Choose(c);}Check(cap<300,"scenario terminates");}
 public static int Run(){
  var m=Blank();var attack=C("slash");m.Seats[0].Hand.Add(attack);Invoke(m,"Use",0,attack,"slash",false);Finish(m);Check(m.Seats[1].Hp==3,"unanswered slash damages");
  m=Blank();attack=C("slash");m.Seats[0].Hand.Add(attack);m.Seats[1].Hand.Add(C("jink"));Invoke(m,"Use",0,attack,"slash",false);Finish(m);Check(m.Seats[1].Hp==4,"one jink cancels normal slash");
  m=Blank("lvbu");attack=C("slash");m.Seats[0].Hand.Add(attack);m.Seats[1].Hand.Add(C("jink"));Invoke(m,"Use",0,attack,"slash",false);Finish(m);Check(m.Seats[1].Hp==3,"wushuang requires two jinks");
  m=Blank("lvbu");attack=C("slash");m.Seats[0].Hand.Add(attack);m.Seats[1].Hand.AddRange(new[]{C("jink"),C("jink")});Invoke(m,"Use",0,attack,"slash",false);Finish(m);Check(m.Seats[1].Hp==4,"two jinks cancel wushuang");
  m=Blank();var draw=C("ex_nihilo",Suit.Heart);m.Seats[0].Hand.AddRange(new[]{draw,C("nullification")});m.DrawPile.AddRange(new[]{C("slash"),C("jink")});Invoke(m,"Use",0,draw,"ex_nihilo",false);Finish(m);Check(m.Seats[0].Hand.Count==3,"AI does not nullify own beneficial trick");
  m=Blank();attack=C("slash");m.Seats[0].Hand.AddRange(new[]{attack,C("peach",Suit.Heart)});m.Seats[1].Hp=1;Invoke(m,"Use",0,attack,"slash",false);Finish(m);Check(m.Winner==0,"AI does not rescue opposing dying seat");
  m=Blank("daqiao");var virtualCard=C("peach",Suit.Diamond);m.Seats[0].Hand.Add(virtualCard);Invoke(m,"Use",0,virtualCard,"indulgence",false);Finish(m);Check(m.Seats[1].Judgments.Single().Kind=="indulgence","guose enters judgment as indulgence");var steal=C("snatch");m.Seats[0].Hand.Add(steal);Invoke(m,"Use",0,steal,"snatch",false);Finish(m);Check(m.Seats[0].Hand.Any(c=>c.Id==virtualCard.Id&&c.Kind=="peach"),"stolen virtual delay reverts to printed physical card");
  m=Blank("shenguanyu");m.Seats[0].Hp=0;m.Seats[1].Nightmare=1;m.DrawPile.Add(C("slash",Suit.Spade));Invoke(m,"Dying",0,1);Finish(m);Check(m.Winner==-1,"wuhun revenge makes simultaneous death a draw");
  m=Blank("shenguanyu");m.Seats[0].Hp=1;m.Seats[1].Nightmare=1;m.Seats[0].Hp=0;m.DrawPile.Add(C("peach",Suit.Heart));Invoke(m,"Dying",0,1);Finish(m);Check(m.Winner==1,"peach judgment survives wuhun");
  m=Blank("zhoutai");m.Seats[0].Hp=0;m.DrawPile.Add(C("slash",Suit.Club,3));Invoke(m,"Dying",0,1);Finish(m);Check(m.Winner==-2&&m.Seats[0].Buqu.Count==1&&m.Seats[0].Hp==0,"old buqu survives without artificially healing");
  m=Blank();var p=new Progress();Check(p.Reserve(101,1),"reserve global allowance");Check(!p.Reserve(202,1),"global allowance cannot target another NPC");Check(p.Stage(101)==0,"failed round leaves same general");Check(p.Reserve(101,2),"failure retry only next world round");
  return count;
 }
}
