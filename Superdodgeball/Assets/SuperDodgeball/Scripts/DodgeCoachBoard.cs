using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeClubhouse
    {
        readonly GameObject[,,] tacticSketches=new GameObject[2,3,DodgeTactics.Names.Length];

        Transform BoardFrame(Transform parent,int team,string label)
        {
            var t=new GameObject(label).transform;t.SetParent(parent,false);
            t.localPosition=new Vector3(0,0,team==0?-26.54f:26.54f);
            t.localRotation=Quaternion.Euler(0, team==0?180:0,0);
            return t;
        }
        static void Quad(CourtSketchMesh mesh,int c,float x,float y,float w,float h,float z)
        {
            mesh.Face(c,new Vector3(x-w/2,y-h/2,z),new Vector3(x-w/2,y+h/2,z),
                new Vector3(x+w/2,y+h/2,z),new Vector3(x+w/2,y-h/2,z));
        }
        void BuildEquipmentBoard(Transform root,Transform training,int team)
        {
            var frame=BoardFrame(root,team,"Coach board and kit pigeonholes");
            for(int i=0;i<2;i++)
            {
                var slot=DodgePartyEffects.FreeSlot(team,i);
                var center=root.InverseTransformPoint(slot); center.y=.032f;
                Part("Equipment mat",root,center,new Vector3(1.84f,.032f,1.26f),RoomFinish(team==0?0x68988b:0xc18b72));
                Part("Mat edge",root,center+new Vector3(0,.019f,team==0?.46f:-.46f),new Vector3(1.68f,.012f,.045f),RoomFinish(Linen));
            }
            var m=new CourtSketchMesh();
            m.Box(DeepWood,new Vector3(0,2.72f,.02f),new Vector3(11.18f,2.96f,.17f));
            Quad(m,0xb58860,0,2.72f,10.99f,2.78f,-.074f);
            Quad(m,Linen,0,3.997f,7.23f,.32f,-.15f);
            for(int i=0;i<2;i++)
                m.Box(Wood,new Vector3(0,1.31f+i*2.83f,-.077f),new Vector3(11.13f,.095f,.12f));
            // Individual pigeonholes sit below the board. The player's ball hits the front of its own bay.
            m.Box(Rail,new Vector3(0,.70f,.048f),new Vector3(10.48f,1.15f,.10f));
            for(int i=0;i<2;i++)m.Box(Wood,new Vector3(0,.14f+i*1.13f,-.30f),new Vector3(10.65f,.10f,.82f));
            const int bays=10;
            const float bayWidth=10.5f/bays;
            for(int i=0;i<=bays;i++)
                m.Box(DeepWood,new Vector3((i-bays*.5f)*bayWidth,.70f,-.30f),new Vector3(.08f,1.13f,.79f));
            roomMeshes.Add(m.BakeSurfaces("Oak frame, cork and equipment shelf",frame,RoomFinish));
            WorldLabel("Locker shop terms",frame,new Vector3(0,1.30f,-.73f),"队伍商店  /  投球购买     ·     地上两把免费   G 拿取   V 丢下",.034f,Color.white);
            string[] stockNames = {"礼炮","保龄球","球棒","大锤","火箭筒","定时弹","鸭背心","钢卷","大药丸","注射器"};
            for(int i=0;i<bays;i++)
            {
                float x=(i-(bays-1)*.5f)*bayWidth;
                var weapon=i<8?(DodgeWeapon)(i+1):DodgeWeapon.Standard;
                var item=i==8?DodgeItem.ThunderPill:DodgeItem.SprintShot;
                var obj=i<8?DodgePartyEffects.WorldWeapon(frame,weapon):DodgePartyVisuals.Item(frame,item);
                obj.localPosition=new Vector3(x,.73f,-.35f);
                obj.localRotation=Quaternion.Euler(0,32, i<8?38:0);
                obj.localScale*=i<8?.53f:.68f;
                int price=i<8?DodgeApp.Price(weapon):2;
                Part("Slot label",frame,new Vector3(x,.30f,-.73f),new Vector3(.98f,.28f,.035f),RoomFinish(Linen));
                WorldLabel("Stock name",frame,new Vector3(x,.30f,-.76f),stockNames[i]+" "+price,.030f,draftInk);
                var hit=RestHit(frame,"Shop target "+team+" "+i,new Vector3(x,.70f,-.71f),new Vector3(bayWidth-.07f,1.11f,.16f),team);
                hit.StockWeapon=weapon; hit.Item=i<8?DodgeItem.None:item;
            }
        }
        static DodgeRestTarget RestHit(Transform parent,string label,Vector3 p,Vector3 size,int team)
        {
            var g=new GameObject(label,typeof(BoxCollider),typeof(DodgeSurface),typeof(DodgeRestTarget));
            g.transform.SetParent(parent,false);g.transform.localPosition=p;g.GetComponent<BoxCollider>().size=size;
            var hit=g.GetComponent<DodgeRestTarget>();hit.Team=team;return hit;
        }
        Transform ClipPage(Transform frame,int team,string label,float x,float y,float w,float h,int i)
        {
            var page=RestHit(frame,label,new Vector3(x,y,-.18f),new Vector3(w,h,.11f),team).transform;
            page.localRotation=Quaternion.Euler(0,0,i%3==0?-1.6f:i%3==1?.7f:-.55f);
            var m=new CourtSketchMesh();float l=-w/2,r=w/2,t=h/2,b=-h/2;
            Quad(m,0x947b59,.032f,-.025f,w,h,-.015f);
            m.Face(0xf8f0d9,new Vector3(l,b,-.04f),new Vector3(l,t,-.04f),new Vector3(r,t,-.04f),
                new Vector3(r,b+.17f,-.04f),new Vector3(r-.15f,b,-.04f));
            m.Face(0xdeceac,new Vector3(r-.15f,b,-.041f),new Vector3(r,b+.17f,-.041f),new Vector3(r-.15f,b+.165f,-.057f));
            Quad(m,0xd7c8aa,0,.04f,w*.82f,.014f,-.062f);
            m.Box(Chrome,new Vector3(0,t-.038f,-.074f),new Vector3(.39f,.082f,.047f));
            m.Box(Rail,new Vector3(0,t+.012f,-.080f),new Vector3(.19f,.081f,.043f));
            roomMeshes.Add(m.BakeSurfaces("Clipped coaching sheet",page,RoomFinish));return page;
        }
        void BuildDraftWall(Transform room,int team,float sign,float yaw)
        {
            var wall=BoardFrame(room,team,"Half-time tactics");draftWalls[team]=wall.gameObject;
            draftHeaders[team]=Text("Draft instruction",wall,new Vector3(0,3.997f,-.21f),"",.039f,draftInk);
            for(int i=0;i<3;i++)
            {
                var page=ClipPage(wall,team,"Tactic target "+team+" "+i,(i-1)*3.53f,2.61f,3.22f,2.05f,i);
                page.GetComponent<DodgeRestTarget>().DraftSlot=i;
                draftNames[team,i]=Text("Tactic name",page,new Vector3(.35f,.47f,-.084f),"",.059f,draftInk);
                rarityLabels[team,i]=Text("Tactic rarity",page,new Vector3(-1.08f,.20f,-.085f),"",.025f,draftInk);
                draftDetails[team,i]=Text("Tactic effect",page,new Vector3(0,-.39f,-.084f),"",.046f,draftInk);
                for(int k=0;k<DodgeTactics.Names.Length;k++)
                {
                    var art=new CourtSketchMesh();
                    art.Push(new Vector3(-1.08f,.58f,-.09f));
                    DrawPlay(art,(DodgeTactic)k);art.Pop();
                    var icon=art.BakeSurfaces("Coach sketch "+DodgeTactics.Names[k],page,RoomFinish);
                    roomMeshes.Add(icon);
                    var go=page.Find(icon.name).gameObject;tacticSketches[team,i,k]=go;go.SetActive(false);
                }
            }
            var reroll = ClipPage(wall,team,"Refresh cards "+team,4.80f,3.91f,1.05f,.50f,0);
            reroll.GetComponent<DodgeRestTarget>().Refresh = true;
            WorldLabel("Refresh price",reroll,new Vector3(0,0,-.083f),"刷新 · 1币",.034f,draftInk);
            wall.gameObject.SetActive(false);
        }
        static void DrawPlay(CourtSketchMesh m,DodgeTactic tactic)
        {
            const int pen=0x366c66,red=0xa84d38;
            void Line(float x,float y,float u,float v,int c=pen)
                =>m.Stick(c,new Vector3(x,y,0),new Vector3(u,v,0),.038f,.016f);
            void Dot(float x,float y,float radius,int c)
            {
                m.Push(new Vector3(x,y,-.012f),Quaternion.Euler(-90,0,0));
                m.Drum(c,Vector3.zero,new Vector3(radius*2,.018f,radius*2),12);m.Pop();
            }
            if(tactic==DodgeTactic.BankShot || tactic==DodgeTactic.Skipper)
            {
                float s=tactic==DodgeTactic.BankShot?1:-1;
                Line(-.32f,-.16f*s,-.01f,.20f*s);Line(-.01f,.20f*s,.32f,-.10f*s,red);
                Line(-.31f,.25f*s,.28f,.25f*s);Dot(.29f,-.09f*s,.059f,red);
            }
            else if(tactic==DodgeTactic.DodgeDouble)
            {Line(-.31f,-.05f,.26f,.19f);Line(-.31f,-.05f,.29f,-.18f,red);Dot(.29f,.20f,.077f,pen);Dot(.31f,-.19f,.077f,red);}
            else if(tactic==DodgeTactic.SmallTeam)
            {Dot(-.17f,.13f,.14f,pen);Dot(.19f,-.10f,.10f,red);Line(-.06f,.13f,.20f,.03f);Line(.20f,.03f,.15f,.13f);}
            else if(tactic==DodgeTactic.PocketDecoy)
            {Dot(0,.18f,.115f,pen);Line(-.22f,-.19f,-.13f,.01f);Line(-.13f,.01f,.13f,.01f);Line(.13f,.01f,.22f,-.19f);Line(-.22f,-.19f,.22f,-.19f,red);}
            else
            {Line(-.20f,.11f,-.14f,-.19f);Line(-.14f,-.19f,.14f,-.19f);Line(.14f,-.19f,.20f,.11f);Line(-.24f,.11f,.24f,.11f);Line(.07f,.12f,.17f,.33f,red);}
        }
    }
}
