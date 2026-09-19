using UnityEngine;
namespace StudentAge.Mahjong
{
    public static class MahjongProjection
    {
        public const float Distance=3000,Focal=2700,Sin=.55f,Cos=.83516465f;
        public static Vector2 Project(float x,float z,float h=0)
        {float scale=Focal/(Distance-z*Cos-h*Sin);return new Vector2(800+x*scale,415+(z*Sin-h*Cos)*scale);}
        public static Vector2 Ground(Vector2 screen)
        {float q=(screen.y-415)/Focal,z=q*Distance/(Sin+q*Cos),scale=Focal/(Distance-z*Cos);return new Vector2((screen.x-800)/scale,z);}
    }
}
