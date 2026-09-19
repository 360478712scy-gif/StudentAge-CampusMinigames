using System;using System.Collections;using System.Linq;
using StudentAge.CampusPuzzles;using StudentAge.CampusUno;using UnityEngine;using UnityEngine.UI;
namespace StudentAge.CampusMinigames
{
    public sealed class BoxView:PuzzleFrame
    {
        public BoxGame Engine{get;private set;}public bool CanPick{get;private set;}public Sprite NativeHand{get;private set;}
        readonly RectTransform[] boxes=new RectTransform[4];readonly RectTransform[] lids=new RectTransform[4];RectTransform hand,boxLayer;Text target,score,message;readonly string[] names={"铅笔","橡皮","直尺","书签"};
        protected override string Title=>"换盒寻物";
        protected override string[] Rules=>new[]{"四件文具分别放进四个盒子，先记住目标的位置。","盒盖关上后，盒子会交换位置，请跟住目标。","交换结束后点击一个盒子，伸手打开查看。","一局"+Engine.TotalRounds+"轮，找对"+Engine.RequiredWins+"轮获胜。","只有交换结束后才能选；选定后不能更改。","没有倒计时，可以想好再打开。"};
        protected override IEnumerator LoadAssets(){bool done=false;GameObject prefab=null;Sdk.ResMgr.LoadAsync<GameObject>("Prefabs/UI/MiniGame/FingerKnifeView@MiniGame",p=>{prefab=p;done=true;});float end=StudentAge.CampusUno.PlayClock.Now+20;while(!done&&StudentAge.CampusUno.PlayClock.Now<end)yield return null;if(prefab!=null){var image=prefab.GetComponentsInChildren<Image>(true).FirstOrDefault(i=>i.name=="hand_left");if(image!=null)NativeHand=image.sprite;}if(NativeHand==null)throw new InvalidOperationException("Original FingerKnife hand asset unavailable");Engine=new BoxGame(Level,Environment.TickCount);}
        protected override void BuildPlay(){score=T(PlayRoot,"",530,111,540,50,28);target=T(PlayRoot,"",480,201,640,65,36);boxLayer=R(PlayRoot,"Boxes",0,0,1600,900);message=T(PlayRoot,"",460,732,680,66,35);hand=I(PlayRoot,"OriginalGameHand",0,900,265,285,NativeHand,Color.white).rectTransform;var arm=R(hand,"Forearm",0,0,265,1000).gameObject.AddComponent<BoxArmGraphic>();arm.raycastTarget=false;hand.gameObject.SetActive(false);}
        protected override void StartGame(){StartCoroutine(Round());}
        float SlotX(int slot)=>235+slot*290;
        IEnumerator Round(){CanPick=false;Engine.NextRound();Clear(boxLayer);score.text="第 "+Engine.Round+" / "+Engine.TotalRounds+" 轮     找对 "+Engine.Correct+" 次";target.text="找到"+names[Engine.Target];message.text="记住它的位置";
            for(int n=0;n<4;n++){int slot=n;boxes[n]=MakeBox(SlotX(n),435,Engine.Items[n],out lids[n]);var button=boxes[n].gameObject.AddComponent<Button>();button.targetGraphic=boxes[n].GetComponent<Image>();var actual=boxes[n];button.onClick.AddListener(()=>Choose(Array.IndexOf(boxes,actual)));SetLid(lids[n],1);}
            yield return new StudentAge.CampusUno.PlayDelay(Engine.MemorizeDuration);
            for(int i=0;i<4;i++)yield return OpenLid(i,false,Engine.CloseDuration);
            message.text="看仔细了";yield return new StudentAge.CampusUno.PlayDelay(.3f);
            foreach(var pair in Engine.Swaps){int a=pair[0],b=pair[1];var first=boxes[a];var second=boxes[b];Vector2 pa=first.anchoredPosition,pb=second.anchoredPosition;Sfx("paper",.24f);yield return Animate(Engine.SwapDuration,t=>{float arc=Mathf.Sin(t*Mathf.PI)*65;first.anchoredPosition=Vector2.Lerp(pa,pb,t)+Vector2.up*arc;second.anchoredPosition=Vector2.Lerp(pb,pa,t)-Vector2.up*arc;});boxes[a]=second;boxes[b]=first;var lid=lids[a];lids[a]=lids[b];lids[b]=lid;Engine.Swap(a,b);yield return new StudentAge.CampusUno.PlayDelay(Engine.SwapGap);}
            CanPick=true;message.text="打开哪个盒子？";
        }
        RectTransform MakeBox(float x,float y,int item,out RectTransform lid){var root=I(boxLayer,"Box-"+item,x,y,240,235,NativeSkin.Paper,new Color(.70f,.46f,.27f)).rectTransform;root.GetComponent<Image>().type=Image.Type.Sliced;root.GetComponent<Image>().raycastTarget=true;
            I(root,"Shadow",6,220,244,18,NativeSkin.Paper,new Color(.25f,.14f,.08f,.25f));I(root,"InsideRim",7,3,226,130,NativeSkin.Paper,new Color(.31f,.19f,.11f));I(root,"Inside",15,15,210,105,NativeSkin.Paper,new Color(.48f,.32f,.20f));DrawItem(root,item,57,19,.9f);I(root,"Front",7,122,227,111,NativeSkin.Paper,new Color(.78f,.58f,.35f));I(root,"Tape",98,126,44,107,null,new Color(.93f,.79f,.55f,.7f));I(root,"Rim",0,115,240,15,NativeSkin.Paper,new Color(.94f,.78f,.52f));
            lid=R(root,"Lid",-5,0,250,135);I(lid,"LidEdge",0,10,250,135,NativeSkin.Paper,new Color(.47f,.30f,.16f));I(lid,"LidTop",0,0,250,128,NativeSkin.Paper,new Color(.88f,.70f,.46f));I(lid,"Fold",9,112,232,3,null,new Color(.62f,.42f,.24f));I(lid,"LidTape",102,5,44,120,null,new Color(.98f,.87f,.64f,.75f));return root;}
        RectTransform DrawItem(Transform p,int kind,float x,float y,float scale){var root=R(p,"Stationery-"+kind,x,y,120,90);root.localScale=Vector3.one*scale;
            if(kind==0){var pen=R(root,"Pencil",7,42,111,17);pen.localEulerAngles=new Vector3(0,0,25);I(pen,"Wood",0,0,101,17,null,new Color(.89f,.67f,.21f));I(pen,"Stripe",0,2,99,4,null,new Color(1,.88f,.39f));I(pen,"Graphite",99,4,12,9,null,new Color(.2f,.19f,.16f));I(pen,"Rubber",0,0,18,17,null,new Color(.86f,.44f,.42f));}
            else if(kind==1){var eraser=I(root,"Eraser",12,24,95,46,NativeSkin.Paper,new Color(.92f,.64f,.58f));I(eraser.transform,"Sleeve",12,2,64,42,null,new Color(.88f,.90f,.84f));I(eraser.transform,"Band",12,26,64,9,null,new Color(.31f,.50f,.51f));}
            else if(kind==2){var ruler=I(root,"Ruler",0,24,118,39,NativeSkin.Paper,new Color(.88f,.72f,.36f));for(int i=0;i<15;i++)I(ruler.transform,"Mark",5+i*7,2,1.4f,i%5==0?17:9,null,new Color(.39f,.29f,.12f));}
            else{var bookmark=I(root,"Bookmark",35,0,50,86,NativeSkin.Paper,new Color(.40f,.59f,.47f));I(bookmark.transform,"Ribbon",22,-8,6,37,null,new Color(.83f,.48f,.28f));I(bookmark.transform,"Line",11,49,27,2,null,new Color(.93f,.86f,.64f));I(bookmark.transform,"Line",11,58,27,2,null,new Color(.93f,.86f,.64f));}return root;}
        void SetLid(RectTransform lid,float p){lid.anchoredPosition=new Vector2(-5,70*p);lid.localScale=new Vector3(1,1-.48f*p,1);lid.localEulerAngles=Vector3.zero;}
        IEnumerator OpenLid(int slot,bool opening,float duration=.5f){var lid=lids[slot];Vector2 box=boxes[slot].anchoredPosition;hand.SetAsLastSibling();hand.gameObject.SetActive(true);var off=new Vector2(box.x+45,-920);var touch=new Vector2(box.x+25,box.y-10);hand.anchoredPosition=off;yield return Animate(duration*.6f,t=>hand.anchoredPosition=Vector2.Lerp(off,touch,t));Sfx("paper",.35f);yield return Animate(duration,t=>{float p=opening?t:1-t;SetLid(lid,p);hand.anchoredPosition=touch+new Vector2(0,70*p);});var from=hand.anchoredPosition;yield return Animate(duration*.6f,t=>hand.anchoredPosition=Vector2.Lerp(from,off,t));hand.gameObject.SetActive(false);}
        public void Choose(int slot){if(!CanPick||slot<0||slot>3||Ending)return;CanPick=false;StartCoroutine(Reveal(slot));}
        IEnumerator Reveal(int slot){bool ok=Engine.Pick(slot);message.text="";yield return OpenLid(slot,true);message.text=ok?"找到了！":"是"+names[Engine.Items[slot]];score.text="第 "+Engine.Round+" / "+Engine.TotalRounds+" 轮     找对 "+Engine.Correct+" 次";Sfx(ok?"pickup":"undo",.3f);yield return new StudentAge.CampusUno.PlayDelay(1.25f);if(Engine.Finished)Finish(Engine.Won);else yield return Round();}
    }
    // Extend the cropped native wrist to the lower edge of the screen.
    public sealed class BoxArmGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();Quad(vh,42,165,-147,37,new Color32(38,27,18,255));Quad(vh,44,163,-144,34,new Color32(255,237,213,255));}
        static void Quad(VertexHelper vh,float left,float right,float bottomLeft,float bottomRight,Color32 color){int n=vh.currentVertCount;vh.AddVert(new Vector3(left,-282),color,Vector2.zero);vh.AddVert(new Vector3(right,-282),color,Vector2.zero);vh.AddVert(new Vector3(bottomRight,-1000),color,Vector2.zero);vh.AddVert(new Vector3(bottomLeft,-1000),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }

}
