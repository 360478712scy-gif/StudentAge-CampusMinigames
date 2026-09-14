using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;
namespace StudentAge.Retro
{
    // Native, fixed 60 Hz simulation. Map/sprite provenance lives beside the assets.
    // Units are original 16-pixel blocks; the host game's timeScale remains paused.
    public sealed class MarioGame:IRetroGame
    {
        const float Dt=1f/60;
        readonly MarioData data;readonly int stage;readonly Dictionary<string,MarioMap> maps;
        readonly Dictionary<int,int> flags;readonly Dictionary<int,EntityDef> blockItems=new Dictionary<int,EntityDef>();
        readonly Dictionary<int,int> hits=new Dictionary<int,int>();readonly List<Body> actors=new List<Body>();
        readonly List<Body> platforms=new List<Body>();readonly List<Body> projectiles=new List<Body>();
        readonly List<Body> particles=new List<Body>();int[] tiles;MarioMap map;
        float age,intro=0,death,finish,grace,star,fireDelay,pipeDelay,checkpoint;int facing=1,sub;bool flagging;
        float clock,jumpBuffer,coyote,jumpMinimum;public float CameraX{get;private set;}public Body Player{get;private set;}
        public int Size{get;private set;}public int Lives{get;private set;}=Tuning.Int("Mario","Lives");public int Score{get;private set;}public int Coins{get;private set;}
        public bool Won{get;private set;}public bool Lost{get;private set;}public string MapName=>map.name;public int Palette=>map.palette;public bool IsWater=>map.water;public bool InTransition=>death>0||finish>0;
        public Queue<string> Sounds{get;}=new Queue<string>();public IReadOnlyList<Body> Actors=>actors;
        public MarioGame(MarioData data,int selectedStage)
        {
            this.data=data;stage=selectedStage;if(stage<0||stage>=data.selected.Length)throw new ArgumentOutOfRangeException(nameof(selectedStage));
            maps=data.maps.ToDictionary(x=>x.name);flags=data.tileDefs.ToDictionary(x=>x.id,x=>x.flags);
            string first=data.selected[stage];if(maps[first].intro&&maps.ContainsKey(first+"_1")){first+="_1";sub=1;}Load(first,-1);
        }
        void Sound(string sound){if(Sounds.Count<32)Sounds.Enqueue(sound);}
        void Load(string name,int from)
        {
            map=maps[name];tiles=(int[])map.tiles.Clone();clock=map.time*Tuning.Get("Mario","TimeScale");actors.Clear();platforms.Clear();projectiles.Clear();particles.Clear();blockItems.Clear();hits.Clear();
            Player=new Body("player",48,192){W=12,H=Size>0?32:16};CameraX=0;pipeDelay=.8f;flagging=false;
            foreach(var e in map.entities)
            {
                string k=e.kind;
                if(k=="spawn"){Player.X=e.x+2;Player.Y=e.y+16-Player.H;}
                if(k=="pipespawn"&&(int)e.a==from){Player.X=e.x+2;Player.Y=e.y-Player.H;}
                if(new[]{"mushroom","oneup","star","manycoins","vine"}.Contains(k)){blockItems[(int)e.y/16*map.width+(int)e.x/16]=e;continue;}
                if(k.StartsWith("platform")||k=="seesaw")
                {
                    float width=e.a>=1&&e.a<=8?e.a*16:48;
                    platforms.Add(new Body(k,e.x,e.y){W=width,H=8,Extra=e.b});
                    if(k=="seesaw")platforms.Add(new Body(k,e.x+96,e.y+24){W=48,H=8,Extra=1});
                }
                else if(k.StartsWith("goomba")||k.StartsWith("koopa")||k=="plant"||k=="hammerbro"||k=="bowser"||k=="bulletbill")
                {
                    var b=new Body(k,e.x+(k.EndsWith("half")?8:0),e.y){VX=-30,H=k.StartsWith("koopa")?24:k=="hammerbro"?32:k=="bowser"?32:16};
                    b.Y=e.y+16-b.H;b.BaseY=b.Y;if(k=="bowser"){b.W=32;b.HP=5;}actors.Add(b);
                }
                else if(k.StartsWith("castlefire")||k=="spring"||k=="flag"||k=="axe")actors.Add(new Body(k,e.x,e.y){Extra=e.a});
            }
            if(checkpoint>0&&name==data.selected[stage]){Player.X=checkpoint;Player.Y=128;}
            CameraX=RetroMath.Clamp(Player.X-112,0,Math.Max(0,map.width*16-256));
        }
        int Index(int x,int y)=>y*map.width+x;
        int Flags(int x,int y){if(x<0||x>=map.width||y<0||y>=15)return 0;int v;return flags.TryGetValue(tiles[Index(x,y)],out v)?v:0;}
        bool Solid(int x,int y,bool upward=false){int f=Flags(x,y);return (f&1)!=0&&((f&2)==0||upward);}
        void Move(Body b,bool human=false)
        {
            float dx=b.VX*Dt,dy=b.VY*Dt,oldBottom=b.Bottom;b.Ground=false;
            b.X+=dx;
            for(int y=(int)Math.Floor(b.Y/16);y<=(int)Math.Floor((b.Bottom-.01f)/16);y++)
            {
                int x=(int)Math.Floor((dx>0?b.Right-.01f:b.X)/16);
                if(dx!=0&&Solid(x,y)){b.X=dx>0?x*16-b.W:(x+1)*16;if(human)b.VX=0;else b.VX=-b.VX;break;}
            }
            b.Y+=dy;
            for(int x=(int)Math.Floor((b.X+.1f)/16);x<=(int)Math.Floor((b.Right-.1f)/16);x++)
            {
                int y=(int)Math.Floor((dy>0?b.Bottom-.01f:b.Y)/16);
                if(dy!=0&&Solid(x,y,human&&dy<0))
                {
                    b.Y=dy>0?y*16-b.H:(y+1)*16;b.Ground=dy>0;b.VY=0;
                    if(human&&dy<0)Hit(x,y);break;
                }
            }
            if(b.VY>=0)foreach(var p in platforms)
                if(b.Right>p.X&&b.X<p.Right&&oldBottom<=p.Y+2&&b.Bottom>=p.Y&&b.Bottom<p.Y+10){b.Y=p.Y-b.H;b.VY=0;b.Ground=true;if(human){b.X+=p.VX*Dt;p.State=1;}break;}
            if(human)b.X=RetroMath.Clamp(b.X,0,map.width*16-b.W);
        }
        bool CanStand(){float top=Player.Bottom-32;for(int y=(int)Math.Floor(top/16);y<=(int)Math.Floor((Player.Y-.01f)/16);y++)for(int x=(int)Math.Floor((Player.X+.1f)/16);x<=(int)Math.Floor((Player.Right-.1f)/16);x++)if(Solid(x,y))return false;return true;}
        void Hit(int x,int y)
        {
            int i=Index(x,y),f=Flags(x,y);if((f&12)==0&&(f&2)==0){Sound("bump");return;}
            EntityDef item;blockItems.TryGetValue(i,out item);
            if((f&4)!=0&&(f&8)==0&&Size>0&&item==null)
            {
                tiles[i]=1;Score+=50;Sound("break");
                for(int n=0;n<4;n++)particles.Add(new Body("brick",x*16+8,y*16+8){VX=(n%2*2-1)*70,VY=-110-n/2*40,Timer=.7f});
            }
            else
            {
                string kind=item==null?"coin":item.kind;
                if(kind=="manycoins"||kind=="coin")
                {Coins++;Score+=200;Sound("coin");particles.Add(new Body("coin",x*16+4,y*16-8){VY=-140,Timer=.5f});}
                else if(kind=="vine")actors.Add(new Body("vine",x*16,y*16-64){H=64,W=16,Extra=item.a});
                else{if(kind=="mushroom"&&Size>0)kind="flower";actors.Add(new Body(kind,x*16,y*16-16){VX=kind=="flower"?0:45,VY=kind=="star"?-150:0,Timer=.35f,Active=true});Sound("powerup-appear");}
                int count;hits.TryGetValue(i,out count);hits[i]=count+1;
                if(kind!="manycoins"||count>=6){tiles[i]=map.palette==2?114:map.palette==3?117:113;blockItems.Remove(i);}
            }
            foreach(var b in actors)if(!b.Dead&&IsEnemy(b)&&b.X<x*16+16&&b.Right>x*16&&Math.Abs(b.Bottom-y*16)<5){b.Dead=true;Score+=100;}
            if(Coins>=100){Coins-=100;Lives++;Sound("oneup");}
        }
        public void Step(RetroInput input)
        {
            if(Won||Lost)return;age+=Dt;grace=Math.Max(0,grace-Dt);star=Math.Max(0,star-Dt);fireDelay-=Dt;pipeDelay-=Dt;
            if(intro>0){intro-=Dt;return;}
            if(death>0){death+=Dt;if(death>.35f){Player.VY+=700*Dt;Player.Y+=Player.VY*Dt;}if(death>2.1f){if(Lives<=0){Lost=true;return;}death=0;Size=0;Load(map.name,-1);intro=.65f;}return;}
            if(finish>0)
            {
                finish+=Dt;if(flagging&&finish<1.1f)Player.Y=RetroMath.Approach(Player.Y,192,100*Dt);
                else{Player.VX=60;Player.VY+=900*Dt;Move(Player,true);}if(finish>3.2f)Won=true;return;
            }
            clock-=2.4f*Dt;if(clock<=0){Die();return;}
            bool grounded=Player.Ground;float oldBottom=Player.Bottom;
            coyote=grounded?.09f:Math.Max(0,coyote-Dt);jumpBuffer=input.JumpPressed?.12f:Math.Max(0,jumpBuffer-Dt);jumpMinimum=Math.Max(0,jumpMinimum-Dt);
            if(input.X!=0){facing=input.X;float goal=(input.Run?153.6f:93.6f)*input.X;float accel=(grounded?(Math.Sign(Player.VX)!=input.X&&Math.Abs(Player.VX)>1?500:input.Run?200:135):125);Player.VX=RetroMath.Approach(Player.VX,goal,accel*Dt);}
            else if(grounded)Player.VX=RetroMath.Approach(Player.VX,0,190*Dt);
            if(jumpBuffer>0&&(coyote>0||map.water)){Player.VY=map.water?-115:-(input.Run?345:325);jumpMinimum=.085f;jumpBuffer=coyote=0;grounded=false;Sound("jump");}
            Player.VY=Math.Min(280,Player.VY+(map.water?230:Player.VY<0&&(input.Jump||jumpMinimum>0)?720:1100)*Dt);
            if(input.Y>0&&grounded&&Size>0){Player.Y=oldBottom-16;Player.H=16;Player.VX=RetroMath.Approach(Player.VX,0,200*Dt);}
            else if(Size>0&&Player.H<32&&CanStand()){Player.Y-=16;Player.H=32;}
            Move(Player,true);
            if(Size==2&&input.FirePressed&&fireDelay<=0&&projectiles.Count(p=>p.Kind=="fireball")<2){projectiles.Add(new Body("fireball",Player.X+facing*8,Player.Y+10){W=8,H=8,VX=facing*220,VY=90,Timer=2.5f,Active=true});fireDelay=.15f;Sound("fire");}
            CameraX=RetroMath.Clamp(Player.X-112,0,Math.Max(0,map.width*16-256));
            for(int yy=(int)Player.Y/16;yy<=(int)(Player.Bottom-1)/16;yy++)for(int xx=(int)Player.X/16;xx<=(int)(Player.Right-1)/16;xx++)
                if((Flags(xx,yy)&16)!=0){tiles[Index(xx,yy)]=1;Coins++;Score+=200;Sound("coin");}
            foreach(var p in platforms)
            {
                float lastX=p.X;p.Clock+=Dt;
                if(p.Kind=="platformright")p.X=p.BaseX+(float)Math.Sin(p.Clock*.8f)*36;
                else if(p.Kind=="platformfall"||p.Kind=="seesaw"){if(p.State==1)p.Y+=40*Dt;else p.Y=RetroMath.Approach(p.Y,p.BaseY,12*Dt);}
                else if(p.Kind=="platformup")p.Y=p.BaseY+(float)Math.Sin(p.Clock)*32;
                else p.Y=p.BaseY+(((p.Clock*32)%208)*(p.Kind.EndsWith("down")?1:-1));
                if(p.Y>248)p.Y-=240;if(p.Y<-32)p.Y+=240;p.VX=(p.X-lastX)/Dt;
            }
            foreach(var b in actors.ToArray())UpdateActor(b,input,oldBottom);
            foreach(var b in projectiles.ToArray())
            {
                b.Timer-=Dt;if(b.Kind=="fireball"){b.VY+=700*Dt;float v=b.VX;Move(b);if(b.Ground)b.VY=-180;if(v!=b.VX)b.Dead=true;
                    foreach(var target in actors)if(!target.Dead&&IsEnemy(target)&&b.Overlap(target)){if(--target.HP<=0){target.Dead=true;Score+=100;}b.Dead=true;Sound("kick");break;}}
                else{b.X+=b.VX*Dt;b.Y+=b.VY*Dt;if(b.Kind=="hammer")b.VY+=450*Dt;if(b.Overlap(Player))Hurt();}
                if(b.Timer<0||b.Y>270||b.X<CameraX-32||b.X>CameraX+300)b.Dead=true;
            }
            projectiles.RemoveAll(b=>b.Dead);actors.RemoveAll(b=>b.Dead&&b.Kind!="bowser");
            foreach(var b in particles){b.X+=b.VX*Dt;b.Y+=b.VY*Dt;b.VY+=450*Dt;b.Timer-=Dt;}particles.RemoveAll(b=>b.Timer<=0);
            foreach(var e in map.entities)
            {
                if(e.kind=="checkpoint"&&Player.X>=e.x)checkpoint=e.x;
                if(e.kind=="pipe"&&pipeDelay<=0&&Math.Abs(Player.X-e.x)<22&&Math.Abs(Player.Bottom-e.y)<28&&(input.Y>0||input.X>0&&Player.VX==0))
                {string target=data.selected[stage]+((int)e.a==0?"":"_"+(int)e.a);if(maps.ContainsKey(target)){int old=sub;sub=(int)e.a;Load(target,old);Sound("pipe");return;}}
            }
            if(Player.Y>256)Die();
        }
        bool IsEnemy(Body b)=>b.Kind.StartsWith("goomba")||b.Kind.StartsWith("koopa")||b.Kind=="plant"||b.Kind=="hammerbro"||b.Kind=="bowser"||b.Kind=="bulletbill";
        void UpdateActor(Body b,RetroInput input,float oldBottom)
        {
            if(b.Dead)return;if(!b.Active){if(b.X>CameraX+280)return;b.Active=true;}if(b.X<CameraX-64)return;b.Clock+=Dt;b.Timer-=Dt;
            string k=b.Kind;
            if(k=="flag"||k=="axe"){if(Player.X+12>=b.X&&Player.X<b.X+28){finish=.01f;flagging=k=="flag";Sound("complete");if(flagging){Player.X=b.X-10;Score+=1000;}else foreach(var a in actors)if(a.Kind=="bowser")a.Dead=true;}return;}
            if(k.StartsWith("castlefire")){int count=b.Extra>0?(int)b.Extra:6;for(int n=0;n<count;n++){double a=b.Clock*(k.EndsWith("ccw")?-1:1);var ball=new Body("hot",b.X+8+(float)Math.Cos(a)*n*8,b.Y+8+(float)Math.Sin(a)*n*8){W=7,H=7};if(ball.Overlap(Player))Hurt();}return;}
            if(k=="spring"){if(Player.VY>=0&&Player.Right>b.X&&Player.X<b.X+16&&oldBottom<=b.Y+8&&Player.Bottom>=b.Y){Player.Y=b.Y-Player.H;Player.VY=input.Jump?-420:-300;Sound("jump");}return;}
            if(k=="vine"){if(input.Y<0&&Math.Abs(Player.X-b.X)<18){Player.Y-=80*Dt;Player.VY=0;if(Player.Y<0){string target=data.selected[stage]+"_"+(int)b.Extra;if(maps.ContainsKey(target)){int old=sub;sub=(int)b.Extra;Load(target,old);}}}return;}
            if(k=="plant"){b.VX=0;b.Y=b.BaseY+24*(.5f+.5f*(float)Math.Sin(b.Clock*1.3f));if(Math.Abs(Player.X-b.X)<20)b.Y=b.BaseY+24;b.H=23;}
            else if(k=="bulletbill")
            {if(b.Timer<=0&&Math.Abs(Player.X-b.X)>48){projectiles.Add(new Body("bullet",b.X,b.Y){W=16,H=14,VX=Player.X<b.X?-95:95,Timer=4});b.Timer=2.6f;}return;}
            else if(k=="bowser")
            {b.VX=(float)Math.Sin(b.Clock)*18;b.VY+=600*Dt;if(b.Ground&&b.Timer<=0){b.VY=-150;b.Timer=1.8f;projectiles.Add(new Body("bowserfire",b.X,b.Y+8){W=22,H=7,VX=-75,Timer=5});}Move(b);}
            else if(k=="flower"){b.VX=0;}
            else
            {
                b.VY=Math.Min(240,b.VY+1000*Dt);
                if(k.Contains("flying")&&b.Ground)b.VY=-180;
                if(k=="star"&&b.Ground)b.VY=-230;
                if(k=="hammerbro"&&b.Timer<=0){projectiles.Add(new Body("hammer",b.X,b.Y){W=10,H=12,VX=Player.X<b.X?-65:65,VY=-190,Timer=3});b.Timer=.8f;b.VX=(float)Math.Sin(b.Clock)*25;if(b.Ground&&Math.Sin(b.Clock*.7)>0.8)b.VY=-210;}
                if(k.Contains("red")&&b.Ground&&!Solid((int)(b.VX<0?b.X-1:b.Right+1)/16,(int)(b.Bottom+2)/16))b.VX=-b.VX;
                Move(b);
            }
            if(b.Y>270){b.Dead=true;return;}
            if(k.StartsWith("koopa")&&b.State==2)foreach(var other in actors)if(other!=b&&!other.Dead&&IsEnemy(other)&&b.Overlap(other)){other.Dead=true;Score+=200;}
            if(!b.Overlap(Player)||death>0)return;
            if(k=="mushroom"||k=="flower"||k=="star"||k=="oneup")
            {b.Dead=true;Score+=1000;if(k=="oneup"){Lives++;Sound("oneup");}else if(k=="star"){star=Tuning.Get("Mario","StarSeconds");Sound("star");}else{if(Size==0){Player.Y-=16;Player.H=32;}Size=k=="flower"?2:Math.Max(1,Size);grace=.5f;Sound("powerup");}return;}
            if(star>0){b.Dead=true;Score+=200;Sound("kick");return;}
            bool stomp=Player.VY>=0&&oldBottom<=b.Y+10&&k!="plant"&&k!="bowser";
            if(stomp){Player.Y=b.Y-Player.H;Player.VY=-180;Score+=100;Sound("stomp");if(k.StartsWith("koopa")){if(k.Contains("flying")){b.Kind="koopa";}else if(b.State==0){b.State=1;b.Y+=b.H-14;b.H=14;b.VX=0;}else{b.State=b.State==2?1:2;b.VX=b.State==2?facing*190:0;}}else b.Dead=true;}
            else if(k.StartsWith("koopa")&&b.State==1){b.State=2;b.VX=Player.X<b.X?190:-190;b.X+=Math.Sign(b.VX)*8;Sound("kick");}
            else Hurt();
        }
        void Hurt(){if(grace>0||star>0||death>0||finish>0)return;if(Size>0){Size=0;Player.Y+=Player.H-16;Player.H=16;grace=Tuning.Get("Mario","HurtGraceSeconds");Sound("pipe");}else Die();}
        void Die(){if(death>0||finish>0)return;Lives--;death=.01f;Player.VY=-240;Sound("dead");}
        public void Draw(IRetroCanvas c)
        {
            c.Clear(map.background==1?0x5c94fcU:0x000000U);
            if(intro>0){c.Clear(0);c.Text("world "+data.selected[stage],80,80);c.Sprite("mario-mario/small_mario_stand",96,114);c.Text("x "+Lives,120,122);return;}
            int left=Math.Max(0,(int)CameraX/16),right=Math.Min(map.width-1,left+17);
            for(int y=0;y<15;y++)for(int x=left;x<=right;x++){int id=tiles[Index(x,y)];if((Flags(x,y)&2)==0)c.Sprite("tile/"+id,x*16-CameraX,y*16);}
            foreach(var p in platforms)for(int x=0;x<p.W;x+=16)c.Sprite("mario-fx/platform-0",p.X+x-CameraX,p.Y);
            foreach(var b in actors)
            {
                if(b.Dead||b.X>CameraX+280||b.X<CameraX-64)continue;string sprite=null,k=b.Kind;int anim=(int)(age*8)%2+1;
                if(k.StartsWith("goomba"))sprite="mario-enemies/"+(map.palette==2?"green":map.palette==3?"grey":"brown")+"_goomba_"+anim;
                else if(k.StartsWith("koopa")){string color=k.Contains("red")?"red":map.palette==2?"teal":"green";sprite="mario-enemies/"+color+(b.State>0?"_koopa_shell":k.Contains("flying")?"_wing_koopa_"+anim:"_koopa_"+anim);}
                else if(k=="plant")sprite="mario-enemies/green_piranha_"+anim;
                else if(k=="hammerbro")sprite="mario-fx/hammerbros-"+(anim-1);
                else if(k=="bowser")sprite="mario-fx/bowser-"+(anim-1);
                else if(k=="mushroom"||k=="oneup")sprite="mario-fx/"+k;
                else if(k=="flower"||k=="star")sprite="mario-fx/"+k+"-"+((int)(age*8)%4);
                else if(k=="spring")sprite="mario-fx/spring-0";
                else if(k=="axe")sprite="mario-extra/axe";
                else if(k=="flag"){c.Rect(b.X+6-CameraX,32,2,176,0x80d010);c.Sprite("mario-extra/flag",b.X-9-CameraX,flagging?Math.Min(184,40+finish*135):40);}
                else if(k.StartsWith("castlefire")){int count=b.Extra>0?(int)b.Extra:6;for(int n=0;n<count;n++){double a=b.Clock*(k.EndsWith("ccw")?-1:1);c.Sprite("mario-fx/fireball-0",b.X+8+(float)Math.Cos(a)*n*8-CameraX,b.Y+8+(float)Math.Sin(a)*n*8);}}
                else if(k=="vine")c.Rect(b.X+6-CameraX,0,4,(int)b.Bottom,0x80d010);
                if(sprite!=null)c.Sprite(sprite,b.X-CameraX,b.Y,b.VX>0&&IsEnemy(b));
            }
            foreach(var b in projectiles)c.Sprite(b.Kind=="fireball"?"mario-fx/fireball-0":b.Kind=="hammer"?"mario-fx/hammer-"+((int)(age*10)%4):b.Kind=="bullet"?"mario-fx/bulletbill-0":"mario-fx/fire-0",b.X-CameraX,b.Y,b.VX>0);
            foreach(var b in particles)if(b.Kind=="coin")c.Sprite("mario-fx/coin-0",b.X-CameraX,b.Y);else c.Rect(b.X-CameraX,b.Y,4,4,0xc84c0c);
            if(grace<=0||(int)(age*16)%2==0)
            {
                string prefix=Size==0?"small":Size==1?"big":"fire";string pose=death>0?"die":flagging&&finish<1.1?"climb_1":!Player.Ground?"jump":Player.H==16&&Size>0?"crouch":Math.Abs(Player.VX)<3?"stand":"run_"+((int)(age*12)%3+1);
                c.Sprite("mario-mario/"+prefix+"_mario_"+pose,Player.X-2-CameraX,Player.Bottom-(Size>0?32:16),facing<0);
            }
            c.Text("mario",16,8);c.Text(Score.ToString("000000"),16,16);c.Text("x"+Coins.ToString("00"),96,16);c.Text("world",144,8);c.Text(data.selected[stage],152,16);c.Text("time",208,8);c.Text(((int)Math.Max(0,clock)).ToString("000"),216,16);
        }
    }
}
