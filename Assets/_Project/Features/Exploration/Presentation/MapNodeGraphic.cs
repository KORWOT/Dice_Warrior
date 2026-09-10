using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // Public type symbols only: swords, a question mark, chest, shop, campfire and boss crown.
    // An undiscovered node has a broken ring and diamond, distinct from the Event symbol.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MapNodeGraphic : MaskableGraphic
    {
        public Color surface = new Color(.047f, .057f, .071f);
        public Color rim = new Color(.42f, .46f, .51f);
        [Range(1, 5)] public float ringWidth = 2;
        NodeType? publicType;
        bool available, completed, unreachable, selected, current, drawSymbol = true;

        public void Bind(NodeType? type, Color tint, bool canTravel, bool done, bool blocked,
            bool preview, bool player, bool symbol)
        {
            publicType = type; rim = tint; available = canTravel; completed = done;
            unreachable = blocked; selected = preview; current = player; drawSymbol = symbol;
            SetVerticesDirty();
        }
        public void SetSelected(bool value) { selected = value; SetVerticesDirty(); }

        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .43f;
            if (radius <= 0) return;
            Vector2 center = rect.center;
            Color tint = current ? new Color(.28f, .66f, 1) : rim;
            if (unreachable) tint = new Color(.29f, .32f, .36f);
            if (available || selected || current)
            {
                Ring(mesh, center, radius + 5, 4, Alpha(tint, selected ? .32f : .16f), false);
                Ring(mesh, center, radius + 9, 2, Alpha(tint, .07f), false);
            }
            Disc(mesh, center, radius, surface);
            Ring(mesh, center, radius, ringWidth, tint, !publicType.HasValue);
            if (selected)
                for (int i = 0; i < 4; i++)
                {
                    var d = new Vector2(Mathf.Cos(i * Mathf.PI / 2), Mathf.Sin(i * Mathf.PI / 2));
                    Line(mesh, center + d * (radius + 3), center + d * (radius + 9), 2, tint);
                }
            if (drawSymbol)
            {
                float scale = radius * .66f;
                if (!publicType.HasValue)
                {
                    Line(mesh, P(center, scale, 0, .23f), P(center, scale, .18f, 0), 2, tint);
                    Line(mesh, P(center, scale, .18f, 0), P(center, scale, 0, -.23f), 2, tint);
                    Line(mesh, P(center, scale, 0, -.23f), P(center, scale, -.18f, 0), 2, tint);
                    Line(mesh, P(center, scale, -.18f, 0), P(center, scale, 0, .23f), 2, tint);
                }
                else Symbol(mesh, center, scale, publicType.Value, tint);
            }
            if (completed)
            {
                var badge = center + new Vector2(radius * .72f, -radius * .65f);
                Disc(mesh, badge, 9, surface);
                Line(mesh, badge + new Vector2(-5, 0), badge + new Vector2(-1, -4), 2.2f, tint);
                Line(mesh, badge + new Vector2(-1, -4), badge + new Vector2(6, 4), 2.2f, tint);
            }
            else if (unreachable)
            {
                var badge = center + new Vector2(radius * .72f, -radius * .65f);
                Disc(mesh, badge, 9, surface);
                Box(mesh, badge + new Vector2(-5, -5), badge + new Vector2(5, 2), tint);
                Ring(mesh, badge + new Vector2(0, 3), 4, 1.5f, tint, false);
            }
        }

        void Symbol(VertexHelper mesh, Vector2 c, float s, NodeType type, Color tint)
        {
            switch (type)
            {
                case NodeType.Combat:
                    Stroke(mesh, c, s, -.58f, -.62f, .54f, .62f, 3, tint);
                    Stroke(mesh, c, s, .58f, -.62f, -.54f, .62f, 3, tint);
                    Stroke(mesh, c, s, -.58f, -.16f, -.13f, -.57f, 2.5f, tint);
                    Stroke(mesh, c, s, .58f, -.16f, .13f, -.57f, 2.5f, tint);
                    Triangle(mesh, P(c,s,.43f,.45f), P(c,s,.70f,.79f), P(c,s,.46f,.71f), tint);
                    Triangle(mesh, P(c,s,-.43f,.45f), P(c,s,-.70f,.79f), P(c,s,-.46f,.71f), tint);
                    break;
                case NodeType.Event:
                    Stroke(mesh,c,s,-.34f,.37f,-.21f,.59f,3,tint);
                    Stroke(mesh,c,s,-.21f,.59f,.22f,.59f,3,tint);
                    Stroke(mesh,c,s,.22f,.59f,.38f,.35f,3,tint);
                    Stroke(mesh,c,s,.38f,.35f,.03f,.02f,3,tint);
                    Stroke(mesh,c,s,.03f,.02f,.03f,-.20f,3,tint);
                    Disc(mesh,P(c,s,.03f,-.51f),2.5f,tint);
                    break;
                case NodeType.Treasure:
                    Box(mesh,P(c,s,-.63f,-.46f),P(c,s,.63f,.19f),tint);
                    Box(mesh,P(c,s,-.60f,.29f),P(c,s,.60f,.57f),tint);
                    Box(mesh,P(c,s,-.11f,-.05f),P(c,s,.11f,.36f),surface);
                    break;
                case NodeType.Shop:
                    Triangle(mesh,P(c,s,-.75f,.16f),P(c,s,0,.70f),P(c,s,.75f,.16f),tint);
                    Stroke(mesh,c,s,-.53f,.12f,-.53f,-.58f,3,tint);
                    Stroke(mesh,c,s,.53f,.12f,.53f,-.58f,3,tint);
                    Stroke(mesh,c,s,-.65f,-.58f,.65f,-.58f,3,tint);
                    Box(mesh,P(c,s,-.18f,-.58f),P(c,s,.18f,-.12f),tint);
                    break;
                case NodeType.Rest:
                    Triangle(mesh,P(c,s,-.46f,-.13f),P(c,s,-.10f,.75f),P(c,s,.16f,-.35f),tint);
                    Triangle(mesh,P(c,s,-.04f,-.33f),P(c,s,.31f,.41f),P(c,s,.50f,-.13f),tint);
                    Stroke(mesh,c,s,-.63f,-.48f,.58f,-.70f,3,tint);
                    Stroke(mesh,c,s,-.58f,-.70f,.63f,-.48f,3,tint);
                    break;
                case NodeType.Boss:
                    Triangle(mesh,P(c,s,-.67f,.54f),P(c,s,-.48f,-.40f),P(c,s,0,-.40f),tint);
                    Triangle(mesh,P(c,s,-.48f,-.40f),P(c,s,0,.79f),P(c,s,.48f,-.40f),tint);
                    Triangle(mesh,P(c,s,0,-.40f),P(c,s,.48f,-.40f),P(c,s,.67f,.54f),tint);
                    Stroke(mesh,c,s,-.49f,-.58f,.49f,-.58f,3,tint);
                    break;
            }
        }

        static Vector2 P(Vector2 center,float scale,float x,float y) => center + new Vector2(x,y)*scale;
        static Color Alpha(Color value,float alpha) { value.a = alpha; return value; }
        void Stroke(VertexHelper mesh,Vector2 c,float s,float x1,float y1,float x2,float y2,float width,Color tint)
            => Line(mesh,P(c,s,x1,y1),P(c,s,x2,y2),width,tint);
        void Line(VertexHelper mesh,Vector2 from,Vector2 to,float width,Color tint)
        {
            Vector2 normal = new Vector2(-(to-from).y,(to-from).x).normalized*width*.5f;
            Quad(mesh,from-normal,from+normal,to+normal,to-normal,tint);
        }
        void Box(VertexHelper mesh,Vector2 min,Vector2 max,Color tint)
            => Quad(mesh,min,new Vector2(min.x,max.y),max,new Vector2(max.x,min.y),tint);
        void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {
            int start=mesh.currentVertCount;
            Vertex(mesh,a,tint);Vertex(mesh,b,tint);Vertex(mesh,c,tint);Vertex(mesh,d,tint);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
        void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {
            int start=mesh.currentVertCount;Vertex(mesh,a,tint);Vertex(mesh,b,tint);Vertex(mesh,c,tint);
            mesh.AddTriangle(start,start+1,start+2);
        }
        void Disc(VertexHelper mesh,Vector2 c,float radius,Color tint)
        {
            const int count=48;
            for(int i=0;i<count;i++) Triangle(mesh,c,c+Unit(i,count)*radius,c+Unit(i+1,count)*radius,tint);
        }
        void Ring(VertexHelper mesh,Vector2 c,float radius,float width,Color tint,bool dashed)
        {
            const int count=64;
            for(int i=0;i<count;i++)
            {
                if(dashed&&i%4>=2)continue;
                var a=Unit(i,count);var b=Unit(i+1,count);
                Quad(mesh,c+a*(radius-width),c+a*radius,c+b*radius,c+b*(radius-width),tint);
            }
        }
        static Vector2 Unit(int index,int count)
        {
            float angle=index*Mathf.PI*2/count;return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        }
        void Vertex(VertexHelper mesh,Vector2 point,Color tint) => mesh.AddVert(point,tint*color,Vector2.zero);
    }
}
