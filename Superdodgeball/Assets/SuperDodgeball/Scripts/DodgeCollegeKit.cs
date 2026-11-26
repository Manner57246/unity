using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeWardrobe
    {
        Renderer headbandInk, crown, fringe;
        GameObject sweatband,hairMesh;
        Mesh hairSurface,bandSurface;
        Texture2D jerseyAtlas;
        int club=-2, seat=-1;

        void InitializeTrackKit(Transform face,Material cloth)
        {
            jerseyAtlas=Resources.Load<Texture2D>("Court/CollegeJerseys");
            crown=cap.transform.Find("Cap crown").GetComponent<Renderer>();
            var leather=new Color32(54,44,45,255);
            hairSurface=Headwear(false);
            fringe=Fit(face,"Swept hair",hairSurface,cloth,leather);
            hairMesh=fringe.gameObject;
            bandSurface=Headwear(true);
            headbandInk=Fit(face,"Woven sweatband",bandSurface,cloth,DodgeColleges.Cream);
            sweatband=headbandInk.gameObject;
            for(int side=-1;side<=1;side+=2)
            {
                var brow=Part("Game brow",face,new Vector3(side*.148f,.120f,.344f),new Vector3(.154f,.042f,.047f),cloth,leather);
                brow.localRotation=Quaternion.Euler(0,side*15f,side*10f);
            }
            Part("Nose",face,new Vector3(.005f,-.039f,.382f),new Vector3(.089f,.095f,.092f),cloth,new Color32(209,137,93,255));
            SetCollege(-1,0);
        }

        static Renderer Fit(Transform socket,string name,Mesh mesh,Material cloth,Color tint)
        {
            var piece=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            piece.transform.SetParent(socket,false);
            piece.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=cloth;Tint(renderer,tint);
            return renderer;
        }

        // Follow the head volume from an asymmetric hairline up to a swept crown.
        // The band has its own tension line and a rolled lip around each edge.
        static Mesh Headwear(bool band)
        {
            const int sides=32;
            int rings=band?4:8;
            var vertices=new Vector3[sides*rings];
            var tris=new int[sides*(rings-1)*6];
            for(int row=0;row<rings;row++)
            {
                float t=row/(float)(rings-1);
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides, front=Mathf.Cos(a),side=Mathf.Sin(a);
                    float line=-.011f+.161f*front+.035f*Mathf.Sin(2*a+.55f)+.018f*Mathf.Sin(5*a)*Mathf.Max(front,0);
                    float y=band?.105f+.080f*t+.030f*side-.032f*(1-front):Mathf.Lerp(line,.393f,t);
                    float taper=band?1:Mathf.Sqrt(Mathf.Max(.0001f,1-Mathf.Pow(Mathf.Max(0,y)/.394f,2)));
                    float inset=band?(row==0||row==rings-1?.992f:1.012f):1;
                    float x=side*(band?.458f:.456f)*taper*inset;
                    float z=front*(band?.420f:.420f)*taper*inset;
                    if(!band){x-=.021f*t*t;z+=.018f*t*t; y+=Mathf.Max(front,0)*.035f*Mathf.Sin(t*Mathf.PI);}
                    vertices[row*sides+i]=new Vector3(x,y,z);
                    if(row==rings-1)continue;
                    int n=(i+1)%sides,l=row*sides+i,r=row*sides+n,k=(row*sides+i)*6;
                    tris[k]=l;tris[k+1]=r;tris[k+2]=l+sides;
                    tris[k+3]=r;tris[k+4]=r+sides;tris[k+5]=l+sides;
                }
            }
            var mesh=new Mesh{name=band?"Rolled sweatband":"Track cut"};
            mesh.vertices=vertices;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();
            return mesh;
        }

        public void SetCollege(int team,int slot)
        {
            if(club==team && seat==slot)return;
            club=team;seat=slot;
            var atlasSlot=(team<0?2:team)*5+Mathf.Clamp(slot,0,4);
            var print=new MaterialPropertyBlock();
            print.SetColor("_BaseColor",Color.white);
            print.SetFloat("_Smoothness",.13f);
            if(jerseyAtlas)
            {
                print.SetTexture("_BaseMap",jerseyAtlas);
                print.SetVector("_BaseMap_ST",new Vector4(.25f,.25f,(atlasSlot%4)*.25f,(3-atlasSlot/4)*.25f));
            }
            shirt.Surface.SetPropertyBlock(print);
            Tint(waistband.Surface,DodgeColleges.Navy);
            Tint(crown,DodgeColleges.Colour(team));
            Tint(headbandInk,team==1?DodgeColleges.Gold:DodgeColleges.Cream);
            Tint(fringe,slot==2?new Color32(123,74,46,255):slot==4?new Color32(74,54,43,255):new Color32(47,43,45,255));
        }

        internal static void Sneaker(Transform foot,int side,Material material)
        {
            // Normalised shoe coordinates inherit the existing physical foot's size.
            Tint(foot.GetComponent<Renderer>(),DodgeColleges.Navy);
            Part("Rubber outsole",foot,new Vector3(0,-.26f,.015f),new Vector3(1.03f,.33f,1.085f),material,DodgeColleges.Cream);
            for(int i=0;i<2;i++)
            {
                var stripe=Part("Track shoe stitch",foot,new Vector3(side*.405f,.065f,.03f+i*.14f),new Vector3(.095f,.48f,.081f),material,DodgeColleges.Cream);
                stripe.localRotation=Quaternion.Euler(-26,side*13,0);
            }
        }
        void OnDestroy(){if(hairSurface)Destroy(hairSurface);if(bandSurface)Destroy(bandSurface);}
    }
}
