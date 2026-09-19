using System;using System.IO;using System.Collections;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
namespace StudentAge.Mahjong
{
    public sealed class MahjongArt:IDisposable
    {
        public Texture2D Atlas,Table;public Shader TileShader,CompositeShader;public RoundedMahjongScene Scene;
        public MahjongArt(string root){Atlas=Load(Path.Combine(root,"tiles.png"));Table=Load(Path.Combine(root,"table.png"));TileShader=Shader.Find("Legacy Shaders/Diffuse");CompositeShader=Shader.Find("UI/Default");if(TileShader==null||CompositeShader==null)throw new IOException("Host Mahjong shaders unavailable");}
        static Texture2D Load(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,true);ImageConversion.LoadImage(t,File.ReadAllBytes(p));t.filterMode=FilterMode.Trilinear;t.anisoLevel=8;t.wrapMode=TextureWrapMode.Clamp;return t;}
        public void Dispose(){if(Scene!=null){Scene.Dispose();UnityEngine.Object.Destroy(Scene.gameObject);}UnityEngine.Object.Destroy(Atlas);UnityEngine.Object.Destroy(Table);}
    }
    public sealed class MahjongTileView:MaskableGraphic,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
    {
        RoundedMahjongScene.TileVisual visual;public int TileId;public bool Moving{get;private set;}public Action<int> Clicked;
        Texture2D atlas;float width,height,angle,lean,airborne,elevation;Vector2 ground,planeOrigin;bool standing,back,selected,hover;Vector2 basePosition;
        public override Texture mainTexture=>atlas;
        public static MahjongTileView Create(Transform parent,MahjongArt art,int tile,float x,float y,float width=58,bool standing=true,bool back=false,float angle=0,float lean=0,float elevation=0)
        {
            var o=new GameObject("MahjongTile-"+tile,typeof(RectTransform));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,width*1.4f);
            var v=o.AddComponent<MahjongTileView>();v.atlas=art.Atlas;v.TileId=tile;v.width=width;v.height=width*1.4f;v.standing=standing;v.back=back;v.angle=angle;v.lean=lean;v.raycastTarget=false;v.basePosition=r.anchoredPosition;v.elevation=elevation;v.planeOrigin=new Vector2(x,y);
            Transform cursor=parent;while(cursor!=null&&cursor.name!="Play"&&cursor.name!="Stage"){var pr=cursor as RectTransform;if(pr!=null)v.planeOrigin+=new Vector2(pr.anchoredPosition.x,-pr.anchoredPosition.y);cursor=cursor.parent;}
            v.ground=MahjongProjection.Ground(v.planeOrigin);if(art.Scene!=null&&width>=40)visualAttach(v,art,tile,standing,back,angle,elevation);return v;
        }
        static void visualAttach(MahjongTileView v,MahjongArt art,int tile,bool standing,bool back,float angle,float elevation){v.visual=art.Scene.Attach(v,tile,standing,back,angle,elevation,v.planeOrigin);}
        void LateUpdate(){if(visual!=null)visual.Sync(rectTransform.anchoredPosition-basePosition,airborne+(!Moving?(selected?17:hover?5:0):0),rectTransform.localScale.x);}
        protected override void OnDisable(){base.OnDisable();if(visual!=null&&visual.Root!=null)visual.Root.SetActive(false);}
        protected override void OnDestroy(){visual?.Dispose();base.OnDestroy();}
        Vector3 P(float x,float z,float y)
        {
            float r=angle*Mathf.Deg2Rad,c=Mathf.Cos(r),sn=Mathf.Sin(r);Vector2 screen=MahjongProjection.Project(ground.x+x*c-z*sn,ground.y+x*sn+z*c,y+elevation);
            return new Vector3(screen.x-planeOrigin.x,planeOrigin.y-screen.y,0);
        }
        Vector2 UV(int frame,float u,float v){if(frame==35){u=Mathf.Lerp(.03f,.97f,u);v=Mathf.Lerp(.03f,.97f,v);}return new Vector2((frame%8+u)/8f,1-(frame/8+v)/5f);}
        void Quad(VertexHelper h,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint,int frame=36,bool aa=false)
        {
            int n=h.currentVertCount;h.AddVert(a,tint,UV(frame,0,0));h.AddVert(b,tint,UV(frame,1,0));h.AddVert(c,tint,UV(frame,1,1));h.AddVert(d,tint,UV(frame,0,1));h.AddTriangle(n,n+1,n+2);h.AddTriangle(n,n+2,n+3);
            if(!aa)return;var vertices=new[]{a,b,c,d};Color transparent=tint;transparent.a=0;float area=0;
            for(int i=0;i<4;i++){var p=vertices[i];var q=vertices[(i+1)%4];area+=p.x*q.y-p.y*q.x;}float sign=area>=0?1:-1;var outer=new Vector3[4];
            for(int i=0;i<4;i++){Vector3 prev=vertices[i]-vertices[(i+3)%4],next=vertices[(i+1)%4]-vertices[i];Vector3 n1=new Vector3(prev.y,-prev.x).normalized*sign,n2=new Vector3(next.y,-next.x).normalized*sign,bisector=(n1+n2).normalized;outer[i]=vertices[i]+bisector*(1.25f/Mathf.Max(.25f,Vector3.Dot(bisector,n2)));}
            for(int i=0;i<4;i++){int j=(i+1)%4,k=h.currentVertCount;h.AddVert(vertices[i],tint,UV(36,.5f,.5f));h.AddVert(vertices[j],tint,UV(36,.5f,.5f));h.AddVert(outer[j],transparent,UV(36,.5f,.5f));h.AddVert(outer[i],transparent,UV(36,.5f,.5f));h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}
        }
        void Face(VertexHelper h,Vector3 a,Vector3 b,Vector3 c,Vector3 d,int frame,Color tint)
        {
            int center=h.currentVertCount;h.AddVert((a+b+c+d)/4,tint,UV(frame,.5f,.5f));int steps=6;float rx=.045f,ry=.032f;
            for(int corner=0;corner<4;corner++)for(int j=0;j<=steps;j++)
            {float ang=(-180+corner*90+j*90f/steps)*Mathf.Deg2Rad;float cx=corner==0||corner==3?rx:1-rx,cy=corner<2?ry:1-ry;float u=cx+Mathf.Cos(ang)*rx,v=cy+Mathf.Sin(ang)*ry;h.AddVert(Vector3.Lerp(Vector3.Lerp(a,b,u),Vector3.Lerp(d,c,u),v),tint,UV(frame,u,v));}
            int count=4*(steps+1);for(int i=0;i<count;i++)h.AddTriangle(center,center+1+i,center+1+(i+1)%count);
        }
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();if(atlas==null)return;float w=width*.5f,hh=height,t=width*.52f,flat=width*.30f;
            float r=angle*Mathf.Deg2Rad,cs=Mathf.Cos(r),sn=Mathf.Sin(r);
            // View direction in the tile's own table axes. This keeps each seat's fronts facing its owner.
            float vx=-ground.x,vz=MahjongProjection.Distance*MahjongProjection.Cos-ground.y;
            float localX=vx*cs+vz*sn,localZ=-vx*sn+vz*cs;
            Color cream=new Color(.96f,.94f,.85f),light=new Color(1,.99f,.91f),side=new Color(.76f,.78f,.66f),green=new Color(.24f,.43f,.35f);
            if(elevation<=0)
            {
                float sh=standing?t*.75f:hh*.62f;Vector3 shift=new Vector3(width*.035f,-airborne-(!Moving?(selected?17:hover?5:0):0));var tint=new Color(1,1,1,Mathf.Lerp(1,.35f,Mathf.Clamp01(airborne/70)));
                Quad(h,P(-w*1.4f,-sh,0)+shift,P(w*1.4f,-sh,0)+shift,P(w*1.4f,sh,0)+shift,P(-w*1.4f,sh,0)+shift,tint,35);
            }
            if(visual!=null)return;
            if(standing)
            {
                float z=t*.5f;
                // Caps form a continuous row; only the exposed end face is visible once the next tile covers it.
                Quad(h,P(-w,-z,hh),P(w,-z,hh),P(w,z,hh),P(-w,z,hh),light,36,true);
                if(localX>0)Quad(h,P(w,z,hh),P(w,-z,hh),P(w,-z,0),P(w,z,0),side,36,true);
                else Quad(h,P(-w,-z,hh),P(-w,z,hh),P(-w,z,0),P(-w,-z,0),cream,36,true);
                bool near=localZ>=0;float faceZ=near?z:-z;int frame=near&&!back?Math.Max(0,TileId/4):34;
                Color tint=selected?new Color(1,1,.88f):Color.white;
                if(near)Face(h,P(-w,faceZ,hh),P(w,faceZ,hh),P(w,faceZ,0),P(-w,faceZ,0),frame,tint);
                else Face(h,P(w,faceZ,hh),P(-w,faceZ,hh),P(-w,faceZ,0),P(w,faceZ,0),34,tint);
            }
            else
            {
                float z=hh*.5f;
                if(localZ>=0)Quad(h,P(-w,z,flat),P(w,z,flat),P(w,z,0),P(-w,z,0),cream,36,true);
                else Quad(h,P(w,-z,flat),P(-w,-z,flat),P(-w,-z,0),P(w,-z,0),side,36,true);
                if(localX>0)Quad(h,P(w,z,flat),P(w,-z,flat),P(w,-z,0),P(w,z,0),side,36,true);
                else Quad(h,P(-w,-z,flat),P(-w,z,flat),P(-w,z,0),P(-w,-z,0),cream,36,true);
                Face(h,P(-w,-z,flat),P(w,-z,flat),P(w,z,flat),P(-w,z,flat),back?34:Math.Max(0,TileId/4),Color.white);
            }
        }
        public void Select(bool on){selected=on;SetPosition();SetVerticesDirty();}
        void SetPosition(){if(!Moving)rectTransform.anchoredPosition=basePosition+new Vector2(0,selected?17:hover?5:0);}
        public void OnPointerClick(PointerEventData e){if(raycastTarget&&!Moving&&e.button==PointerEventData.InputButton.Left)Clicked?.Invoke(TileId);}
        public void OnPointerEnter(PointerEventData e){if(!raycastTarget)return;hover=true;SetPosition();SetVerticesDirty();}
        public void OnPointerExit(PointerEventData e){hover=false;SetPosition();SetVerticesDirty();}
        public void FlyFrom(Vector2 origin,float delay=0,float duration=.28f,float lift=45,float scale=1){StartCoroutine(Fly(new Vector2(origin.x,-origin.y),delay,duration,lift,scale));}
        IEnumerator Fly(Vector2 from,float delay,float duration,float lift,float scale)
        {
            Moving=true;rectTransform.anchoredPosition=from;rectTransform.localScale=Vector3.one*scale;if(delay>0)yield return new StudentAge.CampusUno.PlayDelay(delay);float t=0;
            while(t<duration){t+=StudentAge.CampusUno.PlayClock.Delta;float u=Mathf.Clamp01(t/duration),e=u*u*(3-2*u);airborne=Mathf.Sin(u*Mathf.PI)*lift;SetVerticesDirty();rectTransform.localScale=Vector3.one*Mathf.Lerp(scale,1,e);rectTransform.anchoredPosition=Vector2.Lerp(from,basePosition,e)+new Vector2(0,Mathf.Sin(u*Mathf.PI)*lift);yield return null;}
            Moving=false;airborne=0;SetPosition();SetVerticesDirty();
        }
    }
}
