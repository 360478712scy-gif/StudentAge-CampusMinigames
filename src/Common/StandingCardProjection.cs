using UnityEngine;
using UnityEngine.UI;
namespace StudentAge.CampusUno
{
    // Every graphic in an opponent row uses one continuous strip projection. Cards overlap before the row bends.
    internal sealed class StandingCardProjection:BaseMeshEffect
    {
        public RectTransform Root;public CardVisual Owner;public Vector2 Center;public float Width,Height,Yaw;bool moving;
        void LateUpdate(){bool next=Owner!=null&&Owner.IsDealing;if(next!=moving||Owner!=null&&Owner.IsInMotion){moving=next;graphic.SetVerticesDirty();}}
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive()||Root==null||Owner!=null&&Owner.IsDealing)return;UIVertex vertex=default;float yaw=Yaw*Mathf.Deg2Rad,elevation=26*Mathf.Deg2Rad;
            for(int i=0;i<mesh.currentVertCount;i++)
            {
                mesh.PopulateUIVertex(ref vertex,i);var p=Root.InverseTransformPoint(graphic.transform.TransformPoint(vertex.position));float x=p.x-Center.x,height=p.y-Center.y+Height*.5f;
                float u=x/(Width*.5f),depth=x*Mathf.Sin(yaw)+24*u*u;float perspective=1300/(1300+depth*Mathf.Cos(elevation)-height*Mathf.Sin(elevation));
                var projected=new Vector3(Center.x+x*Mathf.Cos(yaw)*perspective,Center.y+(height*Mathf.Cos(elevation)+depth*Mathf.Sin(elevation))*perspective-Height*.5f,0);
                vertex.position=graphic.transform.InverseTransformPoint(Root.TransformPoint(projected));Color color=vertex.color;float light=Mathf.Lerp(.91f,1,Mathf.Clamp01((u+1)*.5f));color.r*=light;color.g*=light;color.b*=light;vertex.color=color;mesh.SetUIVertex(vertex,i);
            }
        }
    }
}
