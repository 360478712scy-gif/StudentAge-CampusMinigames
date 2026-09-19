using System;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;
namespace StudentAge.CampusMinigames {
public sealed partial class NdsConsole {
 const float OrdinaryReveal=1.85f,UltimateReveal=3.4f;
 CanvasGroup awardWallFade,awardLayerFade,awardQuestion,awardCaption,awardIntro;Image awardDim,awardShadow,awardColor,awardHalo,awardSlot;CanvasGroup awardSlotQuestion;
 NdsBadgeUnlockFx awardFx;Vector2 awardHome;float awardHomeSize;CanvasGroup[] awardWallLabels;CanvasGroup awardWallCrown,awardCrownQuestion,awardAccept;Image[] awardSourceMedals;float awardReturnAt=-1;
 static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
 static float Phase(float t,float start,float duration)=>Ease((t-start)/duration);
 static Vector2 BadgePosition(int index){float a=index*Mathf.PI/6-Mathf.PI/2;return new Vector2(800+Mathf.Cos(a)*560,472+Mathf.Sin(a)*285);}
 void BadgeQuestion(Transform p,NdsBadge b,float x,float y,float w,float h,float size){var mark=PuzzleFrame.R(p,"LockedMark-"+b.ItemId,x,y,w,h);mark.gameObject.AddComponent<CanvasGroup>();BadgeLabel(mark,"?",0,0,w,h,size,BadgeWhite);}
 void ResetAwardVisuals(){awardWallFade=awardLayerFade=awardQuestion=awardCaption=awardIntro=awardSlotQuestion=null;awardDim=awardShadow=awardColor=awardHalo=awardSlot=null;awardFx=null;awardWallLabels=null;awardWallCrown=awardCrownQuestion=awardAccept=null;awardSourceMedals=null;awardReturnAt=-1;}
 public void TryShowAward(){if(!Valid||Booting||InGame||activeAward!=null||badgeQueue.Count==0||canvas==null||!canvas.activeSelf)return;
  activeAward=badgeQueue.Dequeue();awardStart=Time.unscaledTime;awardSoundPlayed=awardImpactPlayed=false;badgeSound?.Stop();music?.FadeMusicPaused(true,.8f);
  // The collection is the stage: pending awards remain silhouettes until their own reveal.
  DrawBadges();var p=badgeOverlay;awardWallFade=p.gameObject.AddComponent<CanvasGroup>();awardWallFade.alpha=1;
  foreach(var b in p.GetComponentsInChildren<Button>())b.interactable=false;
  awardWallLabels=p.GetComponentsInChildren<NdsPixelText>().Where(label=>!label.transform.parent.name.StartsWith("LockedMark-")).Select(label=>label.gameObject.AddComponent<CanvasGroup>()).ToArray();
  if(!activeAward.Ultimate){awardWallCrown=p.Find("UltimateBadgeArt").gameObject.AddComponent<CanvasGroup>();var mark=p.Find("LockedMark-"+NdsBadges.All[12].ItemId);awardCrownQuestion=mark==null?null:mark.GetComponent<CanvasGroup>();}
  awardHome=activeAward.Ultimate?new Vector2(800,472):BadgePosition(Array.IndexOf(NdsBadges.All,activeAward));awardHomeSize=activeAward.Ultimate?360:144;
  awardSlot=p.Find(activeAward.Ultimate?"UltimateBadgeArt":"Medal-"+activeAward.GameId).GetComponent<Image>();awardSlotQuestion=p.Find("LockedMark-"+activeAward.ItemId).GetComponent<CanvasGroup>();
  var layer=PuzzleFrame.R(p,"BadgeUnlockSequence",0,0,1600,900);awardLayerFade=layer.gameObject.AddComponent<CanvasGroup>();
  var blocker=layer.gameObject.AddComponent<Image>();blocker.color=Color.clear;blocker.raycastTarget=true;
  awardDim=Fill(layer,"UnlockBackdropDim",0,0,1600,900,Color.clear);
  awardFx=PuzzleFrame.R(layer,"UnlockGoldLight",0,0,1600,900).gameObject.AddComponent<NdsBadgeUnlockFx>();awardFx.raycastTarget=false;awardFx.Ultimate=activeAward.Ultimate;
  if(activeAward.Ultimate){awardSourceMedals=Enumerable.Range(0,12).Select(n=>p.Find("Medal-"+NdsBadges.All[n].GameId).GetComponent<Image>()).ToArray();for(int n=0;n<12;n++){var at=BadgePosition(n);var r=PuzzleFrame.R(layer,"OrbitBadge-"+n,0,0,144,144);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(at.x,-at.y);var img=r.gameObject.AddComponent<Image>();img.sprite=NdsBadgeAssets.Get(NdsBadges.All[n]);img.raycastTarget=false;awardOrbit.Add(r);}}
  awardMedal=PuzzleFrame.R(layer,"AwardMedal",0,0,awardHomeSize,awardHomeSize);awardMedal.pivot=new Vector2(.5f,.5f);
  awardHalo=Fill(awardMedal,"UnlockGoldOutline",-10,-10,awardHomeSize+20,awardHomeSize+20,Color.clear);awardHalo.sprite=NdsBadgeAssets.Get(activeAward);
  awardShadow=Fill(awardMedal,"UnlockSilhouette",0,0,awardHomeSize,awardHomeSize,Color.black);awardShadow.sprite=NdsBadgeAssets.Get(activeAward);
  awardColor=Fill(awardMedal,"UnlockBadgeColor",0,0,awardHomeSize,awardHomeSize,Color.clear);awardColor.sprite=NdsBadgeAssets.Get(activeAward);
  var question=PuzzleFrame.R(awardMedal,"UnlockQuestion",0,0,awardHomeSize,awardHomeSize);awardQuestion=question.gameObject.AddComponent<CanvasGroup>();BadgeLabel(question,"?",0,0,awardHomeSize,awardHomeSize,activeAward.Ultimate?8:4.8f,BadgeWhite);
  var intro=PuzzleFrame.R(layer,"UnlockIntro",0,0,1600,900);awardIntro=intro.gameObject.AddComponent<CanvasGroup>();BadgeLabel(intro,activeAward.Ultimate?"十二道光芒，正在汇聚":"一场胜利，一枚新的纪念",250,143,1100,48,2.6f,BadgeGold);
  var caption=PuzzleFrame.R(layer,"UnlockCaption",0,0,1600,900);awardCaption=caption.gameObject.AddComponent<CanvasGroup>();
  BadgeLabel(caption,activeAward.Ultimate?"全徽章达成":"徽章已解锁",250,143,1100,48,2.9f,BadgeGold);BadgeLabel(caption,activeAward.Name,250,655,1100,52,3,BadgeWhite);BadgeLabel(caption,"新的纪念，属于你",250,720,1100,30,1.6f,BadgeMuted);
  var accept=PuzzleFrame.R(layer,"UnlockAccept",0,0,1600,900);awardAccept=accept.gameObject.AddComponent<CanvasGroup>();Button(accept,"AcceptBadge",0,0,1600,900,AcceptBadge,Color.clear);BadgeLabel(accept,"收下徽章",630,788,340,56,2.2f,BadgeGold);
  UpdateAward(0);
 }
 void AcceptBadge(){if(activeAward==null||awardReturnAt>=0)return;float t=Time.unscaledTime-awardStart;if(t<(activeAward.Ultimate?UltimateReveal:OrdinaryReveal)+1.05f)return;awardReturnAt=t;}
 void FinishBadgeAward(){activeAward=null;DrawBadges();if(badgeQueue.Count>0)TryShowAward();else RestoreBadgeMusic();}
 void UpdateAward(float t){bool final=activeAward.Ultimate;float reveal=final?UltimateReveal:OrdinaryReveal,returnAt=awardReturnAt>=0?awardReturnAt:float.PositiveInfinity,returnLength=final?1.4f:1.15f,end=returnAt+returnLength+.8f;
  float lift=Phase(t,.45f,final?1.2f:.95f),back=Phase(t,returnAt,returnLength),unlocked=Phase(t,reveal,.7f);var center=new Vector2(800,430);
  awardWallFade.alpha=1;awardLayerFade.alpha=1-Phase(t,returnAt+returnLength,.65f);float focus=Phase(t,.4f,.85f)*(1-Phase(t,returnAt,.9f));awardDim.color=new Color(.012f,.025f,.055f,focus*.88f);foreach(var label in awardWallLabels)label.alpha=1-focus;if(awardWallCrown!=null)awardWallCrown.alpha=1-focus;if(awardCrownQuestion!=null)awardCrownQuestion.alpha=1-focus;
  Vector2 point=Vector2.Lerp(Vector2.Lerp(awardHome,center,lift),awardHome,back);float size=Mathf.Lerp(Mathf.Lerp(awardHomeSize,final?430:330,lift),awardHomeSize,back);
  float pulse=1+Mathf.Sin(Mathf.Clamp01((t-reveal)/.9f)*Mathf.PI)*.045f;
  point.y+=Mathf.Sin(Mathf.Max(0,t-reveal-.7f)*1.65f)*3*Phase(t,reveal+.7f,.5f)*(1-back);awardMedal.anchoredPosition=new Vector2(point.x,-point.y);awardMedal.localScale=Vector3.one*(size/awardHomeSize)*pulse;
  awardColor.color=new Color(1,1,1,unlocked);awardShadow.color=Color.Lerp(Color.black,new Color(.9f,.63f,.2f),Phase(t,reveal-.32f,.35f)*(1-unlocked));
  awardHalo.color=new Color(1,.74f,.24f,(Phase(t,reveal-.5f,.4f)-Phase(t,reveal+.5f,.8f))*.8f);
  awardQuestion.alpha=1-Phase(t,reveal-.3f,.48f);awardQuestion.transform.localScale=Vector3.one*(1+Phase(t,reveal-.3f,.48f)*.3f);
  awardAccept.alpha=Phase(t,reveal+.75f,.45f)*(1-Phase(t,returnAt,.25f));awardAccept.interactable=t>=reveal+1.05f&&awardReturnAt<0;awardAccept.blocksRaycasts=awardAccept.interactable;
  awardIntro.alpha=Phase(t,.7f,.4f)*(1-Phase(t,reveal-.35f,.45f));awardCaption.alpha=Phase(t,reveal+.3f,.65f)*(1-Phase(t,returnAt,.55f));
  float slotHide=Phase(t,.45f,.18f);awardSlot.color=new Color(0,0,0,1-slotHide);awardSlotQuestion.alpha=1-slotHide;
  // Exact endpoint matches the collection slot, then only opacity changes; no scale-zero/pop swap.
  if(t>=returnAt+returnLength){awardSlot.color=Color.white;awardSlotQuestion.alpha=0;awardMedal.gameObject.SetActive(false);}
  for(int n=0;n<awardOrbit.Count;n++){float progress=Phase(t,.65f+n*.028f,2.45f);Vector2 start=BadgePosition(n),offset=start-center;Vector2 tangent=new Vector2(-offset.y,offset.x).normalized;
   Vector2 position=Vector2.Lerp(start,center,progress)+tangent*(Mathf.Sin(progress*Mathf.PI)*100);var tr=awardOrbit[n];tr.anchoredPosition=new Vector2(position.x,-position.y);tr.localScale=Vector3.one*Mathf.Lerp(1,.14f,progress);tr.localEulerAngles=new Vector3(0,0,Mathf.Sin(progress*Mathf.PI)*14);
   float takeoff=Phase(t,.35f,.3f);tr.GetComponent<Image>().color=new Color(1,1,1,takeoff*(1-Phase(progress,.62f,.38f)));
   // Transfer visibility from the actual wall badge into the flying badge, never show a stationary duplicate.
   awardSourceMedals[n].color=new Color(1,1,1,Mathf.Max(1-takeoff,Phase(t,returnAt+.3f,.8f)));
  }
  awardFx.Age=t;awardFx.Center=point;awardFx.Strength=1-back;awardFx.SetVerticesDirty();
  // Lead the visual reveal with the Mario cue while the menu music finishes fading out.
  if(!awardSoundPlayed&&badgeSound!=null&&t>=(final?.75f:.55f)&&(final?ultimateWin:badgeWin)!=null){awardSoundPlayed=true;awardImpactPlayed=final;badgeSound.PlayOneShot(final?ultimateWin:badgeWin,final?1:.85f);}
  if(Keyboard.current!=null&&(Keyboard.current.enterKey.wasPressedThisFrame||Keyboard.current.escapeKey.wasPressedThisFrame))AcceptBadge();
  if(t>=end)FinishBadgeAward();
 }
}
// Stepped gold streaks, a gathering core and breaking seal share the badge's unscaled clock.
public sealed class NdsBadgeUnlockFx:MaskableGraphic {
 public bool Ultimate;public float Age,Strength=1;public Vector2 Center;
 static float Smooth(float a,float b,float t){t=Mathf.Clamp01((t-a)/(b-a));return t*t*(3-2*t);}
 protected override void OnPopulateMesh(VertexHelper h){h.Clear();float reveal=Ultimate?3.4f:1.85f,dt=Age-reveal;
  float charge=Smooth(.85f,reveal,Age)*(1-Smooth(reveal,reveal+.6f,Age))*Strength;
  // A growing stepped cross hides each approaching medal gradually inside light.
  for(int i=7;i>=0;i--){float r=(24+i*12)*charge;Box(h,Center.x-r,Center.y-r,r*2,r*2,new Color(1,.70f,.17f,charge*.045f));}
  if(Ultimate){for(int n=0;n<48;n++){float delay=(n%4)*.085f,q=dt-delay;if(q<0)continue;float u=Mathf.Clamp01(q/1.95f),alpha=Smooth(0,.13f,q)*(1-Smooth(.6f,1.95f,q));if(alpha<=0)continue;
   float a=n*Mathf.PI*2/48+.018f*Mathf.Sin(n*7);Vector2 axis=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float head=90+u*(1050+n%5*105),length=(130+n%7*29)*Mathf.Sin(u*Mathf.PI)*1.4f;float width=6+n%4*3;
   // Long rectangular rays expand to the edge of the screen, with bright narrow cores.
   Strip(h,Center+axis*Mathf.Max(42,head-length),Center+axis*head,width*2,new Color(1,.59f,.08f,alpha*.14f));Strip(h,Center+axis*Mathf.Max(44,head-length*.9f),Center+axis*head,width,new Color(1,.78f,.24f,alpha*.85f));Strip(h,Center+axis*Mathf.Max(45,head-length*.65f),Center+axis*head,Mathf.Max(2,width*.3f),new Color(1,.96f,.74f,alpha));
  }}else{
   // Pixel seal: intact while the silhouette lifts, then the two halves spring apart.
   float split=Smooth(-.04f,.6f,dt),alpha=Smooth(.7f,1.1f,Age)*(1-Smooth(.15f,.65f,dt));float y=Center.y+118+split*38;
   for(int side=-1;side<=1;side+=2){float x=Center.x+side*(9+split*65);Color gold=new Color(1,.81f,.4f,alpha);Box(h,x-9,y,18,27,gold);Box(h,x-9,y-13,7,16,gold);Box(h,x-9,y-17,18,6,gold);}
   for(int n=0;n<24;n++){float u=Mathf.Clamp01(dt/1.15f);if(dt<0)continue;float a=n*2.399963f,r=125+u*(80+n%5*23),alpha2=(1-u)*Mathf.Sin(u*Mathf.PI)*Strength;Vector2 pos=Center+new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r);Box(h,pos.x,pos.y,4+n%3*2,4+n%3*2,new Color(1,.81f,.36f,alpha2));}
  }
  if(dt>=0){float ring=Smooth(0,.65f,dt),alpha=(1-ring)*.7f*Strength;for(int n=0;n<48;n++){float a=n*Mathf.PI/24;Vector2 pos=Center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(90+ring*(Ultimate?340:155));Box(h,pos.x,pos.y,5,5,new Color(1,.85f,.43f,alpha));}}
 }
 static void Box(VertexHelper h,float x,float y,float w,float ht,Color c){if(c.a<=0||w<=0)return;int k=h.currentVertCount;h.AddVert(new Vector3(x,-y),c,Vector2.zero);h.AddVert(new Vector3(x+w,-y),c,Vector2.zero);h.AddVert(new Vector3(x+w,-y-ht),c,Vector2.zero);h.AddVert(new Vector3(x,-y-ht),c,Vector2.zero);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}
 static void Strip(VertexHelper h,Vector2 a,Vector2 b,float width,Color c){if(c.a<=0)return;Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=h.currentVertCount;foreach(var p in new[]{a-side,a+side,b+side,b-side})h.AddVert(new Vector3(p.x,-p.y),c,Vector2.zero);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}
}
}
