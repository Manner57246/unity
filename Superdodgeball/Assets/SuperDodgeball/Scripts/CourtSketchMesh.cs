using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SuperDodgeball
{
    // Low-poly solids with a shared vertex palette. Static set dressing is batched per venue.
    internal sealed class CourtSketchMesh
    {
        readonly List<Vector3> vertices = new(), normals = new();
        readonly List<Color32> colours = new();
        readonly List<int> indices = new();
        readonly Stack<Matrix4x4> transforms = new();
        Matrix4x4 basis = Matrix4x4.identity;

        public void Push(Vector3 position, float yaw = 0) => Push(position, Quaternion.Euler(0,yaw,0));
        public void Push(Vector3 position, Quaternion rotation)
        {
            transforms.Push(basis);
            basis *= Matrix4x4.TRS(position, rotation, Vector3.one);
        }
        public void Pop() => basis = transforms.Pop();
        static Color32 Paint(int rgb) => new((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,255);
        public void Face(int colour, params Vector3[] corners)
        {
            var cross=Vector3.Cross(corners[1]-corners[0],corners[2]-corners[0]);
            float area=cross.magnitude;
            if(area<1e-14f)return;
            // Vector3.normalized rounds tiny bevel faces down to zero, which breaks specular lighting.
            var n=basis.MultiplyVector(cross/area);
            int start = vertices.Count;
            foreach (var p in corners)
            {
                vertices.Add(basis.MultiplyPoint3x4(p)); normals.Add(n); colours.Add(Paint(colour));
            }
            for (int i=1; i<corners.Length-1; i++) { indices.Add(start); indices.Add(start+i); indices.Add(start+i+1); }
        }
        public void Ground(int colour, float y, params Vector2[] boundary)
        {
            var points = new Vector3[boundary.Length];
            for (int i=0;i<points.Length;i++) points[i]=new(boundary[i].x,y,boundary[i].y);
            if (Vector3.Cross(points[1]-points[0],points[2]-points[0]).y<0) System.Array.Reverse(points);
            Face(colour,points);
        }
        static Vector3[] Rim(float x, float z, float bevel)
        {
            float b = Mathf.Min(x,z)*bevel;
            return new Vector3[] {new(x,0,-z+b),new(x,0,z-b),new(x-b,0,z),new(-x+b,0,z),
                new(-x,0,z-b),new(-x,0,-z+b),new(-x+b,0,-z),new(x-b,0,-z)};
        }
        static Vector3[] Oval(int sides,float rx,float rz,float seed)
        {
            var rim = new Vector3[sides];
            for(int i=0;i<sides;i++)
            {
                float a=2*Mathf.PI*i/sides, r=1+Mathf.Sin(i*37.3f+seed)*Mathf.Min(seed,.11f);
                rim[i]=new(Mathf.Cos(a)*rx*r,0,Mathf.Sin(a)*rz*r);
            }
            return rim;
        }
        void Profile(int colour,Vector3 centre,Vector3[] rim,params Vector2[] profile)
        {
            int count=rim.Length;
            Vector3 Point(int j,int i)=>centre+rim[i]*profile[j].x+Vector3.up*profile[j].y;
            for (int j=0;j<profile.Length-1;j++)
                for(int i=0;i<count;i++)
                {
                    int k=(i+1)%count;
                    Face(colour,Point(j,i),Point(j+1,i),Point(j+1,k),Point(j,k));
                }
            int top=profile.Length-1;
            for(int i=0;i<count;i++)
            {
                int k=(i+1)%count;
                Face(colour,centre+Vector3.up*profile[top].y,Point(top,k),Point(top,i));
                Face(colour,centre+Vector3.up*profile[0].y,Point(0,i),Point(0,k));
            }
        }
        public void Box(int colour,Vector3 pos,Vector3 size,float bevel=.15f)
        {
            float h=size.y*.5f, edge=Mathf.Min(h*.35f,Mathf.Min(size.x,size.z)*.10f);
            Profile(colour,pos,Rim(size.x*.5f,size.z*.5f,Mathf.Max(.01f,bevel)),
                new(.90f,-h),new(1,-h+edge),new(1,h-edge),new(.91f,h));
        }
        public void Drum(int colour,Vector3 pos,Vector3 size,int sides=12)
        {
            float h=size.y*.5f;
            Profile(colour,pos,Oval(sides,size.x*.5f,size.z*.5f,0),new(.92f,-h),new(1,-h*.8f),new(1,h*.76f),new(.91f,h));
        }
        public void Plum(int colour,Vector3 pos,Vector3 size,int sides=9,float seed=0)
        {
            float h=size.y*.5f;
            Profile(colour,pos,Oval(sides,size.x*.5f,size.z*.5f,seed),
                new(.29f,-h),new(.82f,-h*.65f),new(1,-h*.08f),new(.9f,h*.52f),new(.5f,h*.91f),new(.2f,h));
        }
        public void Stick(int colour,Vector3 a,Vector3 b,float width,float depth=0)
        {
            Push((a+b)*.5f,Quaternion.FromToRotation(Vector3.up,(b-a).normalized));
            Box(colour,Vector3.zero,new(width,(b-a).magnitude,depth>0?depth:width),.18f); Pop();
        }
        public void Ring(int colour,Vector3 pos,float rx,float rz,float thickness,int count=48)
        {
            for(int i=0;i<count;i++)
            {
                float a=i*2*Mathf.PI/count,b=(i+1)*2*Mathf.PI/count;
                Vector3 P(float angle,float inset)=>pos+new Vector3((rx-inset)*Mathf.Cos(angle),0,(rz-inset)*Mathf.Sin(angle));
                Face(colour,P(a,0),P(a,thickness),P(b,thickness),P(b,0));
            }
        }
        GameObject MakeObject(string name,Transform parent,out Mesh mesh)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(parent,false);
            mesh=new Mesh {name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colours);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            return go;
        }
        public GameObject Bake(string name,Transform parent,Material mat)
        {
            var go=MakeObject(name,parent,out var mesh);
            mesh.SetTriangles(indices,0);
            go.GetComponent<MeshRenderer>().sharedMaterial=mat;
            return go;
        }

        // The indoor scene uses lit wood, fabric, metal and glass. Keep one submesh per finish.
        public Mesh BakeSurfaces(string name,Transform parent,System.Func<int,Material> getMaterial)
        {
            var go=MakeObject(name,parent,out var mesh);
            var surfaces=new Dictionary<int,List<int>>();
            for(int i=0;i<indices.Count;i+=3)
            {
                Color32 c=colours[indices[i]];
                int rgb=c.r<<16 | c.g<<8 | c.b;
                if(!surfaces.TryGetValue(rgb,out var group)) surfaces.Add(rgb,group=new List<int>());
                group.Add(indices[i]); group.Add(indices[i+1]); group.Add(indices[i+2]);
            }
            var materials=new List<Material>();
            mesh.subMeshCount=surfaces.Count;
            foreach(var group in surfaces)
            {
                mesh.SetTriangles(group.Value,materials.Count,false);
                materials.Add(getMaterial(group.Key));
            }
            mesh.RecalculateBounds();
            go.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
            return mesh;
        }
    }
}
