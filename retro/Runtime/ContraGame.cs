using Tuning=StudentAge.CampusUno.MinigameTuning;
using System;
using System.Collections.Generic;
using System.Linq;
namespace StudentAge.Retro
{
    public sealed class ContraGame:IRetroGame
    {
        const float Dt=1f/60;readonly ContraData data;readonly List<Body> actors=new List<Body>(),shots=new List<Body>(),effects=new List<Body>();
        readonly Dictionary<string,string[]> animations;readonly Dictionary<int,List<PlatformDef>> groundIndex=new Dictionary<int,List<PlatformDef>>();
        float age,intro=0,shotDelay,grace=2.5f,death,finish,drop;bool swimming,crouch;int facing=1;bool rapid;
        public Body Player{get;private set;}public int Lives{get;private set;}=Tuning.Int("Contra","Lives");public int Score{get;private set;}
        public float CameraX{get;private set;}public string Weapon{get;private set;}="N";
        public bool InTransition=>death>0||finish>0;public bool Won{get;private set;}public bool Lost{get;private set;}
        public IReadOnlyList<Body> Actors=>actors;public IReadOnlyList<Body> Shots=>shots;
        public Queue<string> Sounds{get;}=new Queue<string>();RetroInput last;
        public ContraGame(ContraData data)
        {
            this.data=data;animations=data.animations.ToDictionary(a=>a.name,a=>a.frames);
            Player=new Body("player",56,-32){W=12,H=30};
            foreach(var p in data.solids){int start=(int)p.x/32,end=(int)(p.x+p.w)/32;for(int x=start;x<=end;x++){List<PlatformDef> bucket;if(!groundIndex.TryGetValue(x,out bucket))groundIndex[x]=bucket=new List<PlatformDef>();bucket.Add(p);}}
            foreach(var e in data.entities)
            {
                var b=new Body(e.kind,e.x,e.y){W=24,H=24,Extra=e.a};
                if(e.kind=="Boss"){b.X=e.x-56;b.Y=56;b.W=112;b.H=160;b.HP=Tuning.Int("Contra","BossHP");}
                else if(e.kind.StartsWith("Enemy")){b.W=14;b.H=30;b.Y-=30;b.VX=-65;b.HP=1;}
                else if(e.kind.StartsWith("Bridge")){b.W=32;b.H=8;}
                else if(e.kind.StartsWith("Battery")){b.X-=12;b.Y-=12;b.W=b.H=24;b.HP=Tuning.Int("Contra","BatteryHP");}
                else{b.W=b.H=24;b.X-=12;b.Y-=12;b.HP=1;}
                actors.Add(b);
            }
            Player=Respawn();
        }
        Body Respawn(){float x=RetroMath.Clamp(CameraX+40,16,data.width-32);var floor=data.solids.Where(p=>p.x<x+12&&p.x+p.w>x&&p.y>=40).OrderBy(p=>p.y).FirstOrDefault();return new Body("player",x,floor==null?-28:floor.y-30){W=12,H=30,Ground=floor!=null};}
        void Sound(string key){if(Sounds.Count<32)Sounds.Enqueue(key);}
        IEnumerable<PlatformDef> Near(Body b)
        {var used=new HashSet<PlatformDef>();for(int x=(int)b.X/32-1;x<=(int)b.Right/32+1;x++){List<PlatformDef> list;if(groundIndex.TryGetValue(x,out list))foreach(var p in list)if(used.Add(p))yield return p;}}
        void Move(Body b,bool human=false)
        {
            float oldBottom=b.Bottom,dx=b.VX*Dt,dy=b.VY*Dt;b.Ground=false;b.X+=dx;
            foreach(var p in Near(b))if(dx!=0&&!p.oneWay&&!p.water&&b.X<p.x+p.w&&b.Right>p.x&&b.Y<p.y+p.h&&b.Bottom>p.y+2)
            {b.X=dx>0?p.x-b.W:p.x+p.w;if(human)b.VX=0;else b.VX=-b.VX;break;}
            b.Y+=dy;
            if(dy>=0)
            {
                float top=float.MaxValue;
                foreach(var p in Near(b))if(b.Right>p.x&&b.X<p.x+p.w&&oldBottom<=p.y+2&&b.Bottom>=p.y&&!(human&&drop>0&&p.oneWay))top=Math.Min(top,p.y);
                foreach(var bridge in actors)if(bridge.Kind.StartsWith("Bridge")&&!bridge.Dead&&b.Right>bridge.X&&b.X<bridge.Right&&oldBottom<=bridge.Y+2&&b.Bottom>=bridge.Y)top=Math.Min(top,bridge.Y);
                if(top!=float.MaxValue){b.Y=top-b.H;b.VY=0;b.Ground=true;}
            }
            if(dy<0)foreach(var p in Near(b))if(!p.oneWay&&!p.water&&b.Right>p.x&&b.X<p.x+p.w&&b.Y<p.y+p.h&&b.Bottom>p.y){b.Y=p.y+p.h;b.VY=0;break;}
            if(human)b.X=RetroMath.Clamp(b.X,0,data.width-b.W);
        }
        public void Step(RetroInput input)
        {
            if(Won||Lost)return;last=input;age+=Dt;grace=Math.Max(0,grace-Dt);shotDelay-=Dt;drop-=Dt;
            if(intro>0){intro-=Dt;return;}
            if(finish>0){finish+=Dt;if((int)(finish*20)%3==0)effects.Add(new Body("boom",CameraX+190+(float)Math.Sin(age*23)*35,100+(float)Math.Cos(age*29)*60){Timer=.5f});UpdateEffects();if(finish>3)Won=true;return;}
            if(death>0){death+=Dt;Player.VY+=600*Dt;Player.Y+=Player.VY*Dt;if(death>1.4f){if(Lives<=0){Lost=true;return;}death=0;Player=Respawn();grace=Tuning.Get("Contra","RespawnGraceSeconds");Weapon="N";rapid=false;}UpdateEffects();return;}
            bool grounded=Player.Ground;swimming=Near(Player).Any(p=>p.water&&Player.Right>p.x&&Player.X<p.x+p.w&&Player.Bottom>=p.y-1);
            crouch=input.Y>0&&grounded&&input.X==0&&!swimming;
            if(!swimming&&Player.H<30&&Near(Player).Any(p=>!p.oneWay&&!p.water&&Player.Right>p.x&&Player.X<p.x+p.w&&Player.Bottom-30<p.y+p.h&&Player.Y>p.y))crouch=true;
            float foot=Player.Bottom;Player.H=crouch||swimming?14:30;Player.Y=foot-Player.H;
            if(input.X!=0)facing=input.X;
            Player.VX=crouch?0:input.X*(swimming?55:90);
            if(input.JumpPressed&&(grounded||swimming))
            {if(input.Y>0&&!swimming&&Near(Player).Any(p=>p.oneWay&&!p.water&&Player.Right>p.x&&Player.X<p.x+p.w&&Math.Abs(Player.Bottom-p.y)<3)){drop=.25f;Player.Y+=3;}else{Player.VY=-265;swimming=false;Sound("jump");}}
            Player.VY=Math.Min(260,Player.VY+700*Dt);Move(Player,true);
            CameraX=RetroMath.Clamp(Player.X-112,0,data.width-256);
            var boss=actors.FirstOrDefault(a=>a.Kind=="Boss");if(boss!=null&&Player.X>boss.X-160)CameraX=Math.Min(CameraX,boss.X-128);
            if(input.Fire&&shotDelay<=0)
            {
                int ax=input.X,ay=input.Y;if(ax==0&&ay==0)ax=facing;if(crouch){ax=facing;ay=0;}
                float length=(float)Math.Sqrt(ax*ax+ay*ay),dx=ax/length,dy=ay/length;
                float sx=Player.X+Player.W/2+dx*11,sy=Player.Y+(crouch?8:swimming?4:11)+dy*8;
                int count=Weapon=="S"?5:1;float angle=(float)Math.Atan2(dy,dx);
                
                for(int n=0;n<count;n++)
                {float a=angle+(n-(count-1)/2f)*.14f;shots.Add(new Body("friendly",sx,sy){W=Weapon=="L"?12:4,H=Weapon=="L"?3:4,VX=(float)Math.Cos(a)*260,VY=(float)Math.Sin(a)*260,Timer=1.5f,State=Weapon=="F"?1:Weapon=="L"?2:0,Extra=n});}
                shotDelay=(Weapon=="M"?.075f:Weapon=="L"?.18f:.15f)*(rapid?.65f:1);Sound("shoot-"+Weapon.ToLowerInvariant());
            }
            foreach(var b in actors)
            {
                if(b.Dead)continue;if(!b.Active){if(b.X>CameraX+280||b.Right<CameraX-30)continue;b.Active=true;}b.Clock+=Dt;b.Timer-=Dt;
                string k=b.Kind;
                if(k.StartsWith("Bridge"))
                {if(b.State==0&&Player.X>b.X-10){b.State=1;b.Timer=.7f;}if(b.State==1&&b.Timer<=0){b.Dead=true;Explode(b.X,b.Y);Sound("explosion");}continue;}
                if(k.StartsWith("Enemy"))
                {
                    if(k=="Enemy0Right"){b.VX=-65;if(b.Ground&&b.Clock>.6f&&!Near(b).Any(p=>p.x>b.Right&&p.x<b.Right+12&&Math.Abs(p.y-b.Bottom)<8)&&b.X>Player.X)b.VY=-180;}
                    else{b.VX=0;if(b.Timer<=0){Aim(b,130);b.Timer=k=="Enemy1"?1.6f:1.2f;}}
                    b.VY=Math.Min(250,b.VY+700*Dt);Move(b);
                    if(b.Overlap(Player))Die();
                    if(b.Y>260||b.Right<CameraX-32)b.Dead=true;
                }
                else if(k.StartsWith("Battery"))
                {if(Math.Abs(Player.X-b.X)<190){b.State=1;if(b.Timer<=0){Aim(b,100);b.Timer=1.1f;}}}
                else if(k=="Boss")
                {if(Player.X>b.X-210&&b.Timer<=0){for(int n=0;n<3;n++)shots.Add(new Body("enemy",b.X+8,b.Y+64+n*18){W=5,H=5,VX=-120,VY=(n-1)*36,Timer=3});b.Timer=.85f;}}
                else if(k=="pickup")
                {b.VY+=450*Dt;Move(b);if(b.Overlap(Player)){char letter=(char)b.State;if(letter=='R')rapid=true;else Weapon=letter.ToString();b.Dead=true;Sound("pickup");Score+=1000;}}
                else if(k.Contains("Fly")){b.X-=58*Dt;b.Y=b.BaseY+(float)Math.Sin(b.Clock*2)*16;}
            }
            var drops=new List<Body>();
            foreach(var s in shots)
            {
                if(s.Dead)continue;s.Timer-=Dt;s.Clock+=Dt;s.X+=s.VX*Dt;s.Y+=s.VY*Dt;
                if(s.Kind=="friendly")
                {
                    foreach(var b in actors)
                    {
                        if(b.Dead||!b.Active||b.Kind=="pickup"||b.Kind.StartsWith("Bridge"))continue;
                        bool hit=b.Kind=="Boss"?s.Right>b.X+6&&s.X<b.X+40&&s.Bottom>b.Y+78&&s.Y<b.Y+128:s.Overlap(b);
                        if(!hit)continue;s.Dead=true;
                        if(b.Kind.Contains("AmmoBox")||b.Kind.Contains("Fly")){b.Dead=true;drops.Add(new Body("pickup",b.X,b.Y){W=16,H=16,VY=-110,VX=25,State=b.Kind[0],Active=true});Explode(b.X,b.Y);}
                        else if(--b.HP<=0){b.Dead=true;Score+=b.Kind=="Boss"?10000:100;Explode(b.X,b.Y);Sound("explosion");if(b.Kind=="Boss"){finish=.01f;Sound("complete");}}
                        break;
                    }
                }
                else if(s.Overlap(Player)){s.Dead=true;Die();}
                if(s.Timer<=0||s.X<CameraX-30||s.X>CameraX+290||s.Y<-24||s.Y>264)s.Dead=true;
            }
            actors.AddRange(drops);shots.RemoveAll(b=>b.Dead);UpdateEffects();
            if(Player.Y>260)Die();
        }
        void Aim(Body b,float speed)
        {float dx=Player.X-b.X,dy=Player.Y+10-b.Y,len=(float)Math.Sqrt(dx*dx+dy*dy);if(len<1)len=1;shots.Add(new Body("enemy",b.X+8,b.Y+8){W=4,H=4,VX=dx/len*speed,VY=dy/len*speed,Timer=3});}
        void Explode(float x,float y){effects.Add(new Body("boom",x,y){Timer=.4f});}
        void UpdateEffects(){foreach(var e in effects){e.Timer-=Dt;e.Clock+=Dt;}effects.RemoveAll(b=>b.Timer<=0);}
        void Die(){if(grace>0||death>0||finish>0)return;Lives--;death=.01f;Player.VY=-130;Sound("dead");}
        public void Draw(IRetroCanvas c)
        {
            c.Clear(0x000000);
            if(intro>0){c.Text("stage 1",100,80);c.Text("jungle",104,104);c.Text("rest "+Lives,100,136);return;}
            foreach(var tile in data.draw)if(tile.x>CameraX-128&&tile.x<CameraX+280)c.Sprite(tile.sprite,tile.x-CameraX,tile.y);
            foreach(var b in actors)
            {
                if(b.Dead||b.X>CameraX+300||b.Right<CameraX-80)continue;string sprite=null,k=b.Kind;
                if(k.StartsWith("Bridge")){c.Sprite("contra-map_tiles/Bridge0_"+(k=="BridgeHead"?0:k=="BridgeTail"?2:1),b.X-CameraX,b.Y);continue;}
                if(k=="Boss")sprite="contra-Boss/Boss_0";
                else if(k.StartsWith("Battery"))sprite="contra-MapObjects/MapObjects_"+(k=="Battery0"?(b.State==0?12:2):17);
                else if(k.StartsWith("Enemy"))sprite="contra-Enemy/Enemy_"+(k=="Enemy0Right"?(int)(age*10)%3:k=="Enemy1"?5:8);
                else if(k=="pickup"){c.Rect(b.X-CameraX,b.Y,16,12,0xcc2030);c.Text(((char)b.State).ToString(),(int)(b.X-CameraX+4),(int)b.Y+2);}
                else if(k.Contains("Fly"))sprite="contra-MapObjects/MapObjects_"+(26+(int)(age*8)%7);
                else if(k.Contains("AmmoBox"))sprite="contra-MapObjects/MapObjects_20";
                if(sprite!=null)c.Sprite(sprite,b.X-CameraX,b.Y,k.StartsWith("Enemy")&&Player.X>b.X);
            }
            foreach(var s in shots)
            {float x=s.X-CameraX,y=s.Y;if(s.State==1){x+=(float)Math.Sin(s.Clock*30)*5;y+=(float)Math.Cos(s.Clock*30)*5;}c.Rect(x,y,s.State==2?12:3,s.State==2?2:3,s.Kind=="friendly"?0xffb850U:0xfff8f8U);}
            foreach(var b in effects)c.Sprite("contra-Effect/Effect_"+((int)(b.Clock*15)%5),b.X-CameraX,b.Y);
            if(grace<=0||(int)(age*14)%2==0||death>0)
            {
                string pose=death>0?"Dead":swimming?(last.Y<0?"SwimUp":last.Fire?"SwimFire":"SwimIdle"):!Player.Ground?(Player.VY<0?"Jump":"Fall"):crouch?"Down":last.X!=0?(last.Y<0?"WalkUp":last.Y>0?"WalkDown":last.Fire?"WalkFire":"Walk"):last.Y<0?"Up":"Idle";
                string[] frames;if(animations.TryGetValue(pose,out frames)){string frame=frames[(int)(age*12)%frames.Length];int width=pose=="Jump"?(frame.EndsWith("_1")||frame.EndsWith("_3")?19:16):32;int foot=pose=="Jump"?(width==19?16:20):pose=="Up"?45:pose.StartsWith("Swim")?29:frame.EndsWith("_24")?10:40;c.Sprite(frame,Player.X+(Player.W-width)/2-CameraX,pose=="Jump"?Player.Y+(Player.H-foot)/2:Player.Bottom-foot,facing<0);}
            }
            c.Text("1p",16,8);c.Text(Score.ToString("000000"),40,8);for(int n=0;n<Math.Max(0,Lives);n++)c.Sprite("contra-life",16+n*10,22);
        }
    }
}
