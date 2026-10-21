using System;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SymbolLoop
{
    // A rotating cube projected into a Canvas. Its six faces are the configured die, never a second RNG.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LoopDieGraphic : MaskableGraphic
    {
        static readonly Vector3[] Normals={Vector3.back,Vector3.right,Vector3.up,Vector3.left,Vector3.down,Vector3.forward};
        static readonly Quaternion Tilt=Quaternion.Euler(16,-22,-7);
        public bool symbols;
        public int Result { get; private set; }=1;
        public float RollProgress { get; private set; }=1;
        int[] values={1,2,3,4,5,6};
        Quaternion rotation= Tilt, settled=Tilt;
        public void Configure(int result,Symbol[] faces=null)
        {
            symbols=faces!=null; values=faces==null ? new[]{1,2,3,4,5,6} : Array.ConvertAll(faces,x=>(int)x);
            Result=result; int face=Array.IndexOf(values,result);
            if(face<0 || values.Length!=6) throw new ArgumentException("The visible result must belong to a six-sided die.");
            settled=Tilt*Quaternion.FromToRotation(Normals[face],Vector3.back); Pose(1,0);
        }
        public void Pose(float progress,int slot)
        {
            RollProgress=Mathf.Clamp01(progress); float remaining=Mathf.Pow(1-RollProgress,2);
            rotation=Quaternion.Euler(remaining*(720+slot*90),remaining*(900+slot*120),remaining*(270-slot*50))*settled;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r=GetPixelAdjustedRect(); float scale=Mathf.Min(r.width,r.height)*.31f;
            for(int f=0;f<6;f++)
            {
                Vector3 normal=Normals[f]; Vector3 turned=rotation*normal;
                if(turned.z>=-.001f) continue;
                Vector3 up=Mathf.Abs(normal.y)>.5f ? Vector3.forward : Vector3.up;
                Vector3 right=Vector3.Cross(normal,up).normalized;
                // Choose a face-local coordinate basis; every symbol remains attached as the cube tumbles.
                Vector2 Project(Vector2 p)
                {
                    Vector3 point=rotation*(normal+right*p.x+up*p.y);
                    float perspective=5f/(5f+point.z);
                    return r.center+new Vector2(point.x,point.y)*scale*perspective;
                }
                float shade=.60f+.40f*(-turned.z);
                Color body=symbols ? new Color(.14f,.24f,.33f) : new Color(.94f,.91f,.79f);
                body*=shade; body.a=1;
                LoopMesh.Polygon(vh,Project,new Color(.02f,.04f,.08f),new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1));
                LoopMesh.Polygon(vh,Project,body,new Vector2(-.95f,-.95f),new Vector2(-.95f,.95f),new Vector2(.95f,.95f),new Vector2(.95f,-.95f));
                Vector2 Icon(Vector2 p)=>Project(p*.66f);
                if(symbols) LoopGlyphGraphic.Draw(vh,Icon,(LoopGlyph)values[f],LoopGlyphGraphic.Tint((LoopGlyph)values[f]));
                else
                {
                    int n=values[f]; Color ink=new Color(.1f,.18f,.22f);
                    void Pip(float x,float y)=>LoopMesh.Disc(vh,Project,ink,new Vector2(x,y),.13f);
                    if((n&1)==1) Pip(0,0);
                    if(n>=2) { Pip(-.5f,.5f); Pip(.5f,-.5f); }
                    if(n>=4) { Pip(.5f,.5f); Pip(-.5f,-.5f); }
                    if(n==6) { Pip(-.5f,0); Pip(.5f,0); }
                }
            }
        }
    }
}
