using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EC2BUnofficialPatch.Features.Mechanics.Minigames;
using StudentAge.CampusUno;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace StudentAge.CampusMinigames
{
    public sealed class BoxEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<BoxView>(c,9104);}
    public sealed class TwentyFourEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<TwentyFourView>(c,9105);}
    public sealed class FallingBlocksEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<FallingBlocksView>(c,9106);}
    public sealed class LandlordEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<LandlordView>(c,9107);}
    public sealed class SnakeEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<SnakeView>(c,9108);}
    public sealed class MazeEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<MazeView>(c,9109);}
    public abstract class PuzzleFrame:MonoBehaviour
    {
        protected IGameSession Session;protected int Level;protected RectTransform Root,PlayRoot;protected GameObject CanvasObject;GameObject ready,guide;protected Font Font=>NativeSkin.TitleFont;
        protected readonly Color Ink=new Color(.37f,.20f,.11f),Cream=new Color(1,.975f,.91f);protected bool Closed,Ending;public bool Started{get;private set;}public bool Prepared{get;private set;}
        float oldScale;bool locked;MinigamePause pause;Action restart;bool autoStart;int gameId;NativeMinigameAudio music;AudioSource audioSource;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();protected readonly List<UnityEngine.Object> Owned=new List<UnityEngine.Object>();
        protected abstract string Title{get;}protected abstract string[] Rules{get;}
        public static void Open<T>(CustomMinigameContext context,int id,int stageCount=5) where T:PuzzleFrame
        {
            Open<T>(new UpSession(context),id,UpLevel.Resolve(context,id,stageCount),stageCount);
        }
        public static void Open<T>(IGameSession session,int id,int level,int stageCount=5,bool skipReady=false) where T:PuzzleFrame
        {
            if(level<1||level>stageCount)throw new InvalidOperationException("Invalid internal stage");if(Plugin.IsBusy)throw new InvalidOperationException("Another minigame is active");
            var host=new GameObject(typeof(T).Name+"Host");DontDestroyOnLoad(host);var view=host.AddComponent<T>();view.Session=session;view.Level=level;view.gameId=id;view.autoStart=skipReady||view is RetroView;view.restart=()=>{view.Close(false,false);Open<T>(session,id,level,stageCount,true);};
            if(!Plugin.AcquireExternal(()=>view.Close(false,true))){Destroy(host);throw new InvalidOperationException("Minigame host unavailable");}
            view.Session.OnInvalidated(()=>view.Close(false,false));view.oldScale=Time.timeScale;Time.timeScale=0;Control.ToggleActionMap(false);view.locked=true;
            view.audioSource=host.AddComponent<AudioSource>();view.audioSource.playOnAwake=false;view.music=NativeMinigameAudio.Open(host,id);view.music.Bind(view.audioSource);view.StartCoroutine(view.LoadAudio());
            NativeSkin.Load(()=>{if(view.Closed)return;view.StartCoroutine(view.Prepare());});
        }
        IEnumerator Prepare(){var load=LoadAssets();while(true){bool more;object next=null;try{more=load.MoveNext();if(more)next=load.Current;}catch(Exception e){Debug.LogException(e);Close(false,true);yield break;}if(!more)break;yield return next;}if(Closed)yield break;try{Build();Prepared=true;if(autoStart)Begin();}catch(Exception e){Debug.LogException(e);Close(false,true);}}
        protected virtual IEnumerator LoadAssets(){yield break;}
        void Build(){CanvasObject=new GameObject(GetType().Name+"Canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var c=CanvasObject.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=32760;var blocker=R(CanvasObject.transform,"Blocker",0,0,0,0);blocker.anchorMin=Vector2.zero;blocker.anchorMax=Vector2.one;blocker.offsetMin=blocker.offsetMax=Vector2.zero;blocker.gameObject.AddComponent<Image>().color=Color.black;
            Root=R(CanvasObject.transform,"Stage",0,0,1600,900);Root.anchorMin=Root.anchorMax=new Vector2(.5f,.5f);Fit();I(Root,"Classroom",0,0,1600,900,NativeSkin.Desk,Color.white);T(Root,Title,440,25,720,75,47);PlayRoot=R(Root,"Play",0,0,1600,900);PlayRoot.gameObject.AddComponent<RectMask2D>();BuildPlay();
            if(autoStart){PlayRoot.gameObject.SetActive(false);return;}
            ready=R(Root,"Preparation",0,0,1600,900).gameObject;var veil=I(ready.transform,"Veil",345,130,910,650,NativeSkin.Paper,new Color(1,1,1,.97f));veil.type=Image.Type.Sliced;veil.raycastTarget=true;
            T(ready.transform,Title,450,215,700,70,44);B(ready.transform,"开始游戏",645,355,310,108,Begin);B(ready.transform,"玩法说明",645,493,310,108,ShowGuide,true);PlayRoot.gameObject.SetActive(false);
        }
        void Fit(){if(Root==null)return;float s=Math.Min(Screen.width/1600f,Screen.height/900f);Root.localScale=Vector3.one*s;Root.anchoredPosition=new Vector2(-800*s,450*s);}
        public void Begin(){if(Closed||Started||!Prepared||guide!=null)return;if(!Session.Begin())return;Started=true;ready?.SetActive(false);PlayRoot.gameObject.SetActive(true);pause=MinigamePause.Attach(this,()=>Started&&!Closed&&!Ending&&Session.IsActive,()=>Finish(false),restart,this is MarioView?"mario":this is ContraView?"contra":"",()=>Close(true,true,true));try{StartGame();}catch(Exception e){Debug.LogException(e);Close(false,true);}}
        void ShowGuide(){if(Started||guide!=null)return;guide=IllustratedGuide.Show(Root,gameId,()=>guide=null);}
        protected abstract void BuildPlay();protected abstract void StartGame();protected virtual void Tick(){}
        protected virtual void Update(){if(Closed)return;if(!Session.IsActive){Close(false,false);return;}if(PlayClock.Paused)return;Fit();if(Started&&!Ending)Tick();}
        protected void Finish(bool win){if(Ending||Closed)return;Ending=true;float until=PlayClock.Now+(win&&gameId==9107&&music!=null?music.PlayResultMusic("landlord-win"):0);NativeMinigameResult.Show(win?Outcome.Win:Outcome.Lose,CanvasObject,oldScale,()=>{if(PlayClock.Now<until)StartCoroutine(CloseAfterResultMusic(until,win));else Close(true,true,win);});}
        IEnumerator CloseAfterResultMusic(float until,bool win){while(!Closed&&PlayClock.Now<until)yield return null;if(!Closed)Close(true,true,win);}
        void Close(bool settle,bool notify,bool win=false){if(Closed)return;Closed=true;pause?.Dispose();StopAllCoroutines();if(CanvasObject!=null){CanvasObject.SetActive(false);Destroy(CanvasObject);}if(locked){locked=false;Time.timeScale=oldScale;Control.ToggleActionMap(true);}if(music!=null)music.Close();Plugin.ReleaseExternal();try{if(notify){if(settle)Session.Finish(win?Outcome.Win:Outcome.Lose);else Session.Cancel();}}finally{Destroy(gameObject);}}
        protected virtual void OnDestroy(){if(!Closed)Close(false,true);foreach(var obj in Owned)if(obj!=null)Destroy(obj);foreach(var clip in clips.Values)Destroy(clip);}
        protected void Sfx(string key,float volume=.35f){AudioClip clip;if(clips.TryGetValue(key,out clip))audioSource.PlayOneShot(clip,volume);}
        protected void TableSound(string key,float volume=.5f){music?.Effect(key,volume);}
        protected void UseLocalMusic(AudioSource source){music?.UseLocalMusic(source);}
        protected void BindAudio(AudioSource source){if(music!=null)music.Bind(source);}
        IEnumerator LoadAudio(){foreach(string key in new[]{"paper","place-1","undo","break","pickup"}){string folder=key=="break"||key=="pickup"?"BubbleAudio":"Audio";string p=Path.Combine(Session.Root,folder,key+".ogg");if(!File.Exists(p))continue;using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(p).AbsoluteUri,AudioType.OGGVORBIS)){yield return request.SendWebRequest();if(Closed)yield break;if(request.result==UnityWebRequest.Result.Success)clips[key]=DownloadHandlerAudioClip.GetContent(request);}}}
        public static RectTransform R(Transform p,string name,float x,float y,float w,float h){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(p,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        protected Image I(Transform p,string name,float x,float y,float w,float h,Sprite sprite,Color color){var i=R(p,name,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;return i;}
        protected Text T(Transform p,string text,float x,float y,float w,float h,int size,bool body=false){var t=R(p,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=body?NativeSkin.BodyFont:Font;t.text=text;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Ink;t.raycastTarget=false;return t;}
        protected Image Paper(Transform p,float x,float y,float w,float h){var i=I(p,"Paper",x,y,w,h,NativeSkin.Paper,Cream);i.type=Image.Type.Sliced;return i;}
        protected Button B(Transform p,string title,float x,float y,float w,float h,Action click,bool orange=false){var i=I(p,title,x,y,w,h,orange?NativeSkin.Orange:NativeSkin.Green,Color.white);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.navigation=new Navigation{mode=Navigation.Mode.None};var t=T(i.transform,title,4,-2,w-8,h,Math.Min(31,(int)(h*.4f)));t.color=Color.white;var edge=t.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.25f,.15f,.09f,.8f);edge.effectDistance=new Vector2(1.1f,-1.1f);b.onClick.AddListener(()=>{if(Closed||Ending)return;Sfx("paper",.17f);click();});return b;}
        protected void Clear(Transform p){foreach(Transform child in p){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        protected IEnumerator Animate(float duration,Action<float> frame){float t=0;while(t<duration&&!Closed){t+=StudentAge.CampusUno.PlayClock.Delta;frame(Mathf.SmoothStep(0,1,Mathf.Clamp01(t/duration)));yield return null;}if(!Closed)frame(1);}
    }
}
