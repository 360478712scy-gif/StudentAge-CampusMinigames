using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StudentAge.CampusUno
{
    internal sealed class UnoTable : MonoBehaviour
    {
        internal static readonly Color Ink = Hex("214C49"), Cream = Hex("FFF5DB"), Gold = Hex("F6CC74");
        static readonly Color[] Colors = { Hex("C64F43"), Hex("397D9E"), Hex("51836B"), Hex("CE9635"), Hex("344F59") };
        static readonly string[] Subjects = { "语文", "数学", "生物", "英语" };
        GameObject canvasObject, tableObject, stateObject, handObject, popupObject;
        RectTransform board, effects, pileLayer, controlLayer, drawPileLayer;
        readonly List<RectTransform> deckSheets=new List<RectTransform>();readonly List<CardVisual> shuffleCards=new List<CardVisual>();CardVisual deckTop;Image deckShadow;int displayedDrawCount=-1;float openingDealAt,shuffleTime;int soundedDeal=-1;
        static readonly Vector2 DeckBase=new Vector2(477,220);
        Vector2 DrawOrigin=>DeckBase-new Vector2(0,Math.Max(0,displayedDrawCount)*.30f);
        Font font, numberFont; Sprite rounded, oval; Texture2D texture, ovalTexture, desk;
        Text actionText; CanvasGroup actionGroup; float actionAt=-100; int lastDrawSeat=-1; float lastDrawAt=-100; GameObject guideObject; Image colorEdge; Button drawButton, unoButton;
        readonly List<CardVisual> handViews = new List<CardVisual>();
        readonly List<NativeSeatBadge> badges = new List<NativeSeatBadge>();
        readonly CardSeatIdentity[] identities=new CardSeatIdentity[4];
        IUnoSession session; UnoGame game; string opponent;
        NativeMinigameAudio music;
        MinigamePause pause;float oldScale, dealUntil, flightTime; bool locked, closing, resultDrawn, started, skinReady;
        int revision = -1, wildIndex = -1, pileCount;
        NativeCardTable physicalTable;TurnDirectionRing directionRing;CardVisual flying; GameObject flightBack; Card shownTop; MoveEvent currentMove;
        internal IUnoSession Session { get { return session; } }
        public void Invalidate(){session=null;CloseInternal(false);}
        public UnoGame Engine { get { return game; } }
        public bool Prepared { get { return skinReady; } }
        bool Animating { get { return StudentAge.CampusUno.PlayClock.Now < dealUntil || flying != null || shuffleCards.Count>0 || (game != null && game.Moves.Count > 0); } }
        string SeatName(int seat) { return identities[seat]!=null?identities[seat].Name:seat==0?"你":opponent; }
        public void Open(string other, IUnoSession social,bool autoBegin=false)
        {
            opponent = other; session = social;
            font = Resources.FindObjectsOfTypeAll<Text>().Where(t => t.font != null && t.font.HasCharacter('学')).Select(t => t.font).FirstOrDefault();
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 32);
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            numberFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            MakeSprites();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CampusUno.Desk"))
            using (var bytes = new MemoryStream()) { stream.CopyTo(bytes); desk = new Texture2D(2, 2); ImageConversion.LoadImage(desk, bytes.ToArray()); }
            canvasObject = new GameObject("CampusUnoModal", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
            var blocker = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image)); blocker.transform.SetParent(canvasObject.transform, false);
            var br = blocker.GetComponent<RectTransform>(); br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = Hex("172C2E");
            tableObject = new GameObject("ClassroomDesk", typeof(RectTransform)); tableObject.transform.SetParent(canvasObject.transform, false);
            board = tableObject.GetComponent<RectTransform>(); board.sizeDelta = new Vector2(1600, 900);
            board.anchorMin = board.anchorMax = new Vector2(.5f, .5f); board.pivot = new Vector2(0, 1);
            oldScale = Time.timeScale; Time.timeScale = 0; locked = true;
            music=NativeMinigameAudio.Open(gameObject,session!=null&&session.IsExternal?9101:Plugin.Instance.GameId.Value);
            try { Control.ToggleActionMap(false); } catch (Exception) { }
            var bg = Rect(board, "AfternoonClassroom", 0, 0, 1600, 900).gameObject.AddComponent<RawImage>(); bg.texture = desk; bg.raycastTarget = false;
            physicalTable=NativeCardTable.Create(board);
            // A soft desk-wide glaze unifies painted background and UI lighting.
            Panel(board, 0, 0, 1600, 900, new Color(.12f,.16f,.13f,.09f), false);
            stateObject = Rect(board, "Round", 0, 0, 1600, 900).gameObject;
            Hint(stateObject.transform,"正在铺好课桌…",550,380,500, 60,26);
            NativeSkin.Load(()=>{if(closing||started)return;if(NativeSkin.TitleFont!=null)font=NativeSkin.TitleFont;skinReady=true;ClearState();Ready();if(autoBegin)Begin();Plugin.Instance.Log("UNO table prepared with native skin");});Fit();
        }
        void MakeSprites()
        {
            texture = new Texture2D(64, 64, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Bilinear;
            ovalTexture = new Texture2D(128,128,TextureFormat.RGBA32,false);
            for (int y=0;y<64;y++) for(int x=0;x<64;x++) {
                float dx=Math.Max(14-x,Math.Max(0,x-49)),dy=Math.Max(14-y,Math.Max(0,y-49));
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(14.5f-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            for(int y=0;y<128;y++) for(int x=0;x<128;x++) ovalTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(63.5f-Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)))));
            texture.Apply(); ovalTexture.Apply();
            rounded=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
            oval=Sprite.Create(ovalTexture,new Rect(0,0,128,128),new Vector2(.5f,.5f));
        }
        void Fit() { float s=Math.Min(Screen.width/1600f,Screen.height/900f); board.localScale=new Vector3(s,s,1);board.anchoredPosition=new Vector2(-800*s,450*s); }
        RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        Image Panel(Transform parent,float x,float y,float w,float h,Color color,bool round=true)
        {
            Image i=Rect(parent,"Paper",x,y,w,h).gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;if(round){i.sprite=rounded;i.type=Image.Type.Sliced;}return i;
        }
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            Text t=Rect(parent,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
        }
        Button Button(Transform parent,string text,float x,float y,float w,float h,Color bg,Color fg,Action action,ButtonSkin skin=ButtonSkin.Green)
        {
            var i=Panel(parent,x,y,w,h,bg);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;
            var sprite=NativeSkin.ButtonSprite(skin);if(sprite!=null){i.sprite=sprite;i.type=skin==ButtonSkin.White||skin==ButtonSkin.Subject?Image.Type.Sliced:Image.Type.Simple;i.color=skin==ButtonSkin.Subject?bg:skin==ButtonSkin.White?Hex("BE926F"):Color.white;}
            var txt=Label(i.transform,text,9,-2,w-18,h,Mathf.RoundToInt(Mathf.Min(30,h*.38f)),skin==ButtonSkin.Go?NativeSkin.TitleColor:Color.white,TextAnchor.MiddleCenter);
            if(NativeSkin.TitleFont!=null)txt.font=NativeSkin.TitleFont;
            Stroke(txt,.75f);
            var shadow=txt.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.26f,.16f,.08f,.6f);shadow.effectDistance=new Vector2(1.4f,-2);
            var colors=b.colors;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(.88f,.88f,.88f);colors.disabledColor=new Color(.6f,.6f,.6f,.8f);b.colors=colors;i.gameObject.AddComponent<NativeButtonMotion>();
            b.onClick.AddListener(()=>{try{action();}catch(Exception e){Plugin.Instance.LogError(e);}});return b;
        }
        Image PaperPanel(Transform parent,float x,float y,float w,float h){var p=Panel(parent,x,y,w,h,Color.white);if(NativeSkin.Paper!=null){p.sprite=NativeSkin.Paper;p.type=Image.Type.Sliced;}return p;}
        Text BodyLabel(Transform parent,string text,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){var label=Label(parent,text,x,y,w,h,size,color,align);if(NativeSkin.BodyFont!=null)label.font=NativeSkin.BodyFont;return label;}
        Text Stroke(Text text,float width=1f)
        {
            var edge=text.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.12f,.09f,.065f,.86f);edge.effectDistance=new Vector2(width,-width);edge.useGraphicAlpha=true;return text;
        }
        Image NoticePanel(Transform parent,float x,float y,float w,float h)
        {
            Panel(parent,x+3,y+5,w,h,new Color(0,0,0,.22f));
            var border=Panel(parent,x-2,y-2,w+4,h+4,Hex("B9A783"));
            var body=Panel(parent,x,y,w,h,new Color(.12f,.17f,.145f,.96f));
            if(NativeSkin.Tips!=null){border.sprite=NativeSkin.Tips;body.sprite=NativeSkin.Tips;}return body;
        }
        Text Hint(Transform parent,string text,float x,float y,float w,float h,int size=18)
        {
            var panel=NoticePanel(parent,x,y,w,h);
            return Stroke(BodyLabel(panel.transform,text,10,0,w-20,h,size,Cream,TextAnchor.MiddleCenter),.7f);
        }
        void ClearState() { foreach(Transform c in stateObject.transform){c.gameObject.SetActive(false);Destroy(c.gameObject);}handViews.Clear();badges.Clear();deckSheets.Clear();shuffleCards.Clear();displayedDrawCount=-1;handObject=null;popupObject=null; }
        void Ready()
        {
            Transform p=stateObject.transform;
            Label(p,"课间 · 最后一张",420,212,760,78,48,Cream,TextAnchor.MiddleCenter).gameObject.AddComponent<Outline>().effectDistance=new Vector2(2,-2);
            Button(p,"开始游戏",630,345,340,120,Ink,Cream,Begin);
            Button(p,"玩法说明",630,480,340,120,Ink,Cream,OpenGuide,ButtonSkin.Orange);
            if(session!=null&&!(session is IPracticeSession))Hint(p,session.IsExternal?"社交挑战 · 按当前关卡结算":"开局消耗 "+session.Cost.ToString("0.#")+" 信任 · 胜利基础好感 +2",450,633,700,46,21);
        }
        void OpenGuide(){if(started||guideObject!=null)return;guideObject=IllustratedGuide.Show(stateObject.transform,9101,()=>guideObject=null);}
        public void Begin()
        {
            if(started||!skinReady||guideObject!=null)return;var prepared=new UnoGame(Environment.TickCount,4,false);
            if(session!=null&&!session.Begin()){Label(stateObject.transform,"关系或信任发生变化，请关闭后重试。",410,625,790,30,20,Ink);return;}
            game=prepared;started=true;pause=MinigamePause.Attach(this,()=>started&&!closing&&!resultDrawn&&game.Result==Outcome.Playing,GiveUp,Restart,skipGame:SkipGame);ClearState();var p=stateObject.transform;
            identities[0]=CardSeatIdentity.Role(0);identities[1]=CardSeatIdentity.Classmate(0);identities[2]=session is ICardSeatSession?CardSeatIdentity.Role(((ICardSeatSession)session).NpcId):CardSeatIdentity.Classmate(1);identities[3]=CardSeatIdentity.Classmate(2);
            var ringRoot=Rect(p,"TurnDirection",800,423,540,300);ringRoot.pivot=new Vector2(.5f,.5f);directionRing=ringRoot.gameObject.AddComponent<TurnDirectionRing>();directionRing.raycastTarget=false;directionRing.SetDirection(game.Direction);
            pileLayer=Rect(p,"DiscardStack",0,0,1600,900);
            BuildDrawPile(p);SetDrawCount(game.DrawCount+game.PlayerCount*7);
            controlLayer=Rect(p,"ActionControls",0,0,1600,900);
            drawButton=Button(controlLayer,"摸一张",491,526,200, 70,Ink,Cream,()=>{if(!Animating){ClosePopup();if(game.PendingDrawIndex>=0){if(game.PassDrawn())Announce("你 · 过牌");}else game.DrawForPlayer();}});
            unoButton=Button(controlLayer,"报到 UNO",1268,742,240,88,Ink,Cream,()=>{if(game.CallUno())Announce("你 · 报到！");},ButtonSkin.Orange);
            SeatBadge(p,0,55,708);SeatBadge(p,1,45,224);SeatBadge(p,2,1085,122);SeatBadge(p,3,1317,224);
            AddPile(game.Top);effects=Rect(p,"FlyingCards",0,0,1600,900);
            actionText=Stroke(Label(effects,"",700,525,620,84,38,Cream,TextAnchor.MiddleCenter),2.5f);
            actionText.gameObject.name="ActionFlash";actionText.rectTransform.pivot=new Vector2(.5f,.5f);actionText.rectTransform.anchoredPosition=new Vector2(1010,-565);
            var shadow=actionText.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.12f,.07f,.035f,.8f);shadow.effectDistance=new Vector2(3,-5);
            actionGroup=actionText.gameObject.AddComponent<CanvasGroup>();actionGroup.blocksRaycasts=false;actionGroup.alpha=0;
            Redraw();openingDealAt=StudentAge.CampusUno.PlayClock.Now;dealUntil=openingDealAt+game.PlayerCount*7*.062f+.1f;
            foreach(var card in handViews){card.Deal(DrawOrigin,(card.Index*game.PlayerCount+card.Seat)*.062f,.058f);if(card.Seat==0){var back=MakeCard(card.transform,new Card(Suit.Wild,Face.Wild),true,new Vector2(80,115),1,0);back.Manual=true;card.DealBack=back.gameObject;}}
        }
        void BuildDrawPile(Transform parent)
        {
            drawPileLayer=Rect(parent,"DrawDeck",0,0,1600,900);deckShadow=Panel(drawPileLayer,DeckBase.x-72,DeckBase.y+19,145,29,new Color(.12f,.075f,.035f,.20f));deckShadow.sprite=oval;deckShadow.type=Image.Type.Simple;
            for(int i=0;i<32;i++){var image=Panel(drawPileLayer,DeckBase.x,DeckBase.y,160,230,i%2==0?Hex("D5C8AF"):Cream);var r=image.rectTransform;r.pivot=new Vector2(.5f,.5f);r.localScale=Vector3.one*.68f;r.localEulerAngles=new Vector3(55,0,-24);deckSheets.Add(r);}
            deckTop=MakeCard(drawPileLayer,new Card(Suit.Wild,Face.Wild),true,DeckBase,.68f,-24);deckTop.Manual=true;deckTop.Shadow.gameObject.SetActive(false);
        }
        void SetDrawCount(int count)
        {
            count=Math.Max(0,count);if(displayedDrawCount==count)return;displayedDrawCount=count;int layers=Math.Min(count,deckSheets.Count);float height=count*.30f;
            for(int i=0;i<deckSheets.Count;i++){deckSheets[i].gameObject.SetActive(i<layers);if(i<layers)deckSheets[i].anchoredPosition=new Vector2(DeckBase.x,-DeckBase.y+(layers==1?0:height*i/(layers-1)));}
            deckShadow.gameObject.SetActive(count>0);deckTop.gameObject.SetActive(count>0);if(count>0)deckTop.SetPose(DrawOrigin,.68f,-24,55);
        }
        void BeginReshuffle()
        {
            Announce("洗牌");shuffleTime=0;var old=pileLayer.Cast<Transform>().ToArray();for(int i=0;i<old.Length-1;i++){old[i].gameObject.SetActive(false);Destroy(old[i].gameObject);}pileCount=1;
            for(int i=0;i<Math.Min(9,currentMove.DrawCountAfter);i++){var card=MakeCard(effects,new Card(Suit.Wild,Face.Wild),true,new Vector2(809+(i%3-1)*9,432),.72f,(i-4)*4);card.Manual=true;card.From=new Vector2(809+(i%3-1)*9,432);shuffleCards.Add(card);}
            effects.SetAsLastSibling();controlLayer.SetAsLastSibling();
        }
        void AnimateReshuffle(float dt)
        {
            shuffleTime+=dt;for(int i=0;i<shuffleCards.Count;i++){float t=Mathf.Clamp01((shuffleTime-i*.028f)/.34f),e=Mathf.SmoothStep(0,1,t);var card=shuffleCards[i];var target=DeckBase-new Vector2(0,currentMove.DrawCountAfter*.30f);card.SetPose(Vector2.Lerp(card.From,target,e)+new Vector2(Mathf.Sin(t*Mathf.PI*2)*8,-Mathf.Sin(t*Mathf.PI)*45),Mathf.Lerp(.72f,.68f,e),Mathf.Lerp((i-4)*4,-24,e),55*e);}
            if(shuffleTime<.34f+(shuffleCards.Count-1)*.028f)return;foreach(var card in shuffleCards)Destroy(card.gameObject);shuffleCards.Clear();SetDrawCount(currentMove.DrawCountAfter);
        }
        void SeatBadge(Transform parent,int seat,float x,float y){badges.Add(NativeSeatBadge.Create(parent,identities[seat],x,y,seat==0?208:238));}
        Vector2 SeatPoint(int seat){return seat==0?new Vector2(800,743):seat==1?new Vector2(300,479):seat==2?new Vector2(800,242):new Vector2(1300,479);}
        CardVisual MakeCard(Transform parent,Card card,bool back,Vector2 position,float scale,float angle,Action click=null,bool legal=false)
        {
            var root=Rect(parent,"PhysicalCard",position.x,position.y,160,230);root.pivot=new Vector2(.5f,.5f);root.localScale=Vector3.one*scale;root.localEulerAngles=new Vector3(0,0,angle);
            var shadow=Panel(root,9,13,166,234,new Color(.09f,.06f,.035f,.24f));
            Panel(root,3,7,160,230,Hex("AD9B7E"));Panel(root,1,4,160,230,Hex("E5D3AE"));
            if(legal)Panel(root,-3,-3,166,236,Gold);
            var rim=Panel(root,0,0,160,230,Cream);var inner=Panel(root,7,7,146,216,back?Ink:Colors[(int)card.Suit]);
            if(back)
            {
                Panel(root,13,13,134,204,Hex("85ABA0"));Panel(root,16,16,128,198,Ink);
                for(int j=-2;j<9;j++){var line=Panel(root,20,40+j*20,120,1,new Color(.7f,.85f,.72f,.14f),false);}
                var emblem=Panel(root,37,67,86,96,Cream);emblem.rectTransform.localEulerAngles=new Vector3(0,0,-12);
                Label(emblem.transform,"课\n间",4,3,78,88,29,Ink,TextAnchor.MiddleCenter);
                Label(root,"最后一张",24,183,112,24,15,Cream,TextAnchor.MiddleCenter);
            }
            else
            {
                for(int j=0;j<6;j++)Panel(root,15,65+j*23,130,1,new Color(1,1,1,.09f),false);
                var cornerMark=Stroke(Label(root,card.Mark,15,9,62,42,30,Cream),1.1f); if(card.Face<=Face.Nine || card.Face==Face.DrawTwo || card.Face==Face.DrawFour){cornerMark.font=numberFont;cornerMark.fontStyle=FontStyle.Bold;}
                Stroke(Label(root,card.IsWild?"选修":Subjects[(int)card.Suit],91,18,52,29,16,Cream,TextAnchor.MiddleRight),.65f);
                var lozenge=Panel(root,29,50,103,129,Cream);lozenge.sprite=oval;lozenge.type=Image.Type.Simple;lozenge.rectTransform.pivot=new Vector2(.5f,.5f);lozenge.rectTransform.anchoredPosition=new Vector2(80,-115);lozenge.rectTransform.localEulerAngles=new Vector3(0,0,-29);
                var bigMark=Label(root,card.Mark,17,58,126,114,card.Mark.Length>1?58:79,card.Suit==Suit.Yellow?Hex("81510C"):Colors[(int)card.Suit],TextAnchor.MiddleCenter);if(card.Face<=Face.Nine || card.Face==Face.DrawTwo || card.Face==Face.DrawFour){bigMark.font=numberFont;bigMark.fontStyle=FontStyle.Bold;}
                Stroke(Label(root,card.Title,10,187,140,27,17,Cream,TextAnchor.MiddleCenter),.85f);
                if(card.IsWild)for(int j=0;j<4;j++)Panel(root,42+j*20,169,17,6,Colors[j]);
            }
            // Subtle edge glint, printed highlights, and three offset layers imply card stock.
            Panel(root,12,10,132,2,new Color(1,1,1,.5f),false);
            var shine=Panel(root,11,42,138,26,new Color(1,1,1,.07f));
            var v=root.gameObject.AddComponent<CardVisual>();v.Setup(root,shadow.rectTransform,position,scale,angle);v.Interactive=click!=null;v.Legal=legal;
            if(click!=null){v.HoverSound=()=>music?.Effect("slide",.16f);rim.raycastTarget=true;var button=rim.gameObject.AddComponent<Button>();button.targetGraphic=rim;button.onClick.AddListener(()=>{if(!Animating&&!resultDrawn&&popupObject==null)click();});}
            return v;
        }
        void Redraw()
        {
            revision=game.Revision;
            if(game.Moves.Count==0 && flying==null && (shownTop.Face!=game.Top.Face || shownTop.Suit!=game.Top.Suit))AddPile(game.Top);
            var oldPositions=new Dictionary<string,Queue<Vector2>>();
            foreach(var view in handViews.Where(v=>v!=null && v.gameObject.activeSelf)){
                string key=view.Seat+":"+view.Value.ToString();if(!oldPositions.ContainsKey(key))oldPositions[key]=new Queue<Vector2>();oldPositions[key].Enqueue(view.Position);
            }
            if(handObject!=null){handObject.SetActive(false);Destroy(handObject);} handViews.Clear();
            handObject=Rect(stateObject.transform,"Hands",0,0,1600,900).gameObject;
            // Card fans always fit; even large penalty hands remain individually selectable.
            for(int seat=0;seat<game.PlayerCount;seat++)
            {
                int n=game.Hand(seat).Count;float span=seat==0?Math.Min(910,(n-1)*93):Math.Min(seat==2?320:340,(n-1)*57);
                for(int i=0;i<n;i++)
                {
                    float u=n<=1?0:(i/(float)(n-1)-.5f);Vector2 center=SeatPoint(seat);float angle;float scale;
                    if(seat==0){center+=new Vector2(u*span,Mathf.Abs(u)*48);angle=-u*27;scale=.97f;}
                    else if(seat==2){scale=.61f;center=new Vector2(800+u*span,245);angle=0;}
                    else{scale=.67f;center=new Vector2((seat==1?300:1300)+u*span,475);angle=0;}
                    if(seat!=0)center.y+=(((int)game.Hand(seat)[i].Face*13+(int)game.Hand(seat)[i].Suit*7+i*5+seat)%7-3)*.8f;
                    int index=i;int currentSeat=seat;
                    var v=MakeCard(handObject.transform,game.Hand(seat)[i],seat!=0,center,scale,angle,seat==0?(Action)(()=>Choose(index)):null,seat==0&&game.CanPlay(0,i));
                    v.Seat=seat;v.Index=i;v.Value=game.Hand(seat)[i];v.Tilt=0;v.Standing=seat!=0;v.Yaw=0;if(v.Standing){v.Shadow.sizeDelta=new Vector2(148,10);v.Shadow.GetComponent<Image>().color=new Color(.09f,.06f,.035f,.16f);v.Shadow.GetComponent<Image>().sprite=oval;v.Shadow.GetComponent<Image>().type=Image.Type.Simple;
                        foreach(var graphic in v.GetComponentsInChildren<Graphic>()){var project=graphic.gameObject.AddComponent<StandingCardProjection>();project.Root=(RectTransform)handObject.transform;project.Owner=v;project.Center=new Vector2(seat==2?800:seat==1?300:1300,seat==2?-245:-475);project.Width=span+160*scale;project.Height=230*scale;project.Yaw=seat==1?42:seat==3?-42:0;}

                    }handViews.Add(v);
                    string key=seat+":"+v.Value.ToString();if(oldPositions.ContainsKey(key) && oldPositions[key].Count>0)v.Reflow(oldPositions[key].Dequeue());
                }
                if(seat==1&&n>0){var row=handViews.Where(c=>c.Seat==seat).ToArray();int first=row[0].transform.GetSiblingIndex();for(int j=0;j<row.Length;j++)row[row.Length-1-j].transform.SetSiblingIndex(first+j);}
                badges[seat].UpdateInfo("",n,game.Turn==seat);
            }
            effects.SetAsLastSibling();controlLayer.SetAsLastSibling();if(popupObject!=null)popupObject.transform.SetAsLastSibling();
        }
        void AddPile(Card card)
        {
            music?.Effect("slap",.42f);shownTop=card;pileCount++;float angle=(pileCount%5-2)*8;
            var v=MakeCard(pileLayer,card,false,new Vector2(809+(pileCount%3-1)*4,432),.79f,angle);v.Tilt=17;v.Land();
            if(card.IsWild){colorEdge=Panel(v.transform,14,211,132,8,Colors[(int)game.Color]);}
            if(pileLayer.childCount>5)Destroy(pileLayer.GetChild(0).gameObject);
        }
        void Update()
        {
            if(closing||board==null||PlayClock.Paused)return;Fit();if(!started||game==null||resultDrawn)return;
            float dt=StudentAge.CampusUno.PlayClock.Delta;if(directionRing!=null)directionRing.SetDirection(game.Direction);
            if(StudentAge.CampusUno.PlayClock.Now>=dealUntil)game.Tick(dt,!Animating);
            if(StudentAge.CampusUno.PlayClock.Now<dealUntil){int dealt=Mathf.Clamp(Mathf.FloorToInt((StudentAge.CampusUno.PlayClock.Now-openingDealAt)/.062f)+1,0,game.PlayerCount*7);SetDrawCount(game.DrawCount+game.PlayerCount*7-dealt);if(dealt!=soundedDeal){soundedDeal=dealt;music?.Effect("slide",.17f);}}
            if(StudentAge.CampusUno.PlayClock.Now>=dealUntil&&!Animating)SetDrawCount(game.DrawCount);
            AnimateMoves(dt);
            if(!Animating&&revision!=game.Revision){wildIndex=-1;ClosePopup();Redraw();}
            AnimateAction();
            for(int i=0;i<badges.Count;i++)badges[i].UpdateInfo("",game.Hand(i).Count,game.Turn==i);
            drawButton.GetComponentInChildren<Text>().text=game.PendingDrawIndex>=0?"过牌":"摸一张";
            drawButton.interactable=game.Turn==0&&!Animating&&game.Result==Outcome.Playing;
            unoButton.interactable=game.Result==Outcome.Playing&&game.Player.Count<=2&&(game.Turn==0||game.NeedsUno);
            if(game.Result!=Outcome.Playing&&!Animating&&!resultDrawn)ShowResult();
        }
        void Announce(string words)
        {
            if(actionText==null)return;actionText.text=words;actionAt=StudentAge.CampusUno.PlayClock.Now;actionText.transform.SetAsLastSibling();
        }
        void AnimateAction()
        {
            actionText.transform.SetAsLastSibling();float t=StudentAge.CampusUno.PlayClock.Now-actionAt;actionGroup.alpha=t<.1f?Mathf.Clamp01(t/.1f):1-Mathf.Clamp01((t-.9f)/.45f);
            float pop=t<.16f?Mathf.Lerp(.68f,1.10f,t/.16f):Mathf.Lerp(1.10f,1,Mathf.Clamp01((t-.16f)/.18f));
            actionText.rectTransform.localScale=Vector3.one*pop;actionText.rectTransform.anchoredPosition=new Vector2(1010,-565+Mathf.Clamp01((t-.8f)/.55f)*28);
        }
        void AnimateMoves(float dt)
        {
            if(StudentAge.CampusUno.PlayClock.Now<dealUntil)return;
            if(shuffleCards.Count>0){AnimateReshuffle(dt);return;}
            if(flying==null&&game.Moves.Count>0)
            {
                currentMove=game.Moves.Dequeue();flightTime=0;music?.Effect(currentMove.Reshuffle?"shuffle":"slide",.35f);
                if(currentMove.Reshuffle){BeginReshuffle();return;}
                if(!currentMove.Draw){Announce(SeatName(currentMove.Seat)+" · "+(currentMove.Card.Face<=Face.Nine?"打出 "+currentMove.Card.Mark:currentMove.Card.Title));lastDrawSeat=-1;}
                else if(lastDrawSeat!=currentMove.Seat || StudentAge.CampusUno.PlayClock.Now-lastDrawAt>1){Announce(SeatName(currentMove.Seat)+" · 摸牌");lastDrawSeat=currentMove.Seat;lastDrawAt=StudentAge.CampusUno.PlayClock.Now;}
                Vector2 from=currentMove.Draw?DrawOrigin:SeatPoint(currentMove.Seat);
                if(currentMove.Draw&&currentMove.DrawCountAfter>=0)SetDrawCount(currentMove.DrawCountAfter);
                var existing=handViews.FirstOrDefault(v=>v.Seat==currentMove.Seat&&v.Index==currentMove.Index);
                if(!currentMove.Draw&&existing!=null){var anchored=((RectTransform)existing.transform).anchoredPosition;from=new Vector2(anchored.x,-anchored.y);existing.gameObject.SetActive(false);}
                flying=MakeCard(effects,currentMove.Card,currentMove.Draw&&currentMove.Seat!=0,from,currentMove.Seat==0?.97f:.52f,0);
                flying.Manual=true;flying.From=from;
                flightBack=null;
                if((currentMove.Draw && currentMove.Seat==0)||(!currentMove.Draw && currentMove.Seat!=0)){
                    var back=MakeCard(flying.transform,currentMove.Card,true,new Vector2(80,115),1,0);back.Manual=true;flightBack=back.gameObject;
                }effects.SetAsLastSibling();controlLayer.SetAsLastSibling();if(popupObject!=null)popupObject.transform.SetAsLastSibling();
            }
            if(flying==null)return;
            flightTime+=dt;float duration=currentMove.Draw?.34f:.48f;float t=Mathf.Clamp01(flightTime/duration),e=1-Mathf.Pow(1-t,3);
            Vector2 target=currentMove.Draw?SeatPoint(currentMove.Seat):new Vector2(809,432);
            Vector2 pos=Vector2.Lerp(flying.From,target,e)+new Vector2(0,-Mathf.Sin(t*Mathf.PI)*(currentMove.Draw?68:135));
            float start=currentMove.Draw?.68f:currentMove.Seat==0?.97f:.52f;float end=currentMove.Draw?(currentMove.Seat==0?.97f:.52f):.79f;
            float scale=Mathf.Lerp(start,end,e)+Mathf.Sin(t*Mathf.PI)*.14f;
            flying.SetPose(pos,scale,Mathf.Lerp(currentMove.Draw?-24:currentMove.Seat==1?-68:currentMove.Seat==3?68:0,(pileCount%5-1)*8,e),Mathf.Sin(t*Mathf.PI)*-22);
            if(flightBack!=null){flightBack.SetActive(t<.5f);var fs=flying.transform.localScale;fs.x*=Mathf.Max(.06f,Mathf.Abs(Mathf.Cos(t*Mathf.PI)));flying.transform.localScale=fs;}
            flying.Shadow.anchoredPosition=new Vector2(9+Mathf.Sin(t*Mathf.PI)*19,-13-Mathf.Sin(t*Mathf.PI)*26);
            if(t>=1){if(!currentMove.Draw)AddPile(currentMove.Card);Destroy(flying.gameObject);flying=null;if(game.Moves.Count==0)Redraw();}
        }
        void Choose(int index)
        {
            if(Animating||!game.CanPlay(0,index))return;
            if(!game.Player[index].IsWild){game.Play(0,index,game.Player[index].Suit);return;}
            ClosePopup();wildIndex=index;popupObject=Rect(stateObject.transform,"ChooseSubject",0,0,1600,900).gameObject;
            var panel=PaperPanel(popupObject.transform,412,321,776,200);panel.raycastTarget=true;
            Label(panel.transform,"选择科目",20,10,736,44,28,Ink,TextAnchor.MiddleCenter);
            for(int i=0;i<4;i++){Suit chosen=(Suit)i;Button(panel.transform,Subjects[i],23+i*187, 70,169, 70,Colors[i],Cream,()=>{game.Play(0,wildIndex,chosen);wildIndex=-1;ClosePopup();},ButtonSkin.Subject);}

        }
        void ClosePopup(){if(popupObject!=null){popupObject.SetActive(false);Destroy(popupObject);popupObject=null;}}
        void ShowResult()
        {
            resultDrawn=true;actionGroup.alpha=0;actionText.gameObject.SetActive(false);ClosePopup();
            Plugin.Instance.Log("UNO round result: "+game.Result+" | time "+game.Elapsed.ToString("0.0"));
            NativeMinigameResult.Show(game.Result,canvasObject,oldScale,Close);
        }

        void SkipGame(){if(closing||resultDrawn||!started||game.Result!=Outcome.Playing)return;resultDrawn=true;var saved=session;session=null;CloseInternal(false);saved?.Finish(Outcome.Win);}
        void Restart(){var saved=session;session=null;CloseInternal(false);Plugin.OpenExternal(opponent,saved,true);}
        void GiveUp(){if(resultDrawn||closing)return;resultDrawn=true;NativeMinigameResult.Show(Outcome.Lose,canvasObject,oldScale,()=>{var saved=session;session=null;CloseInternal(false);saved?.Finish(Outcome.Lose);});}
        public void Abort(){CloseInternal(false);}void Close(){CloseInternal(true);}
        void CloseInternal(bool settle)
        {
            if(closing)return;closing=true;pause?.Dispose();if(canvasObject!=null){canvasObject.SetActive(false);Destroy(canvasObject);}Unlock();
            if(Plugin.Instance!=null&&Plugin.Instance.Table==this)Plugin.Instance.Table=null;
            try{if(session!=null){if(settle&&started&&game!=null&&game.Result!=Outcome.Playing)session.Finish(game.Result);else session.Cancel();}}finally{Destroy(this);}
        }
        void Unlock(){if(!locked)return;locked=false;Time.timeScale=oldScale;if(music!=null)music.Close();try{Control.ToggleActionMap(true);}catch(Exception){}}
        void OnDestroy(){if(!closing&&session!=null)session.Cancel();Unlock();if(canvasObject!=null)Destroy(canvasObject);if(rounded!=null)Destroy(rounded);if(oval!=null)Destroy(oval);if(texture!=null)Destroy(texture);if(ovalTexture!=null)Destroy(ovalTexture);if(desk!=null)Destroy(desk);}
        internal static Color Hex(string value){Color c;ColorUtility.TryParseHtmlString("#"+value,out c);return c;}
    }
    internal sealed class CardVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform rt;public GameObject DealBack;public RectTransform Shadow;public Vector2 Position,From;public Action HoverSound;public bool Interactive,Legal,Manual;public int Seat,Index;public Card Value;public float Tilt,Yaw;public bool Standing;
        CanvasGroup dealVisibility;public bool IsInMotion=>dealStart>=0||reflowStart>=0;public bool IsDealing=>dealStart>=0;
        float scale,angle,hover,dealStart=-1,dealDuration,landStart=-1,reflowStart=-1;Vector2 dealFrom,reflowFrom;bool hovering;int homeSibling;
        public void Setup(RectTransform rect,RectTransform shadow,Vector2 position,float s,float a){rt=rect;Shadow=shadow;Position=position;scale=s;angle=a;}
        public void Reflow(Vector2 from){reflowFrom=from;reflowStart=StudentAge.CampusUno.PlayClock.Now;}
        void Start(){homeSibling=transform.GetSiblingIndex();}
        public void Land(){landStart=StudentAge.CampusUno.PlayClock.Now;}
        public void Deal(Vector2 from,float delay,float duration){dealVisibility=gameObject.AddComponent<CanvasGroup>();dealVisibility.alpha=0;dealVisibility.blocksRaycasts=false;dealFrom=from;dealStart=StudentAge.CampusUno.PlayClock.Now+delay;dealDuration=duration;SetPose(from,.3f,0,0);}
        public void SetPose(Vector2 p,float s,float a,float tilt){rt.anchoredPosition=new Vector2(p.x,-p.y);rt.localScale=Vector3.one*s;rt.localEulerAngles=new Vector3(tilt,Yaw,a);}
        public void OnPointerEnter(PointerEventData e){hovering=Interactive;if(hovering){HoverSound?.Invoke();transform.SetAsLastSibling();}}
        public void OnPointerExit(PointerEventData e){hovering=false;if(Interactive)transform.SetSiblingIndex(homeSibling);}
        void Update()
        {
            if(Manual)return;
            if(dealStart>=0){if(StudentAge.CampusUno.PlayClock.Now<dealStart)return;dealVisibility.alpha=1;float t=Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-dealStart)/dealDuration);float e=1-Mathf.Pow(1-t,3);SetPose(Vector2.Lerp(dealFrom,Position,e)+new Vector2(0,-Mathf.Sin(t*Mathf.PI)*28),Mathf.Lerp(.3f,scale,e),angle*e,Tilt);if(DealBack!=null){float flip=Mathf.Clamp01((t-.55f)/.45f);DealBack.SetActive(flip<.5f);var s=rt.localScale;s.x*=Mathf.Max(.04f,Mathf.Abs(Mathf.Cos(flip*Mathf.PI)));rt.localScale=s;}if(t<1)return;if(DealBack!=null){Destroy(DealBack);DealBack=null;}dealStart=-1;dealVisibility.blocksRaycasts=true;}
            float land=landStart<0?0:Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-landStart)/.25f);
            float bounce=land>0 && land<1?Mathf.Sin(land*Mathf.PI)*.055f:0;
            hover=Mathf.Lerp(hover,hovering?1:0,Mathf.Clamp01(StudentAge.CampusUno.PlayClock.Delta*17));
            Vector2 resting=Position;if(reflowStart>=0){float t=Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-reflowStart)/.22f);resting=Vector2.Lerp(reflowFrom,Position,1-Mathf.Pow(1-t,3));if(t>=1)reflowStart=-1;}
            SetPose(resting+new Vector2(0,-hover*49),scale*(1+hover*.09f-bounce),angle*(1-hover*.7f),Tilt-hover*8);
            Shadow.anchoredPosition=Standing?new Vector2(2,-224):new Vector2(9+hover*7,-13-hover*12);
        }
    }
}
