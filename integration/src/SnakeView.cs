using System;using System.Linq;using System.Collections;using System.Collections.Generic;using StudentAge.CampusPuzzles;using StudentAge.CampusUno;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;
namespace StudentAge.CampusMinigames
{
    public sealed class SnakeView:PuzzleFrame
    {
        public SnakeGame Engine{get;private set;}RectTransform field;Sprite head,body,candy;readonly List<Image> segments=new List<Image>();Image food;Text progress;float clock;int[] previous;
        protected override string Title=>"贪吃蛇";
        protected override string[] Rules=>new[]{"WASD 或方向键控制小蛇。","吃到糖果会变长，达到本局目标即可获胜。","撞到边框、课桌障碍或自己身体就会失败。","小蛇不能直接掉头，转弯指令按先后执行。","糖果只会出现在空格中，没有整局倒计时。","第一关速度较慢，后续目标与障碍逐渐增加。"};
        protected override IEnumerator LoadAssets(){head=ArcadeArt.Load(Session.Root,"snake-head",Owned);body=ArcadeArt.Load(Session.Root,"snake-body",Owned);candy=ArcadeArt.Load(Session.Root,"candy",Owned);Engine=new SnakeGame(Level,Environment.TickCount);yield break;}
        protected override void BuildPlay(){Paper(PlayRoot,391,190,780,558);field=R(PlayRoot,"Field",429,229,704,480);I(field,"Lawn",-5,-5,714,490,null,new Color(.89f,.88f,.71f));for(int y=0;y<15;y++)for(int x=0;x<22;x++)I(field,"Grid",x*32,y*32,30,30,null,(x+y)%2==0?new Color(.55f,.62f,.40f,.13f):new Color(.83f,.84f,.65f,.12f));foreach(int cell in Engine.Walls){var desk=I(field,"Desk",cell%22*32,cell/22*32,31,31,NativeSkin.Paper,new Color(.65f,.45f,.27f));I(desk.transform,"Wood",3,4,25,4,null,new Color(.9f,.74f,.46f));}
            food=I(field,"Candy",0,0,32,32,candy,Color.white);progress=T(PlayRoot,"",66,316,290,95,32);previous=Engine.Body.ToArray();Draw(1);}
        protected override void StartGame(){clock=0;previous=Engine.Body.ToArray();}
        public void Turn(int direction){if(Started&&!Ending)Engine.Turn(direction);}
        protected override void Tick(){var k=Keyboard.current;if(k!=null){if(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)Turn(0);if(k.rightArrowKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)Turn(1);if(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)Turn(2);if(k.leftArrowKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame)Turn(3);}clock+=Math.Min(StudentAge.CampusUno.PlayClock.Delta,.1f);if(clock>=Engine.Interval){clock-=Engine.Interval;previous=Engine.Body.ToArray();int eaten=Engine.Eaten;Engine.Step();if(Engine.Eaten!=eaten)Sfx("pickup",.3f);if(Engine.Won||Engine.Lost){Ending=true;StartCoroutine(Result());}}Draw(clock/Engine.Interval);}
        void Draw(float t){while(segments.Count<Engine.Body.Count){var im=I(field,"Segment",0,0,33,33,body,Color.white);segments.Add(im);}for(int n=segments.Count-1;n>=0;n--){var im=segments[n];if(n>=Engine.Body.Count){im.gameObject.SetActive(false);continue;}im.sprite=n==0?head:body;int cell=Engine.Body[n],old=previous[Math.Min(n,previous.Length-1)];Vector2 to=new Vector2(cell%22*32+16,-cell/22*32-16),from=new Vector2(old%22*32+16,-old/22*32-16);im.rectTransform.pivot=new Vector2(.5f,.5f);im.rectTransform.anchoredPosition=Vector2.Lerp(from,to,Mathf.Clamp01(t));im.rectTransform.localEulerAngles=new Vector3(0,0,n==0?90-Engine.Direction*90:0);im.transform.SetAsLastSibling();}food.rectTransform.anchoredPosition=new Vector2(Engine.Food%22*32,-Engine.Food/22*32);food.gameObject.SetActive(!Engine.Won);progress.text="糖果 "+Engine.Eaten+" / "+Engine.Target;}
        IEnumerator Result(){Draw(1);Sfx(Engine.Won?"pickup":"undo",.3f);yield return new StudentAge.CampusUno.PlayDelay(.85f);Ending=false;Finish(Engine.Won);}
    }
}
