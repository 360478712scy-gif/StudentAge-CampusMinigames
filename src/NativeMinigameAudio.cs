using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Config;
using Sdk;
using UnityEngine;

namespace StudentAge.CampusUno
{
    // Local reference tracks use the installed game music mixer and restore its BGM on close.
    public sealed class NativeMinigameAudio : MonoBehaviour
    {
        readonly List<AudioSource> effects=new List<AudioSource>();
        string selectedUrl;bool active,changed,paused,musicSuspended,localMusicStarted;AudioSource localMusic,tableEffects;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();AudioClip localClip;public AudioSource LocalMusic=>localMusic;public int EffectCount=>clips.Count;public string LastEffect{get;private set;}
        float musicGain=1,fadeFrom,fadeTo,fadeStarted,fadeDuration,localMusicVolume;bool gainControlled,musicFading;
        public static NativeMinigameAudio Open(GameObject host,int gameId)=>Open(host,gameId,false);
        public static NativeMinigameAudio Open(GameObject host,int gameId,bool suspendMusic)
        {
            var scope=host.AddComponent<NativeMinigameAudio>();scope.active=true;scope.musicSuspended=suspendMusic;scope.musicGain=suspendMusic?0:1;
            MinigameCfg cfg;int id=Cfg.MinigameCfgMap!=null&&Cfg.MinigameCfgMap.TryGetValue(gameId,out cfg)?cfg.bgm:8;
            if(suspendMusic&&AudioMgr.Ins!=null){scope.paused=true;scope.changed=true;AudioMgrEx.PauseAllMusic();}
            else if(id>0&&AudioMgr.Ins!=null&&Cfg.AudioCfgMap!=null&&Cfg.AudioCfgMap.ContainsKey(id))
            {scope.selectedUrl=Cfg.AudioCfgMap[id].url;scope.changed=true;AudioMgrEx.PlayEvtBgm(id,true);}
            else if(id==-1&&AudioMgr.Ins!=null){scope.paused=true;scope.changed=true;AudioMgrEx.PauseAllMusic();}
            if(gameId==9101||gameId==9107||gameId==9112||gameId==919901)scope.StartCoroutine(scope.LoadLocal(gameId));
            return scope;
        }
        public void UseLocalMusic(AudioSource source){if(AudioMgr.Ins!=null){AudioMgrEx.PauseAllMusic();source.outputAudioMixerGroup=AudioMgr.Ins.GetChannel(AudioMgrEx.CHANNEL_BGM).source.outputAudioMixerGroup;}paused=changed=true;localMusic=source;localMusicVolume=source.volume;if(gainControlled)source.volume=localMusicVolume*musicGain;localMusicStarted=source.isPlaying;if(musicSuspended&&localMusicStarted)source.Pause();}
        IEnumerator LoadLocal(int id){string root=Path.Combine(BepInEx.Paths.PluginPath,"StudentAgeCampusMinigames");string path=Path.Combine(root,"Music",id+".mp3");
            if(File.Exists(path))using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.MPEG)){yield return request.SendWebRequest();if(!active)yield break;if(request.result==UnityWebRequest.Result.Success){localClip=DownloadHandlerAudioClip.GetContent(request);var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.clip=localClip;source.volume=MinigameTuning.Get("Audio","TableBgmVolume");UseLocalMusic(source);if(!musicSuspended){source.Play();localMusicStarted=true;}}}
            if(id==919901)yield break;
            tableEffects=gameObject.AddComponent<AudioSource>();tableEffects.playOnAwake=false;Bind(tableEffects);string folder=Path.Combine(root,"CardAudio");if(!Directory.Exists(folder))yield break;
            foreach(string file in Directory.GetFiles(folder)){string ext=Path.GetExtension(file).ToLowerInvariant();if(ext!=".ogg"&&ext!=".mp3")continue;string key=Path.GetFileNameWithoutExtension(file);if(id!=9107&&(key.StartsWith("voice-")||ext==".mp3"))continue;using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri,ext==".ogg"?AudioType.OGGVORBIS:AudioType.MPEG)){yield return request.SendWebRequest();if(!active)yield break;if(request.result==UnityWebRequest.Result.Success)clips[key]=DownloadHandlerAudioClip.GetContent(request);}}
        }
        public void SetMusicPaused(bool value)=>FadeMusicPaused(value,0);
        // Use unscaled time: award screens pause world time, not the music envelope.
        public void FadeMusicPaused(bool value,float seconds)
        {
            UpdateMusicFade();float target=value?0:1;
            if((musicFading&&fadeTo==target)||(!musicFading&&musicGain==target&&musicSuspended==value))return;
            gainControlled=true;fadeFrom=musicGain;fadeTo=target;fadeStarted=Time.unscaledTime;fadeDuration=Math.Max(0,seconds);musicFading=true;
            if(!value){musicSuspended=false;if(localMusic!=null){localMusic.volume=localMusicVolume*musicGain;if(localMusicStarted)localMusic.UnPause();else{localMusic.Play();localMusicStarted=true;}}}
            UpdateMusicFade();
        }
        void UpdateMusicFade()
        {
            if(!musicFading)return;float t=fadeDuration<=0?1:Mathf.Clamp01((Time.unscaledTime-fadeStarted)/fadeDuration);float smooth=t*t*(3-2*t);musicGain=Mathf.Lerp(fadeFrom,fadeTo,smooth);
            if(localMusic!=null)localMusic.volume=localMusicVolume*musicGain;
            if(t<1)return;musicFading=false;musicSuspended=fadeTo==0;
            if(musicSuspended){if(localMusic!=null)localMusic.Pause();if(AudioMgr.Ins!=null)AudioMgrEx.PauseAllMusic();}
        }
        public void Effect(string key,float volume=.5f){AudioClip clip;if(active&&!PlayClock.Paused&&tableEffects!=null&&clips.TryGetValue(key,out clip)){LastEffect=key;tableEffects.PlayOneShot(clip,volume);}}
        public void Bind(AudioSource source){if(source==null)return;effects.Add(source);SyncEffects();}
        void SyncEffects()
        {
            if(AudioMgr.Ins==null)return;var native=AudioMgr.Ins.GetChannel(AudioMgrEx.CHANNEL_SOUND_UI).source;
            foreach(var source in effects)if(source!=null){source.outputAudioMixerGroup=native.outputAudioMixerGroup;source.mute=native.mute;source.ignoreListenerPause=native.ignoreListenerPause;}
        }
        void Update()
        {
            if(!active)return;UpdateMusicFade();SyncEffects();if(localMusic!=null&&AudioMgr.Ins!=null){var native=AudioMgr.Ins.GetChannel(AudioMgrEx.CHANNEL_BGM).source;localMusic.mute=native.mute;localMusic.outputAudioMixerGroup=native.outputAudioMixerGroup;}
            // These modal games pause world time. The original audio fade uses scaled delta,
            // so advance just the native BGM channel while our modal owns paused playback.
            if(changed&&!paused&&Time.timeScale==0&&AudioMgr.Ins!=null&&AudioMgrEx.musicUrl==selectedUrl)
                AudioMgr.Ins.GetChannel(AudioMgrEx.CHANNEL_BGM).Update(Time.unscaledDeltaTime);
        }
        public void Close()
        {
            if(!active)return;active=false;StopAllCoroutines();if(localMusic!=null){localMusic.Stop();Destroy(localMusic);}if(tableEffects!=null)Destroy(tableEffects);if(localClip!=null)Destroy(localClip);foreach(var clip in clips.Values)Destroy(clip);clips.Clear();
            if(changed&&AudioMgr.Ins!=null&&(paused||AudioMgrEx.musicUrl==selectedUrl))AudioMgrEx.PlayBgm();
            Destroy(this);
        }
        void OnDestroy(){if(active)Close();}
    }
}
