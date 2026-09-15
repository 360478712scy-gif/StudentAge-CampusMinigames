using System;using System.Linq;using System.Collections;using System.Collections.Generic;using UnityEngine;using StudentAge.Sanguosha;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  AudioSource voice;bool soundsReady;readonly Queue<string> voiceQueue=new Queue<string>();readonly Dictionary<string,int> voiceVariants=new Dictionary<string,int>();
  readonly List<string> playedAudio=new List<string>();readonly HashSet<string> missingAudio=new HashSet<string>();
  public string OutcomeStage{get;private set;}="Playing";public IReadOnlyList<string> PlayedAudio=>playedAudio;public IEnumerable<string> MissingAudio=>missingAudio;
  public bool VoicesIdle=>soundsReady&&voiceQueue.Count==0&&(voice==null||!voice.isPlaying);
  float outcomeAt,outcomeFadeAt,outcomeFadeOutAt=-1,rewardAt;bool outcomeWon;
  void BuildVoices(){voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.loop=false;voice.volume=.9f;scope.Bind(voice);}
  void TraceAudio(string key){playedAudio.Add(key);if(playedAudio.Count>256)playedAudio.RemoveAt(0);}
  string ResolveAudio(string key,int actor){if(key=="recover")key="recover-effect";int slot=Catalog.Slot(key);if(slot>=0)key=slot==0?"weapon":slot==1?"armor":"horse";
   string sex=Engine!=null&&actor>=0&&actor<Engine.Seats.Length&&Engine.Seats[actor].General.Female?"female-":"male-";
   if(clips.ContainsKey(sex+key))return sex+key;
   if(clips.ContainsKey(key+"-2")){int i;voiceVariants.TryGetValue(key,out i);voiceVariants[key]=i+1;if(i%2==1)return key+"-2";}return key;
  }
  void Sound(string key,int actor=0){if(string.IsNullOrEmpty(key)||Closed||key.StartsWith("judgment-",StringComparison.Ordinal))return;key=ResolveAudio(key,actor);
   bool speech=key.StartsWith("male-")||key.StartsWith("female-")||key.StartsWith("death-")||key=="first-blood"||key=="win"||key=="lose"||key=="standoff"||Catalog.Generals.Any(g=>g.Skills.Split(' ').Contains(key.EndsWith("-2")?key.Substring(0,key.Length-2):key));
   if(speech){voiceQueue.Enqueue(key);return;}if(clips.TryGetValue(key,out var clip)){effect.PlayOneShot(clip,.72f);TraceAudio(key);}else if(soundsReady&&missingAudio.Add(key))Debug.LogWarning("[Sanguosha] Missing audio: "+key);
  }
  void TickVoices(){if(Paused||voice==null||voice.isPlaying||voiceQueue.Count==0||!soundsReady)return;string key=voiceQueue.Dequeue();if(clips.TryGetValue(key,out var clip)){voice.clip=clip;voice.Play();TraceAudio(key);}else if(missingAudio.Add(key))Debug.LogWarning("[Sanguosha] Missing voice: "+key);}
  IEnumerator ResultSound(){OutcomeStage="Resolving";
   // Settlement is immediate and idempotent; presentation waits for all lethal-turn events.
   while(events.Count>0||JudgmentHolding||MovementHolding||!VoicesIdle)yield return null;
   outcomeWon=Engine.Winner==0;outcomeFadeAt=Time.unscaledTime;
   if(outcomeWon&&!Engine.Surrendered&&Engine.DefeatedActor==1&&Engine.DefeatSource==0){OutcomeStage="FirstBlood";outcomeAt=Time.unscaledTime;Sound("first-blood");while(!VoicesIdle)yield return null;}
   OutcomeStage="Victory";outcomeAt=Time.unscaledTime;Sound(outcomeWon?"win":Engine.Winner==1?"lose":"standoff");
   while(Time.unscaledTime-outcomeAt<2.5f||!VoicesIdle)yield return null;
   if(Session.NewReward){outcomeFadeOutAt=Time.unscaledTime;yield return new WaitForSecondsRealtime(.22f);rewardAt=Time.unscaledTime;OutcomeStage="Reward";yield return new WaitForSecondsRealtime(1.2f);}resultAt=Time.unscaledTime;OutcomeStage="Ready";
  }
  void DrawOutcome(){if(OutcomeStage=="Ready"){Rect(new Rect(0,40,1600,860),new Color(0,0,0,.66f));Result();return;}if(OutcomeStage=="Resolving"||OutcomeStage=="Playing")return;
   float age=Time.unscaledTime-outcomeAt;float fade=Mathf.SmoothStep(0,1,(Time.unscaledTime-outcomeFadeAt)/.22f);float emblemAlpha=outcomeFadeOutAt<0?1:Mathf.Clamp01(1-(Time.unscaledTime-outcomeFadeOutAt)/.22f);Rect(new Rect(0,40,1600,860),new Color(0,0,0,.66f*fade));
   if(OutcomeStage=="Reward"){DrawGeneralReward();return;}
   // The reference video has a clipped caption at stage y=42. It lies outside the effect; clip that top gutter only.
   if(OutcomeStage=="FirstBlood"){GUI.BeginGroup(new Rect(0,100,1600,800));DrawOriginalEffect("first-blood",age,new Rect(0,-100,1600,900));GUI.EndGroup();return;}
   DrawResultEmblem(emblemAlpha);
  }
  void DrawResultEmblem(float emblemAlpha){float age=Time.unscaledTime-outcomeAt;string prefix=outcomeWon?"victory":"defeat";float textAge=Mathf.Max(0,age-.35f);
   DrawOriginalEffect(prefix+"-base",Mathf.Min(age,1.98f),new Rect(350,-20,900,900),false,emblemAlpha);
   if(age>=.35f)DrawOriginalEffect(prefix+"-text",Mathf.Min(textAge,outcomeWon?.70f:.53f),new Rect(215,-135,1170,1170),false,emblemAlpha);
  }
  void DisposeVoices(){voiceQueue.Clear();if(voice!=null){voice.Stop();Destroy(voice);}voice=null;}
 }
}
