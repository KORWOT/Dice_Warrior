using System;
using UnityEngine;
using UnityEngine.UI;

namespace DiceWarrior.SharedWorld
{
    // The existing LoopDieGraphic Canvas projection adapted to physical v5 face indices.
    // No physics, RNG, camera or runtime scene object determines the result.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V5DieGraphic : MaskableGraphic
    {
        static readonly Vector3[] Normals={Vector3.back,Vector3.right,Vector3.up,Vector3.left,Vector3.down,Vector3.forward};
        static readonly Quaternion Tilt=Quaternion.Euler(16,-22,-7);
        V5FaceDefinition[] definitions;
        Quaternion rotation=Tilt,settled=Tilt;
        bool reveal,held;
        float pulse,hop;
        public int ResultFaceIndex {get;private set;}
        public float RollProgress {get;private set;}=1;
        public bool RevealResult {get=>reveal;set{reveal=value;SetVerticesDirty();}}
        public bool Held {get=>held;set{held=value;SetVerticesDirty();}}
        public float Pulse {get=>pulse;set{pulse=Mathf.Clamp01(value);SetVerticesDirty();}}
        public int FrontFaceIndex
        {
            get{int front=0;for(int i=1;i<6;i++)if((rotation*Normals[i]).z<(rotation*Normals[front]).z)front=i;return front;}
        }
        public string FaceId(int index)
        {if(definitions==null || index<0 || index>=6)throw new ArgumentOutOfRangeException(nameof(index));return definitions[index].id;}
        public void Configure(V5FaceDefinition[] faces,int resultFaceIndex)
        {
            if(faces==null || faces.Length!=6 || Array.Exists(faces,f=>f==null) || resultFaceIndex<0 || resultFaceIndex>=6)
                throw new ArgumentException("A v5 cube requires six physical faces and their result index.");
            definitions=(V5FaceDefinition[])faces.Clone();ResultFaceIndex=resultFaceIndex;
            settled=Tilt*Quaternion.FromToRotation(Normals[resultFaceIndex],Vector3.back);RevealResult=true;Pulse=0;Pose(1,0);
        }
        public void Pose(float progress,int slot)
        {
            RollProgress=Mathf.Clamp01(progress);float remaining=Mathf.Pow(1-RollProgress,2);
            rotation=Quaternion.Euler(remaining*(720+slot*90),remaining*(900+slot*120),remaining*(270-slot*50))*settled;
            hop=12*Mathf.Sin(RollProgress*Mathf.PI)+3*Mathf.Pow(Mathf.Sin(RollProgress*Mathf.PI*3),2)*(1-RollProgress);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(definitions==null)return;var rect=GetPixelAdjustedRect();float unit=Mathf.Min(rect.width,rect.height);
            Vector2 shadow=rect.center+new Vector2(0,-unit*.4f);
            Ellipse(vh,shadow,unit*.35f,unit*.075f,new Color(0,0,0,.32f));
            if(Held || Pulse>0)
            {
                var tint=Held?new Color(.32f,.84f,.8f,.7f):new Color(.95f,.74f,.36f,.65f*Pulse);
                Ring(vh,shadow,unit*.4f,unit*.07f,3+Pulse*2,tint);
            }
            float scale=unit*.23f;Vector2 center=rect.center+new Vector2(0,8+hop);
            for(int f=0;f<6;f++)
            {
                var normal=Normals[f];var turned=rotation*normal;if(turned.z>=-.001f)continue;
                Vector3 up=Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up;Vector3 right=Vector3.Cross(normal,up).normalized;
                Vector2 Project(Vector2 p){Vector3 point=rotation*(normal+right*p.x+up*p.y);float perspective=5/(5+point.z);return center+new Vector2(point.x,point.y)*scale*perspective;}
                float shade=.65f+.35f*(-turned.z);var body=new Color(.96f*shade,.92f*shade,.82f*shade,1);
                Polygon(vh,Project,new Color(.28f,.24f,.19f),new Vector2(-1,-.86f),new Vector2(-1,.86f),new Vector2(-.86f,1),new Vector2(.86f,1),new Vector2(1,.86f),new Vector2(1,-.86f),new Vector2(.86f,-1),new Vector2(-.86f,-1));
                Polygon(vh,p=>Project(p*.96f),body,new Vector2(-1,-.86f),new Vector2(-1,.86f),new Vector2(-.86f,1),new Vector2(.86f,1),new Vector2(1,.86f),new Vector2(1,-.86f),new Vector2(.86f,-1),new Vector2(-.86f,-1));
                // A pending cube is blank; the first tumble frame never presents a readable final face.
                if(RevealResult || (RollProgress>.04f && RollProgress<1))V5FaceGraphic.Draw(vh,p=>Project(p*.68f),definitions[f],false,true);
            }
            if(Held)Lock(vh,shadow+new Vector2(unit*.3f,3));
        }
        static void Lock(VertexHelper vh,Vector2 at)
        {
            var tint=new Color(.32f,.84f,.8f);Vector2 Map(Vector2 p)=>at+p;
            Polygon(vh,Map,tint,new Vector2(-7,-9),new Vector2(-7,3),new Vector2(7,3),new Vector2(7,-9));
            for(int i=0;i<8;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Line(vh,at+new Vector2(Mathf.Cos(a)*5,3+Mathf.Sin(a)*7),at+new Vector2(Mathf.Cos(b)*5,3+Mathf.Sin(b)*7),2.5f,tint);}
            Line(vh,at+new Vector2(0,-1),at+new Vector2(0,-5),2,new Color(.05f,.16f,.2f));
        }
        static void Ellipse(VertexHelper vh,Vector2 center,float x,float y,Color tint)
        {var points=new Vector2[32];for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;points[i]=center+new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y);}Polygon(vh,p=>p,tint,points);}
        static void Ring(VertexHelper vh,Vector2 center,float x,float y,float width,Color tint)
        {for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;Line(vh,center+new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y),center+new Vector2(Mathf.Cos(b)*x,Mathf.Sin(b)*y),width,tint);}}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Polygon(vh,p=>p,tint,a+n,b+n,b-n,a-n);}
        static void Polygon(VertexHelper vh,Func<Vector2,Vector2> map,Color tint,params Vector2[] points)
        {int start=vh.currentVertCount;foreach(var p in points)vh.AddVert(map(p),tint,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
    }
}
