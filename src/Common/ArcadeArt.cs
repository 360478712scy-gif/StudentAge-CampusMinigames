using System.IO;using UnityEngine;
namespace StudentAge.CampusMinigames
{
    public static class ArcadeArt
    {
        public static Sprite Load(string root,string name,System.Collections.Generic.List<UnityEngine.Object> owned){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(Path.Combine(root,"Arcade",name+".png")));t.filterMode=FilterMode.Bilinear;var s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f));owned.Add(t);owned.Add(s);return s;}
    }
}
