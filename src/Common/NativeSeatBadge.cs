using System;
using System.IO;
using Config;
using Sdk;
using UnityEngine;
using UnityEngine.UI;
namespace StudentAge.CampusUno
{
    // Optional participant metadata keeps older external UNO sessions compatible.
    public interface ICardSeatSession { int NpcId { get; } }
    public sealed class CardSeatIdentity
    {
        public string Name,HeadPath;
        public static CardSeatIdentity Classmate(int index)
        {
            return new CardSeatIdentity{Name=index==0?"男同学1":index==1?"女同学1":"女同学2",HeadPath="role_head/role_student_"+(index==0?"male_1":index==1?"female_1":"female_2")};
        }
        public static CardSeatIdentity Role(int id,int fallback=1)
        {
            PersonCfg cfg;if(id<0||!Cfg.PersonCfgMap.TryGetValue(id,out cfg))return Classmate(fallback);
            var role=Singleton<RoleMgr>.Ins.GetRole(id);
            // Title-screen practice has no player/grade data for native portrait resolution.
            if(role==null){var identity=Classmate(fallback);if(id==0)identity.Name="你";return identity;}
            string path=cfg.GetHeadIcon();
            return new CardSeatIdentity{Name=id==0?"你":role!=null?role.Name:cfg.name,HeadPath=path};
        }
    }
    // Both card games use the original paper nameplate and original role_head resources.
    public sealed class NativeSeatBadge:MonoBehaviour
    {
        public CardSeatIdentity Identity{get;private set;}public Image Portrait{get;private set;}public Text NameText{get;private set;}public Text CountText{get;private set;}
        static Sprite countShape;
        static Sprite CountShape { get { if(countShape!=null)return countShape;var texture=new Texture2D(96,128,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;for(int y=0;y<128;y++)for(int x=0;x<96;x++){float dx=Mathf.Max(8-x,Mathf.Max(0,x-87)),dy=Mathf.Max(8-y,Mathf.Max(0,y-119));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8.5f-Mathf.Sqrt(dx*dx+dy*dy))));}texture.Apply();countShape=Sprite.Create(texture,new Rect(0,0,96,128),new Vector2(.5f,.5f));return countShape;} }
        Image paper;readonly Color ink=new Color(.37f,.20f,.11f),cream=new Color(1,.975f,.91f);
        public static NativeSeatBadge Create(Transform parent,CardSeatIdentity identity,float x,float y,float width=238)
        {
            var root=Rect(parent,"SeatInfo-"+identity.Name,x,y,width,176);var v=root.gameObject.AddComponent<NativeSeatBadge>();v.Identity=identity;
            v.paper=v.Paper(root,"NamePaper",0,0,width,58);v.NameText=v.Label(root,"SeatName",identity.Name,8,3,width-16,50,25);
            v.Paper(root,"PortraitPaper",8,64,106,106);v.Portrait=Rect(root,"RolePortrait",13,69,96,96).gameObject.AddComponent<Image>();v.Portrait.raycastTarget=false;v.Portrait.preserveAspect=true;v.Portrait.enabled=false;
            Action<Sprite> loaded=sprite=>{if(v==null||v.Portrait==null)return;v.Portrait.sprite=sprite;v.Portrait.enabled=sprite!=null;};
            string path=identity.HeadPath;if(Path.IsPathRooted(path))ResMgr.LoadExternSpriteAsync(path,loaded,false);else if(path.StartsWith("Mods"))ResMgr.LoadExternSpriteAsync(Singleton<ModCtrl>.Ins.GetFullUrl(path),loaded,false);else ResMgr.LoadSpriteAsync(Path.Combine("Textures",LocalizationMgr.GetLocalizeUrl(path)),loaded);
            v.CountCard(root,"CountBack",126,89,47,61);v.CountCard(root,"CountFront",134,98,47,61);v.CountText=v.Label(root,"CardCount","",135,100,45,56,34);v.CountText.resizeTextMinSize=22;return v;
        }
        public void UpdateInfo(string suffix,int cards,bool current)
        {
            NameText.text=Identity.Name+suffix;CountText.text=cards.ToString();NameText.color=current?new Color(.68f,.31f,.08f):ink;
            paper.color=current?new Color(1,.94f,.79f):cream;
        }
        Image CountCard(Transform parent,string name,float x,float y,float w,float h){var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=CountShape;image.type=Image.Type.Simple;image.color=cream;image.raycastTarget=false;var line=image.gameObject.AddComponent<Outline>();line.effectColor=new Color(.43f,.29f,.18f,.95f);line.effectDistance=new Vector2(1.3f,-1.3f);return image;}
        Image Paper(Transform parent,string name,float x,float y,float w,float h){var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=NativeSkin.Paper;image.type=Image.Type.Sliced;image.color=cream;image.raycastTarget=false;return image;}
        Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size){var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();label.font=NativeSkin.TitleFont;label.text=text;label.fontSize=size;label.resizeTextForBestFit=true;label.resizeTextMinSize=19;label.resizeTextMaxSize=size;label.alignment=TextAnchor.MiddleCenter;label.color=ink;label.raycastTarget=false;return label;}
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    }
}
