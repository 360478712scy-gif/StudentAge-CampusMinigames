using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  sealed class OriginalEffect {public int frames {get;set;} public int columns {get;set;} public int rows {get;set;} public int width {get;set;} public int height {get;set;} public float duration {get;set;} public int[] crop {get;set;} public int stageWidth {get;set;}=512;public int stageHeight {get;set;}=512;}
  Dictionary<string,OriginalEffect> originalEffects;
  void DrawOriginalEffect(string name,float age,Rect stage,bool loop=false,float opacity=1){
   if(Event.current.type!=EventType.Repaint||age<0||opacity<=0)return;
   if(originalEffects==null){var file=Path.Combine(SanguoshaAssets.Root,"official/effects/metadata.json");originalEffects=File.Exists(file)?JsonConvert.DeserializeObject<Dictionary<string,OriginalEffect>>(File.ReadAllText(file)):new Dictionary<string,OriginalEffect>();}
   if(!originalEffects.TryGetValue(name,out var info)||info.frames<1)return;
   if(loop)age%=info.duration;else if(age>=info.duration)return;
   int frame=Math.Min(info.frames-1,(int)(age/info.duration*info.frames)),col=frame%info.columns,row=frame/info.columns;
   var texture=SanguoshaAssets.Image("official/effects/"+name+".png");if(texture==null)return;
   var uv=new Rect((col*(info.width+2)+1)/(float)texture.width,1-(row*(info.height+2)+1+info.height)/(float)texture.height,info.width/(float)texture.width,info.height/(float)texture.height);
   var area=new Rect(stage.x+info.crop[0]/(float)info.stageWidth*stage.width,stage.y+info.crop[1]/(float)info.stageHeight*stage.height,info.width/(float)info.stageWidth*stage.width,info.height/(float)info.stageHeight*stage.height);
   var tint=GUI.color;GUI.color=new Color(tint.r,tint.g,tint.b,tint.a*opacity);GUI.DrawTextureWithTexCoords(area,texture,uv);GUI.color=tint;
  }
 }
}
