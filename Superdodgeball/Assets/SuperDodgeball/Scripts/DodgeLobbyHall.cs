using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeClubhouse
    {
        const int HallStone = 0xd8ccae, HallStoneDark = 0xc2b69d, HallTimber = 0xad7850,
            MintEnamel = 0x34786e, PeachEnamel = 0xa95040, MintPale = 0xc5dfca, PeachPale = 0xf2ceb0;
        readonly MeshRenderer[] checkInLamps = new MeshRenderer[2];
        int indicatedTeam=-2;

        void BuildLobby()
        {
            Transform hall = lobby.transform;
            hall.position = Vector3.forward * LobbyZ;
            // The whole hall uses the same solid slab and the same team footprints as the game rules.
            Part("Clubhouse floor", hall, new Vector3(0,-.10f,-1),new Vector3(30,.20f,27),RoomFinish(HallStone),true);
            Block(hall,"North wall",new Vector3(0,4.5f,10.13f),new Vector3(30,9,.38f));
            Block(hall,"Entrance wall",new Vector3(0,4,-14.43f),new Vector3(30,8,.35f));
            for (int side=-1;side<=1;side+=2)
                Block(hall,"Clubhouse side wall",new Vector3(15*side,3.6f,-1.95f),new Vector3(.36f,7.2f,24.9f));
            var shell = new CourtSketchMesh();
            HallEnvelope(shell);
            HallFloor(shell);
            Pennants(shell);
            ClubDisplayCases(shell);
            RidgeGlazing(hall);
            roomMeshes.Add(shell.BakeSurfaces("Fieldhouse roof, clerestories and inlaid floor",hall,RoomFinish));
            BuildMatchDesk(hall);
            for(int team=0;team<2;team++) BuildCheckIn(hall,team);
            BuildLobbyServices(hall);
            foreach(float x in new[]{-9.8f,0f,9.8f})
                HallLight(hall,new Vector3(x,6.65f,-.7f),new Vector3(0,-.85f,.70f),x==0?32f:25f);
            HallLight(hall,new Vector3(0,6.5f,-8),new Vector3(0,-1,-.25f),22f);
        }

        static void HallLight(Transform root,Vector3 position,Vector3 direction,float power)
        {
            var lamp=new GameObject("Fieldhouse practical",typeof(Light));
            lamp.transform.SetParent(root,false);
            lamp.transform.localPosition=position;
            lamp.transform.localRotation=Quaternion.LookRotation(direction);
            var light=lamp.GetComponent<Light>();
            light.type=LightType.Spot; light.range=16; light.spotAngle=115; light.innerSpotAngle=68;
            light.intensity=power; light.color=new Color(1f,.93f,.82f);
            light.shadows=LightShadows.None;
        }

        static void HallEnvelope(CourtSketchMesh m)
        {
            foreach(int side in new[]{-1,1})
            {
                m.Push(new Vector3(side*14.93f,0,-2.1f),side*90);
                m.Box(Plaster,new Vector3(0,3.70f,0),new Vector3(24.8f,7.4f,.32f));
                m.Box(Grout,new Vector3(0,.91f,-.18f),new Vector3(24.75f,1.82f,.09f));
                for(int row=0;row<3;row++)
                    for(int i=0;i<22;i++)
                        m.Box((i+row)%7==0?0xafc6b8:Tile,new Vector3((i-10.5f)*1.12f,.32f+row*.58f,-.24f),new Vector3(1.105f,.563f,.035f),.015f);
                m.Box(Rail,new Vector3(0,.10f,-.27f),new Vector3(24.75f,.18f,.05f));
                m.Box(Wood,new Vector3(0,1.84f,-.26f),new Vector3(24.8f,.105f,.10f));
                for(int bay=0;bay<4;bay++)
                {
                    float x=(bay-1.5f)*5.9f;
                    m.Box(Rail,new Vector3(x,4.54f,-.20f),new Vector3(5.50f,2.12f,.14f));
                    for(int pane=0;pane<4;pane++)
                    {
                        float xx=x+(pane-1.5f)*1.34f;
                        m.Box(Glass,new Vector3(xx,4.55f,-.285f),new Vector3(1.265f,1.94f,.045f),.015f);
                        m.Box(Steel,new Vector3(xx,4.98f,-.32f),new Vector3(1.32f,.054f,.035f));
                    }
                    m.Box(HallTimber,new Vector3(x,3.45f,-.27f),new Vector3(5.63f,.12f,.42f));
                }
                m.Pop();
                for(int i=0;i<5;i++)
                {
                    float z=-13.4f+i*5.6f;
                    m.Box(HallTimber,new Vector3(side*14.56f,3.64f,z),new Vector3(.34f,7.28f,.39f));
                    m.Stick(HallTimber,new Vector3(side*14.50f,6.02f,z),new Vector3(side*12.75f,7.65f,z),.21f,.26f);
                    m.Stick(HallTimber,new Vector3(side*14.75f,7.28f,z),new Vector3(0,9.05f,z),.32f,.38f);
                    m.Stick(Steel,new Vector3(side*13.95f,7.23f,z),new Vector3(0,7.23f,z),.075f,.075f);
                    m.Stick(Steel,new Vector3(side*7.2f,7.25f,z),new Vector3(side*7.2f,8.01f,z),.07f);
                }
                // Ribbon skylights admit the scene sun between the two roof leaves.
                m.Push(new Vector3(side*8.35f,8.27f,-2.05f),Quaternion.Euler(0,0,-side*6.85f));
                m.Box(Plaster,Vector3.zero,new Vector3(13.8f,.20f,25.06f));
                m.Pop();
            }
            m.Box(Plaster,new Vector3(0,4.52f,10.12f),new Vector3(30,9.05f,.37f));
            m.Box(Plaster,new Vector3(0,4.18f,-14.42f),new Vector3(30,8.36f,.32f));
            m.Face(Plaster,new Vector3(-15,7.25f,-14.22f),new Vector3(15,7.25f,-14.22f),new Vector3(0,9.02f,-14.22f));
            foreach(float z in new[]{-14.21f,9.90f})
            {
                m.Box(Grout,new Vector3(0,.78f,z),new Vector3(29.7f,1.56f,.09f));
                m.Box(Wood,new Vector3(0,1.58f,z),new Vector3(29.72f,.14f,.17f));
            }
            for(int i=0;i<3;i++)
                foreach(int side in new[]{-1,1})
                {
                    var p=new Vector3(side*6.2f,7.1f,-9.3f+8.0f*i);
                    m.Push(p);
                    Tube(m,Rail,new Vector3(0,.73f,0),Vector3.zero,.04f);
                    m.Drum(Rail,new Vector3(0,-.10f,0),new Vector3(.28f,.25f,.28f),16);
                    m.Plum(MintEnamel,new Vector3(0,-.28f,0),new Vector3(.94f,.34f,.94f),20);
                    m.Drum(Sun,new Vector3(0,-.40f,0),new Vector3(.78f,.048f,.78f),20);m.Pop();
                }
            // Entrance joinery closes the view behind the spawn and frames the two service bays.
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*1.13f;
                m.Box(HallTimber,new Vector3(x,1.72f,-14.16f),new Vector3(2.23f,3.44f,.13f));
                m.Box(Glass,new Vector3(x,2.22f,-14.07f),new Vector3(1.79f,1.88f,.05f));
                m.Box(Rail,new Vector3(x,1.35f,-14.0f),new Vector3(1.76f,.075f,.095f));
            }
        }

        void RidgeGlazing(Transform hall)
        {
            var glass=new CourtSketchMesh();
            for(int i=0;i<11;i++)
            {
                float z=-13.2f+2.21f*i;
                glass.Box(Glass,new Vector3(0,9.02f,z),new Vector3(3.35f,.075f,2.22f),.015f);
                glass.Box(Steel,new Vector3(0,8.94f,z-1.10f),new Vector3(3.52f,.11f,.085f));
            }
            foreach(int side in new[]{-1,1})
            {
                glass.Box(Rail,new Vector3(side*1.62f,8.93f,-2.05f),new Vector3(.10f,.15f,24.8f));
                glass.Box(Steel,new Vector3(side*.54f,8.96f,-2.05f),new Vector3(.048f,.06f,24.8f));
            }
            var panes=glass.BakeSurfaces("Frosted ridge lantern",hall,RoomFinish);
            roomMeshes.Add(panes);
            // Frosted panes still admit daylight on the playing floor.
            hall.Find(panes.name).GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void Pennants(CourtSketchMesh m)
        {
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*10.52f;
                Tube(m,Chrome,new Vector3(x-3.24f,7.28f,9.66f),new Vector3(x+3.24f,7.28f,9.66f),.078f);
                for(int i=0;i<3;i++)
                {
                    float xx=x+(i-1)*1.97f, h=i==1?2.06f:1.70f;
                    int dye=(i==1)==(side<0)?MintEnamel:PeachEnamel;
                    m.Push(new Vector3(xx,7.27f,9.59f));
                    // Two cloth panels meet at a shallow fold; the free tip hangs off-centre.
                    var tip=new Vector3(.06f,-h,-.095f);
                    m.Face(dye,new Vector3(-.70f,-.01f,0),new Vector3(0,-.02f,-.065f),tip,new Vector3(-.70f,-h+.47f,-.03f));
                    m.Face(dye,new Vector3(0,-.02f,-.065f),new Vector3(.70f,-.01f,0),new Vector3(.70f,-h+.47f,-.02f),tip);
                    m.Box(Straw,new Vector3(0,-.13f,-.081f),new Vector3(1.36f,.11f,.030f));
                    float cy=-h*.50f;
                    m.Stick(Linen,new Vector3(-.39f,cy+.14f,-.118f),new Vector3(0,cy-.06f,-.128f),.067f);
                    m.Stick(Linen,new Vector3(0,cy-.06f,-.128f),new Vector3(.39f,cy+.14f,-.118f),.067f);
                    m.Pop();
                }
            }
        }

        static void ClubDisplayCases(CourtSketchMesh m)
        {
            // On the way back from the team bays, the entrance is flanked by trophies and a team photograph.
            for(int side=-1;side<=1;side+=2)
            {
                m.Push(new Vector3(side*4.95f,0,-14.06f));
                m.Box(DeepWood,new Vector3(0,2.53f,0),new Vector3(3.57f,2.36f,.23f));
                m.Box(side<0?0x6c8d83:0xb6b196,new Vector3(0,2.53f,.128f),new Vector3(3.32f,2.10f,.055f));
                m.Box(Honey,new Vector3(0,1.40f,.16f),new Vector3(3.68f,.13f,.45f));
                m.Box(Honey,new Vector3(0,3.73f,.12f),new Vector3(3.68f,.12f,.37f));
                if(side<0)
                {
                    for(int i=0;i<3;i++)
                    {
                        float x=(i-1)*1.02f, y=i==1?2.54f:2.27f;
                        m.Box(Deep,new Vector3(x,1.57f,.20f),new Vector3(.69f,.19f,.33f));
                        m.Box(Straw,new Vector3(x,1.59f,.375f),new Vector3(.38f,.07f,.015f));
                        m.Drum(Chrome,new Vector3(x,1.72f,.21f),new Vector3(.49f,.095f,.32f),16);
                        Tube(m,Straw,new Vector3(x,1.73f,.21f),new Vector3(x,y-.24f,.21f),.105f);
                        m.Plum(i==1?Straw:Chrome,new Vector3(x,y,.21f),new Vector3(.63f,.67f,.32f),16);
                        m.Drum(i==1?Straw:Chrome,new Vector3(x,y+.20f,.21f),new Vector3(.72f,.09f,.37f),20);
                        foreach(int handle in new[]{-1,1})
                        {
                            var outer=new Vector3(x+handle*.39f,y+.09f,.23f);
                            Tube(m,Straw,new Vector3(x+handle*.25f,y+.15f,.23f),outer,.050f);
                            Tube(m,Straw,outer,new Vector3(x+handle*.31f,y-.13f,.23f),.050f);
                        }
                    }
                }
                else
                {
                    m.Box(Linen,new Vector3(0,2.50f,.18f),new Vector3(2.87f,1.73f,.035f));
                    m.Box(0x96b1a6,new Vector3(0,2.54f,.204f),new Vector3(2.61f,1.47f,.018f));
                    for(int i=0;i<7;i++)
                    {
                        float x=i<4?(i-1.5f)*.51f:(i-5)*.58f,y=i<4?2.65f:2.17f;
                        m.Box(i%3==0?Linen:i%3==1?MintEnamel:PeachEnamel,new Vector3(x,y,.235f+i*.004f),new Vector3(.43f,.37f,.036f),.26f);
                        m.Plum(i%2==0?Honey:Straw,new Vector3(x,y+.29f,.254f+i*.004f),new Vector3(.30f,.32f,.060f),10);
                        m.Box(Deep,new Vector3(x-.025f,y+.42f,.276f+i*.004f),new Vector3(.27f,.083f,.048f));
                    }
                }
                m.Pop();
            }
            m.Box(HallTimber,new Vector3(0,3.61f,-14.15f),new Vector3(4.74f,.22f,.22f));
            m.Box(Rail,new Vector3(0,4.04f,-14.09f),new Vector3(4.44f,.68f,.13f));
            for(int i=-2;i<=2;i++)m.Box(Glass,new Vector3(i*.86f,4.05f,-14.0f),new Vector3(.77f,.51f,.055f));
        }

        static void HallFloor(CourtSketchMesh m)
        {
            // Flush inlays carry no colliders. The jog between the rooms stays continuous.
            for(int i=-5;i<=5;i++) m.Box(HallStoneDark,new Vector3(i*2.72f,.002f,-2),new Vector3(.018f,.003f,24.5f));
            for(int i=-5;i<=4;i++) m.Box(HallStoneDark,new Vector3(0,.002f,i*2.42f),new Vector3(29.58f,.003f,.018f));
            m.Ring(HallTimber,new Vector3(0,.009f,-1.35f),1.65f,1.65f,.045f,56);
            m.Ring(HallTimber,new Vector3(0,.010f,-1.35f),1.51f,1.51f,.025f,56);
            // Segment a ball seam in the terrazzo centre mark.
            for(int i=0;i<12;i++)
            {
                float a=(i-6)*.18f, b=a+.18f;
                m.Stick(HallTimber,new Vector3(Mathf.Sin(a)*1.34f,.016f,-1.35f+Mathf.Cos(a)*.60f-.35f),
                    new Vector3(Mathf.Sin(b)*1.34f,.016f,-1.35f+Mathf.Cos(b)*.60f-.35f),.030f,.014f);
            }
            var chips=new System.Random(1978);
            for(int i=0;i<1100;i++)
            {
                float x=(float)chips.NextDouble()*29.2f-14.6f,z=(float)chips.NextDouble()*24-14;
                float s=.012f+(float)chips.NextDouble()*.023f;
                m.Ground(i%4==0?Linen:HallStoneDark,.006f,new Vector2(x,z),new Vector2(x+s*.2f,z+s),new Vector2(x+s*1.6f,z+s*.38f));
            }
            foreach(int side in new[]{-1,1})
            {
                int paint=side<0?MintEnamel:PeachEnamel;
                m.Push(new Vector3(side*9.15f,0,0));
                m.Box(paint,new Vector3(0,.010f,0),new Vector3(7.3f,.015f,6.8f),.055f);
                m.Box(side<0?0x91baaa:0xd5a28a,new Vector3(0,.023f,0),new Vector3(7.06f,.014f,6.56f),.055f);
                foreach(int edge in new[]{-1,1})
                {
                    m.Box(Linen,new Vector3(0,.033f,edge*3.13f),new Vector3(6.66f,.008f,.038f));
                    m.Box(Linen,new Vector3(edge*3.35f,.033f,0),new Vector3(.038f,.008f,6.19f));
                }
                m.Pop();
                float z=-1.35f;
                m.Box(paint,new Vector3(side*3.34f,.012f,z),new Vector3(2.96f,.012f,.115f));
                m.Ground(paint,.022f,new Vector2(side*5.13f,z),new Vector2(side*4.46f,z+.38f),new Vector2(side*4.46f,z-.38f));
            }
        }

        // Club badges are cut enamel. Both designs also read in monochrome from across the hall.
        static void BadgeFace(CourtSketchMesh m,int finish,params Vector2[] outline)
        {
            var points=new Vector3[outline.Length];
            for(int i=0;i<points.Length;i++) points[i]=new Vector3(outline[i].x,outline[i].y,-.055f);
            if(Vector3.Cross(points[1]-points[0],points[2]-points[0]).z>0)System.Array.Reverse(points);
            m.Face(finish,points);
        }
        static void ClubBadge(CourtSketchMesh m,Vector3 center,int team)
        {
            m.Push(center);
            m.Push(Vector3.zero,Quaternion.Euler(-90,0,0));
            m.Drum(Linen,Vector3.zero,new Vector3(1.18f,.07f,1.18f),32);m.Pop();
            if(team==0)
            {
                BadgeFace(m,MintEnamel,new(-.43f,-.12f),new(-.22f,.04f),new(-.06f,.23f),new(.11f,.34f),new(.31f,.33f),new(.13f,.19f),new(.05f,.01f),new(-.13f,-.12f));
                BadgeFace(m,MintEnamel,new(-.35f,-.21f),new(-.13f,-.16f),new(.04f,-.08f),new(.27f,-.03f),new(.43f,-.07f),new(.18f,-.17f),new(-.03f,-.28f),new(-.26f,-.28f));
            }
            else
            {
                BadgeFace(m,PeachEnamel,new(-.44f,-.23f),new(.02f,.34f),new(.40f,-.21f),new(.21f,-.21f),new(.01f,.077f),new(-.24f,-.23f));
                BadgeFace(m,PeachEnamel,new(-.16f,-.25f),new(.02f,-.01f),new(.19f,-.25f));
            }
            m.Pop();
        }
    }
}
