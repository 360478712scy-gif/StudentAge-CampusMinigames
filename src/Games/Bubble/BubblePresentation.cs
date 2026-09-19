using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Obj=UnityEngine.Object;

namespace StudentAge.CampusBubble
{
    public sealed class BubbleSkin
    {
        public Sprite Background,Paper,Green,Orange;
        public Font Title,Body;
    }
    public sealed class BubblePresentation : MonoBehaviour
    {
        const float CellW=69,CellH=54,BoardX=351,BoardY=208;
        static readonly Color Ink=new Color(.32f,.18f,.10f),Cream=new Color(1,.975f,.9f);
        public BubbleGame Game{get;private set;}
        public bool Started{get;private set;}public Action<Transform,Action> GuideRequest;public Func<bool> PauseCheck;public Func<float> Clock;float Now=>Clock==null?Time.unscaledTime:Clock();
        public bool Prepared{get;private set;}
        public Func<bool> StartRequest;
        public Action<MatchResult> FinishRequest;
        public Action<MatchResult> ResultRequested;
        public Action<string,float> SoundRequested;
        BubbleSkin skin;GameObject canvasObject,ready,guide,result;RectTransform root,field,dynamicLayer;
        Sprite round,disc,bubbleSprite;readonly List<Texture2D> textures=new List<Texture2D>();readonly List<Sprite> ownedSprites=new List<Sprite>();
        readonly GameObject[] terrain=new GameObject[BubbleGame.Count],pickups=new GameObject[BubbleGame.Count];
        readonly Tile[] drawnTiles=new Tile[BubbleGame.Count];readonly int[] drawnItems=new int[BubbleGame.Count];
        readonly Image[] water=new Image[BubbleGame.Count];readonly RectTransform[] actors=new RectTransform[2];readonly CanvasGroup[] actorGroups=new CanvasGroup[2];readonly Text[] lifeTexts=new Text[2],gearTexts=new Text[2];
        readonly Dictionary<int,RectTransform> bombs=new Dictionary<int,RectTransform>();readonly List<Particle> particles=new List<Particle>();
        int keyX,keyY;bool requestDrop,closing;float finishAt=-1,lastStepSound=-10;
        sealed class Particle{public RectTransform Rect;public Image Image;public Vector2 Start,Velocity;public float Born,Duration,Spin;}
        public void Initialize(BubbleSkin theme,string opponent,int level,int seed)
        {
            skin=theme;Game=new BubbleGame(seed,level);round=MakeSprite(0);disc=MakeSprite(1);bubbleSprite=MakeSprite(2);
            canvasObject=new GameObject("CampusBubbleCanvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var mask=R(canvasObject.transform,"InputBlocker",0,0,0,0);mask.anchorMin=Vector2.zero;mask.anchorMax=Vector2.one;mask.offsetMin=mask.offsetMax=Vector2.zero;mask.gameObject.AddComponent<Image>().color=new Color(.10f,.16f,.15f);
            root=R(canvasObject.transform,"BubbleStage",0,0,1600,900);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);Fit();
            I(root,"Classroom",0,0,1600,900,skin.Background,Color.white);T(root,"课间泡泡",490,27,620,73,48);
            var shadow=I(root,"FieldShadow",BoardX-18,BoardY+7,938,636,round,new Color(.24f,.15f,.08f,.27f));
            var sheet=I(root,"FieldPaper",BoardX-27,BoardY-29,952,650,skin.Paper,Cream);sheet.type=Image.Type.Sliced;sheet.rectTransform.localScale=shadow.rectTransform.localScale=Vector3.one*1.15f;
            field=R(root,"ClassroomField",BoardX,BoardY,897,594);field.localScale=Vector3.one*1.15f;
            for(int y=0;y<BubbleGame.Height;y++)for(int x=0;x<BubbleGame.Width;x++)
            {
                int i=y*BubbleGame.Width+x;I(field,"Floor",x*CellW,y*CellH,CellW-.8f,CellH-.8f,null,(x+y)%2==0?new Color(.86f,.78f,.61f):new Color(.91f,.84f,.69f));
                water[i]=I(field,"Water",x*CellW+1,y*CellH+1,CellW-2,CellH-2,round,new Color(.3f,.82f,.98f,.75f));water[i].gameObject.SetActive(false);
                drawnItems[i]=-1;DrawTile(i);RefreshItem(i);
            }
            dynamicLayer=R(field,"MovingPieces",0,0,897,594);
            for(int n=0;n<2;n++){actors[n]=StudentFigure(dynamicLayer,n==0?new Color(.22f,.48f,.51f):new Color(.8f,.40f,.25f),n==1);actorGroups[n]=actors[n].gameObject.AddComponent<CanvasGroup>();SetActor(n,true);}
            Seat(0,"你",25);Seat(1,opponent,1340);
            ready=R(root,"Preparation",0,0,1600,900).gameObject;var shade=I(ready.transform,"Veil",BoardX-16,BoardY-12,932,618,round,new Color(1,.975f,.91f,.42f));shade.raycastTarget=true;
            B(ready.transform,"开始游戏",649,329,300,105,Begin);B(ready.transform,"玩法说明",649,456,300,105,OpenGuide,true);
            RefreshStats();Prepared=true;
        }
        void Fit(){if(root==null)return;float s=Math.Min(Screen.width/1600f,Screen.height/900f);root.localScale=Vector3.one*s;root.anchoredPosition=Vector2.zero;}
        static RectTransform R(Transform parent,string name,float x,float y,float w,float h){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);r.sizeDelta=new Vector2(w,h);return r;}
        Image I(Transform p,string name,float x,float y,float w,float h,Sprite sprite,Color color){var image=R(p,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;if(sprite==round)image.type=Image.Type.Sliced;return image;}
        Text T(Transform p,string text,float x,float y,float w,float h,int size,bool body=false){var t=R(p,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.text=text;t.font=body?skin.Body:skin.Title;if(t.font==null)t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=size;t.color=Ink;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        Button B(Transform p,string title,float x,float y,float w,float h,Action action,bool orange=false){var image=I(p,title,x,y,w,h,orange?skin.Orange:skin.Green,Color.white);if(image.sprite==null){image.sprite=round;image.color=orange?new Color(.98f,.57f,.22f):new Color(.53f,.69f,.25f);}image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};var txt=T(image.transform,title,0,-3,w,h,29);txt.color=Color.white;var outline=txt.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.29f,.17f,.1f,.8f);outline.effectDistance=new Vector2(1.5f,-1.5f);button.onClick.AddListener(()=>{if(closing)return;Sfx("paper",.19f);action();});return button;}
        Sprite MakeSprite(int kind)
        {
            int size=96;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float nx=(x-47.5f)/46.5f,ny=(y-47.5f)/46.5f,r=Mathf.Sqrt(nx*nx+ny*ny);Color c=Color.white;
                if(kind==0){float ax=Math.Max(0,Math.Abs(x-47.5f)-34),ay=Math.Max(0,Math.Abs(y-47.5f)-34);c.a=Mathf.Clamp01(12-Mathf.Sqrt(ax*ax+ay*ay));}
                else if(kind==1)c.a=Mathf.Clamp01((1-r)*32);
                else{float edge=Mathf.Clamp01((r-.65f)/.3f),shine=Mathf.Exp(-((nx+.33f)*(nx+.33f)+(ny-.42f)*(ny-.42f))*45);c=Color.Lerp(new Color(.43f,.82f,.91f,.48f),new Color(.19f,.64f,.82f,.90f),edge);c=Color.Lerp(c,Color.white,shine);c.a*=Mathf.Clamp01((1-r)*35);}
                tex.SetPixel(x,y,c);
            }tex.Apply();textures.Add(tex);var sprite=Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,kind==0?new Vector4(15,15,15,15):Vector4.zero);ownedSprites.Add(sprite);return sprite;
        }
        void DrawTile(int i)
        {
            if(terrain[i]!=null)Destroy(terrain[i]);drawnTiles[i]=Game.Tiles[i];if(Game.Tiles[i]==Tile.Floor)return;
            int x=i%BubbleGame.Width,y=i/BubbleGame.Width;var r=R(field,"Furniture-"+i,x*CellW,y*CellH,CellW,CellH);terrain[i]=r.gameObject;
            I(r,"Shadow",7,16,62,42,disc,new Color(.25f,.18f,.1f,.22f));
            bool boundary=x==0||y==0||x==12||y==10;
            if(Game.Tiles[i]==Tile.Desk)
            {
                if(boundary){I(r,"WoodEdge",1,5,67,46,round,new Color(.53f,.37f,.23f));I(r,"WoodTop",1,0,67,44,round,new Color(.71f,.53f,.33f));return;}
                I(r,"LeftLeg",12,20,8,34,round,new Color(.32f,.34f,.30f));I(r,"RightLeg",51,20,8,34,round,new Color(.32f,.34f,.30f));
                I(r,"DeskFront",3,2,63,43,round,new Color(.55f,.33f,.17f));I(r,"DeskTop",1,-8,66,39,round,new Color(.82f,.60f,.34f));I(r,"DeskLight",7,-4,52,2,null,new Color(.96f,.77f,.49f));
                var book=I(r,"Book",13,-5,27,21,round,new Color(.24f,.46f,.45f));book.rectTransform.localEulerAngles=new Vector3(0,0,-8);I(book.transform,"Pages",2,2,23,16,round,Cream);I(book.transform,"Spine",0,0,5,21,round,new Color(.21f,.41f,.40f));
            }
            else if(Game.Tiles[i]==Tile.Box)
            {
                I(r,"BoxFront",6,7,56,43,round,new Color(.65f,.43f,.23f));I(r,"BoxLid",5,-3,58,33,round,new Color(.84f,.66f,.41f));I(r,"Tape",28,-3,12,33,null,new Color(.95f,.83f,.59f));I(r,"TapeFront",29,30,12,19,null,new Color(.86f,.70f,.46f));I(r,"ShippingLabel",12,31,13,10,null,Cream);
            }
            else
            {
                Color c=i%3==0?new Color(.64f,.39f,.36f):new Color(.30f,.47f,.51f);I(r,"Handle",25,-5,20,17,round,c*.85f);I(r,"BagBody",13,0,45,48,round,c*.8f);I(r,"BagFlap",10,-1,48,26,round,c);I(r,"BagPocket",19,26,29,18,round,c*1.2f);I(r,"Clasp",31,19,6,9,round,new Color(.93f,.78f,.46f));
            }
        }
        void RefreshItem(int i)
        {
            if(pickups[i]!=null)Destroy(pickups[i]);drawnItems[i]=Game.Items[i];if(Game.Items[i]==0)return;float x=i%BubbleGame.Width*CellW,y=i/BubbleGame.Width*CellH;var r=R(field,"Pickup",x+14,y+5,40,40);pickups[i]=r.gameObject;
            I(r,"Token",0,0,40,40,disc,new Color(.99f,.9f,.56f));if(Game.Items[i]==1){I(r,"Ruler",9,9,22,22,round,new Color(.98f,.69f,.24f));for(int n=0;n<4;n++)I(r,"Tick",13+n*4,10,1,5,null,Ink);}else{I(r,"Bubble",4,9,21,21,bubbleSprite,Color.white);I(r,"Bubble",17,6,19,19,bubbleSprite,Color.white);}
        }
        RectTransform StudentFigure(Transform parent,Color jacket,bool longHair)
        {
            var r=R(parent,"Student",0,0,64,83);I(r,"FootShadow",8,64,53,18,disc,new Color(.15f,.21f,.19f,.25f));
            I(r,"ShoeL",17,66,16,10,round,new Color(.23f,.23f,.22f));I(r,"ShoeR",37,66,16,10,round,new Color(.23f,.23f,.22f));
            I(r,"LegL",21,51,10,21,round,new Color(.21f,.31f,.38f));I(r,"LegR",38,51,10,21,round,new Color(.21f,.31f,.38f));
            I(r,"SleeveL",10,31,14,25,round,jacket);I(r,"SleeveR",47,31,14,25,round,jacket);I(r,"HandL",10,49,11,10,disc,new Color(.96f,.78f,.62f));I(r,"HandR",49,49,11,10,disc,new Color(.96f,.78f,.62f));
            I(r,"Uniform",21,29,29,32,round,jacket);I(r,"Shirt",31,31,7,24,round,Cream);I(r,"CollarL",24,31,9,8,round,Cream);I(r,"CollarR",38,31,9,8,round,Cream);
            if(longHair)I(r,"BackHair",13,8,45,35,round,new Color(.23f,.16f,.13f));
            I(r,"Face",16,2,39,35,disc,new Color(.98f,.83f,.67f));I(r,"Hair",13,-3,44,20,round,new Color(.24f,.17f,.13f));I(r,"Fringe",14,7,12,18,round,new Color(.24f,.17f,.13f));
            I(r,"EyeL",25,18,3,4,disc,Ink);I(r,"EyeR",43,18,3,4,disc,Ink);I(r,"BlushL",21,24,7,3,disc,new Color(.91f,.53f,.44f,.5f));I(r,"BlushR",45,24,7,3,disc,new Color(.91f,.53f,.44f,.5f));return r;
        }
        void Seat(int actor,string name,float x)
        {
            var portrait=StudentFigure(root,actor==0?new Color(.22f,.48f,.51f):new Color(.8f,.40f,.25f),actor==1);portrait.anchoredPosition=new Vector2(x+113,-272);portrait.localScale=Vector3.one*1.65f;
            T(root,name,x-5,361,245,60,32);lifeTexts[actor]=T(root,"",x,424,235,51,34);lifeTexts[actor].color=actor==0?new Color(.20f,.44f,.44f):new Color(.73f,.34f,.25f);
            gearTexts[actor]=T(root,"",x,482,235,38,25,true);
        }
        void RefreshStats(){for(int n=0;n<2;n++){lifeTexts[n].text=new string('●',Math.Max(0,Game.Players[n].Lives));gearTexts[n].text="泡泡 "+Game.Players[n].Capacity+"    水波 "+Game.Players[n].Range;}}
        void SetActor(int n,bool immediate=false)
        {
            int cell=Game.Players[n].Cell;var target=new Vector2(cell%BubbleGame.Width*CellW+CellW*.5f,-cell/BubbleGame.Width*CellH-CellH*.5f+18);
            actors[n].anchoredPosition=immediate?target:Vector2.Lerp(actors[n].anchoredPosition,target,1-Mathf.Exp(-Time.unscaledDeltaTime*22));
            float motion=(actors[n].anchoredPosition-target).sqrMagnitude>.3f?Mathf.Sin(Now*24)*2:Mathf.Sin(Now*2+n)*.7f;actors[n].localEulerAngles=new Vector3(0,0,motion);actorGroups[n].alpha=Game.Clock<Game.Players[n].InvulnerableUntil&&Mathf.Sin(Now*26)>0?.35f:1;
        }
        public void SetInput(int dx,int dy,bool drop){keyX=dx;keyY=dy;requestDrop|=drop;}
        public void Begin(){if(Started||closing||!Prepared||guide!=null)return;if(StartRequest!=null&&!StartRequest())return;Started=true;requestDrop=false;ready.SetActive(false);}
        public void OpenGuide()
        {
            if(Started||guide!=null)return;if(GuideRequest!=null){guide=R(root,"GuideHost",0,0,1600,900).gameObject;GuideRequest(guide.transform,()=>{Destroy(guide);guide=null;});return;}guide=R(root,"Rules",0,0,1600,900).gameObject;var mask=I(guide.transform,"Mask",0,0,1600,900,null,new Color(0,0,0,.57f));mask.raycastTarget=true;var p=I(guide.transform,"Paper",360,112,880,675,skin.Paper,Cream);p.type=Image.Type.Sliced;T(p.transform,"玩法说明",60,49,760,68,44);
            string[] rules={"方向键 / WASD 移动，空格放泡泡。","也可以按住方向按钮，再点击“放泡泡”。","泡泡蓄满后，水波向上下左右扩散。","课桌挡住水波；纸箱和书包会被冲开。","你和同桌都会被水波击中，圆点耗尽即出局。","尺子增加水波距离，双泡泡增加可放数量。","泡泡会连锁引爆，放下后记得转弯躲开。"};for(int i=0;i<rules.Length;i++)T(p.transform,rules[i],55,160+i*48,770,43,25,true);
            B(p.transform,"知道了",310,545,260,86,()=>{guide.SetActive(false);Destroy(guide);guide=null;});
        }
        void Sfx(string key,float volume){if(!closing)SoundRequested?.Invoke(key,volume);}
        void Emit(int cell,bool paper)
        {
            var start=new Vector2(cell%BubbleGame.Width*CellW+CellW*.5f,-cell/BubbleGame.Width*CellH-CellH*.5f);for(int n=0;n<(paper?8:11);n++)
            {float angle=n*Mathf.PI*2/(paper?8:11)+UnityEngine.Random.value;var im=I(dynamicLayer,paper?"PaperScrap":"Droplet",0,0,paper?9:8,paper?14:8,paper?null:disc,paper?new Color(.94f,.78f,.48f):new Color(.63f,.94f,1,.9f));im.rectTransform.anchoredPosition=start;particles.Add(new Particle{Rect=im.rectTransform,Image=im,Start=start,Velocity=new Vector2(Mathf.Cos(angle)*105,Mathf.Sin(angle)*100+65),Born=Now,Duration=.50f+UnityEngine.Random.value*.22f,Spin=UnityEngine.Random.Range(-340,340)});}
        }
        void Update()
        {
            if(closing||!Prepared||PauseCheck!=null&&PauseCheck())return;Fit();if(Started&&Game.Result==MatchResult.Playing){int dx=keyX,dy=keyY;Game.Move(0,dx,dy);if(requestDrop)Game.Drop(0);Game.Tick(Time.unscaledDeltaTime);}requestDrop=false;
            while(Game.Events.Count>0){var e=Game.Events.Dequeue();if(e.Kind=="step"){if(e.Actor==0&&Now-lastStepSound>.15f){lastStepSound=Now;Sfx("step",.12f);}}
                else if(e.Kind=="burst"){Emit(e.Cell,false);Sfx("burst",.33f);}else if(e.Kind=="break"){Emit(e.Cell,true);Sfx("break",.25f);}else if(e.Kind=="finish")finishAt=Now+.8f;else Sfx(e.Kind,e.Kind=="hit"?.38f:.30f);}
            for(int i=0;i<BubbleGame.Count;i++){if(drawnTiles[i]!=Game.Tiles[i])DrawTile(i);if(drawnItems[i]!=Game.Items[i])RefreshItem(i);bool wet=Game.WetUntil[i]>Game.Clock;water[i].gameObject.SetActive(wet);if(wet){float a=Mathf.Clamp01((Game.WetUntil[i]-Game.Clock)/BubbleGame.WaveTime);water[i].color=new Color(.32f,.79f,.95f,.70f*a+.12f);}}
            foreach(var b in Game.Bubbles){RectTransform r;if(!bombs.TryGetValue(b.Id,out r)){r=R(dynamicLayer,"WaterBubble",0,0,55,55);I(r,"BubbleShadow",0,39,56,16,disc,new Color(.12f,.33f,.38f,.20f));I(r,"Bubble",0,0,55,55,bubbleSprite,b.Owner==0?Color.white:new Color(1,.81f,.82f));I(r,"Glint",12,9,10,6,disc,new Color(1,1,1,.95f));bombs.Add(b.Id,r);}r.anchoredPosition=new Vector2(b.Cell%BubbleGame.Width*CellW+CellW*.5f,-b.Cell/BubbleGame.Width*CellH-CellH*.5f+7);float progress=1-(b.BurstAt-Game.Clock)/BubbleGame.Fuse;float pulse=1+Mathf.Sin(Game.Clock*(progress>.75f?30:7))*.035f+progress*.07f;r.localScale=Vector3.one*pulse;}
            foreach(int id in new List<int>(bombs.Keys))if(!Game.Bubbles.Exists(b=>b.Id==id)){Destroy(bombs[id].gameObject);bombs.Remove(id);}
            for(int n=0;n<2;n++)SetActor(n);SortPieces();RefreshStats();
            for(int i=particles.Count-1;i>=0;i--){var p=particles[i];float elapsed=Now-p.Born;if(elapsed>=p.Duration){Destroy(p.Rect.gameObject);particles.RemoveAt(i);continue;}p.Rect.anchoredPosition=p.Start+p.Velocity*elapsed+Vector2.down*160*elapsed*elapsed;p.Rect.localEulerAngles=new Vector3(0,0,p.Spin*elapsed);var color=p.Image.color;color.a=1-elapsed/p.Duration;p.Image.color=color;}
            if(finishAt>=0&&Now>=finishAt&&result==null)ShowResult();
        }
        void SortPieces(){dynamicLayer.SetAsLastSibling();if(actors[0].anchoredPosition.y>actors[1].anchoredPosition.y){actors[0].SetAsLastSibling();actors[1].SetAsLastSibling();}else{actors[1].SetAsLastSibling();actors[0].SetAsLastSibling();}}
        void ShowResult()
        {
            finishAt=-1;
            if(ResultRequested!=null){canvasObject.SetActive(false);ResultRequested(Game.Result);return;}
            result=R(root,"Result",0,0,1600,900).gameObject;var mask=I(result.transform,"Mask",0,0,1600,900,null,new Color(0,0,0,.55f));mask.raycastTarget=true;
            var p=I(result.transform,"Paper",430,232,740,425,skin.Paper,Cream);p.type=Image.Type.Sliced;I(p.transform,"BubbleSeal",321,75,97,97,bubbleSprite,Color.white);
            T(p.transform,Game.Result==MatchResult.Win?"你赢了！":Game.Result==MatchResult.Draw?"平分秋色":"同桌获胜",80,212,580,74,45);
            StartCoroutine(FinishPreview());
        }
        System.Collections.IEnumerator FinishPreview(){yield return new WaitForSecondsRealtime(1.5f);if(!closing)FinishRequest?.Invoke(Game.Result);}
        public void ShutDown(){if(closing)return;closing=true;if(canvasObject!=null){canvasObject.SetActive(false);Destroy(canvasObject);}enabled=false;}
        void OnDestroy(){ShutDown();foreach(var s in ownedSprites)Destroy(s);foreach(var t in textures)Destroy(t);}
    }
    public sealed class BubbleHold : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public Action<bool> Changed;
        public void OnPointerDown(PointerEventData e){Changed?.Invoke(true);}public void OnPointerUp(PointerEventData e){Changed?.Invoke(false);}public void OnPointerExit(PointerEventData e){Changed?.Invoke(false);}
        void OnDisable(){Changed?.Invoke(false);}
    }
}
