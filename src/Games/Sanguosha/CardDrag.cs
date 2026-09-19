using System;
namespace StudentAge.Sanguosha {
 public enum DecisionKind { Choice, Play, Response, Rescue, Nullification, Judgment, SkillCard }
 public enum DropZone { None, Hand, Self, Opponent, Table, ResponseCard, Judgment }
 // A drag belongs to one immutable decision. Releasing elsewhere never spends a card.
 public sealed class CardDrag {
  public const float HoldSeconds=.16f;
  public Prompt Prompt {get;private set;}
  public int Index {get;private set;}=-1;
  public float Started {get;private set;}
  public Option Option=>Prompt!=null&&Index>=0?Prompt.Options[Index]:null;
  public void Begin(Prompt prompt,int index,float now){Cancel();if(prompt==null||index<0||index>=prompt.Options.Count||prompt.Options[index].CardId<0||prompt.Kind==DecisionKind.Choice)return;Prompt=prompt;Index=index;Started=now;}
  public bool Held(float now)=>Option!=null&&now-Started>=HoldSeconds;
  public DropZone Target {
   get {if(Option==null)return DropZone.None;if(Prompt.Kind==DecisionKind.Judgment)return DropZone.Judgment;if(Prompt.Kind==DecisionKind.Rescue)return Option.Target==Prompt.Actor?DropZone.Self:DropZone.Opponent;
    if(Prompt.Kind==DecisionKind.Response||Prompt.Kind==DecisionKind.Nullification)return DropZone.ResponseCard;
    return Option.Target==Prompt.Actor?DropZone.Self:Option.Target>=0?DropZone.Opponent:DropZone.Table;}
  }
  public bool CanDrop(Prompt current,DropZone zone,float now)=>ReferenceEquals(Prompt,current)&&Held(now)&&zone!=DropZone.None&&zone!=DropZone.Hand&&zone==Target;
  public int Release(Prompt current,DropZone zone,float now){int result=CanDrop(current,zone,now)?Index:-1;Cancel();return result;}
  public void Cancel(){Prompt=null;Index=-1;Started=0;}
 }
}
