using System;
using System.Linq;
using UnityEngine;
using StudentAge.Sanguosha;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  readonly CardDrag drag=new CardDrag();
  Vector2 dragOrigin,dragMouse;int dragControl;
  static readonly Rect TableDrop=new Rect(430,340,730,260),ResponseFallback=new Rect(755,390,90,126);
  static readonly Rect EndButton=new Rect(1200,606,160,42);
  public bool DragActive=>drag.Option!=null;
  public int DraggedCard=>drag.Option?.CardId??-1;
  bool BoardInputAllowed=>Engine!=null&&Engine.Winner==-2&&!settled&&!Closed&&!Paused&&!help&&inspect==null&&inspectedCard==null;
  // Wait until the card that created this response has actually arrived on the table.
  bool DecisionReady=>BoardInputAllowed&&PlayerDecision&&!JudgmentHolding&&!MovementHolding&&events.Count==0&&!Engine.Events.Any(e=>e.Sequence>presentedEventSeq);
  bool ChoicePanelOpen {get {var p=Engine?.Pending;if(!PlayerDecision)return false;if(PlayPrompt)return skillMenu;if(p.Kind!=DecisionKind.Choice)return false;var own=Engine.Seats[0].Cards.Select(c=>c.Id).ToArray();return p.Reveal?.Any(c=>!own.Contains(c.Id))==true||p.Options.Any(o=>o.CardId<0&&!IsPass(o));}}
  void CancelDrag(){drag.Cancel();if(dragControl!=0&&GUIUtility.hotControl==dragControl)GUIUtility.hotControl=0;dragControl=0;}
  void BeginCardDrag(int id,Vector2 origin){if(!DecisionReady||ChoicePanelOpen)return;var choices=CardChoices(id).ToArray();if(choices.Length==0)return;
   if(Engine.Pending.Kind==DecisionKind.Choice){SelectOption(choices[0]);return;}
   drag.Begin(Engine.Pending,choices[0],Time.unscaledTime);if(!DragActive)return;dragOrigin=origin;dragMouse=Event.current.mousePosition;dragControl=GUIUtility.GetControlID("SanguoshaCardDrag".GetHashCode(),FocusType.Passive);GUIUtility.hotControl=dragControl;
  }
  Rect ResponseRect(){var p=Engine?.Pending;if(p==null)return ResponseFallback;var c=tableCards.LastOrDefault(t=>t.Exit<0&&t.Card.Id==p.Subject?.Id);return c==null?ResponseFallback:new Rect(c.Position.x-51,c.Position.y-72,102,144);}
  DropZone DropAt(Vector2 mouse){if(Engine?.Pending?.Kind==DecisionKind.Judgment&&JudgmentDrop.Contains(mouse))return DropZone.Judgment;if(new Rect(315,731,880,169).Contains(mouse))return DropZone.Hand;if(SelfPortrait.Contains(mouse))return DropZone.Self;if(EnemyPortrait.Contains(mouse))return DropZone.Opponent;
   if((Engine?.Pending?.Kind==DecisionKind.Response||Engine?.Pending?.Kind==DecisionKind.Nullification)&&ResponseRect().Contains(mouse))return DropZone.ResponseCard;
   return TableDrop.Contains(mouse)?DropZone.Table:DropZone.None;
  }
  void HandleCardDrag(){if(!DragActive)return;if(!DecisionReady||ChoicePanelOpen||!ReferenceEquals(drag.Prompt,Engine.Pending)){CancelDrag();return;}
   var ev=Event.current;dragMouse=ev.mousePosition;if(ev.type==EventType.MouseDown&&ev.button==1){CancelDrag();ev.Use();return;}
   if(ev.type==EventType.MouseDrag){ev.Use();return;}if(ev.type!=EventType.MouseUp||ev.button!=0)return;
   int choice=drag.Release(Engine.Pending,DropAt(dragMouse),Time.unscaledTime);CancelDrag();ev.Use();if(choice>=0)SelectOption(choice);
  }
  void DrawDragArrow(){if(!DragActive||!DecisionReady||!drag.Held(Time.unscaledTime))return;var zone=DropAt(dragMouse);bool valid=drag.CanDrop(Engine.Pending,zone,Time.unscaledTime);
   Rect target=drag.Target==DropZone.Judgment?JudgmentDrop:drag.Target==DropZone.Self?SelfPortrait:drag.Target==DropZone.Opponent?EnemyPortrait:drag.Target==DropZone.ResponseCard?ResponseRect():TableDrop;
   Outline(target,new Color(1,.77f,.25f,valid?.85f:.30f),2);Arrow(dragOrigin,dragMouse,valid?new Color(1,.78f,.18f,.95f):new Color(.85f,.80f,.65f,.65f));
  }
  void DrawResponseTarget(){var p=Engine.Pending;if(!DecisionReady||p.Kind!=DecisionKind.Response&&p.Kind!=DecisionKind.Nullification)return;
   if(p.Subject!=null&&tableCards.Any(c=>c.Exit<0&&c.Card.Id==p.Subject.Id))return;
   var c=p.Subject??new Card(-100,p.SubjectKind??"slash",Suit.Spade,1);CardFace(c,ResponseFallback);T("响应此牌",new Rect(730,524,140,27),tableCenter);
  }
  void OnApplicationFocus(bool focused){if(!focused)CancelDrag();}
 }
}
