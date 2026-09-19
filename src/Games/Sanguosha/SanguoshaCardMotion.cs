using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StudentAge.Sanguosha;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  sealed class HandPose {public Vector2 Center,Velocity,DealTarget,DealVelocity;public float BornAt,Opacity,Shade=1;public bool Arrived;public float Lift,LiftVelocity,Pitch,Yaw,Roll,Scale=1;public Vector2[] Corners;}
  readonly List<Card> visualHand=new List<Card>();int presentedEventSeq;static readonly Vector2 DealOrigin=new Vector2(1060,555);
  readonly Dictionary<int,HandPose> handPoses=new Dictionary<int,HandPose>();
  Material cardMaterial;int hoverCard=-1;float handFrameAt=-1;
  Texture2D CardTexture(Card c)=>SanguoshaAssets.Image("hd/faces/"+c.PrintedKind+"-"+c.Suit.ToString().ToLowerInvariant()+"-"+c.Rank+".png")??SanguoshaAssets.Image("hd/cards/"+c.PrintedKind+".png")??SanguoshaAssets.Image("official/cards/"+c.PrintedKind+".png");
  static Vector2 ProjectCard(float u,float v,Vector2 center,float width,float height,Quaternion rotation,float depth=0){var p=rotation*new Vector3((u-.5f)*width,(v-.5f)*height,depth);float perspective=650f/(650f+p.z);return center+new Vector2(p.x,p.y)*perspective;}
  static bool InCard(Vector2 point,Vector2[] corners){if(corners==null)return false;float sign=0;for(int i=0;i<4;i++){var a=corners[i];var b=corners[(i+1)%4];float cross=(b.x-a.x)*(point.y-a.y)-(b.y-a.y)*(point.x-a.x);if(Mathf.Abs(cross)<.01f)continue;if(sign==0)sign=Mathf.Sign(cross);else if(sign*cross<0)return false;}return true;}
  readonly List<RenderTexture> cardLayers=new List<RenderTexture>();int cardLayerIndex;
  void ProjectedImage(Texture2D texture,Vector2 center,float width,float height,Quaternion rotation,Color tint,bool sheen=false){if(texture==null||Event.current.type!=EventType.Repaint)return;
   if(cardMaterial==null){cardMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};}
   var corners=new[]{ProjectCard(0,0,center,width,height,rotation),ProjectCard(1,0,center,width,height,rotation),ProjectCard(1,1,center,width,height,rotation),ProjectCard(0,1,center,width,height,rotation)};
   float x=corners.Min(v=>v.x)-2,y=corners.Min(v=>v.y)-2,w=corners.Max(v=>v.x)-x+2,h=corners.Max(v=>v.y)-y+2;
   float scale=Mathf.Max(1,GUI.matrix.lossyScale.x);int rw=Mathf.Clamp(Mathf.CeilToInt(w*scale/32)*32,32,1024),rh=Mathf.Clamp(Mathf.CeilToInt(h*scale/32)*32,32,1024);int slot=cardLayerIndex++;
   if(slot==cardLayers.Count)cardLayers.Add(null);var rt=cardLayers[slot];if(rt==null||rt.width!=rw||rt.height!=rh){if(rt!=null){rt.Release();Destroy(rt);}rt=new RenderTexture(rw,rh,0,RenderTextureFormat.ARGB32){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};rt.Create();cardLayers[slot]=rt;}
   // GL draws only into this card's texture. IMGUI then composites cards and dialogs in order.
   var previous=RenderTexture.active;bool srgb=GL.sRGBWrite;RenderTexture.active=rt;GL.Clear(true,true,Color.clear);GL.PushMatrix();
   try{GL.sRGBWrite=false;GL.LoadPixelMatrix(0,w,h,0);cardMaterial.mainTexture=texture;cardMaterial.SetColor("_Color",Color.white);cardMaterial.SetPass(0);GL.Begin(GL.QUADS);
    const int cols=8,rows=10;for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)for(int corner=0;corner<4;corner++){float u=(col+(corner==1||corner==2?1:0))/(float)cols,v=(row+(corner>=2?1:0))/(float)rows;var at=ProjectCard(u,v,center,width,height,rotation);float light=sheen?Mathf.Lerp(.94f,1.035f,u):1;GL.Color(new Color(Mathf.Min(1,tint.r*light),Mathf.Min(1,tint.g*light),Mathf.Min(1,tint.b*light),tint.a));GL.TexCoord2(u,1-v);GL.Vertex3(at.x-x,at.y-y,0);}GL.End();
   }finally{GL.PopMatrix();RenderTexture.active=previous;GL.sRGBWrite=srgb;}
   var color=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(x,y,w,h),rt,ScaleMode.StretchToFill,true);GUI.color=color;
  }
  void CardShadow(Vector2 center,float width,float height,Quaternion rotation,float lift,float alpha=1){ProjectedImage(CardTextureShadow(),center+new Vector2(3+lift*.13f,5+lift*.28f),width+4,height+4,rotation,new Color(0,0,0,.23f*alpha));}
  Texture2D CardTextureShadow()=>SanguoshaAssets.Image("official/cards/slash.png");
  void DrawHand(){var actual=Engine.Seats[0].Hand;
   // Keep a committed outgoing card visible until its public flight is actually presented.
   visualHand.RemoveAll(c=>!actual.Any(a=>a.Id==c.Id)&&!Engine.Events.Any(e=>e.Sequence>presentedEventSeq&&e.Actor==0&&e.Card?.Id==c.Id&&(e.Text.Contains("使用")||e.Text.Contains("打出"))));
   foreach(var c in actual)if(!visualHand.Any(v=>v.Id==c.Id))visualHand.Add(c);var hand=visualHand;var mouse=Event.current.mousePosition;bool interactive=GUI.enabled&&BoardInputAllowed&&!ChoicePanelOpen;int hovered=-1;var order=hand.Select(c=>c.Id).ToList();
   int selected=-1;if(selected>=0&&order.Remove(selected))order.Add(selected);
   if(hoverCard>=0&&order.Remove(hoverCard))order.Add(hoverCard);
   for(int i=order.Count-1;i>=0;i--)if(actual.Any(c=>c.Id==order[i])&&handPoses.TryGetValue(order[i],out var hit)&&(hit.Arrived&&InCard(mouse,hit.Corners)||hit.Arrived&&order[i]==hoverCard&&new Rect(hit.Center.x-55,hit.Center.y-77,110,155).Contains(mouse))){hovered=order[i];break;}
   if(!interactive)hovered=-1;hoverCard=hovered;
   float step=hand.Count<=1?110:Mathf.Min(91,850f/Math.Max(1,hand.Count-1));float total=110+(hand.Count-1)*step;float left=795-total*.5f;
   bool tick=Event.current.type==EventType.Repaint&&handFrameAt!=tableClock;if(tick)handFrameAt=tableClock;float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);int newCards=0;int hi=hand.FindIndex(c=>c.Id==hovered);
   for(int i=0;i<hand.Count;i++){var c=hand[i];float normalized=(i-(hand.Count-1)*.5f)/Math.Max(1,(hand.Count-1)*.5f);bool chosen=false;float lift=chosen?38:c.Id==hovered?24:0;float spread=hi>=0&&i!=hi?(i<hi?-10:10):0;
    Vector2 dest=new Vector2(left+i*step+55+spread,816+normalized*normalized*5);
    if(!handPoses.TryGetValue(c.Id,out var pose)){pose=new HandPose{Center=DealOrigin,DealTarget=dest,BornAt=tableClock+newCards++*.055f,Scale=.78f};handPoses[c.Id]=pose;}
    if(tick&&!Paused){float deal=Mathf.Clamp01((tableClock-pose.BornAt)/.5f);float ease=deal*deal*deal*(deal*(deal*6-15)+10);pose.Opacity=Mathf.Clamp01((tableClock-pose.BornAt)/.10f);
     if(!pose.Arrived){pose.DealTarget=Vector2.SmoothDamp(pose.DealTarget,dest,ref pose.DealVelocity,.12f,1200,dt);pose.Center=Vector2.Lerp(DealOrigin,pose.DealTarget,ease);if(deal>=1){pose.Arrived=true;pose.Velocity=Vector2.zero;}}
     else pose.Center=Vector2.SmoothDamp(pose.Center,dest,ref pose.Velocity,.12f,10000,dt);pose.Lift=Mathf.SmoothDamp(pose.Lift,lift,ref pose.LiftVelocity,.10f,10000,dt);
     float mx=c.Id==hovered?Mathf.Clamp((mouse.x-pose.Center.x)/55,-1,1):0,my=c.Id==hovered?Mathf.Clamp((mouse.y-(pose.Center.y-pose.Lift))/77,-1,1):0;float blend=1-Mathf.Exp(-13*dt);
     float targetShade=PlayerDecision&&!CardChoices(c.Id).Any()?.45f:1;pose.Shade=Mathf.Lerp(pose.Shade,targetShade,blend);pose.Pitch=Mathf.Lerp(pose.Pitch,-4-my*10,blend);pose.Yaw=Mathf.Lerp(pose.Yaw,normalized*3+mx*12,blend);pose.Roll=Mathf.Lerp(pose.Roll,normalized*1.6f+mx*.9f,blend);pose.Scale=Mathf.Lerp(pose.Scale,chosen?1.10f:c.Id==hovered?1.075f:1,blend);}
    Vector2 center=pose.Center-Vector2.up*pose.Lift;var rotation=Quaternion.Euler(pose.Pitch,pose.Yaw,pose.Roll);pose.Corners=new[]{ProjectCard(0,0,center,110*pose.Scale,155*pose.Scale,rotation),ProjectCard(1,0,center,110*pose.Scale,155*pose.Scale,rotation),ProjectCard(1,1,center,110*pose.Scale,155*pose.Scale,rotation),ProjectCard(0,1,center,110*pose.Scale,155*pose.Scale,rotation)};handPositions[c.Id]=center;
   }
   foreach(var id in handPoses.Keys.Where(id=>!hand.Any(c=>c.Id==id)).ToArray())handPoses.Remove(id);
   foreach(var c in hand.Where(c=>c.Id!=hovered))DrawMovingHandCard(c,handPoses[c.Id]);
   var hot=hand.FirstOrDefault(c=>c.Id==hovered);if(hot!=null)DrawMovingHandCard(hot,handPoses[hot.Id]);
   if(interactive&&hovered>=0&&Event.current.type==EventType.MouseDown){var card=hand.First(c=>c.Id==hovered);if(Event.current.button==0)BeginCardDrag(hovered,handPositions[hovered]);else if(Event.current.button==1)inspectedCard=card;Event.current.Use();}
   Stretch("official/seat/handCardsBg.png",new Rect(1260,856,101,32));T(actual.Count+"/"+Engine.Seats[0].Hp,new Rect(1260,854,100,34),tableCenter);
  }
  void DrawMovingHandCard(Card c,HandPose p){if(p.Opacity<=0)return;var center=p.Center-Vector2.up*p.Lift;float w=110*p.Scale,h=155*p.Scale;var rotation=Quaternion.Euler(p.Pitch,p.Yaw,p.Roll);bool chosen=false;CardShadow(center,w,h,rotation,p.Lift,p.Opacity);if(chosen)ProjectedImage(SanguoshaAssets.Image("official/card/normalcard_selected_new.png"),center,w+9,h+9,rotation,new Color(1,.93f,.6f,1));
   float shade=p.Shade;ProjectedImage(CardTexture(c),center,w,h,rotation,new Color(shade,shade,shade,p.Opacity),true);
   if(chosen&&selection.Option.UseKind!=null&&selection.Option.UseKind!=c.Kind){Rect(new Rect(center.x-w/2,center.y+h/2-23,w,23),new Color(.22f,.11f,.03f,.9f));T("视为"+Catalog.CardName(selection.Option.UseKind),new Rect(center.x-w/2,center.y+h/2-23,w,23),tableCenter);}
  }
 }
}
