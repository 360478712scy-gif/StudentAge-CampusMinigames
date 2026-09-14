using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace StudentAge.Retro
{
    public sealed class PixelCanvas:IRetroCanvas,IDisposable
    {
        readonly Dictionary<string,Pixels> sprites=new Dictionary<string,Pixels>();
        readonly Color32[] output=new Color32[256*240];public Texture2D Texture{get;private set;}
        sealed class Pixels{public int Width,Height;public Color32[] Data;}
        public PixelCanvas(string root)
        {
            var atlas=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!atlas.LoadImage(File.ReadAllBytes(Path.Combine(root,"atlas.png"))))throw new IOException("Invalid retro atlas");
                var all=atlas.GetPixels32();var defs=Newtonsoft.Json.JsonConvert.DeserializeObject<AtlasDef>(File.ReadAllText(Path.Combine(root,"atlas.json")));
                foreach(var f in defs.frames)
                {
                    var p=new Pixels{Width=f.w,Height=f.h,Data=new Color32[f.w*f.h]};
                    for(int y=0;y<f.h;y++)Array.Copy(all,(atlas.height-1-f.y-y)*atlas.width+f.x,p.Data,y*f.w,f.w);
                    sprites[f.name]=p;
                }
            }
            finally{UnityEngine.Object.Destroy(atlas);}
            Texture=new Texture2D(256,240,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Retro 256x240"};
        }
        public void Clear(uint color){Color32 c=new Color32((byte)(color>>16),(byte)(color>>8),(byte)color,255);for(int n=0;n<output.Length;n++)output[n]=c;}
        public void Sprite(string name,float px,float py,bool flip=false)
        {
            Pixels p;if(!sprites.TryGetValue(name,out p))return;int x=(int)Math.Floor(px),y=(int)Math.Floor(py);
            int x0=Math.Max(0,-x),x1=Math.Min(p.Width,256-x),y0=Math.Max(0,-y),y1=Math.Min(p.Height,240-y);
            for(int sy=y0;sy<y1;sy++)for(int sx=x0;sx<x1;sx++)
            {Color32 c=p.Data[sy*p.Width+(flip?p.Width-1-sx:sx)];if(c.a>127)output[(239-y-sy)*256+x+sx]=c;}
        }
        public void Rect(float px,float py,int w,int h,uint color)
        {int x=(int)px,y=(int)py;Color32 c=new Color32((byte)(color>>16),(byte)(color>>8),(byte)color,255);for(int yy=Math.Max(0,y);yy<Math.Min(240,y+h);yy++)for(int xx=Math.Max(0,x);xx<Math.Min(256,x+w);xx++)output[(239-yy)*256+xx]=c;}
        public void Text(string text,int x,int y){foreach(char ch in text.ToLowerInvariant()){Sprite("font/"+(int)ch,x,y);x+=8;}}
        public void Present(){Texture.SetPixels32(output);Texture.Apply(false,false);}
        public void Dispose(){if(Texture!=null){UnityEngine.Object.Destroy(Texture);Texture=null;}sprites.Clear();}
    }
}
