using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeClubhouse
    {
        void BuildMatchDesk(Transform hall)
        {
            const float z=8.90f;
            var m=new CourtSketchMesh();
            m.Push(new Vector3(0,0,z));
            m.Box(DeepWood,new Vector3(0,3.47f,0),new Vector3(15.75f,6.32f,.44f),.055f);
            m.Box(Deep,new Vector3(0,3.49f,-.23f),new Vector3(15.36f,5.99f,.12f),.03f);
            for(int i=0;i<18;i++)
                m.Box(i%3==0?HallTimber:Wood,new Vector3((i-8.5f)*.846f,.34f,-.26f),new Vector3(.813f,.50f,.12f));
            m.Box(Honey,new Vector3(0,6.61f,-.015f),new Vector3(16.04f,.17f,.69f));
            m.Box(HallTimber,new Vector3(0,.68f,-.31f),new Vector3(15.72f,.13f,.27f));
            m.Box(MintEnamel,new Vector3(0,5.84f,-.33f),new Vector3(15.14f,1.0f,.075f));
            m.Box(Linen,new Vector3(0,5.33f,-.37f),new Vector3(14.98f,.039f,.04f));
            // Clock housing and the narrow louvres balance the enamel fascia.
            m.Push(new Vector3(-6.65f,5.86f,-.42f));
            Clock(m,Vector3.zero);m.Pop();
            for(int i=0;i<5;i++)m.Box(Deep,new Vector3(6.58f,5.62f+i*.104f,-.40f),new Vector3(.80f,.045f,.035f));
            m.Pop();
            roomMeshes.Add(m.BakeSurfaces("Oak and enamel match control board",hall,RoomFinish));
            Block(hall,"Match control board",new Vector3(0,3.45f,z),new Vector3(15.7f,6.25f,.46f));
            Text("Board title",hall,new Vector3(0,5.87f,8.45f),"比赛登记处",.136f,Color.white);
            boardStatus=Text("Board rules and instructions",hall,new Vector3(0,4.85f,8.49f),"",.053f,Color.white);
            AddTarget(hall,DodgeLobbyAction.Start,new Vector3(-4.95f,3.10f,8.46f),new Vector2(4.40f,1.72f),RoomFinish(Straw),"前往休息间");
            AddTarget(hall,DodgeLobbyAction.AddBot,new Vector3(-.25f,3.64f,8.46f),new Vector2(3.83f,.77f),RoomFinish(MintPale),"+ 人机");
            AddTarget(hall,DodgeLobbyAction.RemoveBot,new Vector3(4.55f,3.64f,8.46f),new Vector2(3.83f,.77f),RoomFinish(PeachPale),"− 人机");
            AddTarget(hall,DodgeLobbyAction.Duration,new Vector3(-.25f,2.58f,8.46f),new Vector2(3.83f,.77f),RoomFinish(Linen),"");
            AddTarget(hall,DodgeLobbyAction.Difficulty,new Vector3(4.55f,2.58f,8.46f),new Vector2(3.83f,.77f),RoomFinish(Linen),"");
            AddTarget(hall,DodgeLobbyAction.Venue,new Vector3(4.55f,1.47f,8.46f),new Vector2(3.83f,.80f),RoomFinish(Straw),"");
            AddTarget(hall,DodgeLobbyAction.Crowd,new Vector3(-4.95f,1.47f,8.46f),new Vector2(4.40f,.80f),RoomFinish(Linen),"");
            AddTarget(hall,DodgeLobbyAction.Drops,new Vector3(-.25f,1.47f,8.46f),new Vector2(3.83f,.80f),RoomFinish(Linen),"");
            Text("Board how to throw",hall,new Vector3(0,.89f,8.47f),"右键瞄准  ·  左键投球选规则",.044f,Color.white);
        }

        void BuildCheckIn(Transform hall,int team)
        {
            int side=team==0?-1:1;
            int enamel=team==0?MintEnamel:PeachEnamel, pale=team==0?MintPale:PeachPale;
            var bay=SideFrame(hall,team==0?"Mint team check-in":"Peach team check-in",side*10.04f);
            bay.localPosition+=Vector3.forward*4.52f;
            var m=new CourtSketchMesh();
            // The roster is a cream pinboard within a timber frame on real supports.
            for(int post=-1;post<=1;post+=2)
            {
                float postX=post*2.88f;
                m.Box(HallTimber,new Vector3(postX,2.29f,.22f),new Vector3(.26f,4.56f,.32f));
                m.Box(Rail,new Vector3(postX,.21f,.22f),new Vector3(.38f,.42f,.43f));
                m.Stick(Honey,new Vector3(postX,3.99f,.20f),new Vector3(postX,4.49f,-.49f),.12f,.12f);
                m.Box(Chrome,new Vector3(postX,.33f,-.009f),new Vector3(.09f,.07f,.014f));
            }
            m.Box(DeepWood,new Vector3(0,2.19f,.14f),new Vector3(5.87f,3.18f,.27f));
            m.Box(Linen,new Vector3(0,2.22f,-.019f),new Vector3(5.58f,2.87f,.075f));
            m.Box(enamel,new Vector3(0,4.04f,.08f),new Vector3(6.06f,1.36f,.34f));
            m.Box(pale,new Vector3(0,3.405f,-.105f),new Vector3(5.85f,.050f,.032f));
            m.Box(Honey,new Vector3(0,4.80f,-.08f),new Vector3(6.32f,.16f,1.12f));
            m.Box(enamel,new Vector3(0,4.91f,-.04f),new Vector3(6.36f,.105f,1.16f));
            ClubBadge(m,new Vector3(-1.98f,4.04f,-.155f),team);
            // Burgundy/mint screenprinted clipboard, paper edge and sprung bulldog clip.
            m.Box(HallTimber,new Vector3(0,2.24f,-.079f),new Vector3(4.70f,2.0f,.053f),.07f);
            m.Box(0xf3ead3,new Vector3(.03f,2.26f,-.116f),new Vector3(4.48f,1.81f,.030f),.025f);
            m.Box(Rail,new Vector3(0,3.13f,-.147f),new Vector3(.74f,.13f,.065f));
            m.Box(Chrome,new Vector3(0,3.21f,-.148f),new Vector3(.40f,.083f,.075f));
            m.Box(Honey,new Vector3(0,.73f,-.11f),new Vector3(5.95f,.12f,.52f));
            m.Box(enamel,new Vector3(0,1.00f,-.084f),new Vector3(4.60f,.33f,.075f));
            m.Box(Rail,new Vector3(2.49f,4.04f,-.108f),new Vector3(.44f,.43f,.10f));
            roomMeshes.Add(m.BakeSurfaces("Team awning, enamel badge and registration sheet",bay,RoomFinish));
            Block(bay,"Check-in board",new Vector3(0,2.58f,.15f),new Vector3(5.99f,4.33f,.34f));
            Text("Team name",bay,new Vector3(.37f,4.05f,-.142f),DodgeColleges.Name(team),.126f,Color.white);
            var roster=Text("Selected team members",bay,new Vector3(0,2.26f,-.145f),"",.064f,draftInk);
            if(team==0)blueRoster=roster;else redRoster=roster;
            Text("Choose side sign",bay,new Vector3(0,1.0f,-.132f),"走入色区即可入队",.040f,Color.white);
            var lens=Part("Your team indicator",bay,new Vector3(2.49f,4.04f,-.18f),new Vector3(.31f,.31f,.12f),RoomFinish(Steel),false,PrimitiveType.Sphere);
            checkInLamps[team]=lens.GetComponent<MeshRenderer>();

            var medallion=new CourtSketchMesh();
            ClubBadge(medallion,Vector3.zero,team);
            var stamp=SideFrame(hall,"Painted team floor badge",side*9.15f);
            stamp.localPosition+=new Vector3(0,.050f,-1.15f);
            stamp.localRotation=Quaternion.Euler(90,0,0);stamp.localScale=new Vector3(1.94f,1.94f,.035f);
            roomMeshes.Add(medallion.BakeSurfaces("Team inlay",stamp,RoomFinish));
            var benches=new CourtSketchMesh();
            float x=side*13.7f;
            Bench(benches,x,.2f,4.5f);
            DrapedTowel(benches,x,-.72f,enamel);
            KitBag(benches,new Vector3(x,.635f,1.38f),enamel);
            if(team==0) BallCart(benches,new Vector3(-13.08f,0,6.68f));
            else
            {
                Bench(benches,13.52f,6.5f,2.75f);
                FoldedTowel(benches,new Vector3(13.50f,.635f,6.94f),.68f,.45f,Linen,enamel);
                Trainers(benches,new Vector3(13.03f,.02f,6.22f),enamel);
            }
            roomMeshes.Add(benches.BakeSurfaces("Waiting bench and team equipment",hall,RoomFinish));
            Block(hall,"Waiting bench",new Vector3(x,.35f,.2f),new Vector3(.79f,.68f,4.52f));
            if(team==0)Block(hall,"Ball cart",new Vector3(-13.08f,.73f,6.68f),new Vector3(1.82f,1.44f,1.03f));
            else Block(hall,"Back bench",new Vector3(13.52f,.31f,6.5f),new Vector3(.8f,.61f,2.75f));
        }

        static void BallCart(CourtSketchMesh m,Vector3 p)
        {
            m.Push(p);
            foreach(int side in new[]{-1,1})
            {
                foreach(int end in new[]{-1,1})
                {
                    float x=side*.86f,z=end*.48f;
                    Tube(m,Rail,new Vector3(x,.23f,z),new Vector3(x,1.36f,z),.065f);
                    m.Plum(Rubber,new Vector3(x,.13f,z),new Vector3(.17f,.22f,.18f),12);
                }
                for(int i=0;i<6;i++)
                {
                    float x=(i-2.5f)*.31f;
                    Tube(m,Steel,new Vector3(x,.41f,side*.48f),new Vector3(x,1.32f,side*.48f),.028f);
                }
                foreach(float y in new[]{.42f,.85f,1.33f})
                {
                    m.Stick(Rail,new Vector3(-.88f,y,side*.48f),new Vector3(.88f,y,side*.48f),.045f);
                    m.Stick(Rail,new Vector3(side*.86f,y,-.48f),new Vector3(side*.86f,y,.48f),.045f);
                }
            }
            m.Box(Steel,new Vector3(0,.36f,0),new Vector3(1.76f,.06f,.98f));
            for(int i=0;i<6;i++)
            {
                int c=i%3==1?Straw:i%3==2?MintEnamel:Orange;
                m.Plum(c,new Vector3((i%3-1)*.53f,.69f+(i/3)*.41f,(i%2-.5f)*.28f),new Vector3(.53f,.54f,.53f),16);
            }
            m.Pop();
        }

        void BuildLobbyServices(Transform hall)
        {
            var m=new CourtSketchMesh();
            foreach(int s in new[]{-1,1})
            {
                float x=s<0?-11.49f:10.65f, w=s<0?6.59f:7.59f;
                int trim=s<0?MintEnamel:PeachEnamel;
                m.Box(Rubber,new Vector3(x,.012f,-8.35f),new Vector3(w,.024f,6.70f),.08f);
                m.Box(s<0?0x9cab9b:0xc2a795,new Vector3(x,.029f,-8.35f),new Vector3(w-.16f,.018f,6.53f),.08f);
                m.Box(DeepWood,new Vector3(x,2.05f,-11.84f),new Vector3(w,4.1f,.31f));
                m.Box(Linen,new Vector3(x,2.01f,-11.65f),new Vector3(w-.20f,3.80f,.075f));
                m.Box(trim,new Vector3(x,3.58f,-11.58f),new Vector3(w-.14f,.78f,.11f));
                m.Box(Honey,new Vector3(x,4.13f,-11.66f),new Vector3(w+.24f,.13f,.65f));
                for(int side=-1;side<=1;side+=2)
                    m.Box(HallTimber,new Vector3(x+side*(w*.5f-.09f),1.75f,-11.58f),new Vector3(.14f,3.49f,.17f));
                Block(hall,"Service board",new Vector3(x,2.02f,-11.83f),new Vector3(w,4.08f,.32f));
            }
            // Visible samples over the three wardrobe targets: a vest, cap and team jersey.
            for(int i=0;i<3;i++)
            {
                float x=-13.5f+2*i;
                m.Push(new Vector3(x,0,-11.46f));
                Tube(m,Chrome,new Vector3(-.74f,3.04f,0),new Vector3(.74f,3.04f,0),.054f);
                m.Stick(HallTimber,new Vector3(-.32f,2.70f,0),new Vector3(0,2.86f,0),.052f);
                m.Stick(HallTimber,new Vector3(.32f,2.70f,0),new Vector3(0,2.86f,0),.052f);
                Tube(m,Chrome,new Vector3(0,2.84f,0),new Vector3(0,3.04f,0),.032f);
                if(i==1)
                {
                    m.Plum(PeachEnamel,new Vector3(0,2.57f,.13f),new Vector3(.66f,.42f,.54f),16);
                    m.Box(Straw,new Vector3(0,2.44f,.35f),new Vector3(.67f,.061f,.48f));
                }
                else
                {
                    m.Push(new Vector3(0,.49f,.08f));
                    Jersey(m,i==0?MintEnamel:PeachEnamel);m.Pop();
                }
                m.Pop();
                AddTarget(hall,(DodgeLobbyAction)((int)DodgeLobbyAction.OutfitBody+i),new Vector3(x,1.55f,-11.48f),new Vector2(1.8f,1.07f),RoomFinish(i==1?PeachPale:MintPale),new[]{"轻装","帽子","运动套装"}[i],180f);
            }
            // A gathered canvas privacy curtain hangs at the outer jamb, clear of the fitting area.
            for(int i=0;i<8;i++)m.Box(i%2==0?MintEnamel:Tile,new Vector3(-14.44f+i*.065f,1.70f,-11.03f+(i%2)*.09f),new Vector3(.10f,2.62f,.18f));
            Text("Wardrobe entrance sign",hall,new Vector3(-11.49f,3.59f,-11.49f),"更衣间",.118f,Color.white,180);
            Text("Wardrobe instruction",hall,new Vector3(-11.49f,.65f,-11.53f),"用球砸衣柜试穿",.050f,draftInk,180);
            // Reception has a ribbed timber front, a brass bell and a coiled desk telephone.
            m.Box(HallTimber,new Vector3(10.62f,.61f,-11.15f),new Vector3(7.31f,1.22f,.95f));
            for(int i=0;i<21;i++)m.Box(Wood,new Vector3(7.10f+i*.35f,.62f,-10.65f),new Vector3(.27f,1.10f,.085f));
            m.Box(Honey,new Vector3(10.62f,1.26f,-11.07f),new Vector3(7.56f,.13f,1.28f));
            m.Box(Deep,new Vector3(10.64f,1.40f,-10.99f),new Vector3(.83f,.15f,.49f));
            m.Plum(MintEnamel,new Vector3(10.64f,1.54f,-11.02f),new Vector3(.92f,.17f,.24f),12);
            for(int s=-1;s<=1;s+=2)m.Plum(MintEnamel,new Vector3(10.64f+s*.37f,1.48f,-11.0f),new Vector3(.25f,.20f,.28f),12);
            m.Drum(Rail,new Vector3(13.75f,1.35f,-10.88f),new Vector3(.44f,.05f,.44f),20);
            m.Plum(Straw,new Vector3(13.75f,1.46f,-10.88f),new Vector3(.38f,.23f,.38f),20);
            m.Drum(Chrome,new Vector3(13.75f,1.59f,-10.88f),new Vector3(.075f,.065f,.075f),10);
            Block(hall,"Friends desk",new Vector3(10.63f,.53f,-11.15f),new Vector3(7.3f,1.06f,.95f));
            Text("Friends sign",hall,new Vector3(10.65f,3.59f,-11.49f),"联机服务台",.113f,Color.white,180);
            AddTarget(hall,DodgeLobbyAction.Host,new Vector3(8.8f,2.28f,-11.48f),new Vector2(3.25f,.91f),RoomFinish(PeachPale),"开放房间",180);
            AddTarget(hall,DodgeLobbyAction.Join,new Vector3(12.5f,2.28f,-11.48f),new Vector2(3.25f,.91f),RoomFinish(MintPale),"加入朋友",180);
            roomMeshes.Add(m.BakeSurfaces("Dressing samples and reception desk",hall,RoomFinish));
        }
    }
}
