using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StudentAge.CampusUno;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UObj=UnityEngine.Object;

namespace StudentAge.CampusMinigames
{
    public sealed class GomokuView : MonoBehaviour
    {
        static GomokuView current;
        public GomokuGame Engine {get;private set;}=new GomokuGame();
        public int Level{get;private set;}public bool Prepared{get;private set;}
        IGameSession session;GameObject canvasObject,ready,guide;RectTransform root,paper;Text turnText;Button undoButton,startButton;
        readonly Image[] stones=new Image[225];readonly List<Texture2D> textures=new List<Texture2D>();readonly List<Sprite> sprites=new List<Sprite>();readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        Sprite black,white;AudioSource audioSource;CancellationTokenSource cancel;Task<int> ai;float aiStarted,oldScale;bool started,closed,locked;int animationIndex=-1,lastSound=-1;float animationTime;Image latest;NativeMinigameAudio music;MinigamePause pause;bool autoBegin;
        public static void Open(IGameSession session,int level)=>Open(session,level,false);
        public static void Open(IGameSession session,int level,bool autoBegin)
        {
            if(current!=null||Plugin.IsBusy)throw new InvalidOperationException("Another minigame is active");
            var obj=new GameObject("CampusGomokuHost");obj.hideFlags=HideFlags.HideAndDontSave;UObj.DontDestroyOnLoad(obj);var view=obj.AddComponent<GomokuView>();current=view;view.session=session;view.Level=level;view.autoBegin=autoBegin;
            if(!Plugin.AcquireExternal(()=>view.Close(false,true))){UObj.Destroy(obj);throw new InvalidOperationException("Minigame host unavailable");}
            session.OnInvalidated(()=>view.Close(false,false));
            try{view.Initialize();}catch{view.Close(false,false);throw;}
        }
        void Initialize()
        {
            oldScale=Time.timeScale;Time.timeScale=0;Control.ToggleActionMap(false);locked=true;
            canvasObject=new GameObject("CampusGomokuModal",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var mask=Rect(canvasObject.transform,"InputBlocker",0,0,0,0);mask.anchorMin=Vector2.zero;mask.anchorMax=Vector2.one;mask.offsetMin=mask.offsetMax=Vector2.zero;mask.gameObject.AddComponent<Image>().color=Color.black;
            root=Rect(canvasObject.transform,"GomokuStage",0,0,1600,900);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);Fit();
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.ignoreListenerPause=true;music=NativeMinigameAudio.Open(gameObject,9102);music.Bind(audioSource);StartCoroutine(LoadAudio());
            NativeSkin.Load(()=>{if(closed)return;try{Build();Prepared=true;if(autoBegin)Begin();}catch(Exception e){Debug.LogException(e);Close(false,true);}});
        }
        void Fit(){float scale=Math.Min(Screen.width/1600f,Screen.height/900f);root.localScale=Vector3.one*scale;root.anchoredPosition=new Vector2(-800*scale,450*scale);}
        RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=(RectTransform)obj.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        Image Image(Transform parent,string name,float x,float y,float w,float h,Sprite sprite,Color color){var i=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;return i;}
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size,bool body=false){var t=Rect(parent,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=body?NativeSkin.BodyFont:NativeSkin.TitleFont;if(t.font==null)t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.text=text;t.fontSize=size;t.color=NativeSkin.TitleColor;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        Button Button(Transform parent,string title,float x,float y,float w,float h,Action click,bool orange=false){var i=Image(parent,title,x,y,w,h,orange?NativeSkin.Orange:NativeSkin.Green,Color.white);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;var text=Label(i.transform,title,5,-3,w-10,h,30);text.color=Color.white;var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.29f,.18f,.08f,.8f);outline.effectDistance=new Vector2(1,-1);b.onClick.AddListener(()=>{if(closed)return;PlaySound("paper",.18f);try{click();}catch(Exception e){Debug.LogException(e);Close(false,true);}});return b;}
        Image Paper(Transform parent,float x,float y,float w,float h){var i=Image(parent,"Paper",x,y,w,h,NativeSkin.Paper,Color.white);i.type=UnityEngine.UI.Image.Type.Sliced;return i;}
        void Build()
        {
            Image(root,"Desk",0,0,1600,900,NativeSkin.Desk,Color.white);Label(root,"五子棋",488,27,650,70,42);
            paper=Paper(root,447,113,724,724).rectTransform;var grid=MakeGrid();Image(paper,"BoardGrid",0,0,724,724,grid,Color.white);black=MakeStone(false);white=MakeStone(true);
            const float spacing=601.72f/14;for(int i=0;i<225;i++){int index=i;float x=61.14f+i%15*spacing,y=61.14f+i/15*spacing;
                var shadow=Image(paper,"StoneShadow",x-18+3,y-18+5,36,36,black,new Color(.22f,.14f,.08f,.25f));shadow.gameObject.SetActive(false);
                var stone=Image(paper,"Stone",x-18,y-18,36,36,black,Color.white);stone.gameObject.SetActive(false);stones[i]=stone;
                var hit=Image(paper,"Point-"+i,x-21,y-21,42,42,null,Color.clear);hit.raycastTarget=true;var button=hit.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>HumanMove(index));var point=hit.gameObject.AddComponent<GomokuPoint>();point.Setup(this,index,stone,shadow);
            }
            latest=Image(paper,"LastMove",0,0,5,5,white,new Color(.83f,.24f,.16f));latest.gameObject.SetActive(false);
            turnText=Label(root,"",1115,275,340,70,31);Label(root,"●  你",150,320,240,75,31);Label(root,"○  "+session.Name,135,590,270,75,29);
            for(int seat=0;seat<2;seat++){var bowl=Image(root,"StoneBowl",205,200+seat*260,135,100,white,new Color(.65f,.44f,.25f));for(int j=0;j<5;j++)Image(bowl.transform,"SpareStone",22+j%3*29,10+j/3*27,44,39,seat==0?black:white,Color.white);}
            undoButton=Button(root,"悔棋",183,759,184,63,Undo);undoButton.gameObject.SetActive(false);
            ready=Rect(root,"Preparation",447,113,724,724).gameObject;var overlay=Image(ready.transform,"PaperVeil",0,0,724,724,null,new Color(1,.98f,.93f,.45f));overlay.raycastTarget=true;
            startButton=Button(ready.transform,"开始游戏",222,250,280,102,Begin);Button(ready.transform,"玩法说明",222,372,280,102,Guide,true);
            Refresh();
        }
        Sprite MakeGrid(){var t=new Texture2D(900,900,TextureFormat.RGBA32,false);var pixels=new Color32[810000];for(int n=0;n<15;n++){int q=Mathf.RoundToInt(76+n*748f/14);for(int v=76;v<=824;v++){pixels[v*900+q]=new Color32(145,106,65,255);pixels[q*900+v]=new Color32(145,106,65,255);}}foreach(int i in new[]{48,56,112,168,176}){int x=Mathf.RoundToInt(76+i%15*748f/14),y=Mathf.RoundToInt(76+i/15*748f/14);for(int dy=-4;dy<=4;dy++)for(int dx=-4;dx<=4;dx++)if(dx*dx+dy*dy<=16)pixels[(y+dy)*900+x+dx]=new Color32(145,106,65,255);}t.SetPixels32(pixels);t.Apply();textures.Add(t);var s=Sprite.Create(t,new UnityEngine.Rect(0,0,900,900),new Vector2(.5f,.5f));sprites.Add(s);return s;}
        Sprite MakeStone(bool light){var t=new Texture2D(96,96,TextureFormat.RGBA32,false);for(int y=0;y<96;y++)for(int x=0;x<96;x++){float radius=Vector2.Distance(new Vector2(x,y),new Vector2(47.5f,47.5f))/47;float glow=Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(32,66))/75);Color c=light?Color.Lerp(new Color(.65f,.63f,.58f),new Color(1,1,.97f),glow):Color.Lerp(new Color(.045f,.075f,.07f),new Color(.40f,.42f,.40f),glow*glow);c.a=Mathf.Clamp01((1-radius)*35);t.SetPixel(x,y,c);}t.Apply();textures.Add(t);var s=Sprite.Create(t,new UnityEngine.Rect(0,0,96,96),new Vector2(.5f,.5f));sprites.Add(s);return s;}
        public void Begin(){if(started||!Prepared||guide!=null)return;if(!session.Begin())return;started=true;pause=MinigamePause.Attach(this,()=>started&&!closed&&session.IsActive&&Engine.Winner==0&&!Engine.Draw,()=>{Engine.Winner=2;CancelSearch();StartCoroutine(ShowResult());},()=>{var saved=session;int level=Level;Close(false,false);Open(saved,level,true);},skipGame:()=>{var saved=session;Close(false,false);saved.Finish(Outcome.Win);});ready.SetActive(false);undoButton.gameObject.SetActive(true);Refresh();}
        void Guide(){if(guide!=null||started)return;guide=IllustratedGuide.Show(root,9102,()=>guide=null);}
        public void HumanMove(int index){if(PlayClock.Paused||!started||closed||Engine.Winner!=0||Engine.Draw||Engine.Turn!=1||ai!=null)return;if(Place(index)&&Engine.Winner==0&&!Engine.Draw)Think();}
        bool Place(int index){if(!Engine.Play(index))return false;animationIndex=index;animationTime=StudentAge.CampusUno.PlayClock.Now;PlaySound("place",.48f);Refresh();if(Engine.Winner!=0||Engine.Draw)StartCoroutine(ShowResult());return true;}
        void Think(){CancelSearch();cancel=new CancellationTokenSource();var token=cancel.Token;var copy=(int[])Engine.Board.Clone();int level=Level;aiStarted=StudentAge.CampusUno.PlayClock.Now;ai=Task.Run(()=>GomokuAI.Choose(copy,2,level,token),token);Refresh();}
        void CancelSearch(){if(cancel!=null){cancel.Cancel();cancel.Dispose();cancel=null;}if(ai!=null){var obsolete=ai;obsolete.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);ai=null;}}
        public void Undo(){if(!started||Engine.Winner!=0||Engine.Draw||Engine.History.Count==0)return;CancelSearch();if(Engine.Turn==1)Engine.Undo();Engine.Undo();animationIndex=-1;PlaySound("undo",.27f);Refresh();}
        void Refresh(){if(turnText==null)return;turnText.text=!started?"":Engine.Winner!=0||Engine.Draw?"对局结束":Engine.Turn==1?"轮到你了":"同桌思考中…";undoButton.interactable=started&&Engine.History.Count>0&&Engine.Winner==0&&!Engine.Draw;for(int i=0;i<225;i++){bool filled=Engine.Board[i]!=0;stones[i].gameObject.SetActive(filled);stones[i].sprite=Engine.Board[i]==2?white:black;stones[i].color=Color.white;var pt=paper.Find("Point-"+i).GetComponent<GomokuPoint>();pt.SetShadow(filled);pt.ResetPosition();}latest.gameObject.SetActive(Engine.History.Count>0);if(Engine.History.Count>0){int i=Engine.History[Engine.History.Count-1];latest.rectTransform.anchoredPosition=new Vector2(61.14f+i%15*(601.72f/14)-2.5f,-61.14f-i/15*(601.72f/14)+2.5f);}latest.transform.SetAsLastSibling();}
        internal void Hover(int index,bool enter){if(!started||closed||Engine.Turn!=1||Engine.Winner!=0||Engine.Draw||Engine.Board[index]!=0)return;stones[index].gameObject.SetActive(enter);stones[index].sprite=black;stones[index].color=new Color(1,1,1,.35f);}
        IEnumerator ShowResult(){yield return new StudentAge.CampusUno.PlayDelay(.75f);if(closed)yield break;NativeMinigameResult.Show(Engine.Draw?Outcome.Draw:Engine.Winner==1?Outcome.Win:Outcome.Lose,canvasObject,oldScale,()=>Close(true,true));}
        IEnumerator LoadAudio(){foreach(string file in new[]{"place-1","place-2","place-3","undo","paper"}){string path=Path.Combine(session.Root,"Audio",file+".ogg");if(!File.Exists(path))continue;using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.OGGVORBIS)){yield return request.SendWebRequest();if(closed)yield break;if(request.result==UnityWebRequest.Result.Success)clips[file]=DownloadHandlerAudioClip.GetContent(request);}}}
        void PlaySound(string kind,float volume){if(closed||audioSource==null||!audioSource.isActiveAndEnabled)return;string file=kind;if(kind=="place"){int next=(lastSound+1+UnityEngine.Random.Range(0,2))%3;lastSound=next;file="place-"+(next+1);}AudioClip clip;if(clips.TryGetValue(file,out clip))audioSource.PlayOneShot(clip,volume);}
        void Update(){if(closed)return;if(root!=null)Fit();if(session!=null&&!session.IsActive){Close(false,false);return;}if(PlayClock.Paused)return;if(animationIndex>=0){float p=Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-animationTime)/.21f);int i=animationIndex;stones[i].rectTransform.anchoredPosition=new Vector2(61.14f+i%15*(601.72f/14)-18,-61.14f-i/15*(601.72f/14)+18+30*Mathf.Pow(1-p,3));if(p>=1)animationIndex=-1;}if(ai!=null&&ai.IsCompleted&&StudentAge.CampusUno.PlayClock.Now-aiStarted>=.42f){var done=ai;ai=null;if(done.IsCanceled)return;if(done.IsFaulted){Debug.LogException(done.Exception);Close(false,true);return;}Place(done.Result);}}
        void Close(bool settle,bool notify){if(closed)return;closed=true;pause?.Dispose();CancelSearch();if(canvasObject!=null){canvasObject.SetActive(false);Destroy(canvasObject);}Unlock();if(music!=null)music.Close();Plugin.ReleaseExternal();if(current==this)current=null;try{if(notify){if(settle)session.Finish(Engine.Winner==1?Outcome.Win:Engine.Draw?Outcome.Draw:Outcome.Lose);else session.Cancel();}}finally{Destroy(gameObject);}}
        void Unlock(){if(!locked)return;locked=false;Time.timeScale=oldScale;Control.ToggleActionMap(true);}
        void OnDestroy(){if(!closed)Close(false,true);Unlock();foreach(var t in textures)Destroy(t);foreach(var s in sprites)Destroy(s);foreach(var c in clips.Values)Destroy(c);}
    }
    public sealed class GomokuPoint : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        GomokuView view;int index;Image stone,shadow;Vector2 origin;
        internal void Setup(GomokuView v,int i,Image s,Image sh){view=v;index=i;stone=s;shadow=sh;origin=s.rectTransform.anchoredPosition;}
        internal void SetShadow(bool visible){shadow.gameObject.SetActive(visible);}internal void ResetPosition(){stone.rectTransform.anchoredPosition=origin;}
        public void OnPointerEnter(PointerEventData data){view.Hover(index,true);}public void OnPointerExit(PointerEventData data){view.Hover(index,false);}
    }
}
