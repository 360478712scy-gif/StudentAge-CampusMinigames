using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;using UnityEngine.Networking;using Sdk;
namespace StudentAge.CampusMinigames {
public sealed partial class NdsConsole {
 static readonly Color BadgeInk=new Color(.055f,.085f,.15f),BadgePanel=new Color(.10f,.16f,.24f),BadgeEdge=new Color(.29f,.43f,.54f),BadgeGold=new Color(.94f,.76f,.44f),BadgeWhite=new Color(.91f,.94f,.90f),BadgeMuted=new Color(.52f,.65f,.71f);
 RectTransform badgeOverlay,awardMedal;NdsBadge selectedBadge,activeAward;readonly Queue<NdsBadge> badgeQueue=new Queue<NdsBadge>();readonly List<RectTransform> awardOrbit=new List<RectTransform>();
 float awardStart;bool awardSoundPlayed,awardImpactPlayed,badgeZoomOpen;AudioSource badgeSound;AudioClip badgeWin,ultimateWin;bool badgeAudioClosed;
 public bool BadgeWallOpen=>badgeOverlay!=null;public NdsBadge ActiveAward=>activeAward;
 bool BadgeOwned(NdsBadge b)=>!TitleMode&&NdsBadges.Has(b)&&b!=activeAward&&!badgeQueue.Contains(b);
 int BadgeCount=>NdsBadges.All.Take(12).Count(BadgeOwned);
 void BuildBadgeButton(){Button(root,"BadgeCollection",1290,40,258,58,OpenBadges,BadgePanel);Picture(root,"BadgeButtonIcon",1300,41,54,54,NdsBadgeAssets.Get(NdsBadges.All[12]));Text(root,"徽章收藏",1360,40,174,58,2.2f,BadgeGold);}
 public void OpenBadges(){if(!Valid||Booting||InGame||activeAward!=null)return;selectedBadge=selectedBadge??NdsBadges.All[0];DrawBadges();}
 public void QueueBadges(IEnumerable<NdsBadge> badges){foreach(var b in badges)if(!badgeQueue.Contains(b)&&activeAward!=b)badgeQueue.Enqueue(b);TryShowAward();}
 void DropBadgeOverlay(){if(badgeOverlay!=null){badgeOverlay.gameObject.SetActive(false);Destroy(badgeOverlay.gameObject);badgeOverlay=null;}awardOrbit.Clear();awardMedal=null;ResetAwardVisuals();}
 void CloseBadges(){badgeZoomOpen=false;DropBadgeOverlay();activeAward=null;RestoreBadgeMusic();}
 RectTransform BadgeBackdrop(string name){DropBadgeOverlay();var r=PuzzleFrame.R(root,name,0,0,1600,900);var i=r.gameObject.AddComponent<Image>();i.color=new Color(.02f,.04f,.09f,1f);i.raycastTarget=true;badgeOverlay=r;return r;}
 void BadgeBox(Transform p,string name,float x,float y,float w,float h,Color body,Color rim){Fill(p,name+"Shadow",x+6,y+7,w,h,new Color(.02f,.04f,.08f));Fill(p,name+"Edge",x,y,w,h,rim);Fill(p,name,x+3,y+3,w-6,h-6,body);Fill(p,name+"Highlight",x+8,y+3,w-16,2,new Color(rim.r+.08f,rim.g+.08f,rim.b+.08f));}
 void BadgeLabel(Transform p,string s,float x,float y,float w,float h,float scale,Color c)=>Text(p,s,x,y,w,h,scale,c);
 void DrawBadges(){badgeZoomOpen=false;var p=BadgeBackdrop("NdsBadgeWall");var sparkle=PuzzleFrame.R(p,"CollectionStars",0,0,1600,900).gameObject.AddComponent<NdsBadgeParticles>();sparkle.Setup(false);
 BadgeLabel(p,"游戏徽章收藏集",130,28,570,54,2.8f,BadgeWhite);BadgeLabel(p,"十二场冒险，一枚属于你的传说",122,82,586,29,1.5f,BadgeMuted);
 BadgeLabel(p,BadgeCount.ToString("00")+" / 12",1220,40,180,45,2.6f,BadgeGold);Button(p,"CloseBadgeWall",1450,40,54,48,CloseBadges,BadgePanel);BadgeLabel(p,"×",1450,40,54,48,2.7f,BadgeWhite);
 // Twelve discrete stars encircle the large ultimate badge; fine stepped links sit behind the art.
 for(int n=0;n<12;n++){float a=n*Mathf.PI/6-Mathf.PI/2,na=(n+1)*Mathf.PI/6-Mathf.PI/2;float x=800+Mathf.Cos(a)*560,y=472+Mathf.Sin(a)*285,nx=800+Mathf.Cos(na)*560,ny=472+Mathf.Sin(na)*285;
  for(int j=3;j<18;j++){float t=j/20f;Fill(p,"OrbitLink",Mathf.Round(Mathf.Lerp(x,nx,t)/3)*3,Mathf.Round(Mathf.Lerp(y,ny,t)/3)*3,3,3,BadgeEdge);}}
 var final=NdsBadges.All[12];bool finalOwned=BadgeOwned(final);
 Button(p,"SelectUltimateBadge",620,292,360,360,()=>OpenBadgeDetail(final),Color.clear);
 var crown=Fill(p,"UltimateBadgeArt",620,292,360,360,finalOwned?Color.white:Color.black);crown.sprite=NdsBadgeAssets.Get(final);
 if(!finalOwned)BadgeQuestion(p,final,620,324,360,290,8);
 BadgeLabel(p,"NDS大玩家",580,650,440,48,2.9f,BadgeGold);
 for(int n=0;n<12;n++){var badge=NdsBadges.All[n];float angle=n*Mathf.PI/6-Mathf.PI/2,x=800+Mathf.Cos(angle)*560,y=472+Mathf.Sin(angle)*285;bool have=BadgeOwned(badge),picked=selectedBadge==badge;
  Button(p,"SelectBadge-"+badge.GameId,x-82,y-78,164,175,()=>OpenBadgeDetail(badge),Color.clear);
  var art=Fill(p,"Medal-"+badge.GameId,x-72,y-72,144,144,have?Color.white:Color.black);art.sprite=NdsBadgeAssets.Get(badge);
  if(!have)BadgeQuestion(p,badge,x-72,y-69,144,138,4.8f);
  BadgeLabel(p,badge.Name,x-102,y+69,204,25,1.25f,have?BadgeWhite:BadgeMuted);

 }
 BadgeLabel(p,TitleMode?"新的收藏，等待你在故事中开启":"点击徽章，放大欣赏",500,849,600,27,1.45f,BadgeMuted);
 }
 void OpenBadgeDetail(NdsBadge badge){selectedBadge=badge;badgeZoomOpen=true;var p=BadgeBackdrop("NdsBadgeZoom");
  var stars=PuzzleFrame.R(p,"BadgeZoomStars",0,0,1600,900).gameObject.AddComponent<NdsBadgeParticles>();stars.Setup(false);
  Button(p,"BackToBadgeWall",105,47,230,54,DrawBadges,BadgePanel);BadgeLabel(p,"← 收藏集",105,47,230,54,2.1f,BadgeWhite);
  bool have=BadgeOwned(badge);var art=Fill(p,"EnlargedBadge",560,166,480,480,have?Color.white:Color.black);art.sprite=NdsBadgeAssets.Get(badge);
  if(!have)BadgeLabel(p,"?",560,201,480,380,10,BadgeWhite);
  BadgeLabel(p,badge.Name,300,658,1000,62,3.4f,badge.Ultimate?BadgeGold:BadgeWhite);
  string hint=have?"已收藏":badge.Ultimate?"集齐十二枚游戏徽章后解锁":TitleMode?"在正式存档中完成挑战":"完成对应游戏全部关卡后解锁";
  BadgeLabel(p,hint,300,740,1000,36,1.9f,BadgeMuted);
 }
 static string[] WrapBadge(string s,int width){var result=new List<string>();for(int i=0;i<s.Length;i+=width)result.Add(s.Substring(i,Math.Min(width,s.Length-i)));return result.ToArray();}
 void RestoreBadgeMusic(){music?.FadeMusicPaused(false,1.1f);}
 bool UpdateBadges(){if(badgeSound!=null&&AudioMgr.Ins!=null){var native=AudioMgr.Ins.GetChannel(AudioMgrEx.CHANNEL_SOUND_UI).source;badgeSound.outputAudioMixerGroup=native.outputAudioMixerGroup;badgeSound.mute=native.mute;}
 if(activeAward!=null){UpdateAward(Time.unscaledTime-awardStart);return true;}
 if(badgeOverlay!=null){if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){if(badgeZoomOpen)DrawBadges();else CloseBadges();}return true;}return false;
 }
 void LoadBadgeAudio(){badgeSound=gameObject.AddComponent<AudioSource>();badgeSound.playOnAwake=false;badgeSound.ignoreListenerPause=true;StartCoroutine(ReadBadgeAudio());}
 IEnumerator ReadBadgeAudio(){foreach(string name in new[]{"badge-win","badge-ultimate"}){string file=Path.Combine(NdsBadgeAssets.Root,name+".mp3");if(!File.Exists(file))continue;using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri,AudioType.MPEG)){yield return request.SendWebRequest();if(badgeAudioClosed)yield break;if(request.result==UnityWebRequest.Result.Success){var clip=DownloadHandlerAudioClip.GetContent(request);if(name=="badge-win")badgeWin=clip;else ultimateWin=clip;}}}}
 void CloseBadgeAudio(){badgeAudioClosed=true;if(badgeSound!=null)badgeSound.Stop();foreach(var clip in new[]{badgeWin,ultimateWin})if(clip!=null)Destroy(clip);}
}
public sealed class NdsBadgeParticles:MaskableGraphic {
 bool ultimate;float began;public void Setup(bool final){ultimate=final;began=Time.unscaledTime;raycastTarget=false;}
 void Update(){SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper h){h.Clear();float t=Time.unscaledTime-began;for(int i=0;i<(ultimate?116:62);i++){float x,y,size,alpha;if(ultimate&&i<64&&t>2.3f){float a=i*2.399963f,age=t-2.3f,r=age*(90+i%9*33);x=800+Mathf.Cos(a)*r;y=420+Mathf.Sin(a)*r*.66f+age*age*22;size=3+i%4;alpha=Mathf.Clamp01(1-age/5);}else{x=(i*397+71)%1580+10;y=((i*179+31)%870-t*(4+i%5)+1800)%900;size=i%7==0?4:2;alpha=.18f+.38f*(.5f+.5f*Mathf.Sin(t*1.7f+i));}Color c=i%3==0?new Color(.63f,.77f,.93f,alpha):new Color(1,.81f,.48f,alpha);Quad(h,x-size,y-size,size*2,size*2,c);if(i%7==0){Quad(h,x-size*2,y-1,size*4,2,c);Quad(h,x-1,y-size*2,2,size*4,c);}}}
 static void Quad(VertexHelper h,float x,float y,float w,float ht,Color c){int k=h.currentVertCount;h.AddVert(new Vector3(x,-y),c,Vector2.zero);h.AddVert(new Vector3(x+w,-y),c,Vector2.zero);h.AddVert(new Vector3(x+w,-y-ht),c,Vector2.zero);h.AddVert(new Vector3(x,-y-ht),c,Vector2.zero);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}
}
}
