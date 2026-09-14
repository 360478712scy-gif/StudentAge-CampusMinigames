using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using StudentAge.Retro;
class Program
{
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};static int checks;
    static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
    static void Set(object o,string name,object v)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static void Main(string[] args)
    {
        string root=Path.GetFullPath(args.Length>0?args[0]:"retro/assets"),qa=Path.GetFullPath("qa/update-0.9.0/engine");Directory.CreateDirectory(qa);
        var mario=JsonSerializer.Deserialize<MarioData>(File.ReadAllText(Path.Combine(root,"mario.json")),Json);
        var contra=JsonSerializer.Deserialize<ContraData>(File.ReadAllText(Path.Combine(root,"contra.json")),Json);
        var atlas=JsonSerializer.Deserialize<AtlasDef>(File.ReadAllText(Path.Combine(root,"atlas.json")),Json);
        var names=atlas.frames.Select(x=>x.name).ToHashSet();Check(mario.selected.Length==8&&mario.selected.Distinct().Count()==8,"eight distinct maps");
        var missing=new HashSet<string>();
        for(int n=0;n<8;n++)
        {
            var game=new MarioGame(mario,n);
            for(int t=0;t<100;t++)game.Step(default);
            Check(!game.Won&&!game.Lost&&game.Lives==3,"safe initial spawn "+n);
            Check(float.IsFinite(game.Player.X)&&float.IsFinite(game.Player.Y),"finite position");
            var draw=new DrawLog(names,missing);game.Draw(draw);File.WriteAllText(Path.Combine(qa,"mario-"+(n+1)+".draw.json"),JsonSerializer.Serialize(draw.Rows,Json));
        }
        var m=new MarioGame(mario,0);for(int t=0;t<100;t++)m.Step(default);float y=m.Player.Y;
        Check(m.Player.Ground,"mario lands");m.Step(new RetroInput{Jump=true,JumpPressed=true});Check(m.Player.Y<y,"jump rises");
        var shortJump=new MarioGame(mario,0);var heldJump=new MarioGame(mario,0);for(int t=0;t<100;t++){shortJump.Step(default);heldJump.Step(default);}
        float minShort=999,minHeld=999;for(int t=0;t<60;t++){shortJump.Step(new RetroInput{JumpPressed=t==0,Jump=t<3});heldJump.Step(new RetroInput{JumpPressed=t==0,Jump=t<25});minShort=Math.Min(minShort,shortJump.Player.Y);minHeld=Math.Min(minHeld,heldJump.Player.Y);}
        Check(minHeld<minShort-10,"held jump is higher");
        var block=mario.maps.First(x=>x.name=="1-1").entities.First(x=>x.kind=="mushroom");m=new MarioGame(mario,0);Call(m,"Hit",(int)block.x/16,(int)block.y/16);
        Check(m.Actors.Any(x=>x.Kind=="mushroom"&&!x.Dead),"block yields living mushroom");
        var item=m.Actors.First(x=>x.Kind=="mushroom");m.Player.X=item.X;m.Player.Y=item.Y;Set(m,"intro",0f);m.Step(default);Check(m.Size==1,"mushroom grows player");
        var flag=m.Actors.First(x=>x.Kind=="flag");m.Player.X=flag.X-5;m.Player.Y=100;
        for(int t=0;t<230&&!m.Won;t++)m.Step(default);Check(m.Won,"flag finish completes once");
        for(int n=0;n<8;n++)
        {
            var g=new MarioGame(mario,n);Set(g,"intro",0f);Set(g,"grace",0f);
            for(int life=0;life<3;life++){Call(g,"Die");for(int t=0;t<200;t++)g.Step(default);}
            Check(g.Lost&&!g.Won,"life exhaustion "+n);
        }
        var c=new ContraGame(contra);for(int t=0;t<110;t++)c.Step(default);Check(c.Lives==3&&!c.Lost,"contra initial spawn");
        var cd=new DrawLog(names,missing);c.Draw(cd);File.WriteAllText(Path.Combine(qa,"contra.draw.json"),JsonSerializer.Serialize(cd.Rows,Json));
        c.Step(new RetroInput{Fire=true,X=1,Y=-1});Check(c.Shots.Any(x=>x.Kind=="friendly"&&x.VX>0&&x.VY<0),"eight direction shooting");
        var core=c.Actors.First(x=>x.Kind=="Boss");Set(c,"finish",.01f);for(int t=0;t<190;t++)c.Step(default);Check(c.Won&&!c.Lost,"contra finish lifecycle");
        // User-reported failures: backtracking, second pipe, crouching feet.
        var backtrack=new MarioGame(mario,0);foreach(var enemy in backtrack.Actors)enemy.Dead=true;backtrack.Player.X=300;backtrack.Player.Y=192;backtrack.Player.Ground=true;
        backtrack.Step(default);float oldCamera=backtrack.CameraX;
        for(int t=0;t<70;t++)backtrack.Step(new RetroInput{X=-1});
        Check(backtrack.Player.X<225&&backtrack.CameraX<oldCamera-50,"Mario returns left of previous camera position");
        foreach(bool grown in new[]{false,true}){
            var pipe=new MarioGame(mario,0);foreach(var enemy in pipe.Actors)enemy.Dead=true;
            if(grown)typeof(MarioGame).GetProperty("Size").SetValue(pipe,1);
            pipe.Player.H=grown?32:16;pipe.Player.X=574;pipe.Player.Y=208-pipe.Player.H;pipe.Player.Ground=true;pipe.Player.VX=93.6f;bool landed=false;
            for(int t=0;t<65;t++){pipe.Step(new RetroInput{X=1,Jump=t<27,JumpPressed=t==0});if(pipe.Player.Ground&&Math.Abs(pipe.Player.Bottom-160)<.1f&&pipe.Player.Right>608&&pipe.Player.X<640)landed=true;}
            Check(landed,"second 48px pipe reachable with ordinary held jump grown="+grown);
        }
        var duck=new MarioGame(mario,0);foreach(var enemy in duck.Actors)enemy.Dead=true;typeof(MarioGame).GetProperty("Size").SetValue(duck,1);duck.Player.H=32;duck.Player.Y=176;duck.Player.Ground=true;
        for(int t=0;t<30;t++)duck.Step(new RetroInput{Y=1});Check(duck.Player.H==16&&Math.Abs(duck.Player.Bottom-208)<.01f,"crouch collider keeps feet on floor");var duckDraw=new DrawLog(names,missing);duck.Draw(duckDraw);var sprite=duckDraw.Rows.Last(x=>x.type=="sprite"&&x.name.StartsWith("mario-mario/"));Check(Math.Abs(sprite.y+32-duck.Player.Bottom)<.01f,"crouch sprite bottom matches collision feet");duck.Step(default);Check(duck.Player.H==32&&Math.Abs(duck.Player.Bottom-208)<.01f,"standing preserves feet");
        // Regression fixtures: side contacts are not stomps; stars must expire and never survive death.
        MarioGame Arena(){var cells=Enumerable.Repeat(1,64*15).ToArray();for(int i=64*13;i<cells.Length;i++)cells[i]=2;return new MarioGame(new MarioData{selected=new[]{"arena"},tileDefs=new[]{new TileDef{id=1,flags=0},new TileDef{id=2,flags=1}},maps=new[]{new MarioMap{name="arena",width=64,height=15,time=999,tiles=cells,entities=Array.Empty<EntityDef>()}}},0);}
        List<Body> Actors(MarioGame g)=>(List<Body>)typeof(MarioGame).GetField("actors",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g);
        MarioGame Grounded(){var g=Arena();for(int i=0;i<10;i++)g.Step(default);return g;}
        foreach(int state in new[]{0,2}){var g=Grounded();Actors(g).Add(new Body("koopa",g.Player.Right-1, state==0?184:194){H=state==0?24:14,State=state,VX=-190,Active=true});g.Step(default);Check(g.Lives==2&&g.InTransition,"small Mario dies from side contact state="+state);}
        var kick=Grounded();var shell=new Body("koopa",kick.Player.Right-1,194){H=14,State=1,Active=true};Actors(kick).Add(shell);kick.Step(default);for(int i=0;i<8;i++)kick.Step(default);Check(kick.Lives==3&&shell.State==2&&shell.X>kick.Player.Right,"stationary shell kick separates and does not hit its kicker immediately");
        var stompGame=Grounded();shell=new Body("koopa",stompGame.Player.X,194){H=14,State=2,VX=0,Active=true};Actors(stompGame).Add(shell);stompGame.Player.Y=177.5f;stompGame.Player.VY=80;stompGame.Step(default);Check(stompGame.Lives==3&&shell.State==1&&stompGame.Player.VY<0,"landing from above stops a moving shell");
        var invincible=Grounded();Actors(invincible).Add(new Body("star",invincible.Player.X,invincible.Player.Y){Active=true});invincible.Step(default);Check(invincible.StarActive&&invincible.Sounds.Contains("star"),"actual star pickup activates protection and music event");
        var starDraw=new DrawLog(names,missing);invincible.Draw(starDraw);Check(starDraw.Rows.Any(x=>x.palette>0&&x.name.Contains("small_mario")),"small star Mario uses visible flashing palette");
        shell=new Body("koopa",invincible.Player.Right-1,194){H=14,State=2,VX=-190,Active=true};Actors(invincible).Add(shell);invincible.Step(default);Check(invincible.Lives==3&&shell.Dead,"star destroys rolling shell without losing life");Call(invincible,"Hurt");Check(invincible.Lives==3,"star also blocks hazards");
        Set(invincible,"star",.01f);invincible.Step(default);Check(!invincible.StarActive,"star timer expires");Call(invincible,"Hurt");Check(invincible.Lives==2,"small Mario vulnerable again after star expires");
        var fall=Grounded();Set(fall,"star",8f);Set(fall,"grace",2f);fall.Player.Y=260;fall.Step(default);Check(fall.Lives==2&&!fall.StarActive,"star does not protect pits and resets on death");for(int i=0;i<200;i++)fall.Step(default);Call(fall,"Hurt");Check(fall.Lives==1,"respawn does not inherit star or injury protection");
        var damage=Grounded();typeof(MarioGame).GetProperty("Size").SetValue(damage,1);damage.Player.H=32;damage.Player.Y=176;Call(damage,"Hurt");Call(damage,"Hurt");Check(damage.Size==0&&damage.Lives==3,"shrinking grants temporary injury protection");for(int i=0;i<130;i++)damage.Step(default);Call(damage,"Hurt");Check(damage.Lives==2,"injury protection also expires");
        var coinFrames=atlas.frames.Where(f=>f.name.StartsWith("mario-fx/coin-")).ToArray();Check(coinFrames.Length==16&&coinFrames.All(f=>f.w==16&&f.h==16),"all coin frames use full 16px source cells, not half-width slices");
        // Small collision fixtures isolate Contra's solid platform and one-way behavior.
        var simple=new ContraData{width=600,solids=new[]{new PlatformDef{x=0,y=200,w=600,h=40}},entities=Array.Empty<EntityDef>(),animations=contra.animations};
        var solid=new ContraGame(simple);for(int t=0;t<10;t++)solid.Step(default);float sx=solid.Player.X;solid.Step(new RetroInput{Y=1});Check(Math.Abs(solid.Player.X-sx)<.01f&&solid.Player.Bottom<=200.01f,"Contra idle/crouch does not push player into solid ground");solid.Step(new RetroInput{Y=1,JumpPressed=true,Jump=true});Check(solid.Player.VY<0&&solid.Player.Bottom<200,"down+jump on solid jumps without sinking");
        simple=new ContraData{width=600,solids=new[]{new PlatformDef{x=0,y=120,w=600,h=8,oneWay=true},new PlatformDef{x=0,y=200,w=600,h=40}},entities=Array.Empty<EntityDef>(),animations=contra.animations};var dropGame=new ContraGame(simple);dropGame.Step(new RetroInput{Y=1,JumpPressed=true});for(int t=0;t<40;t++)dropGame.Step(default);Check(dropGame.Player.Bottom>120&&dropGame.Player.Bottom<=200.01f,"down+jump crosses one-way platform and lands on ground");
        var returnGame=new ContraGame(simple);returnGame.Player.X=310;returnGame.Step(default);float returnCamera=returnGame.CameraX;for(int t=0;t<45;t++)returnGame.Step(new RetroInput{X=-1});Check(returnGame.CameraX<returnCamera-40&&returnGame.Player.X>=returnGame.CameraX,"Contra backtracking keeps player in view");
        var laser=new ContraGame(simple);typeof(ContraGame).GetProperty("Weapon").SetValue(laser,"L");for(int t=0;t<30;t++)laser.Step(new RetroInput{Fire=true});Check(laser.Shots.Count(x=>x.Kind=="friendly")>=2,"laser shots survive next shot at long range");
        var rnd=new Random(1717);
        for(int n=0;n<8;n++)
        {
            var g=new MarioGame(mario,n);for(int t=0;t<3000&&!g.Lost&&!g.Won;t++)
            {g.Step(new RetroInput{X=t%400<360?1:-1,Jump=t%45<24,JumpPressed=t%45==0,Run=true,FirePressed=t%14==0});Check(float.IsFinite(g.Player.X)&&float.IsFinite(g.Player.Y),"finite random mario");if(t%120==0)g.Draw(new DrawLog(names,missing));}
        }
        for(int run=0;run<10;run++)
        {
            var g=new ContraGame(contra);for(int t=0;t<3000&&!g.Lost&&!g.Won;t++)
            {g.Step(new RetroInput{X=1,Y=t%120<20?-1:0,Jump=t%55<20,JumpPressed=t%55==0,Fire=true});Check(float.IsFinite(g.Player.X)&&float.IsFinite(g.Player.Y),"finite random contra");if(t%120==0)g.Draw(new DrawLog(names,missing));}
        }
        File.WriteAllText(Path.Combine(qa,"missing-sprites.json"),JsonSerializer.Serialize(missing));
        Check(missing.Count==0,"all sampled sprite frames exist: "+string.Join(",",missing));
        Console.WriteLine("RETRO_ENGINE_OK checks="+checks+" missingSprites="+string.Join(",",missing));
    }
    sealed class DrawLog:IRetroCanvas,IRetroPaletteCanvas
    {
        readonly HashSet<string> names,missing;public List<Draw> Rows=new List<Draw>();
        public DrawLog(HashSet<string> n,HashSet<string> m){names=n;missing=m;}
        public void Clear(uint color)=>Rows.Add(new Draw{type="clear",color=color});
        public void Sprite(string name,float x,float y,bool flip=false){if(!names.Contains(name))missing.Add(name);Rows.Add(new Draw{type="sprite",name=name,x=x,y=y,flip=flip});}
        public void SpritePalette(string name,float x,float y,bool flip,int palette){Sprite(name,x,y,flip);Rows[Rows.Count-1].palette=palette;}
        public void Rect(float x,float y,int w,int h,uint color)=>Rows.Add(new Draw{type="rect",x=x,y=y,w=w,h=h,color=color});
        public void Text(string text,int x,int y)=>Rows.Add(new Draw{type="text",name=text,x=x,y=y});
    }
    sealed class Draw{public string type,name;public float x,y;public int w,h,palette;public uint color;public bool flip;}
}
