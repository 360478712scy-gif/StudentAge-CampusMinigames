using System;using System.IO;using System.Linq;using System.Collections.Generic;using Config;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Sdk;using UnityEngine;using View.Evt;
namespace StudentAge.CampusMinigames {
 // Stage-one stories: source exports remain verbatim. Runtime IDs are namespaced; events cannot enter random pools.
 // Event 1222100-1222109 = first-stage victory, 1222110-1222119 = first-stage defeat, both keyed by EvtCfg.npc.
 public static class SanguoshaVictoryStory {
  public const int StoryEventType=920092,TalkOffset=700000000,OptionOffset=70000000,EventOffset=700000;
  const int FirstEvent=1222100,LastWinEvent=1222109,LastEvent=1222119;
  static Dictionary<int,TalkCfg> talks;static Dictionary<int,OptionCfg> options;static Dictionary<int,EvtCfg> events;static Dictionary<int,int> wins,losses;
  // Native enabled-Mod EvtCfg rows can bind independent author stories without replacing shared files.
  public const int AuthorWinEventType=920093,AuthorLossEventType=920094;
  public static bool Active{get;private set;}
  public static int EventFor(int npc,bool won=true){Load();int type=won?AuthorWinEventType:AuthorLossEventType;var authored=Cfg.EvtCfgMap?.Values.Where(e=>e.npc==npc&&e.type==type).OrderBy(e=>e.id).ToArray();if(authored!=null&&authored.Length>0){if(authored.Length>1)Debug.LogWarning("[Sanguosha] 人物第一关剧情有多个绑定，使用最小事件编号："+npc+" / "+authored[0].id);return authored[0].id;}return (won?wins:losses).TryGetValue(npc,out int id)?id:0;}
  // Original export ID (e.g. 1222117) that the save record tracks; 0 when no story exists.
  public static int SourceEventFor(int npc,bool won){int id=EventFor(npc,won);return id==0?0:events.ContainsKey(id)?id-EventOffset:id;}
  public static bool IsWinEvent(int sourceId)=>sourceId>=FirstEvent&&sourceId<=LastWinEvent;
  static bool IsTalk(int id)=>id>=1222100001&&id<=1222119999;static bool IsOption(int id)=>id>=122210001&&id<=122211999;static bool IsEvent(int id)=>id>=FirstEvent&&id<=LastEvent;
  static int TalkId(int id)=>IsTalk(id)?id+TalkOffset:id;
  static int OptionId(int id)=>IsOption(id)?id+OptionOffset:id;
  static int EventId(int id)=>IsEvent(id)?id+EventOffset:id;
  static Dictionary<int,JObject> Read(string name)=>JsonConvert.DeserializeObject<Dictionary<int,JObject>>(File.ReadAllText(Path.Combine(SanguoshaAssets.Root,"Stories/FirstWin/"+name+"Cfg.json")));
  static void Remap(JObject obj,string key,Func<int,int> map){if(obj[key] is JArray values)for(int i=0;i<values.Count;i++)values[i]=map((int)values[i]);}
  // Native condition [3,1,evt]/[3,2,option]/[3,3,talk] refers to our own rows by export ID; ShowEvent records history under the runtime ID, so follow the same offset. Other conditions (e.g. 320907, gender) keep their values.
  static void RemapConditions(JObject obj,string key){if(!(obj[key] is JArray checks))return;foreach(var entry in checks){if(!(entry is JArray parts)||parts.Count<3||(int)parts[0]!=3)continue;int sub=(int)parts[1],value=(int)parts[2];int mapped=sub==1?EventId(value):sub==2?OptionId(value):sub==3?TalkId(value):value;if(mapped!=value)parts[2]=mapped;}}
  static void Load(){if(talks!=null)return;var ts=Read("Talk");var os=Read("Option");var es=Read("Evt");var newTalks=new Dictionary<int,TalkCfg>();var newOptions=new Dictionary<int,OptionCfg>();var newEvents=new Dictionary<int,EvtCfg>();var newWins=new Dictionary<int,int>();var newLosses=new Dictionary<int,int>();
   foreach(var row in ts){var j=row.Value;j["id"]=TalkId(row.Key);Remap(j,"nextTalk",TalkId);Remap(j,"nextTalk2",TalkId);Remap(j,"option",OptionId);RemapConditions(j,"check");newTalks.Add((int)j["id"],j.ToObject<TalkCfg>());}
   foreach(var row in os){var j=row.Value;j["id"]=OptionId(row.Key);Remap(j,"talkId",TalkId);Remap(j,"talkId2",TalkId);if(j["nextEvtId"]!=null)j["nextEvtId"]=EventId((int)j["nextEvtId"]);RemapConditions(j,"check");RemapConditions(j,"precondition");newOptions.Add((int)j["id"],j.ToObject<OptionCfg>());}
   foreach(var row in es){var j=row.Value;Remap(j,"talkId",TalkId);var roots=j["talkId"].Values<int>().ToArray();if(roots.Length==0||roots.Any(id=>!newTalks.ContainsKey(id))){Debug.LogWarning("[Sanguosha] 第一关剧情正文未提供，暂不接入："+(string)j["title"]);continue;}j["id"]=EventId(row.Key);j["type"]=StoryEventType;Remap(j,"options",OptionId);RemapConditions(j,"condition");var e=j.ToObject<EvtCfg>();newEvents.Add(e.id,e);var route=IsWinEvent(row.Key)?newWins:newLosses;if(route.ContainsKey(e.npc))throw new InvalidDataException("第一关剧情重复绑定人物 "+e.npc+"："+row.Key);route.Add(e.npc,e.id);}
   foreach(var t in newTalks.Values){foreach(int id in t.nextTalk.Concat(t.nextTalk2))if(id!=0&&!newTalks.ContainsKey(id))throw new InvalidDataException("第一关剧情缺少对白 "+id);foreach(int id in t.option)if(!newOptions.ContainsKey(id))throw new InvalidDataException("第一关剧情缺少选项 "+id);}
   talks=newTalks;options=newOptions;events=newEvents;wins=newWins;losses=newLosses;
  }
  static void CheckSlots<T>(Dictionary<int,T> target,Dictionary<int,T> source){foreach(var p in source)if(target.TryGetValue(p.Key,out var old)&&!ReferenceEquals(old,p.Value)&&!JToken.DeepEquals(JToken.FromObject(old),JToken.FromObject(p.Value)))throw new InvalidDataException("三国杀剧情编号被其他内容占用："+p.Key);}
  public static void Register(){Load();CheckSlots(Cfg.TalkCfgMap,talks);CheckSlots(Cfg.OptionCfgMap,options);CheckSlots(Cfg.EvtCfgMap,events);foreach(var p in talks)Cfg.TalkCfgMap[p.Key]=p.Value;foreach(var p in options)Cfg.OptionCfgMap[p.Key]=p.Value;foreach(var p in events)Cfg.EvtCfgMap[p.Key]=p.Value;}
  public static bool Play(int npc,bool won=true,Action completed=null){if(Active||UIMgr.IsViewOpened<NewTalkView>())return false;int id=EventFor(npc,won);if(id==0)return false;Register();Active=true;try{Singleton<CommonEvtMgr>.Ins.ShowEvent(id,1,()=>{Active=false;completed?.Invoke();},0,false);return true;}catch{Active=false;throw;}}
  public static void Reset(){Active=false;}
 }
 public sealed partial class SanguoshaView {
  // Only the explicit result dismissal starts story playback; invalidation/closing never does.
  public void CompleteResult(){if(Closed||OutcomeStage!="Ready")return;var session=Session;Close();session?.PlayStory();}
 }
}
