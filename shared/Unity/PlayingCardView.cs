using System;using System.Collections.Generic;using System.IO;
using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
namespace StudentAge.PlayingCards
{
    public sealed class CardArt:IDisposable
    {
        readonly string root;readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();readonly List<Texture2D> textures=new List<Texture2D>();readonly Dictionary<bool,Texture2D> contactShadows=new Dictionary<bool,Texture2D>();
        public const int ShadowPadding=20;
        public Texture2D ContactShadow(Sprite silhouette,bool back)
        {
            Texture2D cached;if(contactShadows.TryGetValue(back,out cached))return cached;
            var source=silhouette.texture;int w=source.width+ShadowPadding*2,h=source.height+ShadowPadding*2;var pixels=source.GetPixels32();var alpha=new float[w*h];
            for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)alpha[(y+ShadowPadding)*w+x+ShadowPadding]=pixels[y*source.width+x].a/255f;
            const int radius=16;var weights=new float[radius*2+1];float total=0;for(int i=-radius;i<=radius;i++){float weight=Mathf.Exp(-i*i/(2*7f*7f));weights[i+radius]=weight;total+=weight;}for(int i=0;i<weights.Length;i++)weights[i]/=total;
            var horizontal=new float[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++){float value=0;for(int i=-radius;i<=radius;i++)if(x+i>=0&&x+i<w)value+=alpha[y*w+x+i]*weights[i+radius];horizontal[y*w+x]=value;}
            var output=new Color32[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++){float value=0;for(int i=-radius;i<=radius;i++)if(y+i>=0&&y+i<h)value+=horizontal[(y+i)*w+x]*weights[i+radius];output[y*w+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(value*255));}
            cached=new Texture2D(w,h,TextureFormat.RGBA32,true);cached.SetPixels32(output);cached.Apply();cached.filterMode=FilterMode.Trilinear;cached.wrapMode=TextureWrapMode.Clamp;textures.Add(cached);contactShadows[back]=cached;return cached;
        }
        public CardArt(string path){root=path;}
        public Sprite Face(PlayingCard card)=>Load(card.AssetName);public Sprite Back=>Load("campus_back");
        public Sprite SoftShadow { get { Sprite result;if(sprites.TryGetValue("__shadow",out result))return result;var texture=new Texture2D(112,152,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<152;y++)for(int x=0;x<112;x++){float qx=Math.Abs(x-55.5f)-36,qy=Math.Abs(y-75.5f)-52;float dx=Math.Max(qx,0),dy=Math.Max(qy,0);float distance=Math.Max(0,Mathf.Sqrt(dx*dx+dy*dy)+Math.Min(Math.Max(qx,qy),0)-4);texture.SetPixel(x,y,new Color(1,1,1,Mathf.Exp(-distance*distance/10)));}texture.Apply();textures.Add(texture);result=Sprite.Create(texture,new Rect(0,0,112,152),new Vector2(.5f,.5f));sprites["__shadow"]=result;return result;
        }}
        public Sprite Stock { get { Sprite sprite;if(sprites.TryGetValue("__stock",out sprite))return sprite;
            var texture=new Texture2D(80,112,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Bilinear;
            for(int y=0;y<112;y++)for(int x=0;x<80;x++){float dx=Math.Max(4-x,Math.Max(0,x-75)),dy=Math.Max(4-y,Math.Max(0,y-107));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(4.5f-Mathf.Sqrt(dx*dx+dy*dy))));}
            texture.Apply();textures.Add(texture);sprite=Sprite.Create(texture,new Rect(0,0,80,112),new Vector2(.5f,.5f));sprites["__stock"]=sprite;return sprite;
        }}
        Sprite Load(string name){Sprite sprite;if(sprites.TryGetValue(name,out sprite))return sprite;string path=Path.Combine(root,name+".png");if(!File.Exists(path))throw new FileNotFoundException("Missing shared playing card",path);var texture=new Texture2D(2,2,TextureFormat.RGBA32,true);if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path)))throw new InvalidDataException(path);texture.filterMode=FilterMode.Trilinear;textures.Add(texture);sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));sprites.Add(name,sprite);return sprite;}
        public void Dispose(){foreach(var s in sprites.Values)UnityEngine.Object.Destroy(s);foreach(var t in textures)UnityEngine.Object.Destroy(t);sprites.Clear();textures.Clear();contactShadows.Clear();}
    }
    // Shared by 24 and Landlord. TablePose opts into perspective; the default 24 layout stays flat.
    public sealed class PlayingCardView:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler
    {
        public PlayingCard Card{get;private set;}public bool FaceDown{get;private set;}
        public bool Interactable=true;public Action<PlayingCardView> Clicked;
        public Vector2 Position=>rect.anchoredPosition;public bool Physical=>physical;public bool IsMoving=>travel>0||flipping;
        CanvasGroup travelVisibility;RectTransform rect;Image face,edge,halo;CardArt art;Vector2 home,from;
        float start,travel,flipStart=-10,tilt,restAngle,pitch,yaw,lift,arc,initialScale=1,initialPitch,landAt=-10;
        bool hover,selected,flipping,targetDown,physical;
        readonly List<CardProjection> projections=new List<CardProjection>();
        public static PlayingCardView Create(Transform parent,CardArt art,PlayingCard card,float x,float y,float width=180,bool down=false)
        {
            float h=width*1.4f;var o=new GameObject("PlayingCard-"+card.Id,typeof(RectTransform));o.transform.SetParent(parent,false);
            var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(width,h);r.anchoredPosition=new Vector2(x+width/2,-y-h/2);
            var v=o.AddComponent<PlayingCardView>();v.rect=r;v.art=art;v.Card=card;v.home=r.anchoredPosition;v.FaceDown=down;
            v.edge=v.Image("Selection",new Vector2(8,-10),new Vector2(width+8,h+8),new Color(.2f,.13f,.08f,.27f));
            v.face=v.Image("PrintedFace",Vector2.zero,new Vector2(width*1.05f,h*1.05f),Color.white);v.face.sprite=down?art.Back:art.Face(card);v.face.raycastTarget=true;v.face.alphaHitTestMinimumThreshold=.08f;return v;
        }
        Image Image(string name,Vector2 position,Vector2 size,Color color){var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(transform,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;var i=o.GetComponent<Image>();i.color=color;i.sprite=art.Back;i.raycastTarget=false;return i;}
        public void TablePose(float angle,float pitch=-12,float yaw=0)
        {
            restAngle=angle;this.pitch=pitch;this.yaw=yaw;
            if(!physical){physical=true;edge.gameObject.SetActive(false);Vector2 size=rect.sizeDelta;
                var paper=Image("PaperEdge",new Vector2(0,-.9f),size,new Color(.88f,.85f,.78f));paper.sprite=art.Stock;
                halo=Image("GoldSelection",Vector2.zero,size+new Vector2(3,3),new Color(.96f,.76f,.39f));halo.sprite=art.Stock;halo.gameObject.SetActive(selected);face.transform.SetAsLastSibling();
                foreach(var im in GetComponentsInChildren<Image>(true)){if(im==edge)continue;var effect=im.gameObject.AddComponent<CardProjection>();effect.Shaded=im==face;projections.Add(effect);}
            }
            foreach(var p in projections)p.SetAngles(pitch,yaw);rect.localEulerAngles=new Vector3(0,0,angle);
        }
        public void ShadeFrom(PlayingCardView next)
        {
            var mask=Image("OverlapMask",Vector2.zero,face.rectTransform.sizeDelta,Color.white);mask.sprite=face.sprite;var projection=mask.gameObject.AddComponent<CardProjection>();projection.SetAngles(pitch,yaw);projections.Add(projection);mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var o=new GameObject("OverlapContactShade",typeof(RectTransform));o.transform.SetParent(mask.transform,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=rect.sizeDelta;var shade=o.AddComponent<CardOverlapShade>();shade.Cover=next;shade.raycastTarget=false;
        }
        public Texture2D ContactShadowTexture=>art.ContactShadow(face.sprite,FaceDown);
        public Vector3 ShadowCorner(Vector2 uv)
        {
            // Include the bitmap gutter, then project exactly like the printed face. The blur follows its alpha contour, including rounded corners.
            Vector2 size=face.rectTransform.sizeDelta;var texture=face.sprite.texture;
            size+=new Vector2(size.x*CardArt.ShadowPadding*2/texture.width,size.y*CardArt.ShadowPadding*2/texture.height);
            var point=new Vector3((uv.x-.5f)*size.x-1.5f,(uv.y-.5f)*size.y-.5f,0);var projection=face.GetComponent<CardProjection>();
            return face.transform.TransformPoint(projection!=null?projection.ProjectPoint(point):point);
        }
        public void Select(bool value){selected=value;if(physical)halo.gameObject.SetActive(value);else edge.color=value?new Color(.88f,.62f,.2f):new Color(.2f,.13f,.08f,.27f);}
        public void MoveFrom(Vector2 origin,float delay,float duration=.45f,float angle=0){if(travelVisibility==null)travelVisibility=gameObject.AddComponent<CanvasGroup>();travelVisibility.alpha=delay>0?0:1;from=origin;start=StudentAge.CampusUno.PlayClock.Now+delay;travel=duration;tilt=angle;arc=physical?65:0;initialScale=physical?.55f:1;initialPitch=0;rect.anchoredPosition=origin;}
        public void FlyFrom(Vector2 origin,float delay,float duration,float angle,float scale,float fromPitch){MoveFrom(origin,delay,duration,angle);arc=85;initialScale=scale;initialPitch=fromPitch;}
        public void ReflowFrom(Vector2 origin){MoveFrom(origin,0,.24f,restAngle);arc=0;initialScale=1;initialPitch=pitch;}
        public void Flip(bool down){if(FaceDown==down&&!flipping)return;targetDown=down;flipStart=StudentAge.CampusUno.PlayClock.Now;flipping=true;}
        void Update()
        {
            if(travelVisibility!=null)travelVisibility.alpha=travel>0&&StudentAge.CampusUno.PlayClock.Now<start?0:1;
            float t=travel>0?Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-start)/travel):1,e=1-Mathf.Pow(1-t,3);
            lift=Mathf.Lerp(lift,selected?1:hover&&Interactable?.85f:0,Mathf.Clamp01(StudentAge.CampusUno.PlayClock.Delta*18));
            var target=home+Vector2.up*(physical?lift*30:selected?22:hover&&Interactable?12:0);
            rect.anchoredPosition=Vector2.Lerp(from,target,e)+Vector2.up*(physical?Mathf.Sin(t*Mathf.PI)*arc:0);
            if(t>=1&&travel>0){if(physical&&arc>0)landAt=StudentAge.CampusUno.PlayClock.Now;travel=0;}
            float finalAngle=physical?restAngle*(1-lift*.65f):0;rect.localEulerAngles=new Vector3(0,0,Mathf.Lerp(tilt,finalAngle,e));
            float sx=1;if(flipping){float p=Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-flipStart)/.36f);sx=Mathf.Max(.035f,Mathf.Abs(Mathf.Cos(p*Mathf.PI)));if(p>=.5f&&FaceDown!=targetDown){FaceDown=targetDown;face.sprite=FaceDown?art.Back:art.Face(Card);}if(p>=1)flipping=false;}
            float scale=physical?Mathf.Lerp(initialScale,1,e)+Mathf.Sin(t*Mathf.PI)*.055f+lift*.018f:1;
            if(physical){float land=Mathf.Clamp01((StudentAge.CampusUno.PlayClock.Now-landAt)/.24f);scale-=Mathf.Sin(land*Mathf.PI)*.018f;
                float currentPitch=Mathf.Lerp(initialPitch,pitch+lift*7,e)-Mathf.Sin(t*Mathf.PI)*8;
                foreach(var p in projections)p.SetAngles(currentPitch,yaw*(1-lift*.6f));

            }
            rect.localScale=new Vector3(sx*scale,scale,1);
        }
        public void OnPointerEnter(PointerEventData e){hover=Interactable;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&Interactable&&!FaceDown&&!flipping&&travel==0)Clicked?.Invoke(this);}
    }
    // The covering card's blurred alpha silhouette is clipped to the lower printed face; no rectangular band or desk halo.
    public sealed class CardOverlapShade:MaskableGraphic
    {
        public PlayingCardView Cover;
        public override Texture mainTexture=>Cover!=null?Cover.ContactShadowTexture:Texture2D.whiteTexture;
        void LateUpdate(){SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();if(Cover==null||!Cover.gameObject.activeInHierarchy)return;
            var tint=new Color(.14f,.11f,.075f,.36f);
            var uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            foreach(var p in uv)h.AddVert(rectTransform.InverseTransformPoint(Cover.ShadowCorner(p)),tint,p);
            h.AddTriangle(0,1,2);h.AddTriangle(2,3,0);
        }
    }
    // Project the same printed sprite, paper layers and hit polygon into a tabletop perspective.
    public sealed class CardProjection:BaseMeshEffect,ICanvasRaycastFilter
    {
        float pitch,yaw;public bool Shaded;Quaternion rotation=Quaternion.identity;
        public void SetAngles(float p,float y){if(Mathf.Abs(p-pitch)<.04f&&Mathf.Abs(y-yaw)<.04f)return;pitch=p;yaw=y;rotation=Quaternion.Euler(p,y,0);graphic.SetVerticesDirty();}
        public Vector3 ProjectPoint(Vector3 local)=>Project(local);
        Vector3 Project(Vector3 local){var v=rotation*local;float perspective=1200/(1200-v.z);return new Vector3(v.x*perspective,v.y*perspective,0);}
        public override void ModifyMesh(VertexHelper helper){if(!IsActive())return;var r=graphic.rectTransform.rect;UIVertex v=default;for(int i=0;i<helper.currentVertCount;i++){helper.PopulateUIVertex(ref v,i);float y=Mathf.InverseLerp(r.yMin,r.yMax,v.position.y);v.position=Project(v.position);if(Shaded){Color c=v.color;c*=Color.Lerp(new Color(.94f,.91f,.86f,1),Color.white,y);v.color=c;}helper.SetUIVertex(v,i);}}
        public bool IsRaycastLocationValid(Vector2 screen,Camera camera){Vector2 p;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(graphic.rectTransform,screen,camera,out p))return false;var r=graphic.rectTransform.rect;var q=new[]{(Vector2)Project(new Vector3(r.xMin,r.yMin)),(Vector2)Project(new Vector3(r.xMax,r.yMin)),(Vector2)Project(new Vector3(r.xMax,r.yMax)),(Vector2)Project(new Vector3(r.xMin,r.yMax))};float sign=0;for(int i=0;i<4;i++){Vector2 a=q[i],b=q[(i+1)%4];float cross=(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);if(Mathf.Abs(cross)<.001f)continue;if(sign!=0&&sign*cross<0)return false;sign=cross;}return true;}
    }
}
