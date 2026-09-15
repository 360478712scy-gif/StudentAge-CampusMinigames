using System;
using System.Linq;
using StudentAge.Sanguosha;
static class TableSelectionTests {
 public static int Run(){int checks=0;Action<bool,string> check=(b,s)=>{checks++;if(!b)throw new Exception(s);};
  var p=new Prompt(0,"play",new[]{new Option("杀",0,1){UseKind="slash",Target=1},new Option("桃",0,2){UseKind="peach"},new Option("结束出牌")});
  var selection=new TableSelection();selection.Bind(p);selection.Select(0);check(!selection.CanConfirm,"attack requires target selection");check(selection.Confirm(p)==-1,"selecting a hand card cannot immediately attack");selection.SelectTarget(0);check(!selection.CanConfirm,"cannot select illegal target");selection.SelectTarget(1);check(selection.CanConfirm,"legal target enables confirmation");selection.SelectTarget(1);check(!selection.CanConfirm,"clicking target again deselects it");selection.SelectTarget(1);check(selection.Confirm(p)==0,"explicit confirm emits selected choice");check(!selection.CanConfirm,"confirm clears tentative choice");selection.Select(1);check(selection.CanConfirm,"non-target card does not require a target");selection.Clear();check(selection.Confirm(p)==-1,"cancel never plays a card");selection.Select(0);selection.SelectTarget(1);var next=new Prompt(0,"response",new[]{new Option("闪",0,3)});check(selection.Confirm(next)==-1,"stale choice cannot enter a new prompt");check(selection.Prompt==next&&selection.Index==-1,"prompt replacement resets selection");selection.Select(99);check(selection.Index==-1,"invalid index ignored");
  var m=new Match("zhaoyun","ganning",true,40);int guard=0;while(m.Pending!=null&&!m.Pending.Text.EndsWith(" · 出牌阶段")&&guard++<30)m.Choose(m.AiChoice());
  check(m.Pending!=null,"live engine reaches play prompt");foreach(var o in m.Pending.Options.Where(o=>o.CardId>=0)){check(o.UseKind!=null,"play choice carries effective card type");if(new[]{"slash","duel","snatch","dismantlement","collateral","indulgence"}.Contains(o.UseKind))check(o.Target==1-m.Pending.Actor,"attack target follows acting seat");}
  return checks;
 }
}
