using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using BepInEx;using Config;using HarmonyLib;using Newtonsoft.Json;using Sdk;using UnityEngine;using UnityEngine.InputSystem;using StudentAge.CampusUno;
namespace StudentAge.CampusMinigames{
[BepInPlugin("studio.studentage.nds","NDS掌机",CampusVersion.Value)][BepInDependency("studio.studentage.campusuno")][BepInDependency("sa.EC2B.UnofficialPatch")]
public sealed class NdsPlugin:BaseUnityPlugin{
 Harmony harmony;float next;void Awake(){harmony=new Harmony("studio.studentage.nds");harmony.PatchAll(typeof(NdsPlugin).Assembly);}
 void Update(){if(Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame)NdsConsole.OpenFromTitle();if(Time.unscaledTime<next)return;next=Time.unscaledTime+1;NdsIntegration.Register();}
 void OnDestroy(){NdsConsole.Invalidate();NdsBadgeAssets.Clear();harmony?.UnpatchSelf();}
}
public static class NdsIntegration{
 // EntryView is also the in-game pause menu, so require the title game state as well.
 public static bool IsTitleScreen=>Game.GetGameState()==GameState.Start&&UIMgr.GetTopView() is View.Main.EntryView&&!UIMgr.IsShowingMask()&&!UIMgr.IsViewOpeningOrOpened<View.Main.LoadingView>();
 public const int Id=919901;public const string Name="NDS掌机";public static float Price=>MinigameTuning.Get("NDS","Price");
 public static bool Register(){if(Cfg.ItemCfgMap==null||Cfg.ActionCfgMap==null||Cfg.ShopCfgMap==null||!Cfg.ItemCfgMap.ContainsKey(9032)||!Cfg.ActionCfgMap.ContainsKey(2007))return false;
  if(!Cfg.ItemCfgMap.ContainsKey(Id)){var v=new ItemCfg{rarity=1,itemTag=new List<int>{2}};v.id=Id;v.name=Name;v.desc="2005年新发售的双屏掌机。据说谁先拿到手，谁就能称霸同龄人的课间。";v.icon="item/img_youxiji";v.maxcount=1;v.type=4;v.sell=-1;v.value=Price;v.effect=new List<List<float>>{new List<float>{1,11,1,5},new List<float>{1,11,2,5},new List<float>{1,11,4,5},new List<float>{1,11,0,5}};v.usingEffect=new List<List<float>>();v.precondition=new List<List<double>>();Cfg.ItemCfgMap.Add(Id,v);}
  if(!Cfg.ShopCfgMap.ContainsKey(Id))Cfg.ShopCfgMap.Add(Id,new ShopCfg{id=Id,group=Id,price=Price,time=2005,type=4,maxcount=1,limcount=1,probability=1,precondition=new List<List<double>>(),buyTalk="新到的双屏掌机，里面装了不少游戏。"});
  if(!Cfg.ActionCfgMap.ContainsKey(Id)){var v=new ActionCfg{attrs=new List<string>(),expReward=new List<List<float>>(),beginTime=new List<float>(),endTime=new List<float>(),interactable=new List<List<double>>()};v.id=Id;v.name=Name;v.type=2;v.map=1;v.funcId=Id;v.icon="item/img_youxiji";v.cost=new List<List<float>>();v.unlock=new List<List<double>>();v.effect=new List<List<float>>();Cfg.ActionCfgMap.Add(Id,v);}NdsBadges.Register();return true;
 }
 // Older preview saves owned the console before its passive effect was added. Repair only that case.
 public static void RestorePassive(){if(!Register()||!Owned)return;var item=Singleton<BagMgr>.Ins.GetBagData(4,Id) as ItemData;if(item==null||item.equipEffector!=null)return;item.equipEffector=CommonEvtMgr.GenEffector(Cfg.ItemCfgMap[Id].effect);item.equipEffector.SetTag(Name);item.equipEffector.SetIsInc(true);item.equipEffector.Run();item.effectUids=item.equipEffector.GetBaseIncreaserUids();}
 public static bool Owned=>Singleton<BagMgr>.Ins.HasEnoughItem(Id);
 public static bool OnSale{get{int year=Singleton<RoundMgr>.Ins.GetYear();if(year!=2005)return year>2005;var summer=Cfg.SeasonCfgMap.Values.Where(s=>s.name!=null&&s.name.Contains("夏")&&s.month!=null&&s.month.Count>0).SelectMany(s=>s.month).DefaultIfEmpty(6).Min();return Singleton<RoundMgr>.Ins.GetMonth()[0]>=summer;}}
 public static void SyncAction(ActionData data){if(!Register())return;var row=data.GetAction(Id);if(Owned){if(row==null)data.AddAction(Id);}else if(row!=null)data.DelAction(Id);}
 public static bool CanBuy=>Register()&&OnSale&&!Owned;
}
[HarmonyPatch(typeof(BagMgr),"Load")]static class NdsBeforeBagLoad{static void Prefix(){NdsIntegration.Register();}}
[HarmonyPatch(typeof(BagMgr),"LoadEnd")]static class NdsAfterBagLoad{static void Postfix(){NdsIntegration.RestorePassive();}}
[HarmonyPatch(typeof(ShopMgr),"GetShopDatas")]static class NdsShopList{
 static void Postfix(int _type,int _minCnt,ref List<ShopData> __result){if(!NdsIntegration.Register())return;__result.RemoveAll(v=>v.id==NdsIntegration.Id);if((_type==4||_type==100)&&_minCnt<=1&&NdsIntegration.CanBuy){var v=new ShopData{id=NdsIntegration.Id,type=4,quality=1,cnt=1,showInThisYear=true,canBargin=false,isNew=true};v.RefreshPrize();__result.Insert(0,v);}}
}
[HarmonyPatch(typeof(ShopData),"get_Prize")]static class NdsBasePrice{static void Postfix(ShopData __instance,ref float __result){if(__instance.id==NdsIntegration.Id)__result=NdsIntegration.Price;}}
[HarmonyPatch(typeof(ShopData),"get_FinalPrize")]static class NdsFinalPrice{static void Postfix(ShopData __instance,ref float __result){if(__instance.id==NdsIntegration.Id)__result=NdsIntegration.Price;}}
[HarmonyPatch(typeof(ShopMgr),"Buy",new Type[]{typeof(ShopData),typeof(float)})]static class NdsBuyShop{
 static bool Prefix(ShopData _shopData,ref float _discount,ref bool __result){if(_shopData.id!=NdsIntegration.Id)return true;_discount=1;if(NdsIntegration.CanBuy)return true;__result=false;return false;}}
[HarmonyPatch(typeof(ShopMgr),"Buy",new Type[]{typeof(int)})]static class NdsBuyId{
 static bool Prefix(int _shopId,ref bool __result){if(_shopId!=NdsIntegration.Id||NdsIntegration.CanBuy)return true;__result=false;return false;}}
[HarmonyPatch(typeof(BagMgr),"AddItem")]static class NdsBought{static void Postfix(int _id,bool __result){if(_id==NdsIntegration.Id&&__result)NdsIntegration.SyncAction(Singleton<RoleMgr>.Ins.GetActionData());}}
[HarmonyPatch(typeof(ActionData),"GetActions")]static class NdsActions{static void Prefix(ActionData __instance){NdsIntegration.SyncAction(__instance);}}
[HarmonyPatch(typeof(FuncMgr),"OpenFuncView")]static class NdsOpenAction{static bool Prefix(int _funcId){if(_funcId!=NdsIntegration.Id)return true;if(NdsIntegration.Owned)NdsConsole.Open();return false;}}
[HarmonyPatch]static class NdsInvalidate{
 static IEnumerable<MethodBase> TargetMethods(){yield return AccessTools.Method(typeof(Game),"LoadGame");yield return AccessTools.Method(typeof(Game),"NewGame");yield return AccessTools.Method(typeof(Game),"BackToMain");}
 [HarmonyPriority(Priority.First)]static void Prefix(){NdsConsole.Invalidate();}
}
}
