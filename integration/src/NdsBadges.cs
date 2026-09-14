using System;using System.Collections.Generic;using System.IO;using System.Linq;using Config;using HarmonyLib;using Sdk;using UnityEngine;
namespace StudentAge.CampusMinigames {
public sealed class NdsBadge {
 public readonly int GameId,ItemId;public readonly string Name,Story;public readonly string[] Effects;public readonly List<List<float>> NativeEffects;
 public bool Ultimate=>GameId==0;
 public NdsBadge(int game,string name,string story,string[] effects,params float[][] native){GameId=game;ItemId=game==0?919999:919920+game-9101;Name=name;Story=story;Effects=effects;NativeEffects=native.Select(x=>x.ToList()).ToList();}
}
public static class NdsBadges {
 static float[] Round(int attr,float n)=>new float[]{1,11,attr,n};static float[] Held(int attr,float n)=>new float[]{1,1,attr,n};
 public static readonly NdsBadge[] All={
  new NdsBadge(9101,"王牌同桌","最后一张牌，也要出得漂亮。",new[]{"信任 +5 / 回合"},Round(3,5)),
  new NdsBadge(9102,"五子妙手","方寸棋盘，自有少年天地。",new[]{"智力 +3 / 回合"},Round(1,3)),
  new NdsBadge(9103,"泡泡晴空","把烦恼一颗颗送上晴空。",new[]{"精力恢复 +10","精力恢复上限 +10"},Held(8,10),Held(9,10)),
  new NdsBadge(9104,"寻物奇才","看似寻常的盒子，藏不住你的眼力。",new[]{"阅读速度 +10"},Held(331,10)),
  new NdsBadge(9105,"二十四点星","算式收尾时，答案刚好是你。",new[]{"数学进度 +25% / 回合"}),
  new NdsBadge(9106,"方块建筑师","再乱的局面，也能留出余地。",new[]{"谨慎 +5 / 回合","冷静 +5 / 回合"},Round(508,5),Round(505,5)),
  new NdsBadge(9107,"欢乐牌王","三个人的牌桌，你最有主意。",new[]{"圆滑 +5 / 回合","贪婪 +5 / 回合"},Round(504,5),Round(503,5)),
  new NdsBadge(9108,"贪吃小霸王","下一颗果子，当然也不能放过。",new[]{"贪婪 +10 / 回合"},Round(503,10)),
  new NdsBadge(9109,"迷宫追光者","拐过下个路口，就能吃到星光。",new[]{"体魄 +3 / 回合"},Round(4,3)),
  new NdsBadge(9110,"蘑菇冒险家","越过城堡，旗杆记住了你的名字。",new[]{"成就 +5 / 回合"},Round(100,5)),
  new NdsBadge(9111,"丛林英雄","并肩的勇气，胜过漫天的炮火。",new[]{"阅历 +5 / 回合"},Round(10,5)),
  new NdsBadge(9112,"雀跃高手","摸到好牌，更要沉得住气。",new[]{"情商 +3 / 回合"},Round(2,3)),
  new NdsBadge(0,"NDS大玩家","十二道光芒，汇成属于你的传说。",new[]{"热情 +10 / 回合","心情 +20 / 回合"},Round(11,10),Round(0,20))};
 public static bool Has(NdsBadge badge)=>Singleton<BagMgr>.Ins.HasEnoughItem(badge.ItemId);
 public static int Count=>All.Count(b=>!b.Ultimate&&Has(b));
 public static void Register(){if(Cfg.ItemCfgMap==null)return;foreach(var b in All){if(Cfg.ItemCfgMap.ContainsKey(b.ItemId))continue;Cfg.ItemCfgMap.Add(b.ItemId,new ItemCfg{id=b.ItemId,name=b.Name,desc=b.Story,icon="ndsbadges/badge-"+b.ItemId,type=4,maxcount=1,rarity=b.Ultimate?4:3,sell=-1,value=0,itemTag=new List<int>{2},effect=b.NativeEffects,usingEffect=new List<List<float>>(),precondition=new List<List<double>>()});}}
 // Ownership and native effect UIDs are saved by BagMgr. No account-wide unlocks or reapplication on every frame.
 public static List<NdsBadge> Reconcile(){var awards=new List<NdsBadge>();Register();if(!NdsIntegration.Owned)return awards;foreach(var b in All.Where(b=>!b.Ultimate)){var game=NdsCatalog.Games.First(g=>g.Id==b.GameId);if(NdsProgress.Cleared(game)>=game.Stages)Award(b,awards);}if(Count==12)Award(All[12],awards);return awards;}
 static void Award(NdsBadge b,List<NdsBadge> awards){if(!Has(b)&&Singleton<BagMgr>.Ins.AddItem(b.ItemId,1,b.Name,false))awards.Add(b);}
 public static void MathRound(){var badge=All[4];if(!Has(badge))return;var records=Singleton<CommonEvtMgr>.Ins;string key="studio.studentage.nds.badges.mathRound";int round=Singleton<RoundMgr>.Ins.GetRound();if(records.GetRecordTag(key)==round.ToString())return;
  var study=Singleton<RoleMgr>.Ins.GetStudyData();if(study==null||study.SelectedCourses==null||!study.SelectedCourses.Contains(2))return;
  var course=study.GetCourseData(2);if(course==null)return;var next=course.GetUnLearnOldKnowledge();if(!Cfg.KnowledgeTreeCfgMap.TryGetValue(next.id,out var chapter))return;
  // Learn() halves remedial chapter progress. Compensate so this award remains one quarter of that chapter.
  float points=chapter.max*.25f;if(next.state<Singleton<RoleMgr>.Ins.GetRole().GradeState)points*=2;
  records.AddRecordTag(key,round.ToString());study.AddSubjectProgress(2,points,badge.Name);
 }
}
[HarmonyPatch(typeof(StudyData),"UpdateKnowledgePointEachRound")]static class NdsMathBadgeRound{static void Postfix(){NdsBadges.MathRound();}}
[HarmonyPatch(typeof(ItemData),"GetEffectStr")]static class NdsBadgeDescription{static bool Prefix(ItemData __instance,ref string __result){var b=NdsBadges.All.FirstOrDefault(x=>x.ItemId==__instance.id);if(b==null)return true;__result=string.Join("\n",b.Effects);return false;}}
public static class NdsBadgeAssets {
 static readonly Dictionary<int,Sprite> sprites=new Dictionary<int,Sprite>();
 public static string Root=>Path.Combine(Path.GetDirectoryName(typeof(NdsConsole).Assembly.Location),"Nds","Badges");
 public static Sprite Get(NdsBadge b){if(sprites.TryGetValue(b.ItemId,out var s)&&s!=null)return s;string path=Path.Combine(Root,"badge-"+b.ItemId+".png");if(!File.Exists(path))return null;var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100);sprites[b.ItemId]=s;return s;}
 public static void Clear(){foreach(var s in sprites.Values)if(s!=null){UnityEngine.Object.Destroy(s.texture);UnityEngine.Object.Destroy(s);}sprites.Clear();}
}
[HarmonyPatch(typeof(ResMgr),"LoadSpriteAsync")]static class NdsBadgeItemIcon {
 static bool Prefix(string _path,Action<Sprite> _compCallback){if(_path==null)return true;var b=NdsBadges.All.FirstOrDefault(x=>_path.Replace('\\','/').Contains("ndsbadges/badge-"+x.ItemId));if(b==null)return true;_compCallback?.Invoke(NdsBadgeAssets.Get(b));return false;}
}
}
