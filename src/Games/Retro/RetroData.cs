using System;
using System.Collections.Generic;
namespace StudentAge.Retro
{
    [Serializable] public sealed class FrameDef { public string name; public int x,y,w,h; }
    [Serializable] public sealed class AtlasDef { public FrameDef[] frames; }
    [Serializable] public sealed class EntityDef { public string kind; public float x,y,a,b; }
    [Serializable] public sealed class TileDef { public int id,flags; }
    [Serializable] public sealed class MarioMap { public string name;public int width,height,background,palette,time;public bool intro,water;public int[] tiles;public EntityDef[] entities; }
    [Serializable] public sealed class MarioData { public string[] selected;public TileDef[] tileDefs;public MarioMap[] maps; }
    [Serializable] public sealed class MapDraw { public string sprite;public int x,y,layer; }
    [Serializable] public sealed class PlatformDef { public float x,y,w,h;public bool oneWay,water; }
    [Serializable] public sealed class AnimationDef { public string name;public string[] frames; }
    [Serializable] public sealed class ContraData { public int width,height;public MapDraw[] draw;public PlatformDef[] solids;public EntityDef[] entities;public AnimationDef[] animations; }
    public struct RetroInput { public int X,Y;public bool Jump,JumpPressed,Fire,FirePressed,Run; }
    public interface IRetroCanvas
    {
        void Clear(uint color);
        void Sprite(string name,float x,float y,bool flip=false);
        void Rect(float x,float y,int w,int h,uint color);
        void Text(string text,int x,int y);
    }
    public interface IRetroPaletteCanvas { void SpritePalette(string name,float x,float y,bool flip,int palette); }
    public interface IRetroGame
    {
        bool Won{get;} bool Lost{get;}
        void Step(RetroInput input);
        void Draw(IRetroCanvas canvas);
        Queue<string> Sounds{get;}
    }
    public sealed class Body
    {
        public string Kind;public float X,Y,VX,VY,W=14,H=16,BaseX,BaseY,Clock,Timer,Extra;public int State,HP=1;
        public bool Ground,Dead,Active;public float Right=>X+W;public float Bottom=>Y+H;
        public Body(string kind,float x,float y){Kind=kind;X=BaseX=x;Y=BaseY=y;}
        public bool Overlap(Body b)=>X<b.Right&&Right>b.X&&Y<b.Bottom&&Bottom>b.Y;
    }
    public static class RetroMath
    {
        public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));
        public static float Approach(float value,float goal,float amount)=>value<goal?Math.Min(value+amount,goal):Math.Max(value-amount,goal);
    }
}
