using System;using System.Runtime.InteropServices;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;
using BepInEx;using Config;using Sdk;using HarmonyLib;using UnityEngine;using UnityEngine.InputSystem;using StudentAge.CampusMinigames;using StudentAge.Sanguosha;
[BepInPlugin("studentage.sanguosha.authorstoryqa","Sanguosha story QA","1.0.0")][BepInDependency("studio.studentage.nds")]
public sealed class SanguoshaAuthorStoryQAPlugin:BaseUnityPlugin {void Awake(){var go=new GameObject("Sanguosha_Drag_QA"){hideFlags=HideFlags.HideAndDontSave};DontDestroyOnLoad(go);go.AddComponent<SanguoshaAuthorStoryQA>();}}
public sealed class SanguoshaAuthorStoryQA:MonoBehaviour {
 string dir;readonly List<string> checks=new List<string>();bool failed;float start;
 static object Field(object o,string key)=>AccessTools.Field(o.GetType(),key).GetValue(o);
 static void Set(object o,string key,object v)=>AccessTools.Field(o.GetType(),key).SetValue(o,v);
 void Awake(){dir=Path.Combine(Paths.GameRootPath,"SanguoshaAuthorStoryQA");Directory.CreateDirectory(dir);PathDefine.SAVE_PATH=Path.Combine(dir,"Saves");PathDefine.TEST_SAVE_PATH=PathDefine.SAVE_PATH;PathDefine.IMG_PATH=Path.Combine(dir,"Images");PathDefine.MUSIC_PATH=Path.Combine(dir,"Music");Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;

  Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Exception){failed=true;File.WriteAllText(Path.Combine(dir,"failed.txt"),m+"\n"+s);}};start=Time.realtimeSinceStartup;
 }
 void Update(){if(failed||Time.realtimeSinceStartup-start>600){if(!failed)File.WriteAllText(Path.Combine(dir,"failed.txt"),"QA timeout");File.WriteAllLines(Path.Combine(dir,"checks.txt"),checks);Application.Quit();}}
 void Check(bool b,string message){if(!b)throw new Exception(message);checks.Add(message);File.WriteAllLines(Path.Combine(dir,"checks.txt"),checks);Debug.Log("JUDGMENT QA PASS "+message);}
 IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return null;}
 IEnumerator Start(){float end=Time.realtimeSinceStartup+120;while(Time.realtimeSinceStartup<end){bool ready=false;try{ready=NdsIntegration.IsTitleScreen;}catch{}if(ready)break;yield return null;}Check(NdsIntegration.IsTitleScreen,"official title ready");
  bool done=false,loaded=false;SaveMgrEx.LoadAynsc(PathDefine.SAVE_PATH,"fixture.save",16,ok=>{loaded=ok;done=true;});end=Time.realtimeSinceStartup+40;while(!done&&Time.realtimeSinceStartup<end)yield return null;Check(loaded,"isolated fixture loaded");yield return new WaitForSecondsRealtime(3);
  for(int n=0;n<3;n++){var warning=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="好的"));if(warning==null)break;warning.onClick.Invoke();yield return new WaitForSecondsRealtime(1);}
  var mgr=Singleton<CommonEvtMgr>.Ins;SanguoshaVictoryStory.Register();Check(Encounters.Npcs.Count(n=>SanguoshaVictoryStory.EventFor(n,true)>0)==10,"ten first-stage victory stories registered");Check(Encounters.Npcs.Count(n=>SanguoshaVictoryStory.EventFor(n,false)>0)==10,"ten first-stage defeat stories registered");Check(SanguoshaVictoryStory.EventFor(202,true)==1922107&&SanguoshaVictoryStory.EventFor(202,false)==1922117,"Lin Jiayu stories now registered");
  const int winId=1588001,lossId=1588002,talkId=1588001001;
  var win=Newtonsoft.Json.JsonConvert.DeserializeObject<EvtCfg>(Newtonsoft.Json.JsonConvert.SerializeObject(Cfg.EvtCfgMap[SanguoshaVictoryStory.EventFor(101,true)]));win.id=winId;win.type=SanguoshaVictoryStory.AuthorWinEventType;win.talkId=new List<int>{talkId};
  var lossCfg=Newtonsoft.Json.JsonConvert.DeserializeObject<EvtCfg>(Newtonsoft.Json.JsonConvert.SerializeObject(win));lossCfg.id=lossId;lossCfg.type=SanguoshaVictoryStory.AuthorLossEventType;
  var talk=Newtonsoft.Json.JsonConvert.DeserializeObject<TalkCfg>(Newtonsoft.Json.JsonConvert.SerializeObject(Cfg.TalkCfgMap[1922100001]));talk.id=talkId;talk.content="作者剧情接入检查";talk.nextTalk=new List<int>();talk.nextTalk2=new List<int>();talk.option=new List<int>();talk.check=new List<List<double>>();
  Cfg.EvtCfgMap[winId]=win;Cfg.EvtCfgMap[lossId]=lossCfg;Cfg.TalkCfgMap[talkId]=talk;
  Check(SanguoshaVictoryStory.EventFor(101,true)==winId&&SanguoshaVictoryStory.EventFor(101,false)==lossId,"native author configuration overrides the matching NPC and outcome");
  Check(SanguoshaVictoryStory.SourceEventFor(101,true)==winId&&SanguoshaVictoryStory.SourceEventFor(101,false)==lossId,"author event IDs remain unshifted for persistent story records");
  Check(SanguoshaVictoryStory.SourceEventFor(202,true)==1222107,"built-in source IDs retain existing save compatibility");
  win.npc=900101;Check(SanguoshaVictoryStory.EventFor(900101,true)==winId,"custom NPC outside built-in roster can bind a story");Check(SanguoshaVictoryStory.EventFor(900101,false)==0,"unbound custom outcome stays empty");win.npc=101;
  var duplicate=Newtonsoft.Json.JsonConvert.DeserializeObject<EvtCfg>(Newtonsoft.Json.JsonConvert.SerializeObject(win));duplicate.id=1588003;Cfg.EvtCfgMap[duplicate.id]=duplicate;Check(SanguoshaVictoryStory.EventFor(101,true)==winId,"duplicate bindings select stable lowest event ID");Cfg.EvtCfgMap.Remove(duplicate.id);
  AccessTools.Method(typeof(CommonEvtMgr),"InitEvtCfg").Invoke(mgr,null);
  Check(SanguoshaVictoryStory.Play(101,true),"author event starts through native ShowEvent");end=Time.realtimeSinceStartup+30;while(UIMgr.GetOpeningView<View.Evt.NewTalkView>()==null&&Time.realtimeSinceStartup<end)yield return null;yield return new WaitForSecondsRealtime(2.5f);var native=UIMgr.GetOpeningView<View.Evt.NewTalkView>();Check(native!=null,"author event opens native dialogue view");Check(((TalkCfg)Field(native,"cfg")).id==talkId,"native dialogue keeps author talk ID");native.OnClickNext();yield return new WaitForSecondsRealtime(.5f);native=UIMgr.GetOpeningView<View.Evt.NewTalkView>();if(native!=null)native.OnClickNext();yield return new WaitForSecondsRealtime(.5f);Check(!SanguoshaVictoryStory.Active,"native author completion releases guard");
  Cfg.EvtCfgMap.Remove(winId);Cfg.EvtCfgMap.Remove(lossId);Cfg.TalkCfgMap.Remove(talkId);Check(SanguoshaVictoryStory.EventFor(101,true)!=winId,"removing author configuration restores built-in fallback");Check(SanguoshaVictoryStory.EventFor(900101,true)==0,"removed custom NPC does not leave a cached binding");
  Check(!failed,"no Unity exceptions");File.WriteAllText(Path.Combine(dir,"success.txt"),"SANGUOSHA_AUTHOR_STORY_RUNTIME_OK");yield return new WaitForSecondsRealtime(.3f);Application.Quit();
 }
}
