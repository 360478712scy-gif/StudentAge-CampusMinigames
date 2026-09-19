using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;using UnityEngine.InputSystem;
namespace StudentAge.CampusUno
{
    // Painted desk stays in 2D. Only broad reflected light and a soft foreground shade move independently.
    public sealed class NativeCardTable:MonoBehaviour
    {
        readonly List<Texture2D> textures=new List<Texture2D>();RectTransform reflection;Vector2 look;public bool Ready=>reflection!=null;public int GeometryCount=>0;
        public static NativeCardTable Create(Transform parent){var o=new GameObject("LayeredSchoolDesk",typeof(RectTransform));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(1600,900);var view=o.AddComponent<NativeCardTable>();view.Build();return view;}
        void Build(){reflection=Layer("DeskReflection",new Rect(95,157,1320,630),true);Layer("DeskForegroundShade",new Rect(0,672,1600,228),false);}
        RectTransform Layer(string name,Rect region,bool glow){var texture=new Texture2D(256,128,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;for(int y=0;y<128;y++)for(int x=0;x<256;x++){float u=x/255f,v=y/127f;float opacity=glow?Mathf.Pow(Mathf.Max(0,1-((u-.32f)*(u-.32f)*3.2f+(v-.63f)*(v-.63f)*2.7f)),3)*.065f:Mathf.Pow(1-v,2)*.105f;texture.SetPixel(x,y,glow?new Color(1,.93f,.76f,opacity):new Color(.18f,.13f,.09f,opacity));}texture.Apply();textures.Add(texture);var o=new GameObject(name,typeof(RectTransform),typeof(RawImage));o.transform.SetParent(transform,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(region.x,-region.y);r.sizeDelta=region.size;var image=o.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return r;}
        void Update(){var mouse=Mouse.current;Vector2 desired=mouse==null?Vector2.zero:new Vector2(Mathf.Clamp01(mouse.position.ReadValue().x/Math.Max(1,Screen.width))-.5f,Mathf.Clamp01(mouse.position.ReadValue().y/Math.Max(1,Screen.height))-.5f);look=Vector2.Lerp(look,desired,Mathf.Clamp01(StudentAge.CampusUno.PlayClock.Delta*2));reflection.anchoredPosition=new Vector2(95+look.x*12,-157+look.y*7);}
        void OnDestroy(){foreach(var t in textures)Destroy(t);}
    }
    public sealed class TurnDirectionRing:MaskableGraphic
    {
        public int Direction{get;private set;}=1;float phase,pulse;
        public void SetDirection(int value){int next=value<0?-1:1;if(next!=Direction){Direction=next;pulse=1;}}
        void Update(){phase-=Direction*StudentAge.CampusUno.PlayClock.Delta*.34f;pulse=Mathf.Max(0,pulse-StudentAge.CampusUno.PlayClock.Delta*1.7f);SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper h){h.Clear();Color tint=Color.Lerp(new Color(1,.95f,.74f,.58f),new Color(1,.72f,.31f,.9f),pulse);for(int arc=0;arc<3;arc++){float first=phase+arc*Mathf.PI*2/3;for(int i=0;i<26;i++){float a=first-Direction*i*.046f,b=first-Direction*(i+1)*.046f;int k=h.currentVertCount;h.AddVert(P(a,1),tint,Vector2.zero);h.AddVert(P(a,.955f),tint,Vector2.zero);h.AddVert(P(b,.955f),tint,Vector2.zero);h.AddVert(P(b,1),tint,Vector2.zero);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}float end=first-Direction*26*.046f;int j=h.currentVertCount;h.AddVert(P(end,1.065f),tint,Vector2.zero);h.AddVert(P(end,.875f),tint,Vector2.zero);h.AddVert(P(end-Direction*.17f,.975f),tint,Vector2.zero);h.AddTriangle(j,j+1,j+2);}}
        Vector3 P(float a,float radius)=>new Vector3(Mathf.Cos(a)*rectTransform.rect.width*.5f*radius,Mathf.Sin(a)*rectTransform.rect.height*.5f*radius,0);
    }
}
