using System;using System.Linq;using Sdk;using StudentAge.CampusUno;
namespace StudentAge.CampusMinigames{
// Native record tags belong to the loaded save. Integer half-stage units avoid rounding drift.
public static class NdsProgress{
 const string Prefix="studio.studentage.nds.v1.";
 static int Read(int game,string field,int fallback){var value=Singleton<CommonEvtMgr>.Ins.GetRecordTag(Prefix+game+"."+field);return int.TryParse(value,out int n)?n:fallback;}
 static void Write(int game,string field,int value)=>Singleton<CommonEvtMgr>.Ins.AddRecordTag(Prefix+game+"."+field,value.ToString(System.Globalization.CultureInfo.InvariantCulture));
 public static int LimitUnits=>MinigameTuning.Int("NDS","RoundLimit")*2;
 public static int CostUnits(NdsGame game)=>game.Id==9110?MinigameTuning.Int("NDS","MarioHalfUnits"):2;
 public static int UsedUnits{get{int round=Singleton<RoundMgr>.Ins.GetRound();if(Read(0,"budgetRound",int.MinValue)==round)return Math.Max(0,Read(0,"budgetUsed",0));
 // Old builds allowed one advance per game per round. Import their completed advances once.
 return NdsCatalog.Games.Where(g=>Read(g.Id,"advanceRound",int.MinValue)==round&&Cleared(g)>0).Sum(CostUnits);}}
 public static int RemainingUnits=>Math.Max(0,LimitUnits-UsedUnits);
 public static string RemainingText=>(RemainingUnits/2f).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture);
 public static int Cleared(NdsGame game)=>Math.Max(0,Math.Min(game.Stages,Read(game.Id,"cleared",0)));
 public static bool CanPlay(NdsGame game,int level){if(level<1||level>game.Stages)return false;int cleared=Cleared(game);return level<=cleared||(level==cleared+1&&RemainingUnits>=CostUnits(game));}
 public static string Status(NdsGame game,int level)=>level<=Cleared(game)?"已通关 · 可重玩":level>Cleared(game)+1?"尚未解锁":CanPlay(game,level)?"新关 · 本回合余"+RemainingText+"关":"额度不足 · 下回合开放";
 public static void Complete(NdsGame game,int level,int startRound,Outcome result){if(result!=Outcome.Win||level!=Cleared(game)+1||!CanPlay(game,level)||startRound!=Singleton<RoundMgr>.Ins.GetRound())return;
 int used=UsedUnits+CostUnits(game);Write(0,"budgetUsed",used);Write(0,"budgetRound",startRound);Write(game.Id,"cleared",level);Write(game.Id,"advanceRound",startRound);NdsConsole.Current?.QueueBadges(NdsBadges.Reconcile());}
}
}
