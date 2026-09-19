using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace StudentAge.Retro
{
    public sealed class RetroPreview:MonoBehaviour
    {
        public bool Contra;[Range(1,8)]public int MarioLevel=1;
        PixelCanvas pixels;IRetroGame game;float clock;bool jump,fire;AudioSource source;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        void Start()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;
            string root=Path.Combine(Application.streamingAssetsPath,"CampusRetro");pixels=new PixelCanvas(root);
            game=Contra?(IRetroGame)new ContraGame(Newtonsoft.Json.JsonConvert.DeserializeObject<ContraData>(File.ReadAllText(Path.Combine(root,"contra.json")))):
                new MarioGame(Newtonsoft.Json.JsonConvert.DeserializeObject<MarioData>(File.ReadAllText(Path.Combine(root,"mario.json"))),MarioLevel-1);
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
            foreach(string path in Directory.GetFiles(Path.Combine(root,"audio",Contra?"contra":"mario"),"*.wav"))clips[Path.GetFileNameWithoutExtension(path)]=LoadWave(path);
        }
        void Update()
        {
            if(game==null)return;var input=new RetroInput{X=(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A)?1:0),Y=(Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S)?1:0)-(Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W)?1:0),Jump=Input.GetKey(KeyCode.Z)||Input.GetKey(KeyCode.Space),Fire=Input.GetKey(KeyCode.X)||Input.GetKey(KeyCode.LeftShift),Run=Input.GetKey(KeyCode.X)||Input.GetKey(KeyCode.LeftShift)};
            jump|=Input.GetKeyDown(KeyCode.Z)||Input.GetKeyDown(KeyCode.Space);fire|=Input.GetKeyDown(KeyCode.X)||Input.GetKeyDown(KeyCode.LeftShift);clock+=Math.Min(Time.unscaledDeltaTime,.1f);
            while(clock>=1f/60){input.JumpPressed=jump;input.FirePressed=fire;game.Step(input);jump=fire=false;clock-=1f/60;}
            game.Draw(pixels);pixels.Present();while(game.Sounds.Count>0){AudioClip clip;if(clips.TryGetValue(game.Sounds.Dequeue(),out clip))source.PlayOneShot(clip,.45f);}
        }
        void OnGUI(){if(pixels==null)return;float h=Screen.height,w=h*4/3;GUI.DrawTexture(new Rect((Screen.width-w)/2,0,w,h),pixels.Texture,ScaleMode.StretchToFill,false);}
        static AudioClip LoadWave(string path)
        {
            using(var reader=new BinaryReader(File.OpenRead(path)))
            {
                if(Encoding.ASCII.GetString(reader.ReadBytes(4))!="RIFF")throw new IOException("Not WAV");reader.ReadInt32();if(Encoding.ASCII.GetString(reader.ReadBytes(4))!="WAVE")throw new IOException("Not WAVE");
                int channels=0,rate=0,bits=0,format=0;byte[] pcm=null;
                while(reader.BaseStream.Position+8<=reader.BaseStream.Length)
                {
                    string tag=Encoding.ASCII.GetString(reader.ReadBytes(4));int length=reader.ReadInt32();long end=reader.BaseStream.Position+length;if(length<0||end>reader.BaseStream.Length)throw new IOException("Invalid WAV chunk");
                    if(tag=="fmt "){format=reader.ReadInt16();channels=reader.ReadInt16();rate=reader.ReadInt32();reader.ReadInt32();reader.ReadInt16();bits=reader.ReadInt16();}
                    else if(tag=="data")pcm=reader.ReadBytes(length);
                    reader.BaseStream.Position=end+(length&1);
                }
                if(format!=1||bits!=16||pcm==null||channels<1)throw new IOException("Expected 16-bit PCM reference audio");
                var samples=new float[pcm.Length/2];for(int n=0;n<samples.Length;n++)samples[n]=(short)(pcm[n*2]|pcm[n*2+1]<<8)/32768f;
                var clip=AudioClip.Create(Path.GetFileNameWithoutExtension(path),samples.Length/channels,channels,rate,false);clip.SetData(samples,0);return clip;
            }
        }
        void OnDestroy(){if(pixels!=null)pixels.Dispose();foreach(var clip in clips.Values)Destroy(clip);}
    }
}
