using System;using System.IO;using System.Collections;using System.Collections.Generic;using Sdk;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;
namespace StudentAge.CampusUno {
public static class PlayClock {
    static float stopped,total;public static bool Paused{get;private set;}
    public static float Now=>(Paused?stopped:Time.unscaledTime)-total;
    public static float Delta=>Paused?0:Time.unscaledDeltaTime;
    public static void SetPaused(bool value){if(value==Paused)return;if(value)stopped=Time.unscaledTime;else total+=Time.unscaledTime-stopped;Paused=value;}
}
public sealed class PlayDelay:CustomYieldInstruction {readonly float until;public PlayDelay(float seconds){until=PlayClock.Now+seconds;}public override bool keepWaiting=>PlayClock.Paused||PlayClock.Now<until;}
public sealed class MinigamePause:MonoBehaviour {
    public static MinigamePause Current{get;private set;}public bool IsPaused=>PlayClock.Paused;
    Texture2D fontAtlas,guideAtlas;RectTransform pausePanel;GameObject guide;string fontChars;RectTransform stage;GameObject ui,menu;MonoBehaviour owner;Func<bool> ready;Action surrender,restart,skip;bool skipUsed;string theme;readonly List<AudioSource> sounds=new List<AudioSource>();
    public static MinigamePause Attach(MonoBehaviour host,Func<bool> canPause,Action giveUp,Action retry,string style="",Action skipGame=null){
        if(Current!=null)Current.Dispose();var v=host.gameObject.AddComponent<MinigamePause>();Current=v;v.owner=host;v.ready=canPause;v.surrender=giveUp;v.restart=retry;v.theme=style;v.skip=skipGame;v.Build();return v;
    }
    void Build(){if(theme!=""){string root=Path.Combine(BepInEx.Paths.PluginPath,"StudentAgeCampusMinigames","Nds");fontAtlas=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(fontAtlas,File.ReadAllBytes(Path.Combine(root,"pause-font.png")));fontAtlas.filterMode=FilterMode.Point;fontAtlas.wrapMode=TextureWrapMode.Clamp;fontChars=File.ReadAllText(Path.Combine(root,"pause-font.txt"));}ui=new GameObject("PauseControls",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));ui.transform.SetParent(transform,false);var c=ui.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=32765;var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;stage=Rect(ui.transform,"PauseStage",0,0,1600,900);stage.anchorMin=stage.anchorMax=new Vector2(.5f,.5f);stage.anchoredPosition=new Vector2(-800,450);
    }
    public void Toggle(){if(owner==null||ready==null||!ready())return;if(PlayClock.Paused){if(guide!=null)CloseGuide();else Resume();}else Pause();}
    public void Pause(){if(PlayClock.Paused||ready==null||!ready())return;PlayClock.SetPaused(true);PauseAudio();menu=Rect(stage,"PauseMenu",0,0,1600,900).gameObject;var dim=menu.AddComponent<Image>();dim.color=new Color(0,0,0,.7f);dim.raycastTarget=true;
        bool retro=theme!="";Color panel=theme=="mario"?new Color(.12f,.18f,.42f):theme=="contra"?new Color(.12f,.19f,.12f):new Color(1,.97f,.88f);
        var sheet=Rect(menu.transform,"PausePanel",470,135,660,630);pausePanel=sheet;var bg=sheet.gameObject.AddComponent<Image>();bg.color=panel;if(!retro){bg.sprite=NativeSkin.Paper;bg.type=Image.Type.Sliced;}else{var border=sheet.gameObject.AddComponent<Outline>();border.effectColor=theme=="mario"?new Color(1,.7f,.25f):new Color(.7f,.85f,.5f);border.effectDistance=new Vector2(5,-5);}
        Label(sheet,retro?"PAUSE":"游戏暂停",0,35,660,80,48);
        if(retro){Button(sheet,"Resume","继续游戏",130,140,400,80,Resume);Button(sheet,"RetroGuide","操作说明",130,245,400,80,ShowGuide);Button(sheet,"Restart","重新开始",130,350,400,80,()=>Choose(restart));Button(sheet,"Surrender","认输",130,455,400,80,()=>Choose(surrender));}
        else{Button(sheet,"Resume","继续游戏",130,170,400,92,Resume);Button(sheet,"Restart","重新开始",130,300,400,92,()=>Choose(restart));Button(sheet,"Surrender","认输",130,430,400,92,()=>Choose(surrender));}
    }
    void ShowGuide(){
        if(guide!=null)return;pausePanel.gameObject.SetActive(false);bool mario=theme=="mario";
        var sheet=Rect(menu.transform,"RetroControlGuide",200,80,1200,740);guide=sheet.gameObject;
        sheet.gameObject.AddComponent<Image>().color=mario?new Color(.08f,.12f,.32f):new Color(.06f,.12f,.08f);
        var border=sheet.gameObject.AddComponent<Outline>();border.effectColor=mario?new Color(1,.7f,.25f):new Color(.7f,.85f,.5f);border.effectDistance=new Vector2(5,-5);
        Label(sheet,mario?"MARIO 操作说明":"CONTRA 操作说明",240,35,780,72,42);
        GuideSprite(sheet,mario?"mario-mario/small_mario_stand":"contra-player/player_0",120,50);
        GuideRow(sheet,175,"W A S D",mario?"方向键也可移动；向上攀藤":"方向键也可移动和瞄准");
        GuideRow(sheet,280,"Z / 空格",mario?"跳跃；按住更久跳得更高":"跳跃；下加跳跃穿过单向平台");
        GuideRow(sheet,385,"X / SHIFT",mario?"按住加速；火焰形态发射火球":"按住连续射击，可向八个方向开火");
        GuideRow(sheet,490,"S / ↓",mario?"长大后下蹲；进入可用的水管":"原地卧倒，避开敌人的子弹");
        Label(sheet,mario?"手柄：方向移动  A跳跃  X加速":"手柄：方向瞄准  A跳跃  X射击",30,595,1140,44,24);
        Button(sheet,"GuideBack","返回暂停",450,660,300,60,CloseGuide);
    }
    void GuideRow(Transform p,float y,string key,string caption){var r=Rect(p,"PixelKey",55,y,290,74);r.gameObject.AddComponent<Image>().color=new Color(.28f,.30f,.35f);var edge=r.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.7f,.72f,.75f);edge.effectDistance=new Vector2(3,-3);Label(r,key,0,0,290,74,28);Label(p,caption,365,y,800,74,28);}
    void GuideSprite(Transform p,string name,float x,float y){
        string root=Path.Combine(BepInEx.Paths.PluginPath,"StudentAgeCampusMinigames","Retro");
        if(guideAtlas==null){guideAtlas=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(guideAtlas,File.ReadAllBytes(Path.Combine(root,"atlas.png")));guideAtlas.filterMode=FilterMode.Point;}
        var frames=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root,"atlas.json")))["frames"];
        foreach(var f in frames){if((string)f["name"]!=name)continue;float w=(float)f["w"],h=(float)f["h"];var image=Rect(p,"GuideCharacter",x,y,w*4,h*4).gameObject.AddComponent<RawImage>();image.texture=guideAtlas;image.uvRect=new Rect((float)f["x"]/guideAtlas.width,1-((float)f["y"]+h)/guideAtlas.height,w/guideAtlas.width,h/guideAtlas.height);image.raycastTarget=false;break;}
    }
    void CloseGuide(){if(guide!=null){guide.SetActive(false);Destroy(guide);guide=null;}if(pausePanel!=null)pausePanel.gameObject.SetActive(true);}
    void Choose(Action action){Resume();action?.Invoke();}
    public void Resume(){if(!PlayClock.Paused)return;PlayClock.SetPaused(false);guide=null;if(menu!=null){menu.SetActive(false);Destroy(menu);menu=null;}foreach(var s in sounds)if(s!=null)s.UnPause();sounds.Clear();}
    void PauseAudio(){if(owner!=null)foreach(var s in owner.GetComponents<AudioSource>())if(s!=null&&s.isPlaying&&!sounds.Contains(s)){sounds.Add(s);s.Pause();}if(AudioMgr.Ins!=null){var s=AudioMgr.Ins.GetChannel(AudioMgrEx.CHANNEL_BGM).source;if(s!=null&&s.isPlaying&&!sounds.Contains(s)){sounds.Add(s);s.Pause();}}}
    void Skip(){if(Current!=this||skipUsed||skip==null||owner==null||ready==null||!ready())return;skipUsed=true;Choose(skip);}
    void Update(){bool available=owner!=null&&ready!=null&&ready();if(!available){if(PlayClock.Paused)Resume();return;}var k=Keyboard.current;if(k!=null&&k.f6Key.wasPressedThisFrame){Skip();return;}if(k!=null&&k.escapeKey.wasPressedThisFrame)Toggle();if(PlayClock.Paused)PauseAudio();}
    public void Dispose(){if(Current==this){Resume();Current=null;}if(ui!=null){ui.SetActive(false);Destroy(ui);}Destroy(this);}
    void OnDestroy(){if(Current==this){Resume();Current=null;}if(ui!=null)Destroy(ui);if(fontAtlas!=null)Destroy(fontAtlas);if(guideAtlas!=null)Destroy(guideAtlas);}
    static RectTransform Rect(Transform p,string name,float x,float y,float w,float h){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(p,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    Graphic Label(Transform p,string value,float x,float y,float w,float h,int size){var r=Rect(p,"Label",x,y,w,h);if(theme!=""){var pixel=r.gameObject.AddComponent<PausePixelLabel>();pixel.Setup(fontAtlas,fontChars,fontAtlas.height/16,value,size/14f,Color.white);return pixel;}var t=r.gameObject.AddComponent<Text>();t.font=NativeSkin.TitleFont;t.fontSize=size;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.37f,.2f,.11f);t.raycastTarget=false;return t;}
    Button Button(Transform p,string name,string text,float x,float y,float w,float h,Action action){var r=Rect(p,name,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.color=theme=="mario"?new Color(.6f,.22f,.1f):theme=="contra"?new Color(.25f,.34f,.19f):Color.white;if(theme==""){i.sprite=NativeSkin.Green;i.type=Image.Type.Sliced;}var b=r.gameObject.AddComponent<Button>();b.targetGraphic=i;b.navigation=new Navigation{mode=Navigation.Mode.None};Label(r,text,0,0,w,h,31).color=Color.white;b.onClick.AddListener(()=>action());return b;}
}
public sealed class PausePixelLabel:MaskableGraphic{
 Texture2D atlas;string chars,value;int rows;float scale;public override Texture mainTexture=>atlas;
 public void Setup(Texture2D a,string c,int r,string text,float s,Color tint){atlas=a;chars=c;rows=r;value=text;scale=s;color=tint;raycastTarget=false;SetVerticesDirty();SetMaterialDirty();}
 protected override void OnPopulateMesh(VertexHelper h){h.Clear();if(atlas==null)return;float left=(rectTransform.rect.width-(value.Length*12+2)*scale)*.5f,top=-(rectTransform.rect.height-16*scale)*.5f;for(int n=0;n<value.Length;n++){int index=chars.IndexOf(value[n]);if(index<0)continue;float x=left+n*12*scale,y=top;float u=(index%16)/16f,v=1-(index/16)/(float)rows,du=1/16f,dv=1f/rows;int k=h.currentVertCount;h.AddVert(new Vector3(x,y),color,new Vector2(u,v));h.AddVert(new Vector3(x+14*scale,y),color,new Vector2(u+du,v));h.AddVert(new Vector3(x+14*scale,y-16*scale),color,new Vector2(u+du,v-dv));h.AddVert(new Vector3(x,y-16*scale),color,new Vector2(u,v-dv));h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}}
}
}
