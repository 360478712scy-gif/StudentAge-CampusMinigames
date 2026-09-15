using UnityEngine;
using StudentAge.Sanguosha;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  void DrawGeneralReward(){
   float age=Mathf.Max(0,Time.unscaledTime-rewardAt),enter=Mathf.SmoothStep(0,1,age/.35f),scale=Mathf.Lerp(.92f,1,enter);
   var old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*enter);
   Picture("official/dialogs/getGeneralTitle.png",new Rect(620,98,360,80));
   // Same canvas anchor for portrait and original effect; neither changes its crop origin between frames.
   Rect stage=new Rect(800-700*scale,450-618*scale,1400*scale,1400*scale);
   Rect portrait=new Rect(stage.x+399f/1024*stage.width,stage.y+302f/1024*stage.height,226f/1024*stage.width,300f/1024*stage.height);
   var art=SanguoshaAssets.Image("generals/"+Session.Reward+".jpg");if(art!=null)GUI.DrawTexture(portrait,art,ScaleMode.StretchToFill);
   if(age<.35f)DrawOriginalEffect("general-unlock-start",age/.35f*.232f,stage);else DrawOriginalEffect("general-unlock-loop",age-.35f,stage,true);
   T(Catalog.Get(Session.Reward).Name,new Rect(620,683,360,48),tableLarge);
   GUI.color=old;
   if(OutcomeStage=="Ready"&&Btn("确定",680,752,240,52))CompleteResult();
  }
 }
}
