using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SuperDodgeball
{
    public sealed partial class DodgeClubhouse
    {
        const int Plaster=0xeee6d4, Tile=0xa5c3b8, Grout=0x788e88, Rail=0x314d53, Steel=0x91aaa9,
            Chrome=0xcbd6cf, Wood=0xc7864c, Honey=0xdba267, Straw=0xf0bd78, DeepWood=0x875033,
            Linen=0xffeed1, Stitch=0xe19765, Blue=0x428b91, Rust=0xb75f49, Deep=0x263c41,
            Rubber=0x546b69, Field=0xb9b7a6, Speckle=0xa6a697, Glass=0xd2e6df, Sun=0xffe9af,
            Barrel=0xc4e5e9, Orange=0xe88343, InkBlue=0x274e67;
        const float CubicleFront=3.90f, CubicleNear=.28f, CubicleFar=2.95f;
        readonly List<Mesh> roomMeshes=new List<Mesh>();
        readonly Dictionary<int,Material> roomFinishes=new Dictionary<int,Material>();
        readonly Transform[] roomFrames=new Transform[2];

        Material RoomFinish(int rgb)
        {
            if(roomFinishes.TryGetValue(rgb,out var material))return material;
            Color32 colour=new Color32((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,255);
            material=Mat(colour);
            bool metal=rgb==Chrome || rgb==Steel || rgb==Rail;
            material.SetFloat("_Metallic",metal?.38f:0f);
            material.SetFloat("_Smoothness",metal?.52f:rgb==Tile || rgb==Glass?.48f:.2f);
            if(rgb==Sun || rgb==Glass)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",(Color)colour*(rgb==Sun?.95f:.25f));
            }
            if(rgb==Barrel)
            {
                colour.a=100;material.SetColor("_BaseColor",(Color)colour);
                material.SetFloat("_Surface",1f);material.SetFloat("_ZWrite",0);
                material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType","Transparent");
                material.renderQueue=(int)RenderQueue.Transparent;
            }
            roomFinishes.Add(rgb,material);return material;
        }

        public bool InTeamChangingRoom(Vector3 world,int team)
        {
            if(team<0 || team>1 || !roomFrames[team])return false;
            Vector3 p=roomFrames[team].InverseTransformPoint(world);
            p.x-=DodgeCourt.RestSideExtension;
            return p.x>CubicleFront+.06f && p.x<5.82f && p.z>CubicleNear+.09f && p.z<CubicleFar-.09f && p.y<2.5f;
        }

        void FurnishRestRoom(Transform room,int team,float sign)
        {
            var frame=roomFrames[team]=new GameObject("Locker room fittings").transform;
            frame.SetParent(room,false);
            frame.localPosition=new Vector3(0,0,sign*22);
            frame.localRotation=Quaternion.Euler(0,team==0?180:0,0);
            var architecture=SideFrame(frame,"Room envelope",0);
            architecture.localScale=new Vector3(DodgeCourt.RestHalfWidth/6f,1,1);
            var lockers=SideFrame(frame,"Locker side",-DodgeCourt.RestSideExtension);
            var booth=SideFrame(frame,"Changing side",DodgeCourt.RestSideExtension);
            int kit=team==0?Blue:Rust;
            var shell=new CourtSketchMesh();
            RoomEnvelope(shell,kit);
            roomMeshes.Add(shell.BakeSurfaces("Tile, clerestories and light fittings",architecture,RoomFinish));
            var furnishings=new CourtSketchMesh();
            LockerBank(furnishings,kit);
            Bench(furnishings,-4.26f,.35f,4.22f);
            DrapedTowel(furnishings,-4.26f,1.40f,kit);
            KitBag(furnishings,new Vector3(-4.20f,.635f,-1.12f),team==0?Rust:Blue);
            Trainers(furnishings,new Vector3(-3.91f,.025f,-.18f),kit);
            Hamper(furnishings,new Vector3(-5.20f,0,3.34f),kit);
            roomMeshes.Add(furnishings.BakeSurfaces("Locker bank and home bench",lockers,RoomFinish));
            furnishings=new CourtSketchMesh();
            ChangingBooth(furnishings,kit);
            Bench(furnishings,4.62f,-1.05f,2.0f);
            FoldedTowel(furnishings,new Vector3(4.60f,.636f,-.74f),.65f,.42f,Linen,kit);
            if(team==0)MedicalCorner(furnishings);
            else Hamper(furnishings,new Vector3(5.12f,0,-3.05f),kit);
            roomMeshes.Add(furnishings.BakeSurfaces("Changing booth and supplies",booth,RoomFinish));

            Block(lockers,"Locker bank",new Vector3(-5.45f,1.45f,-.35f),new Vector3(.78f,2.9f,5.38f));
            Block(lockers,"Open locker door",new Vector3(-4.69f,1.55f,-.51f),new Vector3(.86f,2.60f,.075f),28);
            Block(lockers,"Locker bench",new Vector3(-4.26f,.32f,.35f),new Vector3(.77f,.61f,4.22f));
            Block(booth,"Changing bench",new Vector3(4.62f,.32f,-1.05f),new Vector3(.77f,.61f,2f));
            foreach(float z in new[]{CubicleNear,CubicleFar})
                Block(booth,"Cubicle partition",new Vector3(4.92f,1.25f,z),new Vector3(1.86f,2.5f,.095f));
            Block(booth,"Gathered curtain",new Vector3(CubicleFront,1.25f,.82f),new Vector3(.12f,2.4f,1.04f));
            Block(lockers,"Laundry basket",new Vector3(-5.2f,.42f,3.34f),new Vector3(.73f,.84f,.74f));
            WorldLabel("Changing booth sign",booth,new Vector3(3.77f,2.79f,1.66f),"更衣间  /  C 换装",.038f,draftInk,90);
            for(int i=0;i<6;i++)
            {
                if(i==3)continue;
                float z=2.08f-.94f*i;
                WorldLabel("Locker number",lockers,new Vector3(-4.995f,2.31f,z),
                    (team==0?i+4:i+14).ToString("00"),.031f,draftInk,-90);
            }
            if(team==0)
            {
                WorldLabel("Medical log",booth,new Vector3(5.64f,1.98f,-3.0f),"队医登记",.051f,draftInk,90);
                WorldLabel("Medical log entry",booth,new Vector3(5.64f,1.66f,-3.0f),"本场用量   0",.042f,draftInk,90);
            }
            for(int side=-1;side<=1;side+=2)
            {
                var lamp=new GameObject("Locker room practical light",typeof(Light));
                lamp.transform.SetParent(frame,false);lamp.transform.localPosition=new Vector3(side*2.9f,4.4f,-.15f);
                var bulb=lamp.GetComponent<Light>();bulb.type=LightType.Spot;bulb.range=9.2f;
                lamp.transform.localRotation=Quaternion.LookRotation(new Vector3(side*.16f,-1,-.10f));
                bulb.spotAngle=117;bulb.innerSpotAngle=68;
                bulb.intensity=side<0?15f:10f;bulb.color=side<0?new Color(1,.88f,.73f):new Color(.91f,.96f,1);
                bulb.shadows=side<0?LightShadows.Soft:LightShadows.None;
                bulb.shadowStrength=.60f;bulb.shadowBias=.04f;
            }
        }
        static Transform SideFrame(Transform parent,string name,float x)
        {
            var child=new GameObject(name).transform;
            child.SetParent(parent,false);child.localPosition=Vector3.right*x;return child;
        }
        static void Block(Transform parent,string name,Vector3 position,Vector3 size,float yaw=0)
        {
            var go=new GameObject(name,typeof(BoxCollider),typeof(DodgeSurface));go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            go.GetComponent<BoxCollider>().size=size;
        }

        static void RoomEnvelope(CourtSketchMesh m,int kit)
        {
            // The visible terrazzo skin ends at the threshold. The original slab handles gameplay collisions.
            m.Face(Speckle,new Vector3(-5.84f,.007f,-4.65f),new Vector3(-5.84f,.007f,4.69f),
                new Vector3(5.84f,.007f,4.69f),new Vector3(5.84f,.007f,-4.65f));
            for(int x=0;x<10;x++)for(int z=0;z<8;z++)
            {
                float left=-5.83f+x*1.166f, near=-4.64f+z*1.165f, right=left+1.153f, far=near+1.15f;
                m.Face((x+z*3)%7==0?0xb4b3a3:Field,new Vector3(left,.013f,near),new Vector3(left,.013f,far),
                    new Vector3(right,.013f,far),new Vector3(right,.013f,near));
            }
            var seed=new System.Random(23);
            for(int n=0;n<270;n++)
            {
                float x=(float)seed.NextDouble()*11.4f-5.7f,z=(float)seed.NextDouble()*9.1f-4.52f;
                float s=.012f+(float)seed.NextDouble()*.025f;
                m.Face(n%3==0?0xd1cebb:Speckle,new Vector3(x,.015f,z),new Vector3(x+s,.015f,z+s*.8f),new Vector3(x+s*1.55f,.015f,z-.004f));
            }
            m.Box(Plaster,new Vector3(0,4.28f,4.82f),new Vector3(11.69f,2.95f,.18f));
            m.Box(Grout,new Vector3(0,.12f,4.68f),new Vector3(11.67f,.23f,.10f));
            m.Box(kit,new Vector3(0,5.30f,4.67f),new Vector3(11.69f,.11f,.10f));
            Clock(m,new Vector3(-4.73f,4.58f,4.63f));
            // Opaque bases, tiled lower walls and translucent-looking clerestory panes.
            for(int side=-1;side<=1;side+=2)
            {
                m.Box(Plaster,new Vector3(side*5.89f,3.69f,0),new Vector3(.15f,3.97f,9.48f));
                m.Box(Grout,new Vector3(side*5.82f,.87f,0),new Vector3(.04f,1.73f,9.48f));
                for(int row=0;row<4;row++)for(int col=0;col<16;col++)
                    m.Box((row+col*2)%9==0?0xabc6ba:Tile,new Vector3(side*5.79f,.215f+row*.43f,-4.425f+col*.59f),
                        new Vector3(.04f,.419f,.578f),.035f);
                m.Box(kit,new Vector3(side*5.75f,1.78f,0),new Vector3(.10f,.105f,9.43f));
                m.Box(Deep,new Vector3(side*5.73f,.085f,0),new Vector3(.10f,.17f,9.44f));
                m.Box(Rail,new Vector3(side*5.76f,3.58f,-.32f),new Vector3(.14f,.96f,7.57f));
                for(int i=0;i<5;i++)
                {
                    float z=-3.31f+i*1.49f;
                    m.Box(Glass,new Vector3(side*5.67f,3.58f,z),new Vector3(.06f,.80f,1.37f));
                    m.Box(Chrome,new Vector3(side*5.626f,3.47f,z),new Vector3(.035f,.035f,1.39f));
                }
                m.Box(Wood,new Vector3(side*5.64f,3.045f,-.32f),new Vector3(.28f,.10f,7.78f));
                for(int i=0;i<2;i++)
                {
                    float z=-2.50f+i*4.72f;
                    m.Box(Rail,new Vector3(side*2.90f,5.50f,z),new Vector3(.30f,.14f,2.1f));
                    m.Box(Sun,new Vector3(side*2.90f,5.412f,z),new Vector3(.21f,.055f,1.98f));
                }
            }
            m.Box(Plaster,new Vector3(0,5.72f,0),new Vector3(11.92f,.20f,9.6f));
            for(int i=0;i<4;i++)m.Box(0xb3b3a3,new Vector3(0,5.592f,-4.2f+i*2.8f),new Vector3(11.8f,.08f,.09f));
        }
        static void Clock(CourtSketchMesh m,Vector3 at)
        {
            m.Push(at,Quaternion.Euler(-90,0,0));
            m.Drum(Rail,Vector3.zero,new Vector3(.70f,.13f,.70f),36);
            m.Drum(Linen,new Vector3(0,.075f,0),new Vector3(.61f,.027f,.61f),36);
            for(int i=0;i<12;i++)
            {
                m.Push(Vector3.zero,i*30f);
                m.Box(Rail,new Vector3(0,.10f,.243f),new Vector3(.021f,.012f,.052f));m.Pop();
            }
            m.Stick(Deep,new Vector3(0,.11f,0),new Vector3(-.12f,.11f,.10f),.027f);
            m.Stick(Rust,new Vector3(0,.125f,0),new Vector3(.17f,.125f,.12f),.021f);m.Pop();
        }

        static void LockerBank(CourtSketchMesh m,int kit)
        {
            // The bank faces the aisle. One bay is open, with its jersey, hanger and cubby visible.
            m.Push(new Vector3(-5.44f,0,-.27f),90);
            m.Box(Deep,new Vector3(0,.12f,0),new Vector3(5.67f,.24f,.76f));
            m.Box(Steel,new Vector3(0,2.91f,.035f),new Vector3(5.72f,.13f,.88f));
            for(int i=0;i<6;i++)
            {
                float x=(i-2.5f)*.94f;
                m.Push(new Vector3(x,0,0));
                m.Box(Deep,new Vector3(0,1.53f,-.31f),new Vector3(.935f,2.65f,.14f));
                for(int s=-1;s<=1;s+=2)m.Box(Steel,new Vector3(s*.452f,1.53f,0),new Vector3(.042f,2.66f,.75f));
                m.Box(Steel,new Vector3(0,.29f,.02f),new Vector3(.90f,.10f,.72f));
                if(i==3)
                {
                    m.Box(Wood,new Vector3(0,.75f,.025f),new Vector3(.89f,.065f,.68f));
                    m.Box(Chrome,new Vector3(0,2.55f,-.035f),new Vector3(.86f,.047f,.047f));
                    m.Stick(Honey,new Vector3(0,2.44f,.02f),new Vector3(-.28f,2.27f,.02f),.047f);
                    m.Stick(Honey,new Vector3(0,2.44f,.02f),new Vector3(.28f,2.27f,.02f),.047f);
                    m.Stick(Chrome,new Vector3(0,2.44f,.02f),new Vector3(0,2.57f,-.035f),.025f);
                    Jersey(m,kit);
                    FoldedTowel(m,new Vector3(.06f,.803f,.055f),.58f,.39f,Linen,kit);
                    m.Push(new Vector3(-.435f,0,.37f),-62);
                    LockerDoor(m,.435f,kit,false);m.Pop();
                }
                else LockerDoor(m,0,kit,true);
                m.Pop();
            }
            m.Pop();
        }
        static void LockerDoor(CourtSketchMesh m,float offset,int kit,bool closed)
        {
            m.Push(new Vector3(offset,0,closed?.365f:0));
            m.Box(kit,new Vector3(0,1.56f,0),new Vector3(.852f,2.60f,.075f));
            for(int i=0;i<4;i++)
            {
                float y=2.64f-i*.076f;
                m.Box(Deep,new Vector3(0,y,.045f),new Vector3(.51f,.029f,.014f));
                m.Box(Steel,new Vector3(0,y+.022f,.050f),new Vector3(.515f,.018f,.03f));
            }
            for(int i=0;i<3;i++)m.Box(Deep,new Vector3(0,.47f+i*.067f,.047f),new Vector3(.48f,.021f,.013f));
            m.Box(Linen,new Vector3(0,2.31f,.049f),new Vector3(.35f,.22f,.02f));
            m.Box(Rail,new Vector3(.28f,1.55f,.055f),new Vector3(.115f,.30f,.025f));
            m.Box(Chrome,new Vector3(.28f,1.57f,.11f),new Vector3(.041f,.20f,.07f));
            m.Pop();
        }
        static void Jersey(CourtSketchMesh m,int kit)
        {
            // The sleeves flare off the shoulder and the hem hangs crooked on the wooden hanger.
            m.Push(new Vector3(0,1.96f,.13f));
            m.Face(Linen,new Vector3(-.23f,-.37f,.03f),new Vector3(.22f,-.33f,.04f),new Vector3(.195f,.29f,0),new Vector3(-.19f,.29f,0));
            m.Face(Linen,new Vector3(-.36f,.02f,.035f),new Vector3(-.17f,.10f,.035f),new Vector3(-.19f,.29f,0),new Vector3(-.36f,.16f,.025f));
            m.Face(Linen,new Vector3(.17f,.10f,.035f),new Vector3(.36f,.02f,.035f),new Vector3(.37f,.16f,.025f),new Vector3(.195f,.29f,0));
            m.Stick(kit,new Vector3(-.118f,.27f,.025f),new Vector3(0,.19f,.04f),.028f);
            m.Stick(kit,new Vector3(0,.19f,.04f),new Vector3(.118f,.27f,.025f),.028f);
            m.Stick(kit,new Vector3(.079f,.105f,.046f),new Vector3(-.073f,.105f,.046f),.029f);
            m.Stick(kit,new Vector3(-.073f,.105f,.046f),new Vector3(.03f,-.11f,.063f),.03f);m.Pop();
        }
        static void Tube(CourtSketchMesh m,int colour,Vector3 from,Vector3 to,float diameter,int sides=10)
        {
            m.Push((from+to)*.5f,Quaternion.FromToRotation(Vector3.up,to-from));
            m.Drum(colour,Vector3.zero,new Vector3(diameter,(to-from).magnitude,diameter),sides);m.Pop();
        }
        static void Bench(CourtSketchMesh m,float x,float z,float length)
        {
            m.Push(new Vector3(x,0,z));
            for(int i=0;i<4;i++)
            {
                float slat=(i-1.5f)*.201f;
                m.Box(i%2==0?Honey:Wood,new Vector3(slat,.565f,0),new Vector3(.187f,.12f,length),.20f);
                for(int end=-1;end<=1;end+=2)
                    m.Drum(Rail,new Vector3(slat,.627f,end*(length*.5f-.25f)),new Vector3(.031f,.008f,.031f),8);
            }
            for(int s=-1;s<=1;s+=2)
            {
                float foot=s*(length*.5f-.25f);
                m.Box(Rail,new Vector3(0,.475f,foot),new Vector3(.76f,.071f,.085f));
                for(int side=-1;side<=1;side+=2)
                {
                    Tube(m,Rail,new Vector3(side*.235f,.48f,foot),new Vector3(side*.335f,.065f,foot),.077f);
                    m.Box(Deep,new Vector3(side*.335f,.044f,foot),new Vector3(.119f,.069f,.147f));
                }
            }
            m.Box(DeepWood,new Vector3(0,.35f,0),new Vector3(.11f,.14f,length-.4f));m.Pop();
        }
        static void FoldedTowel(CourtSketchMesh m,Vector3 bottom,float width,float depth,int cloth,int stripe)
        {
            m.Push(bottom);
            m.Box(cloth,new Vector3(0,.047f,0),new Vector3(width,.095f,depth),.27f);
            m.Box(cloth,new Vector3(.025f,.115f,-.012f),new Vector3(width*.94f,.075f,depth*.96f),.26f);
            m.Box(stripe,new Vector3(width*.27f,.153f,-.012f),new Vector3(.055f,.012f,depth*.86f));
            m.Box(stripe,new Vector3(width*.27f,.106f,depth*.469f),new Vector3(.055f,.072f,.013f));m.Pop();
        }
        static void DrapedTowel(CourtSketchMesh m,float x,float z,int kit)
        {
            m.Push(new Vector3(x,0,z));
            Vector3[] profile={new(-.21f,.642f,0),new(.10f,.656f,0),new(.34f,.648f,0),new(.414f,.612f,0),new(.434f,.50f,0),new(.448f,.27f,0)};
            for(int j=0;j<profile.Length-1;j++)for(int k=0;k<10;k++)
            {
                Vector3 P(int a,int b)
                {
                    float t=b/10f;
                    var p=profile[a];p.z+=(t-.5f)*.78f;
                    float fold=.017f*Mathf.Sin(t*18f+a*.30f);
                    p.x+=a>=3?fold:0;p.y+=a>=3?.025f*Mathf.Cos(t*12f):fold;
                    return p;
                }
                int cloth=k==1 || k==8?kit:Linen;
                m.Face(cloth,P(j,k),P(j,k+1),P(j+1,k+1),P(j+1,k));
            }
            m.Pop();
        }
        static void KitBag(CourtSketchMesh m,Vector3 at,int canvas)
        {
            m.Push(at,-8);
            m.Push(new Vector3(0,.24f,0),Quaternion.Euler(83,0,0));
            m.Drum(canvas,Vector3.zero,new Vector3(.53f,.88f,.47f),18);
            for(int end=-1;end<=1;end+=2)
            {
                m.Drum(DeepWood,new Vector3(0,end*.415f,0),new Vector3(.49f,.033f,.435f),18);
                m.Drum(canvas,new Vector3(0,end*.439f,0),new Vector3(.435f,.035f,.39f),18);
            }
            m.Pop();
            for(int s=-1;s<=1;s+=2)
            {
                Vector3 a=new Vector3(s*.135f,.427f,-.22f),b=new Vector3(s*.12f,.60f,-.13f);
                Vector3 c=new Vector3(s*.12f,.61f,.10f),d=new Vector3(s*.135f,.44f,.19f);
                Tube(m,Deep,a,b,.041f);Tube(m,Deep,b,c,.041f);Tube(m,Deep,c,d,.041f);
                for(int end=-1;end<=1;end+=2)
                    m.Stick(Deep,new Vector3(s*.147f,.43f,end*.21f),new Vector3(s*.245f,.17f,end*.21f),.038f);
            }
            m.Stick(Chrome,new Vector3(0,.474f,-.33f),new Vector3(0,.502f,.29f),.016f);
            m.Box(Linen,new Vector3(.26f,.24f,-.014f),new Vector3(.015f,.092f,.165f));m.Pop();
        }
        static void Trainers(CourtSketchMesh m,Vector3 at,int kit)
        {
            for(int i=0;i<2;i++)
            {
                m.Push(at+new Vector3(i*.22f,0,i*.17f),i==0?61:78);
                m.Box(Linen,new Vector3(0,.041f,0),new Vector3(.19f,.081f,.37f),.52f);
                m.Plum(kit,new Vector3(0,.10f,-.018f),new Vector3(.178f,.145f,.32f),12);
                m.Box(Deep,new Vector3(0,.164f,-.08f),new Vector3(.10f,.023f,.091f),.48f);
                for(int j=0;j<3;j++)m.Box(Linen,new Vector3(0,.166f-j*.006f,j*.025f),new Vector3(.094f,.012f,.012f));
                m.Pop();
            }
        }
        static void Hamper(CourtSketchMesh m,Vector3 at,int kit)
        {
            m.Push(at,-7);
            var size=new Vector3(.82f,.12f,.78f);
            m.Box(Rail,new Vector3(0,.11f,0),size);
            m.Box(Rubber,new Vector3(0,.51f,0),new Vector3(.72f,.70f,.65f));
            for(int i=0;i<5;i++)
            {
                float level=.22f+i*.14f;
                for(int side=-1;side<=1;side+=2)
                {
                    m.Box(Tile,new Vector3(0,level,side*.358f),new Vector3(.80f,.068f,.05f));
                    m.Box(Tile,new Vector3(side*.384f,level,0),new Vector3(.045f,.068f,.73f));
                }
            }
            for(int i=-1;i<=1;i++)for(int s=-1;s<=1;s+=2)
            {
                m.Box(Tile,new Vector3(i*.359f,.51f,s*.364f),new Vector3(.056f,.72f,.04f));
                m.Box(Tile,new Vector3(s*.388f,.51f,i*.33f),new Vector3(.045f,.72f,.06f));
            }
            FoldedTowel(m,new Vector3(-.06f,.85f,.01f),.62f,.49f,Linen,kit);
            m.Push(new Vector3(.065f,1.01f,.05f),12);
            FoldedTowel(m,Vector3.zero,.55f,.38f,kit,Linen);m.Pop();m.Pop();
        }
        static void ChangingBooth(CourtSketchMesh m,int kit)
        {
            for(int end=0;end<2;end++)
            {
                float z=end==0?CubicleNear:CubicleFar;
                m.Box(Rail,new Vector3(4.92f,1.4f,z),new Vector3(1.92f,2.45f,.09f));
                m.Box(0xd1b996,new Vector3(4.92f,1.4f,z),new Vector3(1.80f,2.29f,.102f));
                m.Box(Chrome,new Vector3(4.02f,.14f,z),new Vector3(.09f,.28f,.09f));
            }
            m.Box(Steel,new Vector3(3.85f,2.79f,1.66f),new Vector3(.12f,.42f,2.50f));
            m.Box(Linen,new Vector3(3.782f,2.79f,1.66f),new Vector3(.018f,.32f,2.35f));
            Tube(m,Chrome,new Vector3(CubicleFront,2.53f,CubicleNear),new Vector3(CubicleFront,2.53f,CubicleFar),.047f);
            // A partly drawn cloth, modelled in folds so the bay reads from either side.
            float[] heights={2.425f,2.32f,2.24f,.54f,.44f,.30f};
            const int folds=36;
            Vector3 Drape(int i,int row)
            {
                float t=(float)i/folds;
                return new Vector3(CubicleFront+.095f*Mathf.Cos(t*Mathf.PI*10f)+.018f*row,heights[row]-.018f*Mathf.Sin(t*27f),
                    CubicleNear+.09f+t*(1.02f+.042f*row));
            }
            for(int i=0;i<folds;i++)for(int j=0;j<heights.Length-1;j++)
            {
                int shade=j==1 || j==3?Linen:kit;
                var a=Drape(i,j);var b=Drape(i,j+1);var c=Drape(i+1,j+1);var d=Drape(i+1,j);
                m.Face(shade,a,b,c,d);m.Face(shade,d,c,b,a);
            }
            for(int i=0;i<6;i++)
            {
                m.Push(new Vector3(CubicleFront,2.496f,CubicleNear+.10f+i*.202f),Quaternion.Euler(90,0,0));
                m.Ring(Chrome,Vector3.zero,.065f,.071f,.018f,12);m.Pop();
            }
            m.Box(Rubber,new Vector3(4.86f,.023f,1.61f),new Vector3(1.67f,.028f,2.46f));
            m.Box(Rail,new Vector3(5.71f,1.73f,1.9f),new Vector3(.10f,1.47f,.94f));
            m.Box(Glass,new Vector3(5.65f,1.73f,1.9f),new Vector3(.032f,1.34f,.81f));
            m.Box(Wood,new Vector3(5.54f,.97f,1.9f),new Vector3(.37f,.066f,1.08f));
            // A second towel hangs from a hook inside the booth.
            Tube(m,Chrome,new Vector3(5.77f,1.97f,.78f),new Vector3(5.54f,1.99f,.78f),.046f);
            m.Box(Linen,new Vector3(5.54f,1.67f,.78f),new Vector3(.09f,.64f,.31f));
            m.Box(kit,new Vector3(5.49f,1.42f,.78f),new Vector3(.016f,.039f,.28f));
        }
        static void MedicalCorner(CourtSketchMesh m)
        {
            m.Box(Wood,new Vector3(5.747f,1.83f,-3),new Vector3(.10f,.85f,1.62f));
            m.Box(Linen,new Vector3(5.692f,1.83f,-3),new Vector3(.013f,.76f,1.50f));
            m.Box(Chrome,new Vector3(5.679f,2.18f,-3),new Vector3(.033f,.080f,.44f));
            m.Push(new Vector3(4.84f,.25f,-3.30f),Quaternion.Euler(-6,-18,72));
            m.Box(Linen,Vector3.zero,new Vector3(.82f,.51f,.56f));
            m.Box(Blue,new Vector3(0,0,.292f),new Vector3(.30f,.084f,.024f));
            m.Box(Blue,new Vector3(0,0,.296f),new Vector3(.085f,.28f,.025f));
            m.Box(DeepWood,new Vector3(0,.265f,0),new Vector3(.71f,.03f,.44f));
            for(int s=-1;s<=1;s+=2)
            {
                m.Push(new Vector3(s*.40f,.265f,0),Quaternion.Euler(0,0,s*-22));
                m.Box(Wood,new Vector3(s*.10f,0,0),new Vector3(.21f,.029f,.54f));m.Pop();
            }
            m.Pop();
            // A single spill, fanning out from the kit. These are empty capped props.
            Vector3[] scatter={new(4.53f,-2.89f,23),new(4.02f,-3.15f,-58),new(3.66f,-3.38f,81),
                new(4.72f,-2.51f,-14),new(3.93f,-2.69f,55),new(3.51f,-2.53f,-43),
                new(4.26f,-3.60f,101),new(3.04f,-3.27f,34),new(3.29f,-2.96f,74),
                new(5.16f,-2.71f,69),new(4.61f,-3.76f,-53),new(2.90f,-2.34f,112),new(3.98f,-2.20f,8)};
            for(int i=0;i<scatter.Length;i++)
            {
                var p=scatter[i];m.Push(new Vector3(p.x,.076f,p.y),p.z);
                EmptySyringe(m,i%3==0);m.Pop();
            }
        }
        static void EmptySyringe(CourtSketchMesh m,bool extended)
        {
            // A visible plunger through the empty barrel, two finger flanges, a capped tip and graduations.
            float end=extended?-.285f:-.21f;
            Tube(m,Barrel,new Vector3(0,0,-.12f),new Vector3(0,0,.14f),.098f,12);
            Tube(m,Chrome,new Vector3(0,0,end),new Vector3(0,0,-.077f),.027f);
            Tube(m,InkBlue,new Vector3(0,0,-.109f),new Vector3(0,0,-.08f),.077f);
            m.Box(Glass,new Vector3(0,0,-.122f),new Vector3(.162f,.07f,.021f),.35f);
            Tube(m,Blue,new Vector3(0,0,end-.012f),new Vector3(0,0,end+.01f),.105f);
            Tube(m,Glass,new Vector3(0,0,.123f),new Vector3(0,0,.145f),.087f);
            Tube(m,Orange,new Vector3(0,0,.147f),new Vector3(0,0,.237f),.04f);
            for(int i=0;i<4;i++)m.Box(InkBlue,new Vector3(.005f,.049f,-.046f+i*.036f),
                new Vector3(i%2==0?.043f:.024f,.007f,.006f));
        }
    }
}
