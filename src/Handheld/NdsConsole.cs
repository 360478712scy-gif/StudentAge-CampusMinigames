using System;using System.Collections;using Sdk;using System.IO;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json.Linq;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using StudentAge.CampusUno;
namespace StudentAge.CampusMinigames{
public sealed class NdsGame{
 public readonly int Id,Stages;public readonly string Name;public readonly Action<IGameSession,int> Launch;
 public NdsGame(int id,string name,int stages,Action<IGameSession,int> launch){Id=id;Name=name;Stages=stages;Launch=launch;}
}
public static class NdsCatalog{
 public static readonly NdsGame[] Games={
 new NdsGame(9101,"课间UNO",5,(s,l)=>{if(!Plugin.OpenExternal(s.Name,s))throw new InvalidOperationException("UNO unavailable");s.OnInvalidated(()=>Plugin.InvalidateExternal(s));}),
 new NdsGame(9102,"五子棋",5,GomokuView.Open),new NdsGame(9103,"课间泡泡",5,BubbleView.Open),
 new NdsGame(9104,"换盒寻物",5,(s,l)=>PuzzleFrame.Open<BoxView>(s,9104,l)),new NdsGame(9105,"算24点",5,(s,l)=>PuzzleFrame.Open<TwentyFourView>(s,9105,l)),
 new NdsGame(9106,"俄罗斯方块",5,(s,l)=>PuzzleFrame.Open<FallingBlocksView>(s,9106,l)),new NdsGame(9107,"斗地主",5,(s,l)=>PuzzleFrame.Open<LandlordView>(s,9107,l)),
 new NdsGame(9108,"贪吃蛇",5,(s,l)=>PuzzleFrame.Open<SnakeView>(s,9108,l)),new NdsGame(9109,"吃豆人",5,(s,l)=>PuzzleFrame.Open<MazeView>(s,9109,l)),
 new NdsGame(9110,"超级马里奥",8,(s,l)=>PuzzleFrame.Open<MarioView>(s,9110,l,8)),new NdsGame(9111,"魂斗罗",1,(s,l)=>PuzzleFrame.Open<ContraView>(s,9111,l,1)),
 new NdsGame(9112,"麻将",5,(s,l)=>PuzzleFrame.Open<MahjongView>(s,9112,l))};
}
public sealed class NdsSession:IGameSession,IPracticeSession{
 readonly NdsConsole owner;readonly NdsGame game;readonly int level;int startRound;bool began,ended;Action invalidated;public NdsSession(NdsConsole o,NdsGame g,int l){owner=o;game=g;level=l;}
 public string Name=>"电脑";public string Root=>CampusResources.ContentRoot;public int NpcId=>-1;public bool IsExternal=>true;public float Cost=>0;
 public bool IsActive=>!ended&&owner!=null&&owner.Valid&&owner.CanUse;public bool Begin(){if(began)return IsActive;if(!IsActive||!owner.CanPlay(game,level))return false;began=true;if(!owner.TitleMode)startRound=Singleton<RoundMgr>.Ins.GetRound();return true;}
 public void Finish(Outcome outcome){if(ended||!began||outcome==Outcome.Playing)return;bool active=IsActive;ended=true;if(active&&!owner.TitleMode)NdsProgress.Complete(game,level,startRound,outcome);if(owner!=null)owner.Resume(this);}
 public void Cancel(){if(ended)return;ended=true;if(owner!=null)owner.Resume(this);}
 public void OnInvalidated(Action action){invalidated+=action;}
 public void Invalidate(){if(ended)return;ended=true;var action=invalidated;invalidated=null;action?.Invoke();}
}
public sealed partial class NdsConsole:MonoBehaviour{
 public static NdsConsole Current{get;private set;}public bool Valid{get;private set;}public bool InGame=>session!=null;public bool Booting{get;private set;}public int Selected{get;private set;}public int Level{get;private set;}=1;
 GameObject canvas;RectTransform root,content;float previousTime;bool menuLock;NdsSession session;Texture2D font;string characters;int fontRows;readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();Sprite shell;Sprite[] icons;NativeMinigameAudio music;
 public bool TitleMode{get;private set;}
 public bool CanUse=>TitleMode?Game.GetGameState()==GameState.Start&&UIMgr.IsViewOpened<View.Main.EntryView>():NdsIntegration.Owned;
 public bool CanPlay(NdsGame game,int level)=>level>=1&&level<=game.Stages&&(TitleMode||NdsProgress.CanPlay(game,level));
 public static void Open()=>Open(false);
 public static void OpenFromTitle(){if(NdsIntegration.IsTitleScreen)Open(true);}
 static void Open(bool titleMode){if(Current!=null||Plugin.IsBusy||(titleMode?!NdsIntegration.IsTitleScreen:!NdsIntegration.Owned))return;var obj=new GameObject("NdsConsoleHost");DontDestroyOnLoad(obj);var v=obj.AddComponent<NdsConsole>();Current=v;v.TitleMode=titleMode;v.previousTime=Time.timeScale;v.Valid=true;v.Booting=true;try{v.Load();v.Build();v.LockMenu();v.StartCoroutine(v.Boot());}catch(Exception e){Debug.LogException(e);v.Close();}}
 public static void Invalidate(){if(Current!=null)Current.Close();}
 void Load(){LoadBadgeAudio();string path=Path.Combine(CampusResources.ContentRoot,"Nds");shell=LoadSprite(Path.Combine(path,"shell.png"));icons=Enumerable.Range(0,12).Select(n=>LoadSprite(Path.Combine(path,"icon-"+n+".png"))).ToArray();font=LoadTexture(Path.Combine(path,"font.png"));var data=JObject.Parse(File.ReadAllText(Path.Combine(path,"font.json")));characters=(string)data["characters"];fontRows=font.height/16;}
 Texture2D LoadTexture(string file){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(file));t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;owned.Add(t);return t;}
 Sprite LoadSprite(string path){var t=LoadTexture(path);var s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100);owned.Add(s);return s;}
 void Build(){canvas=new GameObject("NDS掌机",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=32759;c.pixelPerfect=true;
 var blocker=PuzzleFrame.R(canvas.transform,"InputBlocker",0,0,0,0);blocker.anchorMin=Vector2.zero;blocker.anchorMax=Vector2.one;blocker.offsetMin=blocker.offsetMax=Vector2.zero;blocker.gameObject.AddComponent<Image>().color=new Color(.12f,.18f,.21f,.97f);
 root=PuzzleFrame.R(canvas.transform,"NdsStage",0,0,1600,900);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);Picture(root,"NdsShell",400,0,800,900,shell);content=PuzzleFrame.R(root,"Displays",0,0,1600,900);DrawBoot(0);Fit();BuildBadgeButton();
 Button(root,"ReturnToGame",52,40,192,58,Close,new Color(.76f,.85f,.81f));Text(root,"← 返回",52,40,192,58,2.3f,new Color(.18f,.31f,.35f));
 Button(root,"Power",1076,412,64,38,()=>Close(),Color.clear);
 Button(root,"DPadLeft",440,624,34,32,()=>Select((Selected+11)%12),Color.clear);Button(root,"DPadRight",506,624,34,32,()=>Select((Selected+1)%12),Color.clear);
 Button(root,"DPadUp",474,590,32,34,()=>Select((Selected+9)%12),Color.clear);Button(root,"DPadDown",474,656,32,34,()=>Select((Selected+3)%12),Color.clear);
 Button(root,"A",1136,608,32,32,StartSelected,Color.clear);Button(root,"B",1098,646,32,32,Close,Color.clear);
 }
 IEnumerator Boot(){float start=Time.unscaledTime;while(Valid&&Time.unscaledTime-start<2.4f){DrawBoot(Time.unscaledTime-start);yield return null;}if(!Valid)yield break;Booting=false;Draw();if(!TitleMode)QueueBadges(NdsBadges.Reconcile());}
 void ClearDisplay(){foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
 void DrawBoot(float t){ClearDisplay();Color bg=Color.Lerp(new Color(.14f,.23f,.25f),new Color(.87f,.93f,.83f),Mathf.Clamp01(t*2));Color ink=new Color(.18f,.31f,.35f);Fill(content,"BootTop",570,54,460,336,bg);Fill(content,"BootBottom",570,482,460,336,bg);if(t>.35f){float enter=Mathf.Clamp01((t-.35f)/.65f);float shift=(1-enter)*(1-enter)*90;Fill(content,"BootEmblemTop",735-shift,122,92,60,ink);Fill(content,"BootEmblemTopInset",743-shift,130,76,44,bg);Fill(content,"BootEmblemLower",773+shift,182,92,60,ink);Fill(content,"BootEmblemLowerInset",781+shift,190,76,44,bg);Text(content,"NDS掌机",610,264,380,44,3,ink);}if(t>1.1f){float f=Mathf.Clamp01((t-1.1f)/.95f);for(int n=0;n<12;n++)Fill(content,"BootPixel",651+n*25,628,18,18,n<Mathf.CeilToInt(f*12)?ink:new Color(.65f,.77f,.68f));}}
 void Fit(){if(root==null)return;float scale=Math.Min(Screen.width/1600f,Screen.height/900f);root.localScale=Vector3.one*scale;root.anchoredPosition=new Vector2(-800*scale,450*scale);}
 void LockMenu(){if(!Valid)return;if(!Plugin.AcquireExternal(Close)){Close();return;}menuLock=true;Time.timeScale=0;Control.ToggleActionMap(false);music=NativeMinigameAudio.Open(gameObject,919901,badgeQueue.Count>0);canvas.SetActive(true);}
 public void Select(int index){if(!Valid||Booting||InGame||index<0||index>=NdsCatalog.Games.Length)return;Selected=index;Level=1;Draw();}
 public void SelectLevel(int level){if(!Valid||Booting||InGame||level<1||level>NdsCatalog.Games[Selected].Stages)return;Level=level;Draw();}
 public void StartSelected(){if(!Valid||Booting||InGame||!CanUse||!CanPlay(NdsCatalog.Games[Selected],Level))return;canvas.SetActive(false);if(music!=null){music.Close();music=null;}if(menuLock){menuLock=false;Plugin.ReleaseExternal();}session=new NdsSession(this,NdsCatalog.Games[Selected],Level);try{NdsCatalog.Games[Selected].Launch(session,Level);}catch(Exception e){Debug.LogException(e);session.Cancel();}}
 public void Resume(NdsSession from){if(!Valid||session!=from)return;session=null;if(!CanUse){Close();return;}Draw();LockMenu();TryShowAward();}
 public void Close(){if(!Valid)return;Valid=false;var running=session;session=null;running?.Invalidate();if(menuLock){menuLock=false;Plugin.ReleaseExternal();}if(music!=null)music.Close();if(canvas!=null){canvas.SetActive(false);Destroy(canvas);}Time.timeScale=previousTime;Control.ToggleActionMap(true);if(Current==this)Current=null;Destroy(gameObject);}
 void Update(){if(!Valid)return;Fit();if(!CanUse){Close();return;}if(InGame)return;if(UpdateBadges())return;var k=Keyboard.current;if(k==null)return;
 if(k.escapeKey.wasPressedThisFrame){Close();return;}if(Booting)return;if(k.leftArrowKey.wasPressedThisFrame)Select((Selected+11)%12);else if(k.rightArrowKey.wasPressedThisFrame)Select((Selected+1)%12);else if(k.upArrowKey.wasPressedThisFrame)Select((Selected+9)%12);else if(k.downArrowKey.wasPressedThisFrame)Select((Selected+3)%12);else if(k.qKey.wasPressedThisFrame)SelectLevel(Math.Max(1,Level-1));else if(k.eKey.wasPressedThisFrame)SelectLevel(Math.Min(NdsCatalog.Games[Selected].Stages,Level+1));else if(k.enterKey.wasPressedThisFrame)StartSelected();}
 void Draw(){ClearDisplay();Color ink=new Color(.18f,.31f,.35f),light=new Color(.87f,.93f,.83f),green=new Color(.44f,.67f,.56f);
 Fill(content,"TopLCD",570,54,460,336,light);for(int y=54;y<390;y+=8)Fill(content,"Scanline",570,y,460,2,new Color(.82f,.89f,.80f));Text(content,NdsCatalog.Games[Selected].Name,580,65,440,34,2.5f,ink);Picture(content,"SelectedIcon",738,111,124,124,icons[Selected]);Text(content,TitleMode?"全部关卡开放":NdsProgress.Status(NdsCatalog.Games[Selected],Level),580,242,440,30,1.9f,ink);
 int count=NdsCatalog.Games[Selected].Stages;for(int n=0;n<count;n++){int level=n+1,col=n%4,row=n/4;float x=642+col*82,y=282+row*49;Button(content,"Level-"+level,x,y,70,39,()=>SelectLevel(level),level==Level?green:CanPlay(NdsCatalog.Games[Selected],level)?new Color(.74f,.84f,.75f):new Color(.65f,.70f,.65f));Text(content,level.ToString(),x,y,70,39,2,level==Level?Color.white:ink);if(!CanPlay(NdsCatalog.Games[Selected],level)){Fill(content,"Lock",x+53,y+24,11,9,ink);Fill(content,"LockLoop",x+55,y+20,7,6,ink);}}
 Fill(content,"TouchLCD",570,482,460,336,new Color(.77f,.86f,.82f));int page=Selected/6;
 for(int n=0;n<6;n++){int index=page*6+n;float x=578+n%3*150,y=491+n/3*123;bool picked=index==Selected;Button(content,"Game-"+NdsCatalog.Games[index].Id,x,y,144,115,()=>Select(index),picked?new Color(.93f,.96f,.84f):new Color(.84f,.90f,.86f));if(picked){Fill(content,"SelectionTop",x,y,144,4,green);Fill(content,"SelectionBottom",x,y+111,144,4,green);}Picture(content,"Cartridge",x+43,y+7,58,58,icons[index]);Text(content,NdsCatalog.Games[index].Name,x,y+77,144,32,1.6f,ink);}
 Button(content,"PreviousPage",582,752,48,49,()=>Select((1-page)*6),green);Text(content,"←",582,752,48,49,2,Color.white);Text(content,(page+1)+"/2",638,752,112,49,2,ink);Button(content,"NextPage",758,752,48,49,()=>Select((1-page)*6),green);Text(content,"→",758,752,48,49,2,Color.white);Button(content,"StartGame",837,752,181,49,StartSelected,CanPlay(NdsCatalog.Games[Selected],Level)?new Color(.31f,.52f,.49f):new Color(.52f,.60f,.56f));Text(content,CanPlay(NdsCatalog.Games[Selected],Level)?"开始":"未开放",837,752,181,49,2,Color.white);
 }
 Image Fill(Transform p,string name,float x,float y,float w,float h,Color color){var i=PuzzleFrame.R(p,name,x,y,w,h).gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
 void Picture(Transform p,string name,float x,float y,float w,float h,Sprite sprite){var i=Fill(p,name,x,y,w,h,Color.white);i.sprite=sprite;}
 void Button(Transform p,string name,float x,float y,float w,float h,Action action,Color color){var i=Fill(p,name,x,y,w,h,color);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{if(Valid&&!InGame)action();});}
 void Text(Transform p,string value,float x,float y,float w,float h,float scale,Color color){var r=PuzzleFrame.R(p,"PixelText-"+value,x,y,w,h);var t=r.gameObject.AddComponent<NdsPixelText>();t.Setup(font,characters,fontRows,value,scale,color);}
 void OnDestroy(){if(Valid)Close();CloseBadgeAudio();foreach(var o in owned)if(o!=null)Destroy(o);}
}
public sealed class NdsPixelText:MaskableGraphic{
 Texture2D atlas;string chars,value;int rows;float scale;public override Texture mainTexture=>atlas;
 public void Setup(Texture2D a,string c,int r,string text,float s,Color tint){atlas=a;chars=c;rows=r;value=text;scale=s;color=tint;raycastTarget=false;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper h){h.Clear();if(atlas==null)return;float left=(rectTransform.rect.width-(value.Length*12+2)*scale)*.5f,top=-(rectTransform.rect.height-16*scale)*.5f;for(int n=0;n<value.Length;n++){int index=chars.IndexOf(value[n]);if(index<0)continue;float x=left+n*12*scale,y=top;float u=(index%16)/16f,v=1-(index/16)/(float)rows,du=1/16f,dv=1f/rows;int k=h.currentVertCount;h.AddVert(new Vector3(x,y),color,new Vector2(u,v));h.AddVert(new Vector3(x+14*scale,y),color,new Vector2(u+du,v));h.AddVert(new Vector3(x+14*scale,y-16*scale),color,new Vector2(u+du,v-dv));h.AddVert(new Vector3(x,y-16*scale),color,new Vector2(u,v-dv));h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}}
}
}
