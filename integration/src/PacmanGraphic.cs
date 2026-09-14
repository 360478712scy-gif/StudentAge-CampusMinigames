using UnityEngine;using UnityEngine.UI;
namespace StudentAge.CampusMinigames {
public sealed class PacmanGraphic:MaskableGraphic {
    float mouth=25;public void Pose(int direction,float phase){mouth=8+27*Mathf.Abs(Mathf.Sin(phase));rectTransform.localEulerAngles=new Vector3(0,0,direction==0?90:direction==2?-90:direction==3?180:0);SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper h){h.Clear();Vector2 c=rectTransform.rect.center;float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.48f;int center=0;h.AddVert(c,color,Vector2.zero);const int steps=40;for(int n=0;n<=steps;n++){float a=Mathf.Lerp(mouth,360-mouth,n/(float)steps)*Mathf.Deg2Rad;h.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);if(n>0)h.AddTriangle(center,n,n+1);}Vector2 eye=c+new Vector2(2,radius*.55f);int k=h.currentVertCount;h.AddVert(eye,Color.black,Vector2.zero);for(int n=0;n<=12;n++){float a=n*Mathf.PI/6;h.AddVert(eye+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*1.9f,Color.black,Vector2.zero);if(n>0)h.AddTriangle(k,k+n,k+n+1);}}
}
}
