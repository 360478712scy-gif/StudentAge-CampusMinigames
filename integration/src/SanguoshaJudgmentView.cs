using UnityEngine;
using StudentAge.Sanguosha;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  static readonly Rect JudgmentDrop=new Rect(670,380,260,250);
  MatchEvent judgmentVisual;float judgmentAt,judgmentHoldUntil;
  public string JudgmentStage=>judgmentVisual?.JudgmentStage;
  public Card VisibleJudgmentCard=>judgmentVisual?.Card;
  bool JudgmentHolding=>tableClock<judgmentHoldUntil;
  void RecordJudgment(MatchEvent ev){judgmentVisual=ev;judgmentAt=tableClock;judgmentHoldUntil=tableClock+(ev.JudgmentStage=="result"?2.1f:ev.JudgmentStage=="replace"?.85f:1.2f);}
  void DrawJudgment(){
   if(judgmentVisual==null)return;float age=tableClock-judgmentAt;bool result=judgmentVisual.JudgmentStage=="result";
   if(result&&tableClock>=judgmentHoldUntil)return;
   var tint=GUI.color;float alpha=result?Mathf.Clamp01((judgmentHoldUntil-tableClock)/.25f):1;GUI.color=new Color(1,1,1,alpha);
   Stretch("official/gameBase/gameActionBg.png",new Rect(575,350,450,34));
   T(judgmentVisual.JudgmentReason+(result?" · 最终判定":" · 判定中"),new Rect(575,350,450,34),tableCenter);
   if(judgmentVisual.Card!=null){
    float t=Mathf.Clamp01(age/.5f),ease=1-Mathf.Pow(1-t,3);Vector2 pos=judgmentVisual.JudgmentStage=="reveal"?Vector2.Lerp(new Vector2(1150,55),new Vector2(800,488),ease):new Vector2(800,488);
    // Face-up arrival: ranks and suits remain readable before a player can replace the card.
    float width=144*Mathf.Lerp(.65f,1,ease);CardFace(judgmentVisual.Card,new Rect(pos.x-width/2,pos.y-100,width,200));
   }
   if(result){
    if(judgmentVisual.Card!=null)DrawOriginalEffect(judgmentVisual.JudgmentSuccess?"judgment-success":"judgment-failure",age,new Rect(350,38,900,900));
    Stretch("official/gameBase/gameActionBg.png",new Rect(430,612,740,38));T(judgmentVisual.Text,new Rect(445,612,710,38),new GUIStyle(tableCenter){fontSize=19});
   }else if(Engine.Pending?.Kind==DecisionKind.Judgment){
    Outline(JudgmentDrop,new Color(1,.78f,.3f,.5f),1);T("改判区域",new Rect(680,594,240,30),tableCenter);
   }
   GUI.color=tint;
  }
  void DrawResponseSkills(){
   var p=Engine?.Pending;if(!PlayerDecision||p.Kind!=DecisionKind.Response&&p.Kind!=DecisionKind.Nullification&&p.Kind!=DecisionKind.Rescue&&p.Kind!=DecisionKind.Play)return;
   int row=0;for(int i=0;i<p.Options.Count;i++){var o=p.Options[i];if(o.CardId>=0||IsPass(o))continue;if(p.Kind==DecisionKind.Play&&(Engine.Seats[0].Weapon==null||!o.Label.StartsWith(Catalog.CardName(Engine.Seats[0].Weapon)+"：")))continue;
    if(TableButton(p.Kind==DecisionKind.Play?Catalog.CardName(Engine.Seats[0].Weapon):o.Label,new Rect(1190,480+row*42,180,36),DecisionReady&&!DragActive)){SelectOption(i);return;}row++;}
  }
 }
}
