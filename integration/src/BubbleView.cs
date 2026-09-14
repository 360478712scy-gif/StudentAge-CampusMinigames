using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using StudentAge.CampusBubble;
using StudentAge.CampusUno;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace StudentAge.CampusMinigames
{
    public sealed class BubbleEntry : EC2BUnofficialPatch.Features.Mechanics.Minigames.ICustomMinigame
    {
        public void Open(EC2BUnofficialPatch.Features.Mechanics.Minigames.CustomMinigameContext context)
        {
            int level=context.ActionCfgId-context.GameId*100;if(level<1||level>5)throw new InvalidOperationException("Bubble requires social stage 1-5");
            BubbleView.Open(new UpSession(context),level);
        }
    }
    public sealed class BubbleView : MonoBehaviour
    {
        public BubblePresentation Presentation{get;private set;}
        MinigamePause pause;int level;bool autoBegin;IGameSession session;NativeMinigameAudio music;float oldScale;bool closed,locked,ending;AudioSource sound;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        public static void Open(IGameSession request,int level)=>Open(request,level,false);
        public static void Open(IGameSession request,int level,bool autoBegin)
        {
            if(Plugin.IsBusy)throw new InvalidOperationException("Another minigame is active");
            var obj=new GameObject("CampusBubbleHost");obj.hideFlags=HideFlags.HideAndDontSave;DontDestroyOnLoad(obj);var view=obj.AddComponent<BubbleView>();view.session=request;view.level=level;view.autoBegin=autoBegin;
            if(!Plugin.AcquireExternal(()=>view.Close(false,true))){Destroy(obj);throw new InvalidOperationException("Minigame host unavailable");}
            request.OnInvalidated(()=>view.Close(false,false));
            try{view.oldScale=Time.timeScale;Time.timeScale=0;Control.ToggleActionMap(false);view.locked=true;view.sound=obj.AddComponent<AudioSource>();view.sound.playOnAwake=false;view.sound.ignoreListenerPause=true;view.music=NativeMinigameAudio.Open(obj,9103);view.music.Bind(view.sound);view.StartCoroutine(view.LoadAudio());
                NativeSkin.Load(()=>{if(view.closed)return;try{view.Build(level);}catch(Exception e){Debug.LogException(e);view.Close(false,true);}});
            }catch{view.Close(false,true);throw;}
        }
        void Build(int level)
        {
            Presentation=gameObject.AddComponent<BubblePresentation>();Presentation.GuideRequest=(parent,close)=>IllustratedGuide.Show(parent,9103,close);Presentation.PauseCheck=()=>PlayClock.Paused||ending;Presentation.Clock=()=>PlayClock.Now;Presentation.StartRequest=session.Begin;Presentation.ResultRequested=result=>{ending=true;NativeMinigameResult.Show(result==MatchResult.Win?Outcome.Win:result==MatchResult.Draw?Outcome.Draw:Outcome.Lose,null,oldScale,()=>Close(true,true));};
            Presentation.SoundRequested=(key,volume)=>{AudioClip clip;if(!closed&&clips.TryGetValue(key,out clip))sound.PlayOneShot(clip,volume);};
            Presentation.Initialize(new BubbleSkin{Background=NativeSkin.Desk,Paper=NativeSkin.Paper,Green=NativeSkin.Green,Orange=NativeSkin.Orange,Title=NativeSkin.TitleFont,Body=NativeSkin.BodyFont},session.Name,level,Environment.TickCount);
        }
        IEnumerator LoadAudio()
        {
            foreach(string key in new[]{"drop","burst","break","step","hit","pickup","paper"}){string path=Path.Combine(session.Root,"BubbleAudio",key+".ogg");if(!File.Exists(path))continue;using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.OGGVORBIS)){yield return request.SendWebRequest();if(closed)yield break;if(request.result==UnityWebRequest.Result.Success)clips[key]=DownloadHandlerAudioClip.GetContent(request);}}
        }
        void Update()
        {
            if(closed)return;if(!session.IsActive){Close(false,false);return;}if(Presentation==null)return;if(autoBegin&&Presentation.Prepared){autoBegin=false;Presentation.Begin();}if(pause==null&&Presentation.Started)pause=MinigamePause.Attach(this,()=>!closed&&!ending&&session.IsActive&&Presentation.Started&&Presentation.Game.Result==MatchResult.Playing,()=>{ending=true;NativeMinigameResult.Show(Outcome.Lose,null,oldScale,()=>{var saved=session;Close(false,false);saved.Finish(Outcome.Lose);});},()=>{var saved=session;int selected=level;Close(false,false);Open(saved,selected,true);},skipGame:()=>{var saved=session;Close(false,false);saved.Finish(Outcome.Win);});if(PlayClock.Paused||ending)return;var k=Keyboard.current;int dx=0,dy=0;bool drop=false;
            if(k!=null){if(k.leftArrowKey.isPressed||k.aKey.isPressed)dx=-1;else if(k.rightArrowKey.isPressed||k.dKey.isPressed)dx=1;else if(k.upArrowKey.isPressed||k.wKey.isPressed)dy=-1;else if(k.downArrowKey.isPressed||k.sKey.isPressed)dy=1;drop=k.spaceKey.wasPressedThisFrame;}
            Presentation.SetInput(dx,dy,drop);
        }
        void Close(bool settle,bool notify)
        {
            if(closed)return;closed=true;pause?.Dispose();var result=Presentation==null?MatchResult.Playing:Presentation.Game.Result;if(Presentation!=null)Presentation.ShutDown();Unlock();if(music!=null)music.Close();Plugin.ReleaseExternal();
            try{if(notify){if(settle&&result!=MatchResult.Playing)session.Finish(result==MatchResult.Win?Outcome.Win:result==MatchResult.Draw?Outcome.Draw:Outcome.Lose);else session.Cancel();}}finally{Destroy(gameObject);}
        }
        void Unlock(){if(!locked)return;locked=false;Time.timeScale=oldScale;Control.ToggleActionMap(true);}
        void OnDestroy(){if(!closed)Close(false,true);Unlock();foreach(var clip in clips.Values)Destroy(clip);}
    }
}
