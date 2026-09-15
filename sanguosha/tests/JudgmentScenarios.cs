using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Reflection;using StudentAge.Sanguosha;
static class JudgmentScenarios {
 static int n;static void Check(bool v,string s){n++;if(!v)throw new Exception(s);}
 static object Call(Match m,string name,params object[] args)=>typeof(Match).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,args);
 static Match Blank(string actor="zhaoyun") {var m=new Match(actor,"liubei",false,5);m.DrawPile.Clear();m.Discard.Clear();foreach(var s in m.Seats){s.Hand.Clear();s.Judgments.Clear();s.Buqu.Clear();Array.Clear(s.Equipment,0,4);}((Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m)).Clear();typeof(Match).GetProperty("Pending").SetValue(m,null);m.Events.Clear();return m;}
 static void Start(Match m,string name,params object[] args){((Stack<IEnumerator>)typeof(Match).GetField("stack",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m)).Push((IEnumerator)Call(m,name,args));Call(m,"Pump");}
 static Card C(int id,string kind="slash",Suit suit=Suit.Spade)=>new Card(id,kind,suit,7);
 public static int Run(){
  foreach(string general in new[]{"zhangjiao","simayi"}){
   var m=Blank(general);var original=C(701,"jink",Suit.Heart);var black=C(702);var red=C(703,"peach",Suit.Heart);var equipment=C(704,"qinggang_sword");m.Seats[0].Hand.AddRange(new[]{black,red});m.Seats[0].Equipment[0]=equipment;m.DrawPile.Add(original);
   Start(m,"Judge",0,"乐不思蜀",new Func<Card,bool>(c=>c.Suit==Suit.Heart));
   Check(m.Pending.Kind==DecisionKind.Judgment&&m.Pending.Subject==original,"replacement prompt exposes original judgment card");
   Check(m.Pending.Options.Any(o=>o.CardId==black.Id)&&m.Pending.Options.Any(o=>o.CardId==equipment.Id)==(general=="zhangjiao")&&m.Pending.Options.Any(o=>o.CardId==red.Id)==(general=="simayi"),"guidao black hand/equipment versus guicai hand only");
   var drag=new CardDrag();int index=m.Pending.Options.FindIndex(o=>o.CardId==black.Id);drag.Begin(m.Pending,index,0);Check(drag.Target==DropZone.Judgment&&drag.Release(m.Pending,DropZone.Opponent,1)==-1,"replacement rejects wrong portrait");drag.Begin(m.Pending,index,0);m.Choose(drag.Release(m.Pending,DropZone.Judgment,1));
   Check(m.Events.Where(e=>e.JudgmentStage!=null).Select(e=>e.JudgmentStage).SequenceEqual(new[]{"reveal","replace","result"}),"judgment shows initial, replacement, then final result");
   var result=m.Events.Last(e=>e.JudgmentStage=="result");Check(result.Card==black&&result.JudgmentSuccess&&result.Text.Contains("跳过出牌阶段"),"non-heart makes indulgence effective with visible consequence");
   Check(general=="zhangjiao"?m.Seats[0].Hand.Contains(original):m.Discard.Contains(original),"guidao obtains replaced card; guicai discards it");
  }
  foreach(string reason in new[]{"乐不思蜀","兵粮寸断","八卦阵","洛神","雷击","刚烈","武魂"}){
   var m=Blank();var card=C(710);m.DrawPile.Add(card);Start(m,"Judge",0,reason,new Func<Card,bool>(c=>true));var e=m.Events.Last();
   Check(e.JudgmentStage=="result"&&e.Card==card,"every judgment publishes actual final card: "+reason);
  }
  {
   var m=Blank();var jink=C(720,"jink");m.Seats[0].Hand.Add(jink);m.Seats[0].Equipment[1]=C(721,"eight_diagram");m.DrawPile.Add(C(722));Start(m,"Respond",0,"jink",1,true,C(723));
   Check(m.Pending.Kind==DecisionKind.Response&&m.Pending.Options.Any(o=>o.CardId==jink.Id)&&m.Pending.Options.Any(o=>o.CardId==-4),"jink and optional armor are offered simultaneously without activation gate");m.Choose(m.Pending.Options.FindIndex(o=>o.CardId==jink.Id));Check(m.Pending==null&&m.DrawPile.Count==1&&!m.Seats[0].Hand.Contains(jink),"direct jink does not judge armor or request another activation");
  }
  {
   var m=Blank();var jink=C(730,"jink");m.Seats[0].Hand.Add(jink);m.Seats[0].Equipment[1]=C(731,"eight_diagram");m.DrawPile.Add(C(732));Start(m,"Respond",0,"jink",1,true,C(733));m.Choose(m.Pending.Options.FindIndex(o=>o.CardId==-4));
   Check(m.Pending.Kind==DecisionKind.Response&&!m.Pending.Options.Any(o=>o.CardId==-4)&&m.Pending.Options.Any(o=>o.CardId==jink.Id),"failed armor judgment returns to direct Jink, cannot retry armor");m.Choose(m.Pending.Options.FindIndex(o=>o.CardId==jink.Id));Check(m.Pending==null,"jink after failed armor finishes response");
  }
  foreach(string text in new[]{"天香：弃置红桃手牌","丈八蛇矛：选择手牌 1/2","神速：弃置装备牌","蛊惑：暗置一张手牌","仁德：交给对手一张手牌","青囊：弃一张手牌","结姻：弃手牌 1/2"}){
   var m=Blank();var c=C(740);m.Seats[0].Hand.Add(c);Start(m,"Pick",0,text,m.Seats[0].Hand,false,true,false);Check(m.Pending.Kind==DecisionKind.SkillCard,"own skill-cost cards permit dragging: "+text);var drag=new CardDrag();drag.Begin(m.Pending,0,0);Check(drag.Release(m.Pending,text.StartsWith("仁德")?DropZone.Opponent:DropZone.Table,1)==0,"skill card has concrete drop target: "+text);
  }
  return n;
 }
}
