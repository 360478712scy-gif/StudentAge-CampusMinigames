using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EC2BUnofficialPatch.Features.Mechanics.Minigames;
using StudentAge.Retro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace StudentAge.CampusMinigames
{
    public sealed class MarioEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<MarioView>(c,9110,8);}
    public sealed class ContraEntry:ICustomMinigame{public void Open(CustomMinigameContext c)=>PuzzleFrame.Open<ContraView>(c,9111,1);}
    public abstract class RetroView:PuzzleFrame
    {
        public IRetroGame Engine{get;private set;}public PixelCanvas Pixels{get;private set;}
        float accumulator,jingleUntil;bool jumpQueued,fireQueued;int turboJumpTick,turboFireTick;AudioSource sound,music;string assetRoot;
        readonly Dictionary<string,AudioClip> audio=new Dictionary<string,AudioClip>();
        protected abstract string GameKey{get;}
        protected override IEnumerator LoadAssets()
        {
            assetRoot=Path.Combine(Session.Root,"Retro");Pixels=new PixelCanvas(assetRoot);
            Engine=GameKey=="mario"?(IRetroGame)new MarioGame(Newtonsoft.Json.JsonConvert.DeserializeObject<MarioData>(File.ReadAllText(Path.Combine(assetRoot,"mario.json"))),Level-1):new ContraGame(Newtonsoft.Json.JsonConvert.DeserializeObject<ContraData>(File.ReadAllText(Path.Combine(assetRoot,"contra.json"))));
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;BindAudio(sound);
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;BindAudio(music);
            string folder=Path.Combine(assetRoot,"audio",GameKey);
            if(Directory.Exists(folder))foreach(string path in Directory.GetFiles(folder))
            {
                string ext=Path.GetExtension(path).ToLowerInvariant();AudioType type=ext==".ogg"?AudioType.OGGVORBIS:ext==".wav"?AudioType.WAV:ext==".mp3"?AudioType.MPEG:AudioType.UNKNOWN;if(type==AudioType.UNKNOWN)continue;
                using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,type))
                {yield return request.SendWebRequest();if(Closed)yield break;if(request.result==UnityWebRequest.Result.Success){var clip=DownloadHandlerAudioClip.GetContent(request);audio[Path.GetFileNameWithoutExtension(path)]=clip;Owned.Add(clip);}}
            }
        }
        protected override void BuildPlay()
        {
            I(PlayRoot,"RetroBlack",0,0,1600,900,null,Color.black);
            var image=R(PlayRoot,"NES 4x3",200,0,1200,900).gameObject.AddComponent<RawImage>();image.texture=Pixels.Texture;image.raycastTarget=false;
            Engine.Draw(Pixels);Pixels.Present();
        }
        string Track(){var mario=Engine as MarioGame;return mario==null?"bgm":mario.StarActive?"star":mario.IsWater?"water":mario.Palette==2?"underground":mario.Palette==3?"castle":"bgm";}
        void PlayMusic(){AudioClip clip;if(audio.TryGetValue(Track(),out clip)){if(music.clip!=clip){music.clip=clip;music.Play();}else if(!music.isPlaying)music.UnPause();}}
        protected override void StartGame(){accumulator=jingleUntil=0;turboJumpTick=turboFireTick=0;music.volume=StudentAge.CampusUno.MinigameTuning.Get("Audio",GameKey=="contra"?"ContraBgmVolume":"MarioBgmVolume");UseLocalMusic(music);PlayMusic();}
        protected override void Tick()
        {
            var k=Keyboard.current;var g=Gamepad.current;var input=new RetroInput();
            if(k!=null)
            {
                input.X=(k.rightArrowKey.isPressed||k.dKey.isPressed?1:0)-(k.leftArrowKey.isPressed||k.aKey.isPressed?1:0);
                input.Y=(k.downArrowKey.isPressed||k.sKey.isPressed?1:0)-(k.upArrowKey.isPressed||k.wKey.isPressed?1:0);
                input.Jump=k.jKey.isPressed||k.zKey.isPressed||k.spaceKey.isPressed;input.Fire=k.kKey.isPressed||k.xKey.isPressed||k.leftShiftKey.isPressed;input.Run=input.Fire;
                jumpQueued|=k.jKey.wasPressedThisFrame||k.zKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame;fireQueued|=k.kKey.wasPressedThisFrame||k.xKey.wasPressedThisFrame||k.leftShiftKey.wasPressedThisFrame;
            }
            if(g!=null)
            {
                var axis=g.dpad.ReadValue()+g.leftStick.ReadValue();if(Math.Abs(axis.x)>.35f)input.X=Math.Sign(axis.x);if(Math.Abs(axis.y)>.35f)input.Y=-Math.Sign(axis.y);
                input.Jump|=g.buttonSouth.isPressed;input.Fire|=g.buttonWest.isPressed;input.Run|=g.buttonWest.isPressed;jumpQueued|=g.buttonSouth.wasPressedThisFrame;fireQueued|=g.buttonWest.wasPressedThisFrame;
            }
            accumulator+=Math.Min(StudentAge.CampusUno.PlayClock.Delta,.1f);
            while(accumulator>=1f/60&&!Engine.Won&&!Engine.Lost)
            {var step=input;bool turboJump=k!=null&&k.uKey.isPressed,turboFire=k!=null&&k.iKey.isPressed;
                if(!turboJump)turboJumpTick=0;if(!turboFire)turboFireTick=0;
                step.JumpPressed=jumpQueued||(turboJump&&turboJumpTick%8==0);step.FirePressed=fireQueued||(turboFire&&turboFireTick%8==0);
                step.Jump|=turboJump&&turboJumpTick%8<4;step.Fire|=turboFire&&turboFireTick%8<4;step.Run|=turboFire;
                if(turboJump)turboJumpTick++;if(turboFire)turboFireTick++;Engine.Step(step);jumpQueued=fireQueued=false;accumulator-=1f/60;}
            while(Engine.Sounds.Count>0){string name=Engine.Sounds.Dequeue();AudioClip clip;
                if(name=="star")continue; // Starman is a looping music state, not a one-shot.
                if(name=="complete"||name=="dead"){music.Stop();music.clip=null;sound.Stop();if(audio.TryGetValue(name,out clip)){sound.PlayOneShot(clip,.85f);jingleUntil=StudentAge.CampusUno.PlayClock.Now+clip.length;}}
                else if(audio.TryGetValue(name,out clip))sound.PlayOneShot(clip,.55f);
            }
            bool frozen=Engine is MarioGame?((MarioGame)Engine).InTransition:((ContraGame)Engine).InTransition;
            if(!frozen&&!Engine.Won&&!Engine.Lost&&StudentAge.CampusUno.PlayClock.Now>=jingleUntil)PlayMusic();
            Engine.Draw(Pixels);Pixels.Present();if((Engine.Won||Engine.Lost)&&StudentAge.CampusUno.PlayClock.Now>=jingleUntil){music.Stop();Finish(Engine.Won);}
        }
        protected override void OnDestroy(){if(Pixels!=null){Pixels.Dispose();Pixels=null;}base.OnDestroy();}
    }
    public sealed class MarioView:RetroView
    {
        protected override string GameKey=>"mario";protected override string Title=>"超级马里奥兄弟";
        protected override string[] Rules=>new[]{"方向键或 A / D 移动，J / Z / 空格跳跃，U连续跳跃。","按住 K / X / Shift 加速；I连发。火焰形态下可发射火球。","跳跃按住时间影响高度，落在敌人头顶可以踩倒。","问号砖里有金币或道具，长大后能顶碎砖块。","可进入的水管按下，攀藤按上；到达旗杆或城堡终点过关。","精选八关随社交进度依次进行，局内不选关。","保留经典生命、金币和关卡计时；生命用完本局失败。","手柄：左摇杆/方向键移动，A跳跃、X加速/火球。"};
    }
    public sealed class ContraView:RetroView
    {
        protected override string GameKey=>"contra";protected override string Title=>"魂斗罗";
        protected override string[] Rules=>new[]{"方向键或 WASD 移动与瞄准，J / Z / 空格跳跃，U连续跳跃。","按住 K / X / Shift 射击，I连发，支持八个方向。","原地按下卧倒；按下并跳跃可穿过单向平台。","击破飞行胶囊或武器箱，拾取 M / S / L / F 武器。","拾取R提高射速；中弹后失去武器，复活短暂无敌。","穿过丛林与爆破桥，击破关底防御设施过关。","本版先收录初代丛林关，"+((ContraGame)Engine).Lives+"条生命，生命耗尽失败。","手柄：左摇杆/方向键移动，A跳跃、X射击。"};
    }
}
