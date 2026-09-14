using System;using System.Linq;using System.IO;using System.Collections;using System.Collections.Generic;
using UnityEngine;using UnityEngine.UI;using StudentAge.CampusUno;using StudentAge.Mahjong;using EC2BUnofficialPatch.Features.Mechanics.Minigames;
namespace StudentAge.CampusMinigames
{
    public sealed class MahjongEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<MahjongView>(c,9112);}
    public sealed class MahjongView:PuzzleFrame
    {
        public MahjongGame Engine{get;private set;}public bool InputReady=>Started&&!busy&&!Ending;
        MahjongArt art;RectTransform wallRoot,riverRoot,flyRoot,controls;readonly RectTransform[] handRoots=new RectTransform[4];
        readonly List<MahjongTileView>[] hands=Enumerable.Range(0,4).Select(_=>new List<MahjongTileView>()).ToArray();
        readonly MahjongTileView[] wallViews=new MahjongTileView[136];readonly NativeSeatBadge[] seats=new NativeSeatBadge[4];readonly Text[] words=new Text[4];readonly float[] wordUntil=new float[4];readonly int[] shown=new int[4];
        bool busy;float nextAi;int selected=-1;string controlsKey="";Text turnLabel;Image turnPaper;
        protected override string Title=>"课间麻将";
        protected override string[] Rules=>new[]{"四人一局，136张万、条、筒与字牌；没有花牌。","你坐东位先手，摸一张，再打出一张。","点击选牌，点“出牌”或再次点击该牌打出。","四组顺子或刻子，加一对将，即可胡牌。","顺子是同门连续三张；刻子是三张相同的牌。","七种不同对子组成的七对也能胡；不计番数。","只吃上家的牌；碰、明杠和胡可接任意一家。","胡优先于碰杠，碰杠优先于吃；同时胡取近家。","四张相同可暗杠；已碰的牌可补杠，补杠可被抢胡。","杠后从牌尾补一张；牌墙抽完流局。","能吃碰杠胡时会出现按钮，点“过”可放弃。","提示只帮你选牌，不会自动打出；没有操作倒计时。","你先胡牌则本次社交胜利，别人先胡或流局不算胜利。","每次只打一局，五个内部阶段逐步提高电脑策略。"};
        protected override IEnumerator LoadAssets(){art=new MahjongArt(Path.Combine(Session.Root,"Mahjong"));Engine=new MahjongGame(Level,Environment.TickCount);yield break;}
        protected override void BuildPlay()
        {
            var table=R(PlayRoot,"MahjongTable",0,0,1600,900).gameObject.AddComponent<RawImage>();table.texture=art.Table;table.raycastTarget=false;
            wallRoot=R(PlayRoot,"TwoTierWall",0,0,1600,900);riverRoot=R(PlayRoot,"DiscardedTiles",0,0,1600,900);
            for(int n=0;n<4;n++)handRoots[n]=R(PlayRoot,"Hand-"+n,0,0,1600,900);flyRoot=R(PlayRoot,"Melds",0,0,1600,900);flyRoot.SetSiblingIndex(3);
            art.Scene=RoundedMahjongScene.Create(PlayRoot,art);
            for(int n=0;n<136;n++){var pos=WallPoint(n);float angle=(n/34)%2==0?0:90;wallViews[n]=MahjongTileView.Create(wallRoot,art,0,pos.x,pos.y,58,false,true,angle,0,n%2*24.36f);}
            // Back rows first, then front rows. Every stack's upper tile covers its lower tile.
            foreach(int stack in Enumerable.Range(0,68).OrderBy(n=>WallPoint(n*2).y)){wallViews[stack*2].transform.SetAsLastSibling();wallViews[stack*2+1].transform.SetAsLastSibling();}
            seats[0]=MakeSeat(CardSeatIdentity.Role(0),30,674);seats[1]=MakeSeat(Session.NpcId<0?CardSeatIdentity.Classmate(2):CardSeatIdentity.Role(Session.NpcId),1390,235);seats[2]=MakeSeat(CardSeatIdentity.Classmate(0),1125,25);seats[3]=MakeSeat(CardSeatIdentity.Classmate(1),25,235);
            for(int n=0;n<4;n++){Vector2 p=SpeechPoint(n);words[n]=T(PlayRoot,"",p.x-140,p.y,280,60,42);words[n].color=Cream;var edge=words[n].gameObject.AddComponent<Outline>();edge.effectColor=new Color(.17f,.27f,.21f);edge.effectDistance=new Vector2(2,-2);}
            turnPaper=Paper(PlayRoot,739,415,122,58);turnPaper.color=new Color(.93f,.91f,.77f);turnLabel=T(PlayRoot,"",741,418,118,50,26);
            controls=R(PlayRoot,"MahjongActions",0,0,1600,900);controls.SetAsLastSibling();
            foreach(Transform child in Root){var t=child.GetComponent<Text>();if(t!=null&&t.text==Title)child.gameObject.SetActive(false);}
        }
        NativeSeatBadge MakeSeat(CardSeatIdentity id,float x,float y)
        {
            var badge=NativeSeatBadge.Create(PlayRoot,id,x,y,202);badge.transform.localScale=Vector3.one*.88f;
            foreach(string name in new[]{"CountBack","CountFront","CardCount"})badge.transform.Find(name).gameObject.SetActive(false);
            foreach(string name in new[]{"PortraitPaper","RolePortrait"}){var rect=(RectTransform)badge.transform.Find(name);rect.anchoredPosition+=new Vector2(38,0);}return badge;
        }
        Vector2 SpeechPoint(int seat)=>seat==0?new Vector2(800,616):seat==1?new Vector2(1205,444):seat==2?new Vector2(800,263):new Vector2(395,444);
        public static Vector2 WallPoint(int index)
        {int side=index/34,n=index%34/2;return side==0?MahjongProjection.Project(-480+n*60,510):side==1?MahjongProjection.Project(575,480-n*60):side==2?MahjongProjection.Project(480-n*60,-510):MahjongProjection.Project(-575,-480+n*60);}
        Vector2 HandPoint(int seat,int n,int count,int tileId=-1)
        {
            float j=n-(count-1)*.5f,extra=seat==0&&tileId==Engine.LastDraw&&tileId>=0?12:0;
            return seat==0?MahjongProjection.Project(j*60+extra,670):seat==2?MahjongProjection.Project(-j*60,-650):seat==1?MahjongProjection.Project(650,-360+n*60):MahjongProjection.Project(-650,-360+n*60);
        }
        static float SeatAngle(int seat)=>seat==0?0:seat==1?-90:seat==2?180:90;
        protected override void StartGame(){StartCoroutine(Deal());}
        IEnumerator Deal()
        {
            busy=true;RefreshHands();SetWall(0,135);
            for(int n=0;n<53;n++)
            {
                int seat=n<52?n%4:0,index=shown[seat]++,id=Engine.Wall[n];var from=WallPoint(n);var p=HandPoint(seat,index,Engine.Hands[seat].Count,id);
                var tile=MahjongTileView.Create(handRoots[seat],art,id,p.x,p.y,58,true,seat!=0,SeatAngle(seat));tile.Clicked=SelectTile;hands[seat].Add(tile);tile.FlyFrom(from,0,.06f,18);SetWall(n+1,135);if(n%4==0)TableSound("tile-draw",.23f);yield return new StudentAge.CampusUno.PlayDelay(.05f);
            }
            yield return new StudentAge.CampusUno.PlayDelay(.22f);busy=false;RefreshHands();nextAi=StudentAge.CampusUno.PlayClock.Now+.55f;RefreshControls(true);
        }
        void SetWall(int head,int tail){for(int n=0;n<136;n++)wallViews[n].gameObject.SetActive(n>=head&&n<=tail);}
        void RefreshHands(int incomingSeat=-1,Vector2 origin=default,bool deal=false,int incomingId=-1)
        {
            for(int seat=0;seat<4;seat++)
            {
                var previous=hands[seat].Where(v=>v!=null).ToDictionary(v=>v.TileId,v=>new Vector2(v.rectTransform.anchoredPosition.x,-v.rectTransform.anchoredPosition.y));Clear(handRoots[seat]);hands[seat].Clear();int count=deal||busy&&!Started?Math.Min(shown[seat],Engine.Hands[seat].Count):shown[seat];count=Math.Min(count,Engine.Hands[seat].Count);
                var ordered=Engine.Hands[seat].ToList();if(seat==0&&Engine.State==Phase.Discard&&Engine.Turn==0&&ordered.Remove(Engine.LastDraw))ordered.Add(Engine.LastDraw);
                for(int n=0;n<count;n++)
                {
                    int tile=ordered[n];var p=HandPoint(seat,n,count,tile);bool hide=seat!=0&&!(Engine.State==Phase.Finished&&Engine.Winner==seat);float w=58;
                    var v=MahjongTileView.Create(handRoots[seat],art,tile,p.x,p.y,w,true,hide,SeatAngle(seat),0);v.Clicked=SelectTile;v.Select(selected==tile&&seat==0);hands[seat].Add(v);
                    if(seat==incomingSeat&&(tile==incomingId||deal&&n==count-1))v.FlyFrom(origin,0,deal?.14f:.3f,deal?15:45);
                    else if(previous.ContainsKey(tile)&&!deal)v.FlyFrom(previous[tile],0,.16f,0);
                }
                foreach(var tileView in hands[seat].OrderBy(v=>-v.rectTransform.anchoredPosition.y))tileView.transform.SetAsLastSibling();
            }
        }
        Vector2 RiverPoint(int seat,int n)
        {int row=n/6,col=n%6;float step=Math.Min(82,246f/Math.Max(1,(Engine.Rivers[seat].Count+5)/6-1));return seat==0?MahjongProjection.Project(-150+col*60,165+row*step):seat==2?MahjongProjection.Project(150-col*60,-165-row*step):seat==1?MahjongProjection.Project(240+row*step,-150+col*60):MahjongProjection.Project(-240-row*step,150-col*60);}
        void RefreshTable(List<Move> moves,Dictionary<int,Vector2> positions)
        {
            for(int n=0;n<4;n++)shown[n]=Engine.Hands[n].Count;var draw=moves.LastOrDefault(m=>m.Kind=="摸牌");RefreshHands(draw==null?-1:draw.Seat,WallPoint(draw==null?Math.Min(135,Math.Max(0,Engine.Head-1)):Array.IndexOf(Engine.Wall,draw.Tile)),false,draw==null?-1:draw.Tile);
            Clear(riverRoot);for(int seat=0;seat<4;seat++)for(int n=0;n<Engine.Rivers[seat].Count;n++)
            {
                var record=Engine.Rivers[seat][n];if(record.Taken)continue;var p=RiverPoint(seat,n);var tile=MahjongTileView.Create(riverRoot,art,record.Tile,p.x,p.y,58,false,false,seat==0?0:seat==2?180:seat==1?90:-90);
                if(moves.Any(m=>m.Kind=="出牌"&&m.Tile==record.Tile)){Vector2 from;if(!positions.TryGetValue(record.Tile,out from))from=HandPoint(seat,5,13);tile.FlyFrom(from,0,.31f,65,1.12f);}
            }
            Clear(flyRoot);for(int seat=0;seat<4;seat++){float tileNo=0;foreach(var meld in Engine.Melds[seat]){for(int j=0;j<meld.Tiles.Count;j++)
            {
                int id=meld.Tiles[j];float slot=tileNo+Math.Min(j,2);if(j==3)slot=tileNo+1;Vector2 p=seat==0?MahjongProjection.Project(-430+slot*60,578):seat==2?MahjongProjection.Project(430-slot*60,-578):seat==1?MahjongProjection.Project(650,420-slot*60):MahjongProjection.Project(-650,420-slot*60);
                var v=MahjongTileView.Create(flyRoot,art,id,p.x,p.y,58,false,meld.Concealed&&seat!=0&&(j==0||j==3),seat==0?0:seat==2?180:seat==1?90:-90,0,j==3?24.36f:0);
            }tileNo+=3.25f;}}
            SetWall(Engine.Head,Engine.Tail);
            foreach(var move in moves)
            {
                if(move.Kind=="摸牌"||move.Kind=="出牌")TableSound(move.Kind=="摸牌"?"tile-draw":"tile-place",move.Seat==0?.55f:.4f);
                else{words[move.Seat].text=move.Kind;wordUntil[move.Seat]=StudentAge.CampusUno.PlayClock.Now+1.35f;TableSound("tile-meld",.6f);}
            }
            RefreshControls(true);if(Engine.State==Phase.Finished)StartCoroutine(EndRound());
        }
        void Change(Action action)
        {
            var positions=hands.SelectMany(a=>a).ToDictionary(v=>v.TileId,v=>new Vector2(v.rectTransform.anchoredPosition.x,-v.rectTransform.anchoredPosition.y));action();selected=-1;
            var moves=new List<Move>();while(Engine.Events.Count>0)moves.Add(Engine.Events.Dequeue());RefreshTable(moves,positions);nextAi=StudentAge.CampusUno.PlayClock.Now+.55f;
        }
        public void SelectTile(int id){if(!InputReady||Engine.State!=Phase.Discard||Engine.Turn!=0||hands[0].Any(v=>v.Moving))return;if(selected==id){HumanDiscard();return;}selected=id;foreach(var v in hands[0])v.Select(v.TileId==id);TableSound("slide",.18f);RefreshControls(true);}
        public void HumanDiscard(){if(!InputReady||Engine.State!=Phase.Discard||Engine.Turn!=0||selected<0)return;int tile=selected;Change(()=>Engine.Discard(0,tile));}
        public void HumanCall(Call call,int chi=-1){if(!InputReady||!Engine.Awaiting(0))return;Change(()=>Engine.Respond(0,call,chi));}
        public void HumanHu(){if(InputReady)Change(()=>Engine.Hu(0));}
        public void HumanKong(int kind){if(InputReady)Change(()=>Engine.Kong(0,kind));}
        public void Hint(){if(!InputReady||Engine.Turn!=0||Engine.State!=Phase.Discard)return;selected=Engine.ChooseDiscard(0,5);foreach(var v in hands[0])v.Select(v.TileId==selected);RefreshControls(true);}
        void RefreshControls(bool force=false)
        {
            bool own=InputReady&&Engine.Turn==0&&Engine.State==Phase.Discard;
            foreach(var v in hands[0])v.raycastTarget=own&&!v.Moving;
            for(int n=0;n<4;n++)seats[n].UpdateInfo("",Engine.Hands[n].Count,Engine.State!=Phase.Finished&&Engine.Turn==n);
            turnLabel.text=busy?"发牌":Engine.State==Phase.Finished?"终局":new[]{"东","南","西","北"}[Engine.Turn];
            string key=busy+"/"+Engine.State+"/"+Engine.Turn+"/"+selected+"/"+Engine.Awaiting(0);if(!force&&key==controlsKey)return;controlsKey=key;Clear(controls);if(!InputReady)return;
            if(own)
            {
                var actions=new List<Action>();var labels=new List<string>();labels.Add("提示");actions.Add(Hint);
                if(Engine.CanHu(0)){labels.Add("自摸");actions.Add(HumanHu);}
                foreach(int kind in Engine.OwnKongs(0)){int k=kind;labels.Add("杠"+MahjongGame.Name(k));actions.Add(()=>HumanKong(k));}
                labels.Add("出牌");actions.Add(HumanDiscard);for(int n=0;n<labels.Count;n++){var b=B(controls,labels[n],800-labels.Count*82+n*164,644,152,64,actions[n],labels[n]=="提示");if(labels[n]=="出牌")b.interactable=selected>=0;}
            }
            else if(Engine.Awaiting(0))
            {
                var opts=Engine.Options(0).Where(c=>c!=Call.Chi).ToList();int n=0;float start=800-(opts.Count+1)*76;
                foreach(var call in opts){var c=call;B(controls,c==Call.Hu?"胡":c==Call.Gang?"杠":"碰",start+n++*152,644,140,64,()=>HumanCall(c));}
                B(controls,"过",start+n*152,644,140,64,()=>HumanCall(Call.Pass),true);
                var chi=Engine.ChiOptions(0);for(int j=0;j<chi.Count;j++)
                {
                    int k=chi[j];float x=800-chi.Count*91+j*183;var b=B(controls,"吃",x,568,172,60,()=>HumanCall(Call.Chi,k));var label=b.GetComponentInChildren<Text>();label.rectTransform.sizeDelta=new Vector2(40,60);label.rectTransform.anchoredPosition=new Vector2(6,0);
                    for(int i=0;i<3;i++)MahjongTileView.Create(b.transform,art,(k+i)*4,62+i*33,49,28,true);
                }
            }
            foreach(var b in controls.GetComponentsInChildren<Button>()){var colors=b.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.86f,.86f,.86f);colors.disabledColor=new Color(.65f,.65f,.65f);b.colors=colors;}
        }
        protected override void Tick()
        {
            for(int n=0;n<4;n++)if(StudentAge.CampusUno.PlayClock.Now>wordUntil[n])words[n].text="";RefreshControls();if(busy||Engine.State==Phase.Finished||StudentAge.CampusUno.PlayClock.Now<nextAi||hands.SelectMany(h=>h).Any(v=>v.Moving))return;
            if(Engine.State==Phase.Claim){int seat=Enumerable.Range(1,3).FirstOrDefault(n=>Engine.Awaiting(n));if(seat>0)Change(()=>Engine.BotRespond(seat));}
            else if(Engine.State==Phase.Draw)Change(()=>Engine.Draw());
            else if(Engine.Turn!=0)Change(()=>Engine.BotTurn());
        }
        IEnumerator EndRound(){busy=true;RefreshControls(true);yield return new StudentAge.CampusUno.PlayDelay(1.8f);Finish(Engine.Winner==0);}
        protected override void OnDestroy(){base.OnDestroy();art?.Dispose();}
    }
}
