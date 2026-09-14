using System;using System.Collections;
using StudentAge.CampusPuzzles;using StudentAge.CampusUno;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;
namespace StudentAge.CampusMinigames
{
    public sealed class FallingBlocksView:PuzzleFrame
    {
        public FallingBlocksGame Engine{get;private set;}readonly Image[] cells=new Image[200],ghost=new Image[4],piece=new Image[4];RectTransform board,next;Text progress;const int Cell=34;int revision=-1,locks;float nextRepeat;
        static readonly Color[] Colors={new Color(.19f,.56f,.62f),new Color(.27f,.41f,.67f),new Color(.85f,.48f,.24f),new Color(.84f,.68f,.27f),new Color(.42f,.61f,.36f),new Color(.62f,.40f,.60f),new Color(.75f,.36f,.30f)};
        protected override string Title=>"俄罗斯方块";
        protected override string[] Rules=>new[]{"将下落方块填满一整行即可消除。","达到本局消行目标获胜，方块堆到顶端则失败。","← →或AD移动，↑、W或X旋转，Z反向旋转。","↓或S加速下落，空格直接落到底部。","浅色轮廓是落点预览。","右侧显示接下来的方块，本局没有倒计时。"};
        protected override void BuildPlay(){Engine=new FallingBlocksGame(Level,Environment.TickCount);var body=Paper(PlayRoot,485,125,700,740);body.color=new Color(.75f,.78f,.66f);Paper(PlayRoot,508,143,365,704).color=new Color(.25f,.34f,.29f);I(PlayRoot,"Screen",518,153,344,684,null,new Color(.86f,.87f,.73f));board=R(PlayRoot,"Board",520,155,340,680);
            for(int y=0;y<20;y++)for(int x=0;x<10;x++){I(board,"Grid",x*Cell,y*Cell,32,32,null,new Color(.39f,.46f,.32f,.09f));cells[y*10+x]=Block(board,"Fixed",x*Cell,y*Cell,32);}
            for(int n=0;n<4;n++){ghost[n]=Block(board,"Ghost",0,0,32);piece[n]=Block(board,"Falling",0,0,32);}
            T(PlayRoot,"接下来",930,160,210,58,32);next=R(PlayRoot,"Next",960,260,154,348);progress=T(PlayRoot,"",895,680,260,70,33);
            for(int j=0;j<5;j++)I(PlayRoot,"Speaker",920,780+j*9,149,3,null,new Color(.37f,.43f,.32f,.35f));Draw();}
        protected override void StartGame(){Draw();}
        public void Rotate(int direction){if(!Started||Ending)return;if(Engine.Rotate(direction))Sfx("paper",.17f);}
        public void Drop(){if(!Started||Ending)return;Engine.HardDrop();}
        protected override void Tick(){var k=Keyboard.current;int dir=0;bool down=false;if(k!=null){if(k.leftArrowKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame){Engine.Move(-1);nextRepeat=StudentAge.CampusUno.PlayClock.Now+.24f;}if(k.rightArrowKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame){Engine.Move(1);nextRepeat=StudentAge.CampusUno.PlayClock.Now+.24f;}if(k.leftArrowKey.isPressed||k.aKey.isPressed)dir=-1;else if(k.rightArrowKey.isPressed||k.dKey.isPressed)dir=1;down=k.downArrowKey.isPressed||k.sKey.isPressed;if(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame||k.xKey.wasPressedThisFrame)Rotate(1);if(k.zKey.wasPressedThisFrame)Rotate(-1);if(k.spaceKey.wasPressedThisFrame)Drop();}
            if(StudentAge.CampusUno.PlayClock.Now>=nextRepeat&&(dir!=0||down)){if(dir!=0)Engine.Move(dir);if(down)Engine.Move(0,1);nextRepeat=StudentAge.CampusUno.PlayClock.Now+.07f;}Engine.Tick(StudentAge.CampusUno.PlayClock.Delta);
            if(Engine.Locked!=locks){locks=Engine.Locked;Sfx(Engine.LastClear>0?"break":"place-1",.35f);if(Engine.LastClear>0){foreach(int row in Engine.ClearedRows)StartCoroutine(Flash(row));}}
            if(revision!=Engine.Revision){Draw();revision=Engine.Revision;}if(Engine.Won||Engine.Lost){Ending=true;StartCoroutine(Result());}}
        IEnumerator Result(){yield return new StudentAge.CampusUno.PlayDelay(.8f);Ending=false;Finish(Engine.Won);}
        IEnumerator Flash(int row){var f=I(board,"ClearFlash",0,row*Cell,340,33,null,new Color(1,.95f,.65f));yield return Animate(.3f,p=>{var c=f.color;c.a=1-p;f.color=c;f.rectTransform.localScale=new Vector3(1+p*.1f,1-p*.8f,1);});Destroy(f.gameObject);}
        Image Block(Transform parent,string name,float x,float y,float size){var block=I(parent,name,x,y,size,size,null,Color.white);I(block.transform,"Highlight",1,1,size-2,3,null,new Color(1,1,1,.28f));I(block.transform,"Shade",1,size-4,size-2,3,null,new Color(0,0,0,.18f));return block;}
        void Draw(){progress.text="消行 "+Engine.Lines+" / "+Engine.Target;for(int i=0;i<200;i++){cells[i].gameObject.SetActive(Engine.Board[i]!=0);if(Engine.Board[i]!=0)cells[i].color=Colors[Engine.Board[i]-1];}var shape=FallingBlocksGame.Cells(Engine.Piece,Engine.Rotation);int gy=Engine.GhostY;for(int n=0;n<4;n++){int x=Engine.X+shape[n*2],y=Engine.Y+shape[n*2+1],g=gy+shape[n*2+1];piece[n].gameObject.SetActive(y>=0&&!Engine.Won&&!Engine.Lost);piece[n].rectTransform.anchoredPosition=new Vector2(x*Cell,-y*Cell);piece[n].color=Colors[Engine.Piece];ghost[n].gameObject.SetActive(g>=0&&!Engine.Won&&!Engine.Lost);ghost[n].rectTransform.anchoredPosition=new Vector2(x*Cell,-g*Cell);var c=Colors[Engine.Piece];c.a=.19f;ghost[n].color=c;}
            // Ghosts stay beneath all four cells of the current piece.
            foreach(var g in ghost)g.transform.SetAsLastSibling();foreach(var p in piece)p.transform.SetAsLastSibling();Clear(next);var upcoming=Engine.Next;for(int i=0;i<upcoming.Length;i++){var c=FallingBlocksGame.Cells(upcoming[i],0);for(int n=0;n<4;n++){var b=Block(next,"Preview",c[n*2]*26,c[n*2+1]*26+i*108,24);b.color=Colors[upcoming[i]];}}}
    }
}
