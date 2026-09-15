using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StudentAge.Sanguosha;

namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  readonly TableSelection selection = new TableSelection();
  sealed class TableCard { public Card Card; public int Actor,Target; public string Kind,Movement; public float At,Exit=-1,DrawAt=-1; public Vector2 From,Position,Velocity,ExitFrom; }
  readonly List<TableCard> tableCards = new List<TableCard>();
  readonly Dictionary<int,float> cardLifts = new Dictionary<int,float>();
  readonly Dictionary<int,float> handX = new Dictionary<int,float>();readonly Dictionary<int,Vector2> handPositions=new Dictionary<int,Vector2>();
  Texture2D cloth,glow,handShade; GUIStyle tableText,tableSmall,tableCenter,tableLarge,cardNumber;
  Card inspectedCard; string conversion; bool showLog,skillMenu;
  float tableClock,feedbackAt=-10,drawAt=-10; int drawActor,drawCount;float movementHoldUntil;bool MovementHolding=>tableClock<movementHoldUntil; MatchEvent feedback;
  static readonly Color Gold=new Color(.82f,.67f,.36f), Cream=new Color(.96f,.9f,.76f), Ink=new Color(.12f,.09f,.065f);
  static readonly Rect EnemyPortrait=new Rect(697,61,196,253),SelfPortrait=new Rect(1380,632,196,242);
  public int SelectedTableCard=>selection.Option?.CardId??-1;
  public bool TableCanConfirm=>selection.CanConfirm;
  public int CentralCardCount=>tableCards.Count(c=>c.Exit<0);
  public void ResetTableSelection(){CancelDrag();selection.Bind(Engine?.Pending);conversion=null;skillMenu=false;optionPage=0;}
  void SyncTableSelection(){if(!ReferenceEquals(selection.Prompt,Engine?.Pending))ResetTableSelection();}
  bool PlayPrompt=>Engine?.Pending?.Actor==0&&Engine.Pending.Kind==DecisionKind.Play;
  bool PlayerDecision=>Engine?.Pending?.Actor==0&&Engine.Winner==-2;
  string ConversionKind(string skill)=>skill=="longdan"?(PlayPrompt?"slash":Engine.Pending?.Text.Contains("闪")==true?"jink":"slash"):skill=="wusheng"?"slash":skill=="qingguo"?"jink":skill=="jijiu"?"peach":skill=="qixi"?"dismantlement":skill=="guose"?"indulgence":null;
  Card FindCard(int id)=>Engine.Seats.SelectMany(p=>p.Cards.Concat(p.Judgments)).Concat(Engine.Pending?.Reveal??Enumerable.Empty<Card>()).FirstOrDefault(c=>c.Id==id);
  IEnumerable<int> CardChoices(int id){if(!PlayerDecision)return Enumerable.Empty<int>();var choices=Engine.Pending.Options.Select((o,i)=>new{o,i}).Where(x=>x.o.CardId==id);
   if(conversion!=null){string kind=ConversionKind(conversion);choices=choices.Where(x=>x.o.UseKind==kind||x.o.UseKind==null&&!PlayPrompt);}
   return choices.OrderBy(x=>x.o.UseKind!=null&&x.o.UseKind!=FindCard(id)?.Kind?1:0).Select(x=>x.i);}
  public void SelectTableCard(int id){SyncTableSelection();if(!PlayerDecision||Paused||help||inspect!=null||inspectedCard!=null)return;var choices=CardChoices(id).ToArray();if(choices.Length==0)return;
   if(selection.Option?.CardId==id){selection.Clear();return;}selection.Select(choices[0]);Sound("button-down");}
  public void SelectTableTarget(int actor){SyncTableSelection();if(!PlayerDecision||Paused)return;selection.SelectTarget(actor);}
  public void ConfirmTableSelection(){SyncTableSelection();if(!PlayerDecision||Paused||help||inspect!=null||inspectedCard!=null)return;int choice=selection.Confirm(Engine.Pending);if(choice>=0)SelectOption(choice);}
  void RecordTableEvent(MatchEvent ev){RecordParticipantSpeech(ev);presentedEventSeq=ev.Sequence;feedback=ev;feedbackAt=tableClock;if(ev.JudgmentStage!=null){RecordJudgment(ev);return;}
   if(ev.Text.Contains("摸")&&ev.Sound=="choose-item"){drawAt=tableClock;drawActor=ev.Actor;drawCount=ev.DrawCount;movementHoldUntil=tableClock+.48f+drawCount*.07f;}
   if(ev.Card==null||ev.Movement==null&&!(ev.Text.Contains("使用")||ev.Text.Contains("打出")||ev.Text.Contains("判定")||ev.Text.Contains("改判")||ev.Text.Contains("揭示")))return;
   if(ev.Movement!=null)movementHoldUntil=tableClock+1.05f;Sound("card-place");bool fresh=ev.Text.Contains("使用")&&ev.Sound!="nullification";
   if(fresh||tableCards.Count(c=>c.Exit<0)>=6)foreach(var old in tableCards.Where(c=>c.Exit<0)){old.Exit=tableClock;old.ExitFrom=old.Position;}
   if(tableCards.Any(c=>c.Exit<0&&c.Card.Id==ev.Card.Id&&tableClock-c.At<.7f))return;
   Vector2 origin=ev.EquipmentSlot>=0?EquipmentCardRect(ev.Actor,ev.EquipmentSlot).center:ev.Actor==0&&handPositions.TryGetValue(ev.Card.Id,out var previous)?previous:ev.Actor==0?new Vector2(746,748):EnemyPortrait.center;
   tableCards.Add(new TableCard{Card=ev.Card,Actor=ev.Actor,Target=ev.Target,Movement=ev.Movement,Kind=ev.Sound!=null&&Catalog.CardName(ev.Sound)!=ev.Sound?ev.Sound:ev.Card.Kind,At=tableClock,From=origin,Position=origin});
   tableCards.RemoveAll(c=>c.Exit>=0&&tableClock-c.Exit>1.2f);
  }
  void TableStyles(){if(tableText!=null)return;
   tableText=new GUIStyle(label){fontSize=20,normal={textColor=Cream}};
   tableSmall=new GUIStyle(tableText){fontSize=18};tableCenter=new GUIStyle(tableText){alignment=TextAnchor.MiddleCenter};
   tableLarge=new GUIStyle(tableCenter){fontSize=33};cardNumber=new GUIStyle(tableSmall){font=Font.CreateDynamicFontFromOSFont(new[]{"Arial","DejaVu Sans"},22),fontSize=22,alignment=TextAnchor.UpperCenter};
   cloth=new Texture2D(256,144,TextureFormat.RGB24,false);var pixels=new Color[256*144];
   for(int y=0;y<144;y++)for(int x=0;x<256;x++){float dx=(x-128)/128f,dy=(y-78)/120f;float radial=Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dy*dy)*.73f);float n=(Mathf.PerlinNoise(x*.43f,y*.43f)-.5f)*.027f;pixels[y*256+x]=Color.Lerp(new Color(.18f,.14f,.1f),new Color(.62f,.53f,.4f),radial)+new Color(n,n,n,0);}
   cloth.SetPixels(pixels);cloth.Apply();cloth.filterMode=FilterMode.Bilinear;
   handShade=new Texture2D(1,128,TextureFormat.RGBA32,false);for(int y=0;y<128;y++)handShade.SetPixel(0,y,new Color(.045f,.03f,.02f,Mathf.Pow(1-y/127f,.8f)*.85f));handShade.Apply();handShade.filterMode=FilterMode.Bilinear;
   glow=new Texture2D(64,64,TextureFormat.RGBA32,false);pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/32f;pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Max(0,1-d),2));}glow.SetPixels(pixels);glow.Apply();glow.filterMode=FilterMode.Bilinear;
  }
  static void Line(Vector2 a,Vector2 b,float width,Color color){var matrix=GUI.matrix;var d=b-a;GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);Rect(new Rect(0,-width/2,d.magnitude,width),color);GUI.matrix=matrix;}
  static void Outline(Rect r,Color color,float width=1){Rect(new Rect(r.x,r.y,r.width,width),color);Rect(new Rect(r.x,r.yMax-width,r.width,width),color);Rect(new Rect(r.x,r.y,width,r.height),color);Rect(new Rect(r.xMax-width,r.y,width,r.height),color);}
  void Halo(Vector2 center,float size,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(center.x-size/2,center.y-size/2,size,size),glow);GUI.color=old;}
  void T(string text,Rect r,GUIStyle style=null){var face=style??tableText;if(!DrawOriginalText(text,r,face))GUI.Label(r,text,face);}
  void Panel(Rect r,float opacity=.86f){var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*opacity);Stretch("official/dialogs/selection-bg.png",r);GUI.color=color;}
  void Stretch(string path,Rect r){var image=SanguoshaAssets.Image(path);if(image!=null)GUI.DrawTexture(r,image,ScaleMode.StretchToFill);}
  void CampFrame(string path,Rect r){var tex=SanguoshaAssets.Image(path);if(tex==null)return;float top=62f/tex.height,bottom=22f/tex.height,scale=r.width/tex.width,th=62*scale,bh=22*scale;
   GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y,r.width,th),tex,new Rect(0,1-top,1,top));GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y+th,r.width,r.height-th-bh),tex,new Rect(0,bottom,1,1-top-bottom));GUI.DrawTextureWithTexCoords(new Rect(r.x,r.yMax-bh,r.width,bh),tex,new Rect(0,0,1,bottom));}
  bool TableButton(string text,Rect r,bool enabled=true,bool selected=false){bool can=GUI.enabled&&enabled;bool hover=can&&r.Contains(Event.current.mousePosition);Stretch("official/selfseat/selfSeatCancelBtn_"+(!can?"disable":hover||selected?"over":"normal")+".png",r);var old=tableCenter.normal.textColor;tableCenter.normal.textColor=can?Cream:new Color(.54f,.51f,.44f);T(text,r,tableCenter);tableCenter.normal.textColor=old;bool clicked=can&&GUI.Button(r,GUIContent.none,GUIStyle.none);if(clicked)Sound("button-down");return clicked;}
  void TableBackdrop(){TableStyles();Stretch("decade/table.jpg",new Rect(0,0,1600,900));GUI.DrawTexture(new Rect(0,670,1600,230),handShade);}
  void Board(){TableStyles();SyncTableSelection();bool boardEnabled=GUI.enabled;GUI.enabled=boardEnabled&&BoardInputAllowed;HandleCardDrag();
   Rect(new Rect(0,0,1600,39),new Color(.14f,.09f,.055f,.92f));Line(new Vector2(0,40),new Vector2(1600,40),1,new Color(.8f,.65f,.35f,.45f));
   T("三国杀",new Rect(23,3,120,32),tableCenter);T("单挑  ·  "+(easy?"简单模式":"标准规则"),new Rect(166,3,330,32),tableSmall);T(Session.Name+"  ·  第"+(Session.Stage+1)+"关",new Rect(574,3,480,32),tableSmall);
   T("第 "+Engine.TurnNumber+" 回合",new Rect(1310,3,145,32),tableSmall);if(TableButton("规则",new Rect(1480,5,95,28)))help=true;
   DrawDeck();DrawPortrait(1,EnemyPortrait);DrawPortrait(0,SelfPortrait);EnemyEquipment();EquipmentAt(0,new Rect(1210,678,149,159));
   DrawParticipants();DrawCentralCards();DrawFeedback();DrawHand();DrawSkills();DrawCommandBar();DrawLog();DrawResponseTarget();DrawJudgment();DrawResponseSkills();
   if(Engine.Winner!=-2){GUI.enabled=boardEnabled;DrawOutcome();return;}DrawChoiceOverlay();DrawDragArrow();GUI.enabled=boardEnabled;
  }

  void DrawDeck(){T("牌堆 "+Engine.DrawPile.Count+"  ·  弃牌 "+Engine.Discard.Count,new Rect(1030,5,275,28),tableSmall);
   // The player's actual new cards fly into their slots in DrawHand; never send dummy backs after them.
   if(drawActor==1&&tableClock-drawAt<.48f+drawCount*.07f){for(int i=0;i<drawCount;i++){float t=Mathf.Clamp01((tableClock-drawAt-i*.07f)/.48f);if(t<=0||t>=1)continue;float ease=t*t*(3-2*t);var pos=Vector2.Lerp(DealOrigin,EnemyPortrait.center,ease);var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(t*8)*Mathf.Clamp01((1-t)*5));Picture("back.png",new Rect(pos.x-26,pos.y-37,52,74));GUI.color=old;}}}
  string KingdomKey(string k)=>k=="魏"?"wei":k=="蜀"?"shu":k=="吴"?"wu":k=="神"?"god":"qun";
  static Rect PortraitArtRect(Rect r)=>new Rect(r.x+9,r.y+3,r.width-17,r.height-7);
  // Align the luminous centerline, not the outer transparent/glow bounds, with the art frame.
  static Rect PortraitGlowStage(Rect r,bool yellow){var art=PortraitArtRect(r);var core=yellow?new Rect(164,131,188,253):new Rect(166,133,184,249);float sx=art.width/core.width,sy=art.height/core.height;return new Rect(art.x-core.x*sx,art.y-core.y*sy,512*sx,512*sy);}
  void DrawPortrait(int actor,Rect r){var p=Engine.Seats[actor];bool target=DragActive&&drag.Option.Target==actor,chosen=target&&DropAt(dragMouse)==(actor==0?DropZone.Self:DropZone.Opponent),active=Engine.Winner==-2&&Engine.Turn==actor;
   if(active||target){string effect=target?"seatStateYellow":"seatStateRed";DrawOriginalEffect(effect,tableClock,PortraitGlowStage(r,target),true,target&&!chosen?.58f:1);}
   Stretch("official/seat_bottom/seat_bottom_projection.png",new Rect(r.x-3,r.y-3,r.width+6,r.height+6));var image=SanguoshaAssets.Image("portraits/"+p.General.Id+".png");var old=GUI.color;if(p.FaceDown)GUI.color=new Color(.43f,.43f,.43f,1);if(image!=null)GUI.DrawTexture(PortraitArtRect(r),image,ScaleMode.ScaleAndCrop);GUI.color=old;
   string key=KingdomKey(p.General.Kingdom);if(key=="god")key="shen";
   Stretch("official/seat/"+key+"Bg.png",new Rect(r.x,r.y+25,29,r.height-33));CampFrame("official/seat/gradeCountryBg"+(key=="shen"?5:3)+".png",new Rect(r.x-13,r.y-12,47,r.height+13));Picture("official/seat/"+key+".png",new Rect(r.x-13,r.y-13,47,52));
   T(string.Join("\n",p.General.Name.Select(c=>c.ToString()).ToArray()),new Rect(r.x+2,r.y+56,24,125),new GUIStyle(tableCenter){fontSize=20});
   DrawHealth(p,r);Stretch("official/seat/cardNumBg1.png",new Rect(r.x-2,r.yMax-34,29,32));T(p.Hand.Count.ToString(),new Rect(r.x-1,r.yMax-34,27,30),tableCenter);
   if(actor==1)Picture("official/seat/Figure_Enemy.png",new Rect(r.xMax-42,r.y+3,38,41));
   if(p.FaceDown){Stretch("official/seat/TurnOverMask.png",r);T("翻面",new Rect(r.x+35,r.y+85,r.width-55,48),tableLarge);}
   if(p.Nightmare>0||p.Buqu.Count>0)T(p.Nightmare>0?"梦魇 "+p.Nightmare:"不屈 "+p.Buqu.Count,new Rect(r.x+28,r.y+4,r.width-34,28),tableSmall);
   if(actor==1){T(Session.Name,new Rect(r.x+24,r.yMax+4,r.width-35,24),tableCenter);var names=string.Join(" · ",p.General.Skills.Split(' ').Select(Catalog.SkillName).ToArray());Rect(new Rect(r.x+28,r.yMax-30,r.width-48,24),new Color(.08f,.065f,.05f,.64f));T(names,new Rect(r.x+28,r.yMax-30,r.width-48,24),tableCenter);}
   for(int j=0;j<p.Judgments.Count;j++){var mark=p.Judgments[j];var badge=new Rect(r.x+35+j*39,r.y+4,38,38);Picture("official/seat/"+(mark.Kind=="indulgence"?"DecideLeBuSiShu":"DecideBingLiangCunDuan")+".png",badge);if(GUI.enabled&&!DragActive&&GUI.Button(badge,new GUIContent("",mark.Name),GUIStyle.none))inspectedCard=mark;}

   if(p.Drunk)T("酒",new Rect(r.x+38,r.y+43,32,34),new GUIStyle(tableCenter){normal={textColor=new Color(1,.40f,.25f)}});
   if(GUI.enabled&&!DragActive&&r.Contains(Event.current.mousePosition)&&Event.current.type==EventType.MouseDown&&Event.current.button==1){inspect=p.General.Id;Event.current.Use();}
  }
  void DrawHealth(Seat p,Rect portrait){bool number=p.MaxHp>5;float height=number?116:Mathf.Max(72,p.MaxHp*22+16);Stretch("official/seat/gradeHpBg.png",new Rect(portrait.xMax-20,portrait.yMax-height,32,height));float x=portrait.xMax-10,bottom=portrait.yMax-20;string style=p.Hp<=1?"redBlood":p.Hp<=p.MaxHp/2f?"yellowBlood":"greenBlood";
   if(number){T(p.Hp+"\n/\n"+p.MaxHp,new Rect(x-3,bottom-76,21,68),new GUIStyle(tableCenter){fontSize=16,normal={textColor=Gold}});Picture("official/seat/"+style+".png",new Rect(x-2,bottom-3,20,22));return;}
   for(int i=0;i<p.MaxHp;i++)Picture("official/seat/"+(i<p.Hp?style:"empBlood")+".png",new Rect(x-2,bottom-i*22,20,22));
  }
  static Rect EquipmentCardRect(int actor,int slot)=>actor==1?new Rect(450+(slot%2)*108,98+(slot/2)*147,96,134):new Rect(1210,678+(slot+1)*159f/5,149,159f/5-5);
  Card DisplayEquipment(int actor,int slot){var card=Engine.Seats[actor].Equipment[slot];foreach(var ev in Engine.Events.Where(e=>e.Sequence>presentedEventSeq).Reverse()){if(ev.Actor!=actor||ev.Card==null)continue;if(ev.EquipmentSlot==slot)card=ev.Card;else if(ev.Movement==null&&ev.Text.Contains("使用")&&Catalog.Slot(ev.Sound)==slot)card=null;}return card;}
  void EnemyEquipment(){for(int slot=0;slot<4;slot++){var c=DisplayEquipment(1,slot);if(c==null)continue;var r=EquipmentCardRect(1,slot);bool can=PlayerDecision&&Engine.Pending.Kind==DecisionKind.Choice&&CardChoices(c.Id).Any();CardFace(c,r,false,can);if(GUI.enabled&&r.Contains(Event.current.mousePosition)&&Event.current.type==EventType.MouseDown){if(Event.current.button==0&&can&&DecisionReady)SelectOption(CardChoices(c.Id).First());else if(Event.current.button==1)inspectedCard=c;Event.current.Use();}}}
  void EquipmentAt(int actor,Rect r){var p=Engine.Seats[actor];float row=actor==0?r.height/5:r.height/4;if(actor==0)Stretch("official/seat_bottom/selfSeatEquipBg.png",r);
   for(int i=0;i<4;i++){var c=p.Equipment[i];Rect rr=new Rect(r.x,r.y+(actor==0?i+1:i)*row,r.width,row-5);if(c==null)continue;var art=SanguoshaAssets.Image("official/equipment/"+c.Kind+".png");if(art!=null)GUI.DrawTexture(rr,art,ScaleMode.StretchToFill);else T(c.Name,rr,tableSmall);bool can=actor==0&&CardChoices(c.Id).Any();if(can)Outline(rr,Gold,1);
    string rank=c.Rank==1?"A":c.Rank==11?"J":c.Rank==12?"Q":c.Rank==13?"K":c.Rank.ToString();Picture("official/card/"+(c.Red?"red":"black")+"_"+rank+".png",new Rect(rr.xMax-34,rr.y+3,14,17));Picture("official/card/"+c.Suit.ToString().ToLowerInvariant()+".png",new Rect(rr.xMax-19,rr.y+5,13,15));
    if(GUI.enabled&&rr.Contains(Event.current.mousePosition)&&Event.current.type==EventType.MouseDown){if(Event.current.button==0&&can)BeginCardDrag(c.Id,rr.center);else if(Event.current.button==1)inspectedCard=c;Event.current.Use();}}

  }
  void CardFace(Card c,Rect r,bool dim=false,bool chosen=false,string useKind=null){
   if(chosen)Stretch("official/card/normalcard_selected_new.png",new Rect(r.x-4,r.y-4,r.width+8,r.height+8));var texture=CardTexture(c);var old=GUI.color;if(dim)GUI.color=new Color(.45f,.45f,.45f,1);if(texture!=null)GUI.DrawTexture(r,texture,ScaleMode.StretchToFill);GUI.color=old;
   if(c.Kind!=c.PrintedKind)useKind=c.Kind;if(!string.IsNullOrEmpty(useKind)&&useKind!=c.PrintedKind){Rect(new Rect(r.x,r.yMax-22,r.width,22),new Color(.25f,.12f,.035f,.9f));T("视为"+Catalog.CardName(useKind),new Rect(r.x,r.yMax-22,r.width,22),tableCenter);}
  }
  void DrawCentralCards(){var current=tableCards.Where(c=>c.Exit<0).ToArray();foreach(var c in tableCards){float age=tableClock-c.At,exit=c.Exit<0?0:Mathf.Clamp01((tableClock-c.Exit)/.42f);if(exit>=1)continue;int i=Array.IndexOf(current,c);int count=current.Length;Vector2 destination=new Vector2(800+(i-(count-1)*.5f)*86,455+(c.Card.Id%3-1)*3);
    float t=Mathf.Clamp01(age/.42f),ease=1-Mathf.Pow(1-t,3);Vector2 center=c.Exit>=0?c.ExitFrom-new Vector2(0,exit*22):Vector2.Lerp(c.From,destination,ease);if(c.Exit<0)center.y-=Mathf.Sin(t*Mathf.PI)*26;
    if(Event.current.type==EventType.Repaint&&!Paused&&c.DrawAt!=tableClock){float dt=c.DrawAt<0?Time.unscaledDeltaTime:tableClock-c.DrawAt;c.DrawAt=tableClock;if(c.Exit>=0)c.Position=center;else c.Position=Vector2.SmoothDamp(c.Position,center,ref c.Velocity,.10f,10000,Mathf.Min(.05f,dt));}center=c.Position;
    float size=Mathf.Lerp(1.23f,1,ease);var rotation=Quaternion.Euler(-24*(1-ease),(c.Actor==0?-20:20)*(1-ease),(c.Actor==0?-7:7)*(1-ease)+(c.Card.Id%5-2)*.7f*ease);
    CardShadow(center,89*size,125*size,rotation,20*(1-ease),1-exit);ProjectedImage(CardTexture(c.Card),center,89*size,125*size,rotation,new Color(1,1,1,1-exit),true);
    var old=GUI.color;GUI.color=new Color(1,1,1,1-exit);Rect r=new Rect(center.x-44.5f,center.y+41,89,21);Rect(r,new Color(.16f,.12f,.08f,.55f));T(c.Movement=="discard"?"弃置":c.Movement=="obtain"?(c.Target==0?"收入手牌":"对手获得"):c.Actor==0?"你":Engine.Seats[c.Actor].General.Name,r,new GUIStyle(tableCenter){fontSize=14});GUI.color=old;
    if(c.Kind!=c.Card.PrintedKind)T("视为"+Catalog.CardName(c.Kind),new Rect(center.x-90,center.y-90,180,23),new GUIStyle(tableCenter){fontSize=15});
    string fx=c.Movement!=null?null:c.Kind=="slash"?(c.Card.Red?"cardSkillHongSha":"cardSkillHeiSha"):c.Kind=="jink"?"cardSkillShan":null;
    if(fx!=null){float stage=370*Mathf.Lerp(1.25f,1,ease);DrawOriginalEffect(fx,age,new Rect(center.x-stage/2,center.y-stage/2,stage,stage),false,1-exit);}
   }}
  void Arrow(Vector2 from,Vector2 to,Color color){var delta=to-from;float length=delta.magnitude;if(length<1)return;to-=delta/length*24;delta=to-from;var matrix=GUI.matrix;var tint=GUI.color;GUI.color=color;GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(from.x,from.y,0),Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);Stretch("official/seat/seatui_directline_new1.png",new Rect(0,-22,delta.magnitude,44));GUI.matrix=matrix;GUI.color=tint;}
  void DrawFeedback(){if(feedback==null||feedback.JudgmentStage!=null)return;float age=tableClock-feedbackAt;if(age>1.25f)return;Rect victim=feedback.Target==0?SelfPortrait:EnemyPortrait;
   if(feedback.Sound=="judgment-result"){Stretch("official/gameBase/gameActionBg.png",new Rect(465,555,670,38));T(feedback.Text,new Rect(465,555,670,38),tableCenter);}
   else if(feedback.Sound!=null&&feedback.Sound.StartsWith("injure")){float fade=Mathf.Clamp01((1.05f-age)/.3f);float pop=1+Mathf.Sin(Mathf.Clamp01(age/.22f)*Mathf.PI)*.2f;var old=GUI.color;GUI.color=new Color(1,1,1,fade);float y=victim.center.y-12-Mathf.Max(0,age-.22f)*35;
    Stretch("official/seat_bottom/hurt_Bg.png",new Rect(victim.center.x-64*pop,y-45*pop,128*pop,90*pop));int amount=1;var m=System.Text.RegularExpressions.Regex.Match(feedback.Text,@"受到(\d+)点伤害");if(m.Success)int.TryParse(m.Groups[1].Value,out amount);string number="-"+amount;for(int i=0;i<number.Length;i++)Picture("official/seat_bottom/hurt_"+number[i]+".png",new Rect(victim.center.x-number.Length*19*pop+i*38*pop,y-24*pop,42*pop,47*pop));GUI.color=old;
    if(age<.20f)Rect(victim,new Color(1,.1f,.025f,(1-age/.20f)*.22f));
   }else if(feedback.Sound=="recover"){Halo(victim.center,280,new Color(.36f,1,.43f,.30f*(1-age/1.25f)));}
   else if(feedback.Card==null&&feedback.Sound!=null&&feedback.Text.Contains("发动")){var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01((1.25f-age)/.3f));Stretch("official/gameBase/gameActionBg.png",new Rect(485,345,630,36));var ink=tableCenter.normal.textColor;tableCenter.normal.textColor=Gold;T(feedback.Text,new Rect(485,345,630,36),tableCenter);tableCenter.normal.textColor=ink;GUI.color=old;}
  }
  void DrawSkills(){var p=Engine.Seats[0];string[] skills=p.General.Skills.Split(' ');float w=175f/Math.Max(1,skills.Length);
   for(int i=0;i<skills.Length;i++){string skill=skills[i],name=Catalog.SkillName(skill);Rect r=new Rect(1393+i*w,842,w-3,30);var indexes=PlayerDecision?Engine.Pending.Options.Select((o,n)=>new{o,n}).Where(x=>x.o.CardId<0&&(x.o.Label==name||x.o.Label.StartsWith(name+" · ",StringComparison.Ordinal))).Select(x=>x.n).ToArray():new int[0];bool transform=PlayerDecision&&ConversionKind(skill)!=null&&Engine.Pending.Options.Any(o=>o.CardId>=0&&(o.UseKind==ConversionKind(skill)||!PlayPrompt&&o.Label.Contains(" → ")));bool can=indexes.Length>0||transform;
    Stretch("official/selfseat/"+(can?(conversion==skill?"skill_common_over":"skill_common_up"):"skill_comon_disabled")+".png",r);T(name,r,tableCenter);
    if(GUI.enabled&&r.Contains(Event.current.mousePosition)&&Event.current.type==EventType.MouseDown){if(Event.current.button==1||!can)inspect=p.General.Id;else if(indexes.Length==1)SelectOption(indexes[0]);else if(indexes.Length>1){skillMenu=!skillMenu;conversion=skill;}else{conversion=conversion==skill?null:skill;selection.Clear();}Event.current.Use();}}
  }
  void DrawCommandBar(){if(Engine.Winner!=-2)return;
   string phase=JudgmentHolding||Engine.Pending?.Kind==DecisionKind.Judgment?"判定阶段":Engine.Phase;int pi=phase.Contains("判定")?2:phase.Contains("摸牌")?3:phase.Contains("弃牌")?5:phase.Contains("结束")?6:phase.Contains("准备")?1:4;
   Picture("official/selfseat/SeatRoundState_"+pi+".png",new Rect(388,650,94,21));
   if(JudgmentHolding){T("正在展示判定结果",new Rect(490,664,620,30),tableCenter);return;}
   if(!PlayerDecision){T(Engine.Seats[1].General.Name+" · "+(Engine.Turn==1?Engine.Phase:"正在响应"),new Rect(490,664,620,30),tableCenter);return;}
   var prompt=Engine.Pending;string hint=PlayPrompt?"长按手牌，拖向目标使用":prompt.Text;if(conversion!=null)hint=Catalog.SkillName(conversion)+"：长按要转换的牌";
   Stretch("official/gameBase/gameActionBg.png",new Rect(473,650,652,33));T(hint,new Rect(488,650,620,30),new GUIStyle(tableCenter){fontSize=18});
   var pass=prompt.Options.Select((o,i)=>new{o,i}).FirstOrDefault(x=>IsPass(x.o));
   if(pass!=null&&TableButton(PlayPrompt?"结束回合":pass.o.Label=="放弃响应"?"不出":pass.o.Label,EndButton,DecisionReady&&!DragActive))SelectOption(pass.i);
  }
  bool IsPass(Option o)=>o.CardId<0&&(o.Label=="结束出牌"||o.Label=="放弃响应"||o.Label=="不使用"||o.Label=="不改判"||o.Label=="不处理"||o.Label=="取消"||o.Label.StartsWith("确定，",StringComparison.Ordinal));
  void DrawChoiceOverlay(){if(!PlayerDecision||Engine.Pending.Kind==DecisionKind.Judgment||Engine.Pending.Kind==DecisionKind.SkillCard||Engine.Pending.Kind==DecisionKind.Response||Engine.Pending.Kind==DecisionKind.Nullification||Engine.Pending.Kind==DecisionKind.Rescue)return;var prompt=Engine.Pending;var own=new HashSet<int>(Engine.Seats[0].Cards.Select(c=>c.Id));
   var revealed=(prompt.Reveal??new List<Card>()).Where(c=>!own.Contains(c.Id)).ToArray();var noncards=prompt.Options.Select((o,i)=>new{o,i}).Where(x=>x.o.CardId<0&&!IsPass(x.o)).ToArray();
   if(PlayPrompt){if(!skillMenu)return;noncards=noncards.Where(x=>x.o.Label.StartsWith(Catalog.SkillName(conversion??"guhuo")+" · ",StringComparison.Ordinal)).ToArray();}
   if(revealed.Length==0&&noncards.Length==0)return;
   Rect area=new Rect(335,Math.Max(334,532-(revealed.Length>0?205:85)),780,revealed.Length>0?202:91);Panel(area,.97f);
   if(revealed.Length>0){int count=Math.Min(6,revealed.Length-optionPage*6);float left=area.x+20+(740-count*107)/2;for(int i=0;i<count;i++){var c=revealed[optionPage*6+i];Rect r=new Rect(left+i*107,area.y+12,95,138);bool can=CardChoices(c.Id).Any();CardFace(c,r,false,selection.Option?.CardId==c.Id);if(GUI.enabled&&GUI.Button(r,GUIContent.none,GUIStyle.none)){if(can){SelectOption(CardChoices(c.Id).First());return;}else inspectedCard=c;}}
    if(revealed.Length>6){if(TableButton("‹",new Rect(area.x+6,area.y+66,30,36),optionPage>0))optionPage--;if(TableButton("›",new Rect(area.xMax-36,area.y+66,30,36),(optionPage+1)*6<revealed.Length))optionPage++;}
   }
   if(noncards.Length>0){float y=area.y+(revealed.Length>0?159:12);int perPage=4;int pages=(noncards.Length+perPage-1)/perPage;int pageIndex=skillMenu?optionPage:0;for(int i=0;i<perPage;i++){int idx=pageIndex*perPage+i;if(idx>=noncards.Length)break;var v=noncards[idx];if(TableButton(v.o.Label=="发动"&&prompt.Text.Contains("：")?prompt.Text.Substring(0,prompt.Text.IndexOf("：")):v.o.Label,new Rect(area.x+18+i*185,y,176,34),DecisionReady,selection.Index==v.i)){SelectOption(v.i);return;}}
    if(pages>1&&skillMenu){if(TableButton("‹",new Rect(area.x+310,area.y+53,65,29),optionPage>0))optionPage--;if(TableButton("›",new Rect(area.x+394,area.y+53,65,29),optionPage<pages-1))optionPage++;}}
  }
  void DrawLog(){Rect icon=new Rect(24,831,52,52);bool hot=icon.Contains(Event.current.mousePosition);Stretch("official/nsOptBar/nsOptBarChatBtn"+(hot?"Over":"Normal")+".png",icon);if(GUI.enabled&&GUI.Button(icon,new GUIContent("","战报"),GUIStyle.none))showLog=!showLog;if(hot)T("战报",new Rect(82,841,90,28),tableSmall);if(!showLog)return;Panel(new Rect(20,355,281,475),.96f);T("战报",new Rect(34,365,235,32),tableCenter);float height=0;foreach(var line in Engine.Log)height+=tableSmall.CalcHeight(new GUIContent(line),236)+8;logScroll=GUI.BeginScrollView(new Rect(32,405,255,410),logScroll,new Rect(0,0,234,Math.Max(410,height)));float y=0;foreach(var line in Engine.Log){float h=tableSmall.CalcHeight(new GUIContent(line),232);T(line,new Rect(0,y,232,h),tableSmall);y+=h+8;}GUI.EndScrollView();}
  string CardRule(Card c){switch(c.Kind){
   case "slash":return "出牌时指定攻击范围内的对手。对手需要用闪响应，否则受到 1 点伤害。每回合的出杀次数取决于所选模式和武将技能。";
   case "jink":return "在需要闪避杀或万箭齐发时打出。通常不能在出牌阶段主动使用。部分武将技能可以把闪转换成其他牌。";
   case "peach":return "受伤时可以使用，回复 1 点体力；有人进入濒死状态时，也可以用桃进行救援。体力不会超过上限。";
   case "supply_shortage":return "置于距离为1的对手判定区。判定不为梅花，跳过其摸牌阶段；出牌阶段照常进行。";
   case "drowning":return "对手选择弃置全部装备（至少一张），或受到1点伤害。没有装备时直接受到伤害。";
   case "wine":return "出牌阶段限一次，对自己使用，令本回合下一张杀伤害+1。自己濒死时也可用酒回复1点体力；自救不占出牌次数，不能救对手。";
   case "crossbow":return "装备期间，不再受通常的出杀次数限制。";
   case "double_sword":return "对异性武将使用杀时，可以让对方选择：弃一张手牌，或者让你摸一张牌。";
   case "qinggang_sword":return "你使用的杀无视目标防具。";
   case "blade":return "杀被闪避后，可以继续对同一目标出杀。";
   case "spear":return "需要杀时，可以把两张手牌当作一张杀使用或打出。";
   case "axe":return "杀被闪避后，可以弃置另外两张牌，使这次杀仍然命中。";
   case "halberd":return "攻击范围为 4。单挑牌局只有一个对手，不产生额外目标。";
   case "kylin_bow":return "杀命中对手时，可以弃置对方装备区的一匹马。";
   case "ice_sword":return "杀命中时，可以放弃造成伤害，改为弃置对方两张牌。";
   case "eight_diagram":return "需要用闪响应杀时，可以进行判定；红色判定牌视为成功出闪，黑色时仍可以从手牌中出闪。";
   case "renwang_shield":return "装备期间，抵消对你使用的黑色杀。青釭剑可以无视此防具。";
   case "jueying":case "dilu":case "zhuahuangfeidian":return "防御马：其他角色计算到你的距离时增加 1。";
   case "chitu":case "dayuan":case "zixing":return "进攻马：你计算到其他角色的距离时减少 1。";
   case "amazing_grace":return "亮出与存活人数相同数量的牌，从使用者开始依次选择并获得一张。";
   case "god_salvation":return "所有受伤的角色依次回复 1 点体力。";
   case "savage_assault":return "对手需要打出一张杀，否则受到 1 点伤害。";
   case "archery_attack":return "对手需要打出一张闪，否则受到 1 点伤害。";
   case "duel":return "从对手开始，双方交替打出杀。先无法响应的一方受到伤害。无双会使对手每次需要连续打出两张杀。";
   case "ex_nihilo":return "为自己摸两张牌。";
   case "snatch":return "获得距离为 1 的对手的一张牌。可选择明置装备或判定牌，或随机抽取手牌；获得的牌收入你的手牌，不会自动装备。";
   case "dismantlement":return "选择对手的明置装备、判定牌，或随机抽取一张手牌弃置。弃牌公开展示并进入弃牌堆，不会加入你的手牌。";
   case "collateral":return "单挑时，令有武器的对手对你出杀；如果对方不出杀，你获得其武器。";
   case "nullification":return "响应正在生效的锦囊，抵消它对一名角色的效果。也可以继续响应无懈可击。";
   case "indulgence":return "置入对手判定区。其判定阶段若不是红桃，跳过当回合的出牌阶段。";
   case "lightning":return "先置入自己的判定区。判定为黑桃 2—9 时受到 3 点伤害，否则传给下一名可以接收闪电的角色。";
   default:return c.Name;
  }}
  void CardDetail(){TableStyles();var c=inspectedCard;Rect(new Rect(0,0,1600,900),new Color(0,0,0,.8f));Panel(new Rect(370,112,860,670),.98f);CardFace(c,new Rect(410,158,330,464));T(c.Text,new Rect(773,175,415,58),tableLarge);string type=c.Equip?(c.Slot==0?"武器 · 攻击范围 "+Catalog.Range(c.Kind):c.Slot==1?"防具":"坐骑"):c.Delayed?"延时锦囊":c.Trick?"锦囊牌":"基本牌";T(type,new Rect(773,244,415,35),tableCenter);Line(new Vector2(790,294),new Vector2(1172,294),1,new Color(.7f,.55f,.3f,.5f));T(CardRule(c),new Rect(789,331,380,254),new GUIStyle(tableText){fontSize=23,wordWrap=true,alignment=TextAnchor.UpperLeft});if(TableButton("返回牌桌",new Rect(896,694,286,45)))inspectedCard=null;}
  void DisposeTableArt(){if(cloth!=null)Destroy(cloth);if(glow!=null)Destroy(glow);if(handShade!=null)Destroy(handShade);if(cardNumber?.font!=null)Destroy(cardNumber.font);if(cardMaterial!=null)Destroy(cardMaterial);foreach(var rt in cardLayers){rt.Release();Destroy(rt);}cardLayers.Clear();CancelDrag();handPoses.Clear();}
 }
}
