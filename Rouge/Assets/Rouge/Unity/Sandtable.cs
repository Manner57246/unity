using System;
using System.Collections.Generic;
using Rouge.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace Rouge.Unity
{
    // One orthographic survey of the same mesh supplies the overview, local crop and locator.
    internal sealed class Sandtable : IDisposable
    {
        internal static readonly Vector2[] Sites = { new(.30f,.62f), new(.70f,.64f), new(.26f,.32f), new(.74f,.34f), new(.50f,.23f), new(.50f,.52f), new(.50f,.81f) };
        internal static readonly Vector2 Camp = new(.40f,.66f);
        internal static Rect Bounds(int region) => new Rect(Sites[region] - new Vector2(.097f,.142f),new Vector2(.194f,.284f));
        private const float Span = 100, Depth = 68, Pitch = 58;
        private readonly WorldAtlas atlas;
        private readonly Random rand;
        private readonly List<Vector3> vertices = new();
        private readonly List<Color32> colors = new();
        private readonly List<int> indices = new();
        private readonly GameObject root;
        private readonly Mesh mesh;
        private readonly Material paint;
        internal Texture2D Picture { get; }
        private static readonly Vector3 Light = new Vector3(-.52f,.84f,.34f).normalized;
        private static readonly Color Grass = Rgb(0x8e9b67), Moss = Rgb(0x607a58), Rock = Rgb(0x968a76);
        private static readonly Color Stone = Rgb(0xb0a488), Roof = Rgb(0x384d63), Timber = Rgb(0x715141);
        private static Color Rgb(int hex) => new Color32((byte)(hex>>16),(byte)(hex>>8),(byte)hex,255);
        private float Range(float lo,float hi) => Mathf.Lerp(lo,hi,(float)rand.NextDouble());
        private static float Noise(float x,float z,float frequency) => Mathf.PerlinNoise(x*frequency+31.1f,z*frequency+17.4f);
        private static float Basin(Vector2 uv,int region,float noise)
        {
            var p = (uv-Sites[region])/Bounds(region).size;
            return Mathf.SmoothStep(0,1,(.53f-p.magnitude+(noise-.5f)*.12f)*8);
        }

        internal Sandtable(WorldAtlas atlas, int seed)
        {
            this.atlas = atlas;
            rand = new Random(seed);
            Ground();
            Dress();
            mesh = new Mesh { name="Borderlands miniature", indexFormat=IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices,0); mesh.RecalculateBounds();
            vertices.Clear(); colors.Clear(); indices.Clear();
            vertices.TrimExcess(); colors.TrimExcess(); indices.TrimExcess();
            root = new GameObject("Continental sandtable");
            root.transform.position = Vector3.right*6000;
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            paint = new Material(Resources.Load<Shader>("Sandtable"));
            root.AddComponent<MeshRenderer>().sharedMaterial = paint;
            Picture = Photograph();
        }

        private Texture2D Photograph()
        {
            var rig = new GameObject("Survey camera");
            var cam = rig.AddComponent<Camera>();
            cam.enabled=false;
            cam.orthographic=true;
            cam.transform.rotation=Quaternion.Euler(Pitch,0,0);
            cam.transform.position=root.transform.position+new Vector3(Span/2,0,Depth/2)-cam.transform.forward*130;
            cam.orthographicSize=Depth*.5f*Mathf.Sin(Pitch*Mathf.Deg2Rad);
            cam.aspect=Span/(2*cam.orthographicSize);
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=Rgb(0x244957); cam.farClipPlane=200; cam.allowHDR=false;
            int width=Mathf.Min(3584,SystemInfo.maxTextureSize), height=Mathf.RoundToInt(width/cam.aspect);
            var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            try
            {
                cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
                var photo = new Texture2D(width,height,TextureFormat.RGB24,true){name="Survey of the Borderlands",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
                photo.ReadPixels(new Rect(0,0,width,height),0,0,false); photo.Apply(true,true);
                return photo;
            }
            finally { cam.targetTexture=null; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(rig); }
        }

        private static Vector2 World(float x,float z) => new Vector2(x/Span,1-z/Depth);
        private int Region(Vector2 uv)
        {
            for (int i=0;i<Sites.Length;i++) if(Bounds(i).Contains(uv))return i;
            return -1;
        }
        private TerrainKind Kind(Vector2 uv,int region)
        {
            if(region<0)return TerrainKind.Meadow;
            var b=Bounds(region); var grid=atlas.Regions[region].Terrain;
            int x=Mathf.Clamp((int)((uv.x-b.x)/b.width*grid.Width),0,grid.Width-1);
            int y=Mathf.Clamp((int)((1-(uv.y-b.y)/b.height)*TerrainGrid.Height),0,TerrainGrid.Height-1);
            return grid[grid.Cell(x,y)];
        }
        private static float Coast(float x,float z)
        {
            float u=(x-50)/43.5f,v=(z-34)/27.7f,theta=Mathf.Atan2(v,u);
            float mainland = 1-Mathf.Sqrt(u*u+v*v)+.065f*Mathf.Sin(5*theta+.7f)+.052f*Mathf.Cos(8*theta);
            float peninsula = .64f - new Vector2((x-50)/21,(z-14)/21).magnitude;
            return Mathf.Max(mainland,peninsula) + .095f*(Noise(x,z,.16f)-.5f);
        }
        private float Surface(float x,float z)
        {
            float coast=Coast(x,z);
            if(coast<0)return -.55f;
            var uv=World(x,z);int r=Region(uv);var k=Kind(uv,r);
            float n=Noise(x,z,.16f);
            float hill=.65f+1.55f*n+.40f*Noise(x,z,.50f);
            if(r==WorldAtlas.Mine)
            {
                float basin=Basin(uv,r,n);
                hill=Mathf.Lerp(hill,.48f+.38f*n,basin)+Mathf.Sin(basin*Mathf.PI)*1.65f;
            }
            if(r==1 || r==3 || r==4)
            {
                var b=Bounds(r); Vector2 p=(uv-b.min)/b.size;
                float edge=Mathf.Clamp01(7*Mathf.Min(Mathf.Min(p.x,1-p.x),Mathf.Min(p.y,1-p.y)));
                hill+=edge*(r==1?2.5f*(1-p.y)+.65f:r==3?.45f:1.1f);
            }
            if(k==TerrainKind.Cliff)hill=.35f+.18f*n;
            if(k==TerrainKind.Lake)hill=.27f;
            return Mathf.Lerp(-.34f,hill,Mathf.SmoothStep(0,1,coast/.065f));
        }
        internal Vector2 Project(Vector2 uv)
        {
            float h=Surface(uv.x*Span,(1-uv.y)*Depth);
            return uv-Vector2.up*(h/(Depth*Mathf.Tan(Pitch*Mathf.Deg2Rad)));
        }
        internal Vector2 Node(RouteMap map,int cell)
        {
            var b=Bounds(map.Region);int w=map.Terrain.Width;
            return Project(b.min+Vector2.Scale(b.size,new Vector2((cell%w+.5f)/w,1-(cell/w+.5f)/TerrainGrid.Height)));
        }
        private void Triangle(Vector3 a,Vector3 b,Vector3 c, Color color)
        {
            var normal=Vector3.Cross(b-a,c-a).normalized;
            float lit=.75f+.27f*Vector3.Dot(normal,Light);
            Color32 shade=new Color(color.r*lit,color.g*lit,color.b*lit,1);
            int first=vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            colors.Add(shade); colors.Add(shade); colors.Add(shade);
            indices.Add(first);indices.Add(first+1);indices.Add(first+2);
        }
        private Color Pigment(float x,float z)
        {
            float coast=Coast(x,z), n=Noise(x,z,.22f);
            if(coast<0)return Color.Lerp(Rgb(0x1f4858),Rgb(0x69aa9e),Mathf.Clamp01(1+coast*9)*.8f+n*.18f);
            if(coast<.035f)return Rgb(0xd6bf91);
            var uv=World(x,z);int r=Region(uv);var k=Kind(uv,r);
            if(r==WorldAtlas.Mine || r==WorldAtlas.Forge)
            {
                float basin=Basin(uv,r,n);
                Color floor = r==WorldAtlas.Mine ? Color.Lerp(Rgb(0x454355),Rgb(0x74645c),n) : Color.Lerp(Rgb(0x594247),Rgb(0x92735f),n);
                if(k==TerrainKind.Cliff) floor=Rgb(r==WorldAtlas.Mine ? 0x232637 : 0xd47b42);
                return Color.Lerp(Color.Lerp(Grass,Moss,n),floor,basin);
            }
            if(k==TerrainKind.Lake)return Color.Lerp(Rgb(0x366e7b),Rgb(0x76b0a0),n);
            if(k==TerrainKind.Cliff)return Rgb(r==1?0x494958:0x646065);
            Color baseColor=Color.Lerp(Grass,Moss,n);
            for(int i=0;i<Sites.Length;i++)
            {
                Vector2 dist=(uv-Sites[i])/Bounds(i).size;
                float influence=Mathf.Clamp01(1.3f-dist.magnitude*1.45f);
                Color tint=i==1?Rgb(0xb7a17d):i==3?Rgb(0x9c7668):i==4?Rgb(0xa6aa94):i==2?Rgb(0x587958):Rgb(0x759776);
                baseColor=Color.Lerp(baseColor,tint,influence);
            }
            return baseColor*(.88f+.21f*n);
        }
        private void Ground()
        {
            const int cols=236,rows=160;
            var points=new Vector3[(cols+1)*(rows+1)];
            for(int j=0;j<=rows;j++)for(int i=0;i<=cols;i++)
            {
                float x=Span*(i+Range(-.18f,.18f))/cols,z=Depth*(j+Range(-.18f,.18f))/rows;
                points[j*(cols+1)+i]=new Vector3(x,Surface(x,z),z);
            }
            for(int j=0;j<rows;j++)for(int i=0;i<cols;i++)
            {
                int k=j*(cols+1)+i;
                var a=points[k];var b=points[k+cols+1];var c=points[k+cols+2];var d=points[k+1];
                var color=Pigment((a.x+c.x)*.5f,(a.z+c.z)*.5f);
                if(Mathf.Max(Mathf.Max(a.y,b.y),Mathf.Max(c.y,d.y))-Mathf.Min(Mathf.Min(a.y,b.y),Mathf.Min(c.y,d.y))>.88f)color=Rock;
                Triangle(a,b,c,color); Triangle(a,c,d,color);
            }
        }

        // Rings model tree crowns, stone towers, walls and faceted rock spires.
        private void Frustum(Vector3 p,float bottom,float top,float h,int sides,Color color,float angle=0,float stretch=1)
        {
            Vector3 Cap(float a,float radius,float y) => p+new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius*stretch);
            for(int i=0;i<sides;i++)
            {
                float a=angle+2*Mathf.PI*i/sides,b=angle+2*Mathf.PI*(i+1)/sides;
                Vector3 l=Cap(a,bottom,0),r=Cap(b,bottom,0),u=Cap(a,top,h),v=Cap(b,top,h);
                Triangle(l,u,v,color); Triangle(l,v,r,color);
                if(top>.001f)Triangle(p+Vector3.up*h,v,u,color);
            }
        }
        private void Block(Vector3 p,float width,float depth,float height,Color color)
        {
            Frustum(p,width*.7071f,width*.7071f,height,4,color,Mathf.PI*.25f,depth/width);
        }
        private void Shadow(Vector3 p,float size)
        {
            Frustum(p+new Vector3(.12f,.035f,-.16f)*size,size,size,.02f,9,Rgb(0x596951),0,.64f);
        }
        private void Pine(Vector3 p,float s)
        {
            Shadow(p,.55f*s);
            Frustum(p,.13f*s,.085f*s,.65f*s,5,Timber);
            Color green=Color.Lerp(Rgb(0x2f665b),Rgb(0x547e64),Range(0,1));
            for(int k=0;k<3;k++)Frustum(p+Vector3.up*(.32f+.39f*k)*s,(.63f-k*.11f)*s,.025f*s,(.90f-.065f*k)*s,7,green*(.88f+.09f*k),k*.46f);
        }
        private void Tower(Vector3 p,float s,bool spire)
        {
            Frustum(p,.68f*s,.61f*s,1.8f*s,8,Stone);
            Frustum(p+Vector3.up*1.62f*s,.63f*s,.75f*s,.23f*s,8,Rgb(0xc5b499));
            Frustum(p+Vector3.up*1.85f*s,.76f*s,spire?.03f*s:.66f*s,spire?1.25f*s:.17f*s,8,spire?Roof:Stone);
            if(!spire)for(int j=0;j<8;j++){
                float a=j*Mathf.PI/4; Block(p+new Vector3(.63f*Mathf.Cos(a),1.98f,.63f*Mathf.Sin(a))*s,.22f*s,.22f*s,.26f*s,Stone);
            }
            Block(p+new Vector3(0,.75f,-.60f)*s,.17f*s,.085f*s,.54f*s,Rgb(0x403c42));
        }
        private void Hall(Vector3 p,float size,Color stone,Color roof)
        {
            Block(p,1.8f*size,1.3f*size,1.25f*size,stone);
            Frustum(p+Vector3.up*1.22f*size,1.55f*size,.33f*size,.95f*size,4,roof,Mathf.PI*.25f,.76f);
            Block(p+new Vector3(0,0,-.655f)*size,.48f*size,.05f*size,.78f*size,Rgb(0x333541));
        }
        private void Landmark(Vector3 p,int r)
        {
            if(r==3)
            {
                Block(p,5.4f,3.6f,.55f,Rock);
                var basePoint=p+Vector3.up*.55f;
                Hall(basePoint,1.55f,Stone,Rgb(0x884e48));
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Tower(basePoint+new Vector3(2.25f*x,0,1.45f*z),1.12f,false);
                for(int x=-1;x<=1;x+=2)Block(basePoint+Vector3.right*x*2.25f,.45f,3,1.3f,Stone);
                for(int x=-1;x<=1;x+=2)Block(basePoint+new Vector3(x*1.45f,0,-1.55f),1.7f,.42f,1.1f,Stone);
            }
            else if(r==4)
            {
                Frustum(p,2.8f,2.65f,.44f,8,Stone);
                Hall(p+Vector3.up*.44f,1.8f,Rgb(0xd4cbaf),Roof);
                for(int x=-1;x<=1;x+=2)Tower(p+new Vector3(x*1.55f,.38f,-.85f),1.1f,true);
                Tower(p+new Vector3(0,1.4f,.5f),1.1f,true);
            }
            else if(r==WorldAtlas.Mine) PitFrame(p);
            else if(r==WorldAtlas.Forge)
            {
                Frustum(p,2.6f,2.2f,.65f,8,Rgb(0x413c44));
                for(int i=-1;i<=1;i++) Frustum(p+new Vector3(i*1.1f,.63f,.55f),.55f,.24f,2.8f-.6f*Math.Abs(i),5,Rgb(0x563d41));
                Frustum(p+Vector3.up*.65f,.67f,.14f,1.4f,6,Rgb(0xf2a14a));
            }
            else if(r==1)Tower(p,1.18f,false);
            else if(r==0 || r==2)
            {
                Hall(p,.95f, r==0?Stone:Timber,r==0?Rgb(0x94664c):Roof);
                Hall(p+Vector3.right*1.65f,.65f,Stone,Rgb(0x986e53));
            }
            else
            {
                for(int i=0;i<4;i++){float a=i*1.57f;Frustum(p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.1f,.9f,.045f,1.3f,4,i%2==0?Rgb(0xb87549):Rgb(0xd5bd8c),.78f,.75f);}
                Frustum(p,.27f,.04f,.65f,5,Rgb(0xffbc5c));
            }
        }
        private Vector3 Land(Vector2 uv)
        {
            float x=uv.x*Span,z=(1-uv.y)*Depth;
            return new Vector3(x,Surface(x,z),z);
        }
        private Vector3 GridLand(RouteMap map,float x,float y)
        {
            var b=Bounds(map.Region);
            var uv=b.min+Vector2.Scale(b.size,new Vector2((x+.5f)/map.Terrain.Width,1-(y+.5f)/TerrainGrid.Height));
            return Land(uv);
        }

        private void Flag(Vector3 p,float height,Color cloth)
        {
            Frustum(p,.06f,.038f,height,5,Timber);
            var top=p+Vector3.up*(height-.08f);
            Triangle(top,top-Vector3.up*.46f,top+new Vector3(.72f,-.23f,.13f),cloth);
        }

        private void PitFrame(Vector3 p)
        {
            for(int side=-1;side<=1;side+=2) Block(p+Vector3.right*side*.64f,.20f,.28f,1.26f,Timber);
            Block(p+Vector3.up*1.24f,1.7f,.34f,.22f,Timber);
            Block(p+new Vector3(.44f,.97f,-.14f),.19f,.18f,.24f,Rgb(0xffbf68));
        }

        private void MineWorks(RouteMap map)
        {
            for(int y=2;y<27;y++)
            {
                var track=GridLand(map,7,y);
                Block(track,1.05f,.15f,.09f,Timber);
                for(int side=-1;side<=1;side+=2) Block(track+new Vector3(side*.33f,.09f,0),.08f,.72f,.08f,Stone);
                if(y%6==2) PitFrame(track);
                for(int x=1;x<28;x++)
                {
                    if(map.Terrain[map.Terrain.Cell(x,y)]!=TerrainKind.Cliff || x%3!=0 || y%3!=0)continue;
                    var vein=GridLand(map,x,y);
                    Frustum(vein,.52f,.05f,1.15f,5,Rgb(0x63899b));
                    Frustum(vein+Vector3.right*.36f,.30f,.03f,.74f,5,Rgb(0xa2c1bc));
                }
            }
        }

        private void Highland(RouteMap map)
        {
            for(int y=2;y<TerrainGrid.Height-1;y++)
            for(int x=2;x<map.Terrain.Width-2;x++)
            {
                if(map.Terrain[map.Terrain.Cell(x,y)]!=TerrainKind.Cliff)continue;
                foreach(int side in new[]{-1,1})
                {
                    if(map.Terrain[map.Terrain.Cell(x+side,y)]==TerrainKind.Cliff)continue;
                    var bank=GridLand(map,x+side*.69f,y);
                    var floor=GridLand(map,x,y);
                    float step=(bank.y-floor.y)/4;
                    var basePoint=new Vector3(bank.x,floor.y,bank.z);
                    for(int k=0;k<4;k++)
                    {
                        var p=basePoint+new Vector3(side*.045f*k,k*step,0);
                        Frustum(p,.39f+.03f*k,.36f+.035f*k,step*.97f,6,
                            k==3?Rgb(0xb5a282):k%2==0?Rgb(0x807367):Rgb(0x9a816e),.3f,1.25f);
                    }
                }
            }
            var west=GridLand(map,9,23);
            var foot=GridLand(map,7,14);
            for(int i=0;i<26;i++)
            {
                float t=i/25f;
                Vector3 plank=Vector3.Lerp(west,foot,t);
                plank.x+=.50f*Mathf.Sin(t*2*Mathf.PI);plank.y=Surface(plank.x,plank.z);
                Block(plank,.83f,.29f,.11f,i%5==0?Stone:Timber);
                if(i%4==0)Block(plank+Vector3.right*.40f,.11f,.11f,.53f,Timber);
            }
            Flag(west+Vector3.left*.75f,2.25f,Rgb(0xca774b));
            Flag(GridLand(map,20,12),1.8f,Rgb(0xca774b));
            Hall(foot+Vector3.left*.8f,.60f,Stone,Rgb(0x9f6a49));
        }

        private void ShoreDetails(RouteMap map)
        {
            for(int y=2;y<TerrainGrid.Height-2;y++)for(int x=1;x<map.Terrain.Width-1;x++)
            {
                if(map.Terrain[map.Terrain.Cell(x,y)]!=TerrainKind.Lake ||
                    map.Terrain[map.Terrain.Cell(x-1,y)]==TerrainKind.Lake)continue;
                var p=GridLand(map,x,y);
                for(int i=0;i<7;i++)
                    Frustum(p+new Vector3(Range(-.2f,.18f),0,Range(-.30f,.30f)),.055f,.01f,Range(.18f,.49f),4,Rgb(0x93a16b));
                if(y%7!=0)continue;
                for(int i=0;i<7;i++)Block(p+Vector3.left*(.14f*i),.13f,.55f,.19f,Timber);
                Frustum(p+Vector3.right*.5f,.43f,.51f,.19f,7,Timber,.2f,.37f);
            }
        }

        private void Dress()
        {
            for(int i=0;i<7800;i++)
            {
                float x=Range(1,99),z=Range(1,67),coast=Coast(x,z);
                if(coast<.075f)continue;
                var uv=World(x,z); int r=Region(uv); var k=Kind(uv,r);
                if(k==TerrainKind.Lake || k==TerrainKind.Cliff)continue;
                float cluster=Noise(x,z,.20f);
                var p=new Vector3(x,Surface(x,z),z);
                if((r==WorldAtlas.Mine || r==WorldAtlas.Forge) && Basin(uv,r,cluster)>.25f)
                {
                    if(i%4==0)Frustum(p,.35f,.07f,Range(.45f,1.12f),5,Rgb(r==WorldAtlas.Mine?0x61596a:0x573e40));
                    continue;
                }
                if((k==TerrainKind.Forest && cluster>.32f) || (cluster>.61f && (r<0 || r==2)))
                    Pine(p,Range(.43f,1.03f)*(r==2?1.22f:1));
                else if(cluster<.29f && i%2==0)
                    Frustum(p,Range(.35f,.84f),.16f,Range(.46f,1.51f),6,Rock,Range(0,3),.74f);
            }
            for(int i=0;i<70;i++)
            {
                float x=Range(29,66),z=49+4*Mathf.Sin(x*.22f)+Range(-2.6f,2.6f);
                if(Region(World(x,z))>=0 || Coast(x,z)<.09f)continue;
                var p=new Vector3(x,Surface(x,z),z);float s=Range(1.2f,2.7f);
                Frustum(p,s,s*.36f,s*1.05f,7,Rock,.25f);
                Frustum(p+Vector3.up*s*1.05f,s*.36f,.02f,s*.65f,7,Rgb(0xddd6bd),.25f);
            }
            for(int r=0;r<Sites.Length;r++)
            {
                var map=atlas.Regions[r];
                var node=map.Nodes[0];
                foreach(var site in map.Nodes)if(site.Kind==RoomKind.Boss){node=site;break;}
                var b=Bounds(r);int cell=map.Cell(node);
                var uv=b.min+Vector2.Scale(b.size,new Vector2((cell%map.Terrain.Width+.5f)/map.Terrain.Width,1-(cell/map.Terrain.Width+.5f)/TerrainGrid.Height));
                Landmark(Land(uv),r);
                if(r==WorldAtlas.Mine)MineWorks(map);
                if(r==0 || r==2) ShoreDetails(map);
                if(r==1)
                {
                    Highland(map);
                    // The rock causeway occupies the passable row used by walking paths.
                    for(int x=1;x<map.Terrain.Width-1;x++)if(map.Terrain[map.Terrain.Cell(x,13)]==TerrainKind.Cliff)
                    {
                        var bridge=b.min+Vector2.Scale(b.size,new Vector2((x+.5f)/map.Terrain.Width,1-12.5f/TerrainGrid.Height));
                        var pos=Land(bridge);
                        Block(new Vector3(pos.x,.34f,pos.z),.24f,.53f,Mathf.Max(.1f,pos.y-.34f),Stone);
                        for(int j=0;j<4;j++)Block(pos+Vector3.right*(j*.18f-.27f),.17f,.55f,.20f,Timber);
                        Block(pos+Vector3.forward*.22f,.76f,.07f,.34f,Stone);
                    }
                }
            }
            Landmark(Land(Camp),-1);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(Picture); UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(mesh); UnityEngine.Object.Destroy(paint);
        }
    }
}
