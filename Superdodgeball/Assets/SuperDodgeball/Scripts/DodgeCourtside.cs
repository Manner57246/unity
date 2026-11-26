using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeClubhouse
    {
        internal void BuildCourtside(Transform stadium)
        {
            for(int side=-1;side<=1;side+=2)
            {
                int kit=side<0?Blue:Rust;
                var end=SideFrame(stadium,"End line equipment "+side,0);
                end.localPosition=Vector3.forward*side*19.46f;
                end.localRotation=Quaternion.Euler(0,side<0?180:0,0);
                var m=new CourtSketchMesh();
                const float deckHeight=.52f;
                // Keep both work areas clear of the outer faces of the locker-room walls.
                const float deskX=-10.05f,trolleyX=8.65f,waterX=11.72f;
                for(int wing=-1;wing<=1;wing+=2)
                {
                    Vector3 landing=new Vector3(wing*8.35f,deckHeight/2,.07f);
                    var extent=new Vector3(9.7f,deckHeight,2.24f);
                    m.Box(DeepWood,landing,extent);
                    Block(end,"Raised equipment landing",landing,extent);
                    for(int panel=0;panel<6;panel++)
                    {
                        float x=wing*(3.24f+2.04f*panel);
                        m.Box(kit,new Vector3(x,.54f,-1.704f),new Vector3(2.005f,.95f,.067f));
                        m.Box(Linen,new Vector3(x,.88f,-1.745f),new Vector3(2.005f,.040f,.022f));
                    }
                    m.Box(Deep,new Vector3(wing*8.34f,1.055f,-1.35f),new Vector3(12.27f,.085f,.73f));
                }
                m.Push(new Vector3(0,deckHeight,0));
                m.Push(new Vector3(deskX,0,-.05f));
                TimekeepersTable(m,kit);m.Pop();
                m.Push(new Vector3(trolleyX,0,.05f),-9);
                BallTrolley(m,kit);m.Pop();
                m.Push(new Vector3(waterX,0,.1f),4);
                DrinksBench(m,kit);m.Pop();m.Pop();
                for(int x=-1;x<=1;x+=2)
                {
                    m.Push(new Vector3(x*13.62f,5.75f,1.36f));
                    Speaker(m);m.Pop();
                    // Enamel kick plates and a chair rail on the hall side of the dressing rooms.
                    m.Box(kit,new Vector3(x*4.72f,.77f,1.19f),new Vector3(4.37f,1.45f,.045f));
                    m.Box(Honey,new Vector3(x*4.72f,1.52f,1.15f),new Vector3(4.37f,.085f,.07f));
                    m.Box(Rail,new Vector3(x*4.15f,1.93f,1.64f),new Vector3(.11f,3.81f,.11f));
                    for(int k=0;k<4;k++)m.Box(Steel,new Vector3(x*4.15f,.53f+k*.91f,1.565f),new Vector3(.16f,.04f,.085f));
                }
                roomMeshes.Add(m.BakeSurfaces("Match desk, ball cage and drinks station",end,RoomFinish));
                WorldLabel("Officials desk lettering",end,new Vector3(deskX,deckHeight+.75f,-.573f),"记  录  台",.068f,new Color(.94f,.91f,.80f));
                Block(end,"Officials desk",new Vector3(deskX,deckHeight+.64f,-.05f),new Vector3(3.7f,1.28f,1.00f));
                Block(end,"Ball trolley",new Vector3(trolleyX,deckHeight+.74f,.05f),new Vector3(2.04f,1.48f,.95f),-9);
                Block(end,"Water bench",new Vector3(waterX,deckHeight+.48f,.10f),new Vector3(2.72f,.91f,.88f),4);
                var flags=new CourtSketchMesh();
                for(int wing=-1;wing<=1;wing+=2)
                {
                    flags.Push(new Vector3(wing*12.05f,12.20f,1.29f));
                    CollegeStandard(flags,side<0,false);flags.Pop();
                    flags.Push(new Vector3(wing*17.85f,11.55f,1.29f));
                    CollegeStandard(flags,side<0,true);flags.Pop();
                }
                roomMeshes.Add(flags.BakeSurfaces((side<0?DodgeColleges.Cape:DodgeColleges.Ridge)+" · end wall standards",end,RoomFinish));
                var bank=SideFrame(stadium,"Grandstand colours "+side,0);
                var trim=new CourtSketchMesh();
                for(int n=0;n<3;n++)
                {
                    float z=-12+n*12;
                    trim.Push(new Vector3(side*21.64f,6.95f,z),side>0?90:-90);
                    Banner(trim,kit,n);trim.Pop();
                }
                float boundary=11.5f*DodgeCourt.Scale-.35f*DodgeCourt.Scale;
                for(int k=0;k<12;k++)
                {
                    float z=-16.5f+3*k;
                    trim.Box(0x294a4c,new Vector3(side*(boundary-.015f),.56f,z),new Vector3(.038f,.93f,.041f));
                    trim.Box(Steel,new Vector3(side*(boundary-.044f),.37f,z),new Vector3(.027f,.12f,.16f));
                }
                roomMeshes.Add(trim.BakeSurfaces("Supporters pennants and barrier fastenings",bank,RoomFinish));
            }
        }

        static void TimekeepersTable(CourtSketchMesh m,int kit)
        {
            m.Box(Wood,new Vector3(0,1.26f,0),new Vector3(3.82f,.15f,1.07f));
            m.Box(kit,new Vector3(0,.75f,-.48f),new Vector3(3.69f,.83f,.07f));
            m.Box(Linen,new Vector3(0,.42f,-.525f),new Vector3(3.54f,.035f,.027f));
            for(int s=-1;s<=1;s+=2)
            {
                m.Box(Steel,new Vector3(s*1.69f,.63f,.04f),new Vector3(.09f,1.17f,.08f));
                m.Box(Rail,new Vector3(s*1.69f,.085f,.04f),new Vector3(.17f,.11f,.89f));
                m.Push(new Vector3(s*.97f,0,.94f),s*4.5f);
                for(int i=-1;i<=1;i+=2)
                {
                    Tube(m,Rail,new Vector3(i*.37f,.04f,-.27f),new Vector3(i*.37f,1.28f,.25f),.069f);
                    Tube(m,Rail,new Vector3(i*.37f,.04f,.32f),new Vector3(i*.37f,.56f,-.28f),.069f);
                }
                m.Box(Honey,new Vector3(0,.54f,0),new Vector3(.83f,.095f,.62f));
                m.Box(kit,new Vector3(0,1.10f,.22f),new Vector3(.83f,.36f,.076f));m.Pop();
            }
            m.Box(Deep,new Vector3(-1.25f,1.37f,-.08f),new Vector3(.55f,.10f,.39f));
            Tube(m,Chrome,new Vector3(-1.28f,1.4f,-.06f),new Vector3(-1.20f,1.79f,-.08f),.040f);
            m.Plum(Deep,new Vector3(-1.19f,1.86f,-.09f),new Vector3(.14f,.22f,.14f));
            m.Push(new Vector3(.05f,1.35f,0),9);
            m.Box(DeepWood,Vector3.zero,new Vector3(.78f,.045f,.53f));
            m.Box(Linen,new Vector3(0,.036f,0),new Vector3(.70f,.029f,.47f));
            for(int i=0;i<4;i++)m.Box(Steel,new Vector3(.03f,.055f,i*.065f-.10f),new Vector3(.45f,.008f,.011f));m.Pop();
            m.Drum(Deep,new Vector3(1.19f,1.39f,-.05f),new Vector3(.39f,.10f,.39f));
            m.Plum(Straw,new Vector3(1.19f,1.48f,-.05f),new Vector3(.33f,.23f,.33f),16);
            m.Drum(Chrome,new Vector3(1.19f,1.63f,-.05f),new Vector3(.07f,.08f,.07f));
        }
        static void BallTrolley(CourtSketchMesh m,int kit)
        {
            m.Box(Rail,new Vector3(0,.31f,0),new Vector3(2.09f,.12f,1.03f));
            for(int s=-1;s<=1;s+=2)for(int e=-1;e<=1;e+=2)
            {
                m.Push(new Vector3(s*.86f,.17f,e*.38f),Quaternion.Euler(0,0,90));
                m.Drum(Deep,Vector3.zero,new Vector3(.30f,.10f,.30f));m.Drum(Steel,new Vector3(0,s*.063f,0),new Vector3(.14f,.028f,.14f));m.Pop();
                Tube(m,Chrome,new Vector3(s*.99f,.31f,e*.47f),new Vector3(s*.99f,1.51f,e*.47f),.063f);
            }
            for(int e=-1;e<=1;e+=2)
            {
                Tube(m,kit,new Vector3(-1.02f,1.50f,e*.47f),new Vector3(1.02f,1.50f,e*.47f),.084f);
                for(int i=0;i<9;i++)
                    m.Box(Steel,new Vector3((i-4)*.215f,.92f,e*.472f),new Vector3(.028f,1.10f,.028f));
                for(int i=0;i<4;i++)m.Box(Steel,new Vector3(0,.48f+i*.27f,e*.47f),new Vector3(1.98f,.026f,.026f));
                for(int j=0;j<3;j++)m.Box(Steel,new Vector3(e*.99f,.90f,(j-1)*.24f),new Vector3(.028f,1.14f,.028f));
            }
            for(int j=0;j<2;j++)for(int i=0;i<3;i++)
            {
                var p=new Vector3((i-1)*.63f+(j==0?0:.075f),.67f+j*.53f,(i%2==0?.08f:-.08f)-j*.036f);
                m.Plum(i==0 && j==1?Honey:0xcd6147,p,new Vector3(.65f,.64f,.65f),16);
                m.Push(p,Quaternion.Euler(i*15+10,0,18+j*19));m.Ring(Linen,Vector3.zero,.333f,.333f,.021f,26);m.Pop();
            }
            for(int e=-1;e<=1;e+=2)Tube(m,Chrome,new Vector3(1.0f,1.49f,e*.46f),new Vector3(1.17f,1.70f,e*.46f),.060f);
            Tube(m,Rail,new Vector3(1.17f,1.70f,-.46f),new Vector3(1.17f,1.70f,.46f),.087f);
        }
        static void DrinksBench(CourtSketchMesh m,int kit)
        {
            for(int i=-1;i<=1;i++)m.Box(i==0?Honey:Wood,new Vector3(0,.85f,i*.27f),new Vector3(2.73f,.10f,.25f));
            for(int s=-1;s<=1;s+=2)for(int e=-1;e<=1;e+=2)
                m.Stick(Rail,new Vector3(s*1.12f,.05f,e*.35f),new Vector3(s*1.06f,.82f,e*.24f),.07f);
            m.Box(kit,new Vector3(-.57f,1.34f,0),new Vector3(.97f,.88f,.67f),.26f);
            m.Box(Linen,new Vector3(-.57f,1.81f,0),new Vector3(1.02f,.14f,.73f),.30f);
            m.Box(Rail,new Vector3(-.57f,1.11f,-.36f),new Vector3(.16f,.14f,.10f));
            m.Box(Chrome,new Vector3(-.57f,1.09f,-.438f),new Vector3(.057f,.08f,.09f));
            FoldedTowel(m,new Vector3(.70f,.91f,.02f),.72f,.46f,Linen,kit);
            for(int j=0;j<3;j++)m.Drum(Linen,new Vector3(.17f,1.04f+j*.058f,-.16f),new Vector3(.19f,.23f,.19f));
        }
        static void Banner(CourtSketchMesh m,int kit,int index)
        {
            const float w=1.16f;
            Tube(m,Chrome,new Vector3(-1.46f,1.48f,0),new Vector3(1.46f,1.48f,0),.072f);
            for(int x=-1;x<=1;x+=2)m.Box(Linen,new Vector3(x*.87f,1.37f,-.06f),new Vector3(.18f,.22f,.05f));
            m.Face(kit,new Vector3(-w,1.3f,-.09f),new Vector3(w,1.3f,-.09f),new Vector3(w,-.95f,-.07f),
                new Vector3(.12f,-1.70f,-.15f),new Vector3(-w,-1.01f,-.11f));
            Quad(m,Linen,0,1.11f,2.30f,.095f,-.102f);
            m.Stick(Straw,new Vector3(-w,-.80f,-.132f),new Vector3(.12f,-1.48f,-.165f),.074f,.026f);
            m.Stick(Straw,new Vector3(.12f,-1.48f,-.165f),new Vector3(w,-.79f,-.13f),.074f,.026f);
            m.Push(new Vector3(0,.14f,-.14f),Quaternion.Euler(-90,0,0));
            m.Drum(Linen,Vector3.zero,new Vector3(.93f,.035f,.93f),32);
            m.Pop();
            m.Push(new Vector3(0,.14f,-.174f),Quaternion.Euler(0,0,index%2==0?24:-24));
            m.Stick(kit,new Vector3(-.42f,-.13f,0),new Vector3(.42f,.13f,0),.057f,.018f);m.Pop();
        }
        // Local origin is the hanging rod. Negative Z faces the court at either end.
        static void CollegeStandard(CourtSketchMesh m,bool cape,bool swallowtail)
        {
            int dye=cape?0x226968:0xa34736;
            int shade=cape?0x164e55:0x763837;
            int gold=0xdfb35e;
            float half=swallowtail?1.38f:2.48f;
            float height=swallowtail?4.90f:6.55f;
            Tube(m,DeepWood,new(-half-.24f,0,0),new(half+.24f,0,0),.12f,12);
            for(int side=-1;side<=1;side+=2)
            {
                m.Plum(gold,new Vector3(side*(half+.25f),0,0),Vector3.one*.19f,10);
                m.Box(Rail,new Vector3(side*(half-.16f),0,.18f),new Vector3(.10f,.16f,.40f));
            }
            for(int i=-1;i<=1;i++)
                m.Box(swallowtail?Linen:dye,new Vector3(i*half*.77f,-.105f,-.028f),new Vector3(.19f,.29f,.10f));

            // A broad pressed centre carries the crest. The hems turn forward in shallow folds.
            Vector3 Cloth(float x,float t)
            {
                float edge=Mathf.Abs(x);
                float length=swallowtail?height*(.80f+.20f*edge):height*(1-.18f*edge);
                float fold=edge<.68f?0:edge<.89f?(edge-.68f)*.82f:(1-edge)*1.55f;
                return new Vector3(x*half,-.22f-length*t,-.12f-fold*(.45f+.55f*t));
            }
            float[] seams={-1f,-.94f,-.89f,-.68f,-.12f,.12f,.68f,.89f,.94f,1f};
            for(int i=0;i<seams.Length-1;i++)
            {
                float x=seams[i],right=seams[i+1],mid=Mathf.Abs((x+right)*.5f);
                int field=swallowtail?(mid<.12f || mid>.94f?dye:Linen):(mid>.94f?shade:mid>.89f?gold:dye);
                void Band(float top,float bottom,int ink)
                {
                    m.Face(ink,Cloth(x,top),Cloth(right,top),Cloth(right,bottom),Cloth(x,bottom));
                }
                Band(0,.045f,swallowtail?dye:shade);
                Band(.045f,.065f,gold);
                Band(.065f,.838f,field);
                Band(.838f,.862f,swallowtail?dye:Linen);
                Band(.862f,.915f,field);
                Band(.915f,.937f,gold);
                Band(.937f,1,field);
            }
            if(swallowtail)return;
            // The same seabird over a wave and split mountain used on the athletics signs.
            m.Push(new Vector3(0,-2.95f,-.138f));
            void Patch(params Vector2[] outline)
            {
                // The bird's swept wing has a concave leading edge. Fan from inside the patch.
                Vector2 centre=Vector2.zero;
                foreach(var point in outline)centre+=point;
                centre/=outline.Length;
                for(int i=0;i<outline.Length;i++)
                    m.Face(Linen,centre,outline[i],outline[(i+1)%outline.Length]);
            }
            if(cape)
            {
                Patch(new(-1.48f,-.41f),new(-.70f,.08f),new(-.30f,.74f),new(.40f,1.18f),new(1.22f,1.03f),new(.64f,.52f),new(.35f,-.04f),new(-.36f,-.42f));
                Patch(new(-1.28f,-.73f),new(-.64f,-.57f),new(.15f,-.17f),new(.90f,-.01f),new(1.47f,-.20f),new(.65f,-.55f),new(-.13f,-.98f),new(-.95f,-1.00f));
            }
            else
            {
                Patch(new(-1.48f,-.83f),new(.05f,1.26f),new(.05f,.32f),new(-.70f,-.82f));
                Patch(new(.05f,1.26f),new(1.46f,-.81f),new(.70f,-.81f),new(.05f,.32f));
                Patch(new(-.55f,-.89f),new(.05f,-.12f),new(.62f,-.89f));
            }
            m.Pop();
        }

        static void Speaker(CourtSketchMesh m)
        {
            m.Box(Rail,new Vector3(0,0,.08f),new Vector3(.76f,1.31f,.42f));
            for(int i=0;i<2;i++)
            {
                float size=i==0?.53f:.29f;
                m.Push(new Vector3(0,i==0?-.22f:.34f,-.15f),Quaternion.Euler(-90,0,0));
                m.Drum(Deep,Vector3.zero,new Vector3(size,.075f,size),20);
                m.Drum(Steel,new Vector3(0,.045f,0),new Vector3(size*.45f,.024f,size*.45f),20);m.Pop();
            }
        }
    }
}
