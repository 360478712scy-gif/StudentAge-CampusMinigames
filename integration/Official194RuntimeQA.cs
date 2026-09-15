using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;
using BepInEx;using Config;using Sdk;using HarmonyLib;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using StudentAge.CampusUno;using StudentAge.CampusMinigames;
[BepInPlugin("studentage.official194.qa","Official 1.94 QA","1.0.0")]
[BepInDependency("studio.studentage.nds")]
public sealed class Official194QAPlugin:BaseUnityPlugin{
 void Awake(){var go=new GameObject("Official194_QA"){hideFlags=HideFlags.HideAndDontSave};DontDestroyOnLoad(go);go.AddComponent<Official194QA>();}
}
public sealed class Official194QA:MonoBehaviour{
 string dir;readonly List<string> checks=new List<string>();readonly List<string> errors=new List<string>();
 void Awake(){dir=Path.Combine(Paths.GameRootPath,"CampusUpQA");Directory.CreateDirectory(dir);PathDefine.SAVE_PATH=Path.Combine(dir,"Saves");PathDefine.TEST_SAVE_PATH=PathDefine.SAVE_PATH;PathDefine.IMG_PATH=Path.Combine(dir,"Images");PathDefine.MUSIC_PATH=Path.Combine(dir,"Music");Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Exception)errors.Add(m+"\n"+s);};}
 void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);Debug.Log("194 PASS "+text);}
 IEnumerator Start(){var run=Run();while(true){bool more=false;object next=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){File.WriteAllText(Path.Combine(dir,"failed.txt"),e.ToString());break;}if(!more){File.WriteAllText(Path.Combine(dir,"success.txt"),"OFFICIAL_194_OK");break;}yield return next;}File.WriteAllLines(Path.Combine(dir,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit();}
 IEnumerator Run(){
 float end=Time.realtimeSinceStartup+120;while(Time.realtimeSinceStartup<end){bool ready=false;try{ready=NdsIntegration.IsTitleScreen;}catch{}if(ready)break;yield return null;}yield return new WaitForSecondsRealtime(3);
 Check(NdsIntegration.IsTitleScreen,"official game reached title: "+Game.GetGameState()+" / "+UIMgr.GetTopView()?.GetType().FullName);
 Check(NdsRuntime.Instance!=null,"NDS runtime survived title cleanup");
 Check(Plugin.AcquireExternal(()=>{}),"base runtime survived title cleanup");Plugin.ReleaseExternal();
 Check(!CampusResources.IsManualInstall&&CampusResources.Root.Contains("91020002"),"resources resolve to enabled Workshop mod");
 var up=AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="EC2BUnofficialPatch");
 var catalog=AccessTools.Method(up.GetType("EC2BUnofficialPatch.Workshop.ContentRootCatalog"),"Discover").Invoke(null,null);
 var paths=((IEnumerable)AccessTools.Property(catalog.GetType(),"Roots").GetValue(catalog)).Cast<object>().Select(o=>(string)AccessTools.Property(o.GetType(),"Path").GetValue(o)).ToArray();
 Check(paths.Any(p=>p.EndsWith("91020003")),"UP includes enabled data-only mod");
 Check(!paths.Any(p=>p.EndsWith("91020004")),"UP excludes disabled downloaded mod");
 Check(!(bool)AccessTools.Field(up.GetType("EC2BUnofficialPatch.Core.Updates.UpdateService"),"_started").GetValue(null),"UP Workshop updater stays stopped");
 Check(Cfg.MinigameCfgMap.ContainsKey(9112)&&Cfg.MinigameActionCfgMap.ContainsKey(911201),"default metadata registered from Workshop resources");
 InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.F7));yield return new WaitForSecondsRealtime(.3f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(2);
 Check(SanguoshaView.Current!=null,"F7 opens Sanguosha directory");ScreenCapture.CaptureScreenshot(Path.Combine(dir,"f7.png"));yield return null;SanguoshaView.Invalidate();yield return new WaitForSecondsRealtime(.5f);
 InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.F8));yield return new WaitForSecondsRealtime(.3f);InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(3);
 Check(NdsConsole.Current!=null,"F8 opens NDS console");ScreenCapture.CaptureScreenshot(Path.Combine(dir,"f8.png"));yield return null;
 Check(Cfg.ItemCfgMap.ContainsKey(NdsIntegration.Id)&&Cfg.ItemCfgMap.ContainsKey(SanguoshaIntegration.BoxId),"both item registrations survive cleanup");
 NdsConsole.Invalidate();yield return new WaitForSecondsRealtime(.3f);
 bool done=false,loaded=false;SaveMgrEx.LoadAynsc(PathDefine.SAVE_PATH,"fixture.save",16,ok=>{loaded=ok;done=true;});end=Time.realtimeSinceStartup+40;while(!done&&Time.realtimeSinceStartup<end)yield return null;Check(loaded,"isolated save loads in 1.94");yield return new WaitForSecondsRealtime(3);
 var round=Singleton<RoundMgr>.Ins;var shop=Singleton<ShopMgr>.Ins;
 Check(Cfg.ItemCfgMap[SanguoshaIntegration.BoxId].desc=="你杀不过我你信吗？","new box description");
 round.SetTime(2001,12);Check(!shop.GetShopDatas(4).Any(v=>v.id==SanguoshaIntegration.BoxId),"Sanguosha not sold in 2001");
 int spring=Cfg.SeasonCfgMap.Values.Where(s=>s.name.Contains("春")).SelectMany(s=>s.month).Min();
 round.SetTime(2002,spring);Check(shop.GetShopDatas(4).Any(v=>v.id==SanguoshaIntegration.BoxId),"Sanguosha sold from first spring month in 2002");
 round.SetTime(2005,7);Check(shop.GetShopDatas(4).Any(v=>v.id==NdsIntegration.Id),"NDS sold after summer 2005");
 shop.shopTog=4;UIMgr.OpenView<View.Shop.ShopView>(UILayerType.None,null,Array.Empty<object>());yield return new WaitForSecondsRealtime(2);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"shop.png"));yield return null;
 var relation=Singleton<RoleMgr>.Ins.GetRelationData();int oldCount=relation.searchFriendCnt;
 var person=Cfg.PersonCfgMap.Values.First(p=>p.init!=null&&p.init.Count>0&&p.init[0]>1&&Singleton<RoleMgr>.Ins.GetRole(p.id)!=null&&StudentAge.Sanguosha.Encounters.For(p.id).Any(id=>id!="zhaoyun"&&!StudentAge.Sanguosha.Encounters.Sold(id)));
 var npc=Singleton<RoleMgr>.Ins.GetRole(person.id);int oldRelation=npc.Relation;npc.Relation=0;
 string card=StudentAge.Sanguosha.Encounters.For(person.id).First(id=>id!="zhaoyun"&&!StudentAge.Sanguosha.Encounters.Sold(id));
 relation.searchFriendCnt=4;Check(!SanguoshaIntegration.IsShopGeneral(card),"four follows keep NPC reward out of shop");
 relation.searchFriendCnt=5;Check(SanguoshaIntegration.IsShopGeneral(card),"five follows add unfollowed NPC reward to shop");
 Singleton<BagMgr>.Ins.AddItem(SanguoshaIntegration.BoxId,1L,"isolated QA");
 var offer=shop.GetShopDatas(SanguoshaIntegration.TabId).Single(v=>v.id==SanguoshaIntegration.CardId(card));Check(offer.FinalPrize==20,"unfollowed NPC card costs 20");
 Singleton<BagMgr>.Ins.AddItem(SanguoshaIntegration.CardId(card),1L,"isolated QA");Check(!shop.GetShopDatas(SanguoshaIntegration.TabId).Any(v=>v.id==SanguoshaIntegration.CardId(card)),"owned NPC card disappears from shop");
 relation.searchFriendCnt=oldCount;npc.Relation=oldRelation;
 Check(NdsRuntime.Instance!=null,"runtime survives save transition");Check(errors.Count==0,"no Unity exceptions: "+string.Join(";",errors));
 }
}
