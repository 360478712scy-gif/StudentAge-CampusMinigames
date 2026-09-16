using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Config;
using Newtonsoft.Json;
using Sdk;
using UnityEngine;
using StudentAge.Sanguosha;
using StudentAge.CampusUno;

namespace StudentAge.CampusMinigames {
 public sealed class SanguoshaDialogueLine {
  public string Text="";public string Expression="neutral";public float Seconds=2.2f;
 }
 public sealed class SanguoshaExpression {
  public string Image="";public int FaceId=-1;public float Zoom=1,CenterX=.5f,CenterY=.5f;
 }
 public sealed class SanguoshaSpeaker {
  public Dictionary<string,SanguoshaExpression> Expressions=new Dictionary<string,SanguoshaExpression>();
  public Dictionary<string,SanguoshaDialogueLine> Lines=new Dictionary<string,SanguoshaDialogueLine>();
 }
 public sealed class SanguoshaDialogueConfig {
  public int Schema=1;public bool Enabled=true;public float MaxBubbleWidth=300;
  public Dictionary<string,SanguoshaDialogueLine> Lines=new Dictionary<string,SanguoshaDialogueLine>{
   {"slash",new SanguoshaDialogueLine{Text="杀！",Expression="slash"}},
   {"jink",new SanguoshaDialogueLine{Text="闪！",Expression="jink"}}
  };
  public Dictionary<string,SanguoshaSpeaker> Speakers=new Dictionary<string,SanguoshaSpeaker>();
  public const string Table="SanguoshaDialogueCfg";
  /// <summary>提供过对话表的 Cfgs/zh-cn 目录（先默认后覆盖）；相对图片路径按后者优先查找。</summary>
  public static readonly List<string> SourceDirs=new List<string>();
  public static string ResolveRelative(string path){for(int i=SourceDirs.Count-1;i>=0;i--){string full=Path.GetFullPath(Path.Combine(SourceDirs[i],path));if(File.Exists(full))return full;}return Path.GetFullPath(Path.Combine(CampusResources.CfgRoot,path));}
  // 默认说话人：本插件 Cfgs/zh-cn/SanguoshaDialogueCfg.json 已包含；其他 Mod 的同名文件按键合并覆盖。
  public static SanguoshaDialogueConfig Load(){var value=new SanguoshaDialogueConfig();
   foreach(string key in new[]{"player_female","player_male","3","101","102","103","104","105","201","202","203","204"})value.Speakers[key]=new SanguoshaSpeaker{Expressions=new Dictionary<string,SanguoshaExpression>{{"neutral",new SanguoshaExpression()},{"slash",new SanguoshaExpression()},{"jink",new SanguoshaExpression()}}};
   SourceDirs.Clear();foreach(string dir in ModCfgLocator.CfgDirectories()){string file=Path.Combine(dir,Table+".json");if(!File.Exists(file))continue;
    try{JsonConvert.PopulateObject(File.ReadAllText(file),value);SourceDirs.Add(dir);if(value.Schema!=1)throw new InvalidDataException("未知对话配置版本");}
    catch(Exception e){Debug.LogWarning("[Sanguosha] 对话配置读取失败，已跳过 "+file+"："+e.Message);}}
   value.Lines=value.Lines??new Dictionary<string,SanguoshaDialogueLine>();value.Speakers=value.Speakers??new Dictionary<string,SanguoshaSpeaker>();return value;}
 }
 public sealed partial class SanguoshaView {
  sealed class Participant {
   public string Key,Name,Head;public int RoleId,Cloth,Grade;public string Speech="",Expression="neutral";public float At,Until;
   public Texture2D BubbleTexture;public Vector2 BubbleSize;public float BubbleAnchor;
   public SanguoshaSpeaker Config;public readonly Dictionary<string,Texture2D> Images=new Dictionary<string,Texture2D>();
  }
  readonly Participant[] participants=new Participant[2];SanguoshaDialogueConfig dialogue;int participantGeneration;GUIStyle speechStyle;
  static readonly Rect[] ParticipantRects={new Rect(110,688,124,124),new Rect(1298,101,116,116)};
  public bool ParticipantPortraitsReady=>participants.All(p=>p!=null&&p.Images.ContainsKey("neutral"));
  public string ParticipantHead(int actor)=>participants[actor]?.Head;
  public string ParticipantSpeech(int actor)=>participants[actor]!=null&&tableClock<participants[actor].Until?participants[actor].Speech:"";
  void BuildParticipants(){DisposeParticipants();dialogue=SanguoshaDialogueConfig.Load();int generation=participantGeneration;
   for(int actor=0;actor<2;actor++){
    int id=actor==0?0:Session.NpcId;var role=Session.TestMode?null:Singleton<RoleMgr>.Ins.GetRole(id);int grade=Session.TestMode?1:Session.PlayerGrade;
    var gender=actor==0?Session.PlayerGender:GenderDefine.Unknown;
    var p=new Participant{RoleId=id,Cloth=role?.ClothId??0,Grade=grade,Key=actor==0?(gender==GenderDefine.Female?"player_female":"player_male"):id.ToString(),Name=actor==0?Session.PlayerName:Session.Name};participants[actor]=p;
    if(!Cfg.PersonCfgMap.TryGetValue(id,out var cfg))continue;p.Head=cfg.GetHeadIcon(gender,p.Cloth,grade);
    dialogue.Speakers.TryGetValue(p.Key,out p.Config);p.Config=p.Config??new SanguoshaSpeaker();p.Config.Expressions=p.Config.Expressions??new Dictionary<string,SanguoshaExpression>();p.Config.Lines=p.Config.Lines??new Dictionary<string,SanguoshaDialogueLine>();
    // Preload bounded expression assets outside OnGUI; callbacks cannot revive a closed/replaced view.
    var expressions=p.Config.Expressions.Take(32).ToDictionary(x=>x.Key,x=>x.Value);if(!expressions.ContainsKey("neutral"))expressions["neutral"]=new SanguoshaExpression();
    foreach(var pair in expressions){var expression=pair.Value??new SanguoshaExpression();string path=expression.Image;
     if(string.IsNullOrWhiteSpace(path)&&expression.FaceId>=0)path=RoleMgr.GetExpressionIcon(cfg,p.Cloth,expression.FaceId,grade);
     if(string.IsNullOrWhiteSpace(path))path=p.Head;if(string.IsNullOrEmpty(path))continue;string key=pair.Key;
     Action<Sprite> loaded=s=>{if(Closed||generation!=participantGeneration)return;if(s==null){Debug.LogWarning("[Sanguosha] 人物图不存在："+path);return;}try{var t=RoundParticipant(s,expression);if(p.Images.TryGetValue(key,out var old))Destroy(old);p.Images[key]=t;}catch(Exception e){Debug.LogWarning("[Sanguosha] 人物头像读取失败："+e.Message);}};
     try{if(path.StartsWith("native:",StringComparison.Ordinal))LoadParticipantNative(path.Substring(7),loaded);else if(Path.IsPathRooted(path))ResMgr.LoadExternSpriteAsync(path,loaded,false);else if(path.StartsWith("Mods",StringComparison.Ordinal))ResMgr.LoadExternSpriteAsync(Singleton<ModCtrl>.Ins.GetFullUrl(path),loaded,false);else if(path==p.Head)LoadParticipantNative(path,loaded);else ResMgr.LoadExternSpriteAsync(SanguoshaDialogueConfig.ResolveRelative(path),loaded,false);}
     catch(Exception e){Debug.LogWarning("[Sanguosha] 人物表情读取失败："+e.Message);}
    }
   }
  }
  static void LoadParticipantNative(string path,Action<Sprite> loaded)=>ResMgr.LoadSpriteAsync("Textures/"+LocalizationMgr.GetLocalizeUrl(path).Replace('\\','/'),loaded);
  static Texture2D RoundParticipant(Sprite sprite,SanguoshaExpression expression){
   var area=sprite.textureRect;float zoom=Mathf.Clamp(expression.Zoom,1,4),size=Mathf.Min(area.width,area.height)/zoom;
   float x=area.x+Mathf.Clamp01(expression.CenterX)*area.width-size/2,y=area.y+Mathf.Clamp01(expression.CenterY)*area.height-size/2;
   x=Mathf.Clamp(x,area.x,area.xMax-size);y=Mathf.Clamp(y,area.y,area.yMax-size);
   var rt=RenderTexture.GetTemporary(256,256,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;Texture2D image=null;
   try{Graphics.Blit(sprite.texture,rt,new Vector2(size/sprite.texture.width,size/sprite.texture.height),new Vector2(x/sprite.texture.width,y/sprite.texture.height));RenderTexture.active=rt;image=new Texture2D(256,256,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,256,256),0,0);var pixels=image.GetPixels();
    for(int yy=0;yy<256;yy++)for(int xx=0;xx<256;xx++){int i=yy*256+xx;float radius=Vector2.Distance(new Vector2(xx+.5f,yy+.5f),new Vector2(128,128));var source=pixels[i];var backing=new Color(.18f,.13f,.09f,1);pixels[i]=Color.Lerp(backing,new Color(source.r,source.g,source.b,1),source.a);if(radius>121){float rim=(radius-121)/6;pixels[i]=Color.Lerp(new Color(.66f,.51f,.30f),new Color(.17f,.11f,.07f),rim);}pixels[i].a=Mathf.Clamp01(128-radius);}
    image.SetPixels(pixels);image.Apply(false,true);image.filterMode=FilterMode.Bilinear;image.wrapMode=TextureWrapMode.Clamp;return image;
   }catch{if(image!=null)Destroy(image);throw;}finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
  }
  void RecordParticipantSpeech(MatchEvent ev){if(ev.Actor<0||ev.Actor>1||ev.JudgmentStage!=null||!(ev.Text.Contains("使用")||ev.Text.Contains("打出")))return;if(ev.Sound=="slash"||ev.Sound=="jink")SpeakParticipant(ev.Actor,ev.Sound);}
  // Future story hooks may use arbitrary configured line keys; card events only call slash/jink.
  public void SpeakParticipant(int actor,string lineKey){if(Closed||dialogue==null||!dialogue.Enabled||actor<0||actor>1)return;var p=participants[actor];if(p==null)return;SanguoshaDialogueLine line=null;p.Config?.Lines?.TryGetValue(lineKey,out line);if(line==null)dialogue.Lines.TryGetValue(lineKey,out line);if(line==null||string.IsNullOrWhiteSpace(line.Text))return;
   p.Speech=line.Text.Length>240?line.Text.Substring(0,240):line.Text;p.Expression=string.IsNullOrEmpty(line.Expression)?"neutral":line.Expression;p.At=tableClock;p.Until=tableClock+Mathf.Clamp(line.Seconds,.8f,15);
  }
  string LayoutSpeech(Participant p,out float width,out float height){float max=Mathf.Clamp(dialogue.MaxBubbleWidth,150,360),line=0;int lines=1;const float font=26;string text="";width=0;
   foreach(char c in p.Speech){if(c=='\r')continue;float advance=originalFont!=null&&originalFont.glyphs.TryGetValue(c.ToString(),out var glyph)?glyph.advance*font/originalFont.size:c<128?font*.6f:font;
    if(c=='\n'||line+advance>max-36){width=Mathf.Max(width,line);if(lines==7){text=text.TrimEnd();if(text.Length>0)text=text.Substring(0,text.Length-1)+"…";break;}line=0;lines++;text+="\n";if(c=='\n')continue;}text+=c;line+=advance;}
   width=Mathf.Clamp(Mathf.Max(width,line)+36,88,max);height=lines*font*1.2f+28;return text;
  }
  public Rect ParticipantBubble(int actor){var p=participants[actor];if(p==null||string.IsNullOrEmpty(p.Speech))return new Rect();LayoutSpeech(p,out float width,out float height);
   return actor==0?new Rect(Mathf.Max(103,ParticipantRects[0].center.x-width+22),644-height,width,height):new Rect(Mathf.Min(1437-width,ParticipantRects[1].center.x-22),245,width,height);
  }
  void DrawParticipants(){if(dialogue==null||!dialogue.Enabled)return;foreach(int actor in new[]{0,1}){var p=participants[actor];if(p==null)continue;bool speaking=tableClock<p.Until&&!string.IsNullOrEmpty(p.Speech);if(!p.Images.TryGetValue(speaking?p.Expression:"neutral",out var portrait))p.Images.TryGetValue("neutral",out portrait);if(portrait!=null)GUI.DrawTexture(ParticipantRects[actor],portrait);
    if(!speaking)continue;var r=ParticipantBubble(actor);float alpha=Mathf.Min(Mathf.Clamp01((tableClock-p.At)/.12f),Mathf.Clamp01((p.Until-tableClock)/.23f));var old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*alpha);
    DrawSpeechBubble(p,r,actor);
    if(speechStyle==null)speechStyle=new GUIStyle(tableCenter){fontSize=26,wordWrap=true,padding=new RectOffset(),normal={textColor=new Color(1,.92f,.74f)}};
    T(LayoutSpeech(p,out _,out _),new Rect(r.x+18,r.y+14,r.width-36,r.height-28),speechStyle);GUI.color=old;
   }}
  static float NineCoordinate(float x,float length,int source){const float edge=10;return x<edge?x:x>length-edge?source-(length-x):edge+(x-edge)/(length-2*edge)*(source-2*edge);}
  static Color SampleSpeech(Color[] pixels,int w,int h,float x,float y){x=Mathf.Clamp(x-.5f,0,w-1);y=Mathf.Clamp(h-y-.5f,0,h-1);int ix=(int)x,iy=(int)y;return Color.Lerp(Color.Lerp(pixels[iy*w+ix],pixels[iy*w+Math.Min(ix+1,w-1)],x-ix),Color.Lerp(pixels[Math.Min(iy+1,h-1)*w+ix],pixels[Math.Min(iy+1,h-1)*w+Math.Min(ix+1,w-1)],x-ix),y-iy);}
  void DrawSpeechBubble(Participant p,Rect r,int actor){const float tailHeight=16;float anchor=Mathf.Clamp(ParticipantRects[actor].center.x-r.x,22,r.width-22);float pad=actor==1?tailHeight:0;
   if(p.BubbleTexture==null||p.BubbleSize!=r.size||Mathf.Abs(p.BubbleAnchor-anchor)>.01f){if(p.BubbleTexture!=null)Destroy(p.BubbleTexture);var source=SanguoshaAssets.Image("official/dialogs/speech-bg.png");if(source==null)return;
    var src=source.GetPixels();var fill=source.GetPixel(source.width/2,source.height/2);var rim=source.GetPixel(source.width/2,source.height-2);int width=Mathf.CeilToInt(r.width*2),height=Mathf.CeilToInt((r.height+tailHeight)*2);var pixels=new Color[width*height];
    for(int yy=0;yy<height;yy++)for(int xx=0;xx<width;xx++){float x=(xx+.5f)/2,y=(yy+.5f)/2,py=y-pad;Color color=Color.clear;if(py>=0&&py<r.height)color=SampleSpeech(src,source.width,source.height,NineCoordinate(x,r.width,source.width),NineCoordinate(py,r.height,source.height));
     float q=actor==0?py-r.height:-py;if(q>=-3&&q<=tailHeight){float half=11*(1-Mathf.Max(0,q)/tailHeight),distance=half-Mathf.Abs(x-anchor);if(distance>=-.5f){float edge=Mathf.Clamp01((1.1f-distance)/.8f)*Mathf.Clamp01(1+q/3);color=Color.Lerp(fill,rim,edge);color.a*=Mathf.Clamp01(distance+.5f);}}
     pixels[(height-1-yy)*width+xx]=color;
    }
    p.BubbleTexture=new Texture2D(width,height,TextureFormat.RGBA32,false);p.BubbleTexture.SetPixels(pixels);p.BubbleTexture.Apply(false,true);p.BubbleTexture.filterMode=FilterMode.Bilinear;p.BubbleTexture.wrapMode=TextureWrapMode.Clamp;p.BubbleSize=r.size;p.BubbleAnchor=anchor;
   }
   GUI.DrawTexture(new Rect(r.x,r.y-pad,r.width,r.height+tailHeight),p.BubbleTexture);
  }
  void DisposeParticipants(){participantGeneration++;for(int i=0;i<participants.Length;i++){if(participants[i]!=null){foreach(var image in participants[i].Images.Values)if(image!=null)Destroy(image);if(participants[i].BubbleTexture!=null)Destroy(participants[i].BubbleTexture);}participants[i]=null;}}
 }
}
