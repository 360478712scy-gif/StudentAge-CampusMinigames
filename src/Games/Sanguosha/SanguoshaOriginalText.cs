using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
namespace StudentAge.CampusMinigames {
 public sealed partial class SanguoshaView {
  sealed class OriginalGlyph {public int index {get;set;} public float advance {get;set;}}
  sealed class OriginalFont {public int pageSize {get;set;} public int size {get;set;} public int cell {get;set;} public int columns {get;set;} public int rows {get;set;} public int baseline {get;set;} public int bearing {get;set;} public Dictionary<string,OriginalGlyph> glyphs {get;set;}}
  OriginalFont originalFont;bool fontLoaded;
  bool DrawOriginalText(string text,Rect rect,GUIStyle style){
   if(!fontLoaded){fontLoaded=true;var path=Path.Combine(SanguoshaAssets.Root,"official/font/fzkt.json");if(File.Exists(path))originalFont=JsonConvert.DeserializeObject<OriginalFont>(File.ReadAllText(path));}
   if(originalFont==null||string.IsNullOrEmpty(text))return false;
   foreach(char c in text)if(c!='\n'&&c!='\r'&&!originalFont.glyphs.ContainsKey(c.ToString()))return false;
   if(Event.current.type!=EventType.Repaint)return true;
   var texture=SanguoshaAssets.Image("official/font/fzkt.png");if(texture==null)return false;
   float size=style.fontSize>0?style.fontSize:18,scale=size/originalFont.size,lineHeight=size*1.2f,available=rect.width-style.padding.horizontal;
   var lines=new List<string>();var widths=new List<float>();string line="";float width=0;
   foreach(char c in text){if(c=='\r')continue;if(c=='\n'){lines.Add(line);widths.Add(width);line="";width=0;continue;}float advance=originalFont.glyphs[c.ToString()].advance*scale;if(style.wordWrap&&width+advance>available&&line.Length>0){lines.Add(line);widths.Add(width);line="";width=0;}line+=c;width+=advance;}lines.Add(line);widths.Add(width);
   int horizontal=(int)style.alignment%3,vertical=(int)style.alignment/3;float total=lines.Count*lineHeight;float y=rect.y+style.padding.top+(vertical==1?(rect.height-style.padding.vertical-total)*.5f:vertical==2?rect.height-style.padding.vertical-total:0);
   var previous=GUI.color;GUI.color=previous*style.normal.textColor;
   for(int n=0;n<lines.Count;n++){float x=rect.x+style.padding.left+(horizontal==1?(available-widths[n])*.5f:horizontal==2?available-widths[n]:0);foreach(char c in lines[n]){var g=originalFont.glyphs[c.ToString()];int index=g.index,page=originalFont.pageSize>0?index/originalFont.pageSize:0;if(originalFont.pageSize>0)index%=originalFont.pageSize;texture=SanguoshaAssets.Image("official/font/fzkt"+(page==0?"":"-"+page)+".png");int col=index%originalFont.columns,row=index/originalFont.columns;var uv=new Rect(col/(float)originalFont.columns,1-(row+1)/(float)originalFont.rows,1f/originalFont.columns,1f/originalFont.rows);var area=new Rect(x-originalFont.bearing*scale,y+size*.9f-originalFont.baseline*scale,originalFont.cell*scale,originalFont.cell*scale);GUI.DrawTextureWithTexCoords(area,texture,uv);x+=g.advance*scale;}y+=lineHeight;}
   GUI.color=previous;return true;
  }
 }
}
