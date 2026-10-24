using System;
using DiceWarrior.SymbolLoop;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld
{
    // Authored vector graphic: face identity is independent of skill and damage type.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V5FaceGraphic : MaskableGraphic
    {
        public V5FaceKind kind;
        public string iconId;
        public bool current;
        public void Configure(V5FaceDefinition face,bool highlighted=false)
        { kind=face.kind;iconId=face.iconId;current=highlighted;SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=GetPixelAdjustedRect();float scale=Mathf.Min(rect.width,rect.height)*.47f;
            DrawFace(vh,p=>rect.center+p*scale,kind,iconId,current,false);
        }
        public static void Draw(VertexHelper vh,Func<Vector2,Vector2> map,V5FaceDefinition face,bool highlighted=false,bool onLightSurface=false)
        {
            if(face==null)throw new ArgumentNullException(nameof(face));
            DrawFace(vh,map,face.kind,face.iconId,highlighted,onLightSurface);
        }
        static void DrawFace(VertexHelper vh,Func<Vector2,Vector2> Map,V5FaceKind kind,string iconId,bool current,bool onLightSurface)
        {
            void Line(Vector2 a,Vector2 b,float width,Color tint)
            {
                var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int start=vh.currentVertCount;
                foreach(var p in new[]{a+n,b+n,b-n,a-n}) vh.AddVert(Map(p),tint,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
            Color tint=kind==V5FaceKind.Blank?new Color(.62f,.66f,.7f):kind==V5FaceKind.Class?new Color(1,.78f,.38f):new Color(.55f,.86f,1);
            if(onLightSurface)tint=kind==V5FaceKind.Blank?new Color(.3f,.34f,.4f):kind==V5FaceKind.Class?new Color(.53f,.29f,.08f):new Color(.1f,.25f,.34f);
            Color frame=current?Color.white:tint;
            var points=kind==V5FaceKind.Class?new[]{new Vector2(-.5f,1),new Vector2(.5f,1),new Vector2(1,0),new Vector2(.5f,-1),new Vector2(-.5f,-1),new Vector2(-1,0)}:
                kind==V5FaceKind.Blank?Circle():new[]{new Vector2(-.8f,1),new Vector2(.8f,1),new Vector2(1,.8f),new Vector2(1,-.8f),new Vector2(.8f,-1),new Vector2(-.8f,-1),new Vector2(-1,-.8f),new Vector2(-1,.8f)};
            for(int i=0;i<points.Length;i++) Line(points[i],points[(i+1)%points.Length],current ? .075f : .04f,frame);
            Func<Vector2,Vector2> inset=p=>Map(p*.67f);
            if(kind==V5FaceKind.Blank)
            { Line(new Vector2(-.37f,-.37f),new Vector2(.37f,.37f),.11f,tint);Line(new Vector2(-.37f,.37f),new Vector2(.37f,-.37f),.11f,tint);return; }
            if(iconId=="swordsman")
            {
                LoopGlyphGraphic.Draw(vh,p=>inset(new Vector2(p.x*.7f-p.y*.5f,p.x*.5f+p.y*.7f)),LoopGlyph.Sword,tint);
                LoopGlyphGraphic.Draw(vh,p=>inset(new Vector2(p.x*.7f+p.y*.5f,-p.x*.5f+p.y*.7f)),LoopGlyph.Sword,tint);return;
            }
            if(iconId=="runecaster")
            {
                var circle=Circle();for(int i=0;i<circle.Length;i++) Line(circle[i]*.55f,circle[(i+1)%circle.Length]*.55f,.06f,tint);
                Line(new Vector2(0,.63f),new Vector2(0,-.63f),.07f,tint);Line(new Vector2(-.34f,0),new Vector2(.34f,0),.07f,tint);return;
            }
            var glyph=iconId=="sword"?LoopGlyph.Sword:iconId=="staff"?LoopGlyph.Staff:iconId=="fire"?LoopGlyph.Fire:LoopGlyph.Shield;
            LoopGlyphGraphic.Draw(vh,inset,glyph,tint);
            if(iconId=="guardian") for(int i=-1;i<=1;i++) Line(new Vector2(i*.26f,-.28f),new Vector2(i*.26f,.27f),.07f,new Color(.06f,.13f,.17f));
        }
        static Vector2[] Circle()
        { var result=new Vector2[24];for(int i=0;i<result.Length;i++) {float angle=i*Mathf.PI*2/result.Length;result[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));}return result; }
    }
}
