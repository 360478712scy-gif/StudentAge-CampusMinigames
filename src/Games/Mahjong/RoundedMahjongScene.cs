using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;
namespace StudentAge.Mahjong
{
    // A private, supersampled tile layer. The illustrated table and all native UI remain in the host canvas.
    public sealed class RoundedMahjongScene:MonoBehaviour
    {
        public RenderTexture Target{get;private set;}public Camera ViewCamera{get;private set;}
        bool disposed;GameObject world;Material tiles,composite;Mesh body;readonly Dictionary<int,Mesh> faces=new Dictionary<int,Mesh>();readonly List<TileVisual> pieces=new List<TileVisual>();
        public static RoundedMahjongScene Create(Transform parent,MahjongArt art)
        {
            var o=new GameObject("RoundedMahjongTiles",typeof(RectTransform),typeof(RawImage));o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(0,35);r.sizeDelta=new Vector2(1600,900);
            var scene=o.AddComponent<RoundedMahjongScene>();scene.Build(art);return scene;
        }
        void Build(MahjongArt art)
        {
            world=new GameObject("CampusMahjongPrivateStage");world.transform.position=new Vector3(20000,20000,20000);
            var camera=new GameObject("TileCamera");camera.transform.SetParent(world.transform,false);ViewCamera=camera.AddComponent<Camera>();camera.transform.localPosition=new Vector3(0,MahjongProjection.Distance*MahjongProjection.Sin,MahjongProjection.Distance*MahjongProjection.Cos);camera.transform.localRotation=Quaternion.LookRotation(-camera.transform.localPosition,Vector3.up);
            ViewCamera.fieldOfView=2*Mathf.Atan(450/MahjongProjection.Focal)*Mathf.Rad2Deg;ViewCamera.aspect=1600f/900;ViewCamera.nearClipPlane=1;ViewCamera.farClipPlane=7000;ViewCamera.clearFlags=CameraClearFlags.SolidColor;ViewCamera.backgroundColor=new Color(.95f,.94f,.88f,0);ViewCamera.cullingMask=1<<30;ViewCamera.allowHDR=false;ViewCamera.allowMSAA=true;ViewCamera.renderingPath=RenderingPath.Forward;
            Target=new RenderTexture(3200,1800,24,RenderTextureFormat.ARGB32){antiAliasing=4,filterMode=FilterMode.Bilinear,useMipMap=false,name="Mahjong 2x MSAA"};Target.Create();ViewCamera.targetTexture=Target;
            tiles=new Material(art.TileShader){mainTexture=art.Atlas,color=new Color(.98f,.97f,.91f)};MakeLight("Soft window light",new Vector3(35,180,0),.87f);MakeLight("Table fill",new Vector3(40,-45,0),.55f);composite=new Material(art.CompositeShader);var image=GetComponent<RawImage>();image.texture=Target;image.material=composite;image.raycastTarget=false;body=RoundedBox(new Vector3(58,81.2f,24.36f),4.8f);
        }
        void MakeLight(string name,Vector3 angles,float intensity)
        {var o=new GameObject(name);o.transform.SetParent(world.transform,false);o.transform.localRotation=Quaternion.Euler(angles);var light=o.AddComponent<Light>();light.type=LightType.Directional;light.intensity=intensity;light.color=Color.white;light.cullingMask=1<<30;light.shadows=LightShadows.None;}
        public TileVisual Attach(MahjongTileView owner,int tile,bool standing,bool back,float angle,float elevation,Vector2 origin)
        {
            var visual=new TileVisual(this,owner,tile,standing,back,angle,elevation,origin);pieces.Add(visual);return visual;
        }
        Mesh Face(int kind,bool reverse)
        {
            int key=kind+(reverse?100:0);Mesh mesh;if(faces.TryGetValue(key,out mesh))return mesh;
            float x=24.2f,y=35.8f,z=reverse?-12.23f:12.23f;var verts=new[]{new Vector3(-x,y,z),new Vector3(x,y,z),new Vector3(x,-y,z),new Vector3(-x,-y,z)};
            Func<float,float,Vector2> uv=(u,v)=>new Vector2((kind%8+u)/8f,1-(kind/8+v)/5f);
            mesh=new Mesh{name="Mahjong face "+key};mesh.vertices=verts;mesh.normals=new[]{reverse?Vector3.back:Vector3.forward,reverse?Vector3.back:Vector3.forward,reverse?Vector3.back:Vector3.forward,reverse?Vector3.back:Vector3.forward};mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            mesh.uv=!reverse?new[]{uv(.91f,.065f),uv(.09f,.065f),uv(.09f,.935f),uv(.91f,.935f)}:new[]{uv(.09f,.065f),uv(.91f,.065f),uv(.91f,.935f),uv(.09f,.935f)};mesh.triangles=reverse?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};mesh.RecalculateBounds();faces[key]=mesh;return mesh;
        }
        void Part(Transform parent,Mesh mesh)
        {var o=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));o.layer=30;o.transform.SetParent(parent,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=tiles;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        public sealed class TileVisual
        {
            readonly RoundedMahjongScene scene;readonly MahjongTileView owner;readonly Vector2 origin;readonly bool upright;readonly float elevation;public readonly GameObject Root;
            internal TileVisual(RoundedMahjongScene s,MahjongTileView o,int tile,bool standing,bool back,float angle,float e,Vector2 p)
            {
                scene=s;owner=o;origin=p;elevation=e;upright=standing&&(back||Mathf.Abs(angle)<1);
                Root=new GameObject("Rounded tile "+tile);Root.layer=30;Root.transform.SetParent(s.world.transform,false);s.Part(Root.transform,s.body);s.Part(Root.transform,s.Face(Math.Max(0,tile/4),false));s.Part(Root.transform,s.Face(34,true));
                Root.transform.localRotation=Quaternion.Euler(0,angle,0)*(upright?Quaternion.identity:Quaternion.Euler(back?90:-90,0,0));Sync(Vector2.zero,0,1);
            }
            public void Sync(Vector2 delta,float lift,float scale)
            {
                if(Root==null||scene.world==null)return;bool active=owner!=null&&owner.gameObject.activeInHierarchy;if(Root.activeSelf!=active)Root.SetActive(active);if(!active)return;
                Vector2 ground=MahjongProjection.Ground(origin+new Vector2(delta.x,-delta.y+lift));float factor=MahjongProjection.Focal/(MahjongProjection.Distance-ground.y*MahjongProjection.Cos);
                float h=lift/(MahjongProjection.Cos*factor);Root.transform.localPosition=new Vector3(-ground.x,(upright?40.6f:12.18f)*scale+elevation+h,ground.y);Root.transform.localScale=Vector3.one*scale;
            }
            public void Dispose(){if(Root!=null)UnityEngine.Object.Destroy(Root);}
        }
        static Mesh RoundedBox(Vector3 size,float radius)
        {
            Vector3 half=size*.5f,inner=half-Vector3.one*radius;var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            Func<float,float[]> steps=h=>new[]{-h,-h+radius*.20f,-h+radius*.50f,-h+radius*.78f,-h+radius,0,h-radius,h-radius*.78f,h-radius*.50f,h-radius*.20f,h};
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;var xs=steps(half[u]);var ys=steps(half[v]);int start=verts.Count;
                for(int j=0;j<ys.Length;j++)for(int i=0;i<xs.Length;i++)
                {
                    var p=Vector3.zero;p[axis]=half[axis]*sign;p[u]=xs[i];p[v]=ys[j];var closest=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));var normal=(p-closest).normalized;verts.Add(closest+normal*radius);normals.Add(normal);uv.Add(new Vector2(4.5f/8,1-4.5f/5));colors.Add(new Color(1,.99f,.94f));
                }
                for(int j=0;j<ys.Length-1;j++)for(int i=0;i<xs.Length-1;i++)
                {
                    int a=start+j*xs.Length+i,b=a+1,c=a+xs.Length+1,d=a+xs.Length;Vector3 expected=Vector3.zero;expected[axis]=sign;bool forward=Vector3.Dot(Vector3.Cross(verts[b]-verts[a],verts[d]-verts[a]),expected)>0;
                    if(forward)triangles.AddRange(new[]{a,b,c,a,c,d});else triangles.AddRange(new[]{a,c,b,a,d,c});
                }
            }
            var mesh=new Mesh{name="Rounded ivory mahjong"};mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        void LateUpdate(){pieces.RemoveAll(v=>v.Root==null);}
        public void Dispose()
        {if(disposed)return;disposed=true;if(ViewCamera!=null){ViewCamera.enabled=false;ViewCamera.targetTexture=null;}if(world!=null){Destroy(world);world=null;}if(Target!=null){Target.Release();Destroy(Target);Target=null;}if(body!=null)Destroy(body);foreach(var f in faces.Values)Destroy(f);faces.Clear();if(tiles!=null)Destroy(tiles);if(composite!=null)Destroy(composite);}
        void OnDestroy(){Dispose();}
    }
}
