using System;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SymbolLoop
{
    public enum LoopGlyph { Sword, Staff, Fire, Shield, Pawn, Enemy, Gem, Camp, Shop, Star }

    // Resolution-independent artwork, shared by board pieces, effects and physical die faces.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LoopGlyphGraphic : MaskableGraphic
    {
        public LoopGlyph glyph;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=GetPixelAdjustedRect(); float scale=Mathf.Min(r.width,r.height)*.46f;
            Draw(vh,p=>r.center+p*scale,glyph,color);
        }
        public static Color Tint(LoopGlyph value)
        {
            switch(value)
            {
                case LoopGlyph.Sword:return new Color(.56f,.88f,1);
                case LoopGlyph.Staff:return new Color(.75f,.57f,1);
                case LoopGlyph.Fire:return new Color(1,.47f,.22f);
                case LoopGlyph.Shield:return new Color(.28f,.93f,.75f);
                default:return new Color(1,.79f,.35f);
            }
        }
        public static void Draw(VertexHelper vh,Func<Vector2,Vector2> map,LoopGlyph glyph,Color c)
        {
            void Poly(params Vector2[] points) => LoopMesh.Polygon(vh,map,c,points);
            void Line(float x,float y,float ex,float ey,float w) => LoopMesh.Line(vh,map,c,new Vector2(x,y),new Vector2(ex,ey),w);
            void Dot(float x,float y,float radius) => LoopMesh.Disc(vh,map,c,new Vector2(x,y),radius);
            switch(glyph)
            {
                case LoopGlyph.Sword:
                    Poly(new Vector2(-.13f,-.35f),new Vector2(-.13f,.52f),new Vector2(0,.94f),new Vector2(.13f,.52f),new Vector2(.13f,-.35f));
                    Line(-.43f,-.33f,.43f,-.33f,.12f); Line(0,-.35f,0,-.75f,.15f); Dot(0,-.82f,.12f); break;
                case LoopGlyph.Staff:
                    Line(-.28f,-.88f,.19f,.34f,.14f); Poly(new Vector2(.22f,.98f),new Vector2(.63f,.56f),new Vector2(.22f,.18f),new Vector2(-.2f,.56f));
                    Line(-.56f,.56f,-.36f,.56f,.07f); Line(.7f,.1f,.87f,.1f,.07f); break;
                case LoopGlyph.Fire:
                    Poly(new Vector2(0,-.87f),new Vector2(-.57f,-.55f),new Vector2(-.66f,-.05f),new Vector2(-.31f,.57f),new Vector2(-.25f,.08f),new Vector2(.16f,.98f),new Vector2(.51f,.37f),new Vector2(.35f,.05f),new Vector2(.65f,-.22f),new Vector2(.5f,-.67f)); break;
                case LoopGlyph.Shield:
                    Poly(new Vector2(-.67f,.68f),new Vector2(0,.94f),new Vector2(.67f,.68f),new Vector2(.57f,-.35f),new Vector2(0,-.93f),new Vector2(-.57f,-.35f));
                    LoopMesh.Line(vh,map,new Color(.04f,.16f,.2f,c.a),new Vector2(0,-.44f),new Vector2(0,.54f),.12f); break;
                case LoopGlyph.Pawn:
                    Dot(0,.48f,.36f); Poly(new Vector2(-.18f,.2f),new Vector2(.18f,.2f),new Vector2(.43f,-.64f),new Vector2(-.43f,-.64f));
                    Line(-.62f,-.78f,.62f,-.78f,.22f); break;
                case LoopGlyph.Enemy:
                    Poly(new Vector2(-.7f,.35f),new Vector2(-.8f,.91f),new Vector2(-.32f,.61f),new Vector2(.32f,.61f),new Vector2(.8f,.91f),new Vector2(.7f,.35f),new Vector2(.64f,-.5f),new Vector2(0,-.86f),new Vector2(-.64f,-.5f));
                    LoopMesh.Line(vh,map,new Color(.08f,.06f,.13f,c.a),new Vector2(-.45f,.18f),new Vector2(-.14f,.08f),.13f);
                    LoopMesh.Line(vh,map,new Color(.08f,.06f,.13f,c.a),new Vector2(.45f,.18f),new Vector2(.14f,.08f),.13f); break;
                case LoopGlyph.Camp:Poly(new Vector2(0,.8f),new Vector2(.84f,-.7f),new Vector2(-.84f,-.7f)); break;
                case LoopGlyph.Shop:Line(-.7f,.5f,.7f,.5f,.3f); Line(-.5f,.25f,-.5f,-.65f,.18f); Line(.5f,.25f,.5f,-.65f,.18f); Line(-.65f,-.7f,.65f,-.7f,.18f); break;
                default:Poly(new Vector2(0,.9f),new Vector2(.65f,0),new Vector2(0,-.9f),new Vector2(-.65f,0)); break;
            }
        }
    }
    internal static class LoopMesh
    {
        public static void Polygon(VertexHelper vh,Func<Vector2,Vector2> map,Color c,params Vector2[] points)
        {
            int start=vh.currentVertCount; foreach(var p in points) vh.AddVert(map(p),c,Vector2.zero);
            for(int i=1;i<points.Length-1;i++) vh.AddTriangle(start,start+i,start+i+1);
        }
        public static void Line(VertexHelper vh,Func<Vector2,Vector2> map,Color c,Vector2 a,Vector2 b,float width)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            Polygon(vh,map,c,a+n,b+n,b-n,a-n);
        }
        public static void Disc(VertexHelper vh,Func<Vector2,Vector2> map,Color c,Vector2 center,float radius)
        {
            const int count=16; int start=vh.currentVertCount; vh.AddVert(map(center),c,Vector2.zero);
            for(int i=0;i<count;i++) { float a=i*Mathf.PI*2/count; vh.AddVert(map(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius),c,Vector2.zero); }
            for(int i=0;i<count;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }
}
