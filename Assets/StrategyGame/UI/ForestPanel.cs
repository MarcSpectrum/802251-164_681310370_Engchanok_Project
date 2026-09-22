using UnityEngine;
using UnityEngine.UI;

namespace Engchanok.StrategyGame
{
    // Resolution-independent paper shapes. Uses the standard UI Image material and raycast behavior.
    public sealed class ForestPanel : Image
    {
        public float cornerRadius = 22;
        public bool stitched;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect=GetPixelAdjustedRect();
            float radius=Mathf.Min(cornerRadius,Mathf.Min(rect.width,rect.height)*.5f);
            var points=Outline(rect,radius);
            vh.AddVert(rect.center,color,Vector2.zero);
            for(int i=0;i<points.Length;i++) vh.AddVert(points[i],color,Vector2.zero);
            for(int i=0;i<points.Length;i++) vh.AddTriangle(0,i+1,(i+1)%points.Length+1);
            if(!stitched||rect.width<80||rect.height<60) return;
            rect.xMin+=12; rect.xMax-=12; rect.yMin+=12; rect.yMax-=12;
            points=Outline(rect,Mathf.Max(4,radius-7));
            float walked=0;
            Color thread=new Color(.79f,.55f,.43f,.46f*color.a);
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=points[i], b=points[(i+1)%points.Length], delta=b-a;
                float length=delta.magnitude;
                if(length<.001f) continue;
                Vector2 direction=delta/length, normal=new Vector2(-direction.y,direction.x)*.85f;
                float cursor=0;
                while(cursor<length)
                {
                    float phase=Mathf.Repeat(walked+cursor,14);
                    float step=Mathf.Min(length-cursor,(phase<8?8:14)-phase);
                    if(step<.001f) step=Mathf.Min(.001f,length-cursor);
                    if(phase<8)
                    {
                        Vector2 start=a+direction*cursor,end=start+direction*step;
                        int first=vh.currentVertCount;
                        vh.AddVert(start-normal,thread,Vector2.zero); vh.AddVert(start+normal,thread,Vector2.zero);
                        vh.AddVert(end+normal,thread,Vector2.zero); vh.AddVert(end-normal,thread,Vector2.zero);
                        vh.AddTriangle(first,first+1,first+2); vh.AddTriangle(first,first+2,first+3);
                    }
                    cursor+=step;
                }
                walked+=length;
            }
        }
        static Vector2[] Outline(Rect rect,float radius)
        {
            const int steps=8;
            var points=new Vector2[4*(steps+1)];
            for(int corner=0;corner<4;corner++)
            {
                Vector2 center=corner switch {
                    0=>new Vector2(rect.xMax-radius,rect.yMax-radius),
                    1=>new Vector2(rect.xMin+radius,rect.yMax-radius),
                    2=>new Vector2(rect.xMin+radius,rect.yMin+radius),
                    _=>new Vector2(rect.xMax-radius,rect.yMin+radius) };
                for(int i=0;i<=steps;i++)
                {
                    float angle=(corner*90+i*90f/steps)*Mathf.Deg2Rad;
                    points[corner*(steps+1)+i]=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                }
            }
            return points;
        }
    }
}
