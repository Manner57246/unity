using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeLobbyAction { Start,AddBot,RemoveBot,Duration,Difficulty,Crowd,Drops,Host,Join,OutfitBody,OutfitHat,OutfitSuit }

    // These are actual ball targets. No raycast-to-menu shortcut is used.
    public sealed class DodgeLobbyTarget : MonoBehaviour
    {
        public DodgeLobbyAction Action;
        public TextMesh Label;
        float nextHit;
        public bool Hit(DodgeBall ball)
        {
            if(!DodgeApp.Active || DodgeApp.Instance.Page!=DodgePage.Lobby || !ball.owner || ball.IsEgg || !ball.IsDangerous)return false;
            if(Time.unscaledTime>=nextHit)
            {
                nextHit=Time.unscaledTime+.55f;
                DodgeApp.Instance.HandleLobbyHit(Action,ball.owner);
                DodgeGame.Instance.EmitFoam(transform.position-Vector3.forward*.2f,Vector3.back,7,.8f);
                DodgeGame.Instance.PlayTone(520f,.08f,.065f);
            }
            return true;
        }
    }

    public sealed class DodgeClubhouse : MonoBehaviour
    {
        DodgeApp app;DodgeGame game;
        GameObject lobby,rooms;
        Font font;
        Shader worldTextShader;
        readonly Dictionary<Font,Material> fontMaterials=new Dictionary<Font,Material>();
        Material ink,cream,mint,peach,lilac,dark,floor,white;
        readonly List<Material> materials=new List<Material>();
        readonly Dictionary<DodgeLobbyAction,DodgeLobbyTarget> targets=new Dictionary<DodgeLobbyAction,DodgeLobbyTarget>();
        TextMesh boardStatus,blueRoster,redRoster,blueRest,redRest,blueCourt,redCourt;
        GameObject[] gates=new GameObject[2];
        Collider[] originalBumpers; Renderer[] originalBumperRenderers;
        public const float LobbyZ=-49f;
        public Vector3 LobbySpawn(int slot)
        {
            int team=app.Room.seats[slot].team;
            if(app.Room.seats[slot].kind==DodgeSeatKind.Bot && team>=0)return new Vector3(team==0?-8.5f:8.5f,.05f,LobbyZ+1.8f+(slot%3)*1.3f);
            return new Vector3((slot-2)*1.65f,.05f,LobbyZ-5f);
        }
        public Vector3 RestSpawn(int team,int slot)
        {
            bool bot=app.Room.seats[slot].kind==DodgeSeatKind.Bot;
            int rank=app.Room.seats.Take(slot).Count(s=>s.team==team && s.kind==app.Room.seats[slot].kind);
            float sign=team==1?1f:-1f;
            // Waiting bots stand beside the benches, leaving the central route clear.
            return bot?new Vector3(rank%2==0?-3.2f:3.2f,.05f,sign*DodgeCourt.RestZ(21.5f+(rank/2)*1.8f))
                :new Vector3((rank-1)*2f,.05f,sign*DodgeCourt.RestZ(22.3f));
        }
        public Vector3 CourtSpawn(int team,int slot)=>new Vector3((slot%3-1)*3f,.05f,team==1?10f:-10f);
        public int SideAt(Vector3 p)=>p.y<.8f && Mathf.Abs(p.z-LobbyZ)<3.4f && Mathf.Abs(p.x)>5.5f && Mathf.Abs(p.x)<12.8f ? (p.x<0?0:1):-1;
        public bool InWardrobe(Vector3 p)=>p.x< -8f && p.x> -14.8f && p.z<LobbyZ-4f && p.z>LobbyZ-12.5f;
        public bool InCourt(Vector3 p,int team)=>team>=0 && Mathf.Abs(p.x)<DodgeCourt.PlayWidth && Mathf.Abs(p.z)<DodgeCourt.PlayDepth && p.y<2.5f
            && (team==0?p.z<=-.8f:p.z>=.8f);
        public DodgeLobbyTarget Target(DodgeLobbyAction action)=>targets[action];

        public void Build(DodgeApp owner,DodgeGame source)
        {
            app=owner;game=source;
            worldTextShader=Resources.Load<Shader>("WorldText");
            Font.textureRebuilt+=RefreshFont;
            foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(text.font)text.GetComponent<Renderer>().sharedMaterial=WorldFont(text.font);
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Microsoft YaHei UI","Arial"},64);
            ink=Mat(new Color(.17f,.25f,.29f));cream=Mat(new Color(1f,.96f,.85f));
            mint=Mat(new Color(.42f,.8f,.77f));peach=Mat(new Color(.97f,.66f,.49f));
            lilac=Mat(new Color(.77f,.73f,.88f));dark=Mat(new Color(.23f,.35f,.41f));
            floor=Mat(new Color(.83f,.88f,.81f));white=Mat(new Color(1f,.99f,.94f));
            lobby=new GameObject("07 - Walkable party clubhouse");
            rooms=new GameObject("08 - Team rest rooms and court entrances");
            BuildLobby();BuildRooms();
        }
        Material WorldFont(Font source)
        {
            Material material;
            if(!fontMaterials.TryGetValue(source,out material))
            {
                material=new Material(worldTextShader);material.mainTexture=source.material.mainTexture;
                fontMaterials.Add(source,material);materials.Add(material);
            }
            return material;
        }
        void RefreshFont(Font source)
        {
            Material material;
            if(fontMaterials.TryGetValue(source,out material))material.mainTexture=source.material.mainTexture;
        }
        Material Mat(Color c)
        {
            var m=new Material(game.popMaterial);m.SetColor("_BaseColor",c);m.mainTexture=null;materials.Add(m);return m;
        }
        Transform Part(string name,Transform parent,Vector3 p,Vector3 scale,Material material,bool solid=false,PrimitiveType shape=PrimitiveType.Cube)
        {
            var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(parent,false);
            g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
            if(solid)g.AddComponent<DodgeSurface>();else {g.GetComponent<Collider>().enabled=false;Destroy(g.GetComponent<Collider>());}
            return g.transform;
        }
        TextMesh Text(string name,Transform parent,Vector3 p,string value,float size,Color color,float yaw=0f)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(0f,yaw,0f);
            var t=g.AddComponent<TextMesh>();t.font=font;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;
            t.color=color;t.text=value;t.GetComponent<MeshRenderer>().sharedMaterial=WorldFont(font);return t;
        }
        public TextMesh WorldLabel(string name,Transform parent,Vector3 p,string value,float size,Color color,float yaw=0f)
            =>Text(name,parent,p,value,size,color,yaw);
        void Pillar(Transform parent,Vector3 p,Material material)
        {
            Part("Soft column",parent,p,new Vector3(.65f,3.5f,.65f),material,true,PrimitiveType.Capsule);
        }
        void BuildLobby()
        {
            var root=lobby.transform;root.position=new Vector3(0,0,LobbyZ);
            Part("Clubhouse foundation",root,new Vector3(0,-.45f,-1),new Vector3(33,.8f,30),lilac);
            Part("Clubhouse floor",root,new Vector3(0,-.06f,-1),new Vector3(30,.12f,27),floor,true);
            Part("Back wall",root,new Vector3(0,2.4f,10),new Vector3(30,5,.4f),cream,true);
            Part("Left wall",root,new Vector3(-15,1.6f,-1),new Vector3(.4f,3.4f,27),cream,true);
            Part("Right wall",root,new Vector3(15,1.6f,-1),new Vector3(.4f,3.4f,27),cream,true);
            Part("Entrance bumper",root,new Vector3(0,.6f,-14.4f),new Vector3(30,1.3f,.45f),lilac,true);
            Part("Giant party screen",root,new Vector3(0,3.5f,8.9f),new Vector3(17.4f,6.5f,.5f),dark,true);
            Text("Board title",root,new Vector3(0,5.92f,8.6f),"PARTY CLUB  /  用球选玩法",.115f,Color.white);
            boardStatus=Text("Board rules and instructions",root,new Vector3(0,4.92f,8.57f),"",.065f,Color.white);
            AddTarget(root,DodgeLobbyAction.Start,new Vector3(-5.3f,3.4f,8.5f),new Vector2(4.35f,1.4f),peach,"前往休息间");
            AddTarget(root,DodgeLobbyAction.AddBot,new Vector3(-.2f,3.7f,8.5f),new Vector2(3.9f,.8f),mint,"+ 人机");
            AddTarget(root,DodgeLobbyAction.RemoveBot,new Vector3(4.5f,3.7f,8.5f),new Vector2(3.9f,.8f),lilac,"- 人机");
            AddTarget(root,DodgeLobbyAction.Duration,new Vector3(-.2f,2.65f,8.5f),new Vector2(3.9f,.8f),cream,"");
            AddTarget(root,DodgeLobbyAction.Difficulty,new Vector3(4.5f,2.65f,8.5f),new Vector2(3.9f,.8f),cream,"");
            AddTarget(root,DodgeLobbyAction.Crowd,new Vector3(-3.1f,1.5f,8.5f),new Vector2(5.9f,.8f),cream,"");
            AddTarget(root,DodgeLobbyAction.Drops,new Vector3(3.5f,1.5f,8.5f),new Vector2(5.9f,.8f),cream,"");
            Text("Board how to throw",root,new Vector3(0,.5f,8.5f),"按住右键瞄准 · 左键投球 · 房主砸屏幕选规则",.056f,Color.white);
            for(int team=0;team<2;team++)
            {
                float x=team==0?-9f:9f;var color=team==0?mint:peach;
                Part("Walk here to choose a side",root,new Vector3(x,.018f,0),new Vector3(7,.03f,6.5f),color);
                Part("Team roster board",root,new Vector3(x,2.2f,4.7f),new Vector3(4.7f,3.1f,.2f),cream,true);
                Text("Team name",root,new Vector3(x,3.25f,4.53f),team==0?"薄荷队":"蜜桃队",.13f,ink.color);
                var roster=Text("Selected team members",root,new Vector3(x,2.13f,4.5f),"",.078f,ink.color);
                if(team==0)blueRoster=roster;else redRoster=roster;
                Text("Choose side sign",root,new Vector3(x,.84f,4.5f),"走到这边，就加入这队",.067f,ink.color);
                Pillar(root,new Vector3(x-3.2f,1.5f,4.7f),color);Pillar(root,new Vector3(x+3.2f,1.5f,4.7f),color);
            }
            // A small dressing nook with three ball-selectable outfits.
            Part("Dressing room rug",root,new Vector3(-11.4f,.025f,-8.3f),new Vector3(6.7f,.04f,7.2f),lilac);
            Part("Dressing room back",root,new Vector3(-11.5f,1.8f,-11.8f),new Vector3(6.5f,3.6f,.25f),cream,true);
            Text("Wardrobe entrance sign",root,new Vector3(-11.4f,3.65f,-5.25f),"更衣间",.16f,ink.color);
            for(int i=0;i<3;i++)
            {
                var pos=new Vector3(-13.5f+i*2f,1.55f,-11.55f);
                AddTarget(root,(DodgeLobbyAction)((int)DodgeLobbyAction.OutfitBody+i),pos,new Vector2(1.8f,1.2f),i==0?mint:i==1?peach:lilac,new[]{"轻装","帽子","运动套装"}[i],180f);
                Part("Wardrobe hanger",root,new Vector3(pos.x,2.6f,-11.5f),Vector3.one*.35f,white,false,PrimitiveType.Sphere);
            }
            Text("Wardrobe instruction",root,new Vector3(-11.5f,.55f,-11.5f),"走进来，用球砸衣柜试穿",.05f,ink.color,180f);
            // Networking uses the same physical targets; only the IP form accepts typing.
            Part("Friends corner rug",root,new Vector3(10.7f,.025f,-8.2f),new Vector3(8,.04f,6.8f),mint);
            Part("Friends terminal",root,new Vector3(10.7f,2,-11.8f),new Vector3(7.5f,4,.3f),cream,true);
            Text("Friends sign",root,new Vector3(10.7f,3.3f,-11.55f),"叫上朋友",.13f,ink.color,180f);
            AddTarget(root,DodgeLobbyAction.Host,new Vector3(8.8f,1.9f,-11.5f),new Vector2(3.25f,1.25f),peach,"开放房间",180f);
            AddTarget(root,DodgeLobbyAction.Join,new Vector3(12.5f,1.9f,-11.5f),new Vector2(3.25f,1.25f),lilac,"加入朋友",180f);
            Text("Friends instruction",root,new Vector3(10.7f,.65f,-11.5f),"用球砸按钮 · Esc 修改名字与端口",.05f,ink.color,180f);
            for(int i=0;i<7;i++)Part("Floor wayfinding",root,new Vector3(0,.013f,-7+i*1.8f),new Vector3(.09f,.015f,.6f),white);
        }
        void AddTarget(Transform root,DodgeLobbyAction action,Vector3 position,Vector2 size,Material material,string label,float yaw=0)
        {
            var part=Part("Ball menu - "+action,root,position,new Vector3(size.x,size.y,.22f),material,true);
            var target=part.gameObject.AddComponent<DodgeLobbyTarget>();target.Action=action;
            target.Label=Text("Option label - "+action,root,position+Vector3.forward*(yaw==0?-.14f:.14f),label,.085f,ink.color,yaw);
            targets.Add(action,target);
        }
        void BuildRooms()
        {
            originalBumpers=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.name=="End bumper").ToArray();
            originalBumperRenderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name=="End bumper").ToArray();
            var roomsRoot=rooms.transform;
            for(int team=0;team<2;team++)
            {
                float sign=team==0?-1:1;
                var root=new GameObject("Team rest room "+team).transform;root.SetParent(roomsRoot,false);root.localPosition=Vector3.forward*sign*DodgeCourt.RoomOffset;
                var c=team==0?mint:peach;float yaw=team==0?180f:0f;
                Part("Rest room floor",root,new Vector3(0,-.06f,sign*21),new Vector3(12,.12f,12),floor,true);
                Part("Court entrance walkway",root,new Vector3(0,-.06f,sign*15),new Vector3(4,.12f,5),c,true);
                Part("Rest room far wall",root,new Vector3(0,2,sign*26.9f),new Vector3(12,4,.3f),cream,true);
                Part("Rest room left wall",root,new Vector3(-6,1.7f,sign*21),new Vector3(.3f,3.4f,12),cream,true);
                Part("Rest room right wall",root,new Vector3(6,1.7f,sign*21),new Vector3(.3f,3.4f,12),cream,true);
                for(int side=-1;side<=1;side+=2)
                {
                    Part("End bumper with doorway",root,new Vector3(side*8.3375f,.5f,sign*14.5f),new Vector3(12.275f,1.1f,.7f),peach,true);
                    Part("Rest room doorway wall",root,new Vector3(side*4.2f,1.6f,sign*17.2f),new Vector3(3.6f,3.2f,.25f),cream,true);
                    Pillar(root,new Vector3(side*2.25f,1.6f,sign*17.2f),c);
                    Part("Rest room bench",root,new Vector3(side*4.4f,.55f,sign*21),new Vector3(1.2f,.35f,3.5f),c,true);
                    for(int k=0;k<2;k++)Part("Bench legs",root,new Vector3(side*4.4f,.2f,sign*(20+k*2)),new Vector3(.7f,.4f,.3f),cream);
                }
                Part("Doorway arch",root,new Vector3(0,3.15f,sign*17.2f),new Vector3(5.2f,.5f,.65f),c,true);
                Text("Court doorway sign",root,new Vector3(0,3.65f,sign*17.72f),"走进球场 →",.1f,ink.color,team==0?0f:180f);
                Text("Rest room title",root,new Vector3(0,4.65f,sign*26.65f),(team==0?"薄荷队":"蜜桃队")+" · 休息间",.10f,ink.color,yaw);
                var status=Text("Rest room readiness",root,new Vector3(0,4.15f,sign*18.4f),"",.055f,ink.color,team==0?0f:180f);
                if(team==0)blueRest=status;else redRest=status;
                DodgeRestTarget.Build(this,root,team);
                var court=Text("Court countdown sign",root,new Vector3(0,3.2f,sign*14.75f),"",.12f,ink.color,yaw);
                if(team==0)blueCourt=court;else redCourt=court;
                gates[team]=Part("Closed during match",root,new Vector3(0,1.6f,sign*14.5f),new Vector3(4.4f,3.2f,.5f),c,true).gameObject;
            }
        }
        void OriginalBarriers(bool enabled)
        {
            foreach(var c in originalBumpers)if(c)c.enabled=enabled;
            foreach(var r in originalBumperRenderers)if(r)r.enabled=enabled;
        }
        public void SetSecurityPassage(int team,bool open)
        {
            if(app.Page==DodgePage.Playing && gates[team])gates[team].SetActive(false);
        }
        public void SetPhase(DodgePage page)
        {
            OriginalBarriers(page==DodgePage.Home || page==DodgePage.Join || page==DodgePage.Lobby);
            if(lobby)lobby.SetActive(page==DodgePage.Lobby || page==DodgePage.Join);
            if(rooms)rooms.SetActive(page!=DodgePage.Home && page!=DodgePage.Join && page!=DodgePage.Lobby);
            foreach(var gate in gates)if(gate)gate.SetActive(page==DodgePage.Results);
        }
        void Update()
        {
            if(!app || !app.Initialized)return;
            if(DodgeApp.BypassForChecks){OriginalBarriers(true);if(lobby)lobby.SetActive(false);if(rooms)rooms.SetActive(false);return;}
            string[] names={"薄荷队","蜜桃队"};
            if(app.Page==DodgePage.Lobby)
            {
                targets[DodgeLobbyAction.Duration].Label.text="时长  "+Mathf.RoundToInt(app.Room.duration/60f)+" 分钟";
                targets[DodgeLobbyAction.Difficulty].Label.text="人机  "+new[]{"轻松","标准","热闹"}[app.Room.difficulty];
                targets[DodgeLobbyAction.Crowd].Label.text="观众干扰  "+(app.Room.crowd?"开启":"关闭");
                targets[DodgeLobbyAction.Drops].Label.text="武器空投  "+(app.Room.drops?"开启":"关闭");
                boardStatus.text=(app.Online?"房主地址 "+app.Net.LocalAddress+" : "+app.Net.Port:"自己的大厅 · 可以先和人机玩")+"\n"+
                    (string.IsNullOrEmpty(app.Message)?"走向两侧选队，更衣间在后方左侧":app.Message);
                blueRoster.text=Roster(0);redRoster.text=Roster(1);
            }
            if(app.Page==DodgePage.Staging || app.Page==DodgePage.Countdown || app.Page==DodgePage.Playing)
            {
                string status=app.Page==DodgePage.Playing?"比赛中！":app.Page==DodgePage.Countdown?"所有人已入场  "+Mathf.CeilToInt(app.Countdown):"走过门口进入本方半场\n双方全员入场后开始倒计时";
                blueRest.text=status;redRest.text=status;
                string count=app.Page==DodgePage.Countdown?Mathf.CeilToInt(app.Countdown).ToString():app.Page==DodgePage.Playing?"GO!":
                    "已入场 "+app.Room.seats.Count(s=>s.kind!=DodgeSeatKind.Empty && s.inCourt)+" / "+app.Room.seats.Count(s=>s.kind!=DodgeSeatKind.Empty);
                blueCourt.text=count;redCourt.text=count;
            }
            app.PersistLocalOutfit();
        }
        string Roster(int team)
        {
            var people=app.Room.seats.Where(s=>s.kind!=DodgeSeatKind.Empty && s.team==team).Select(s=>s.name+(s.kind==DodgeSeatKind.Bot?" · 人机":""));
            return people.Any()?string.Join("\n",people):"还没人选这边";
        }
        void OnDestroy()
        {
            Font.textureRebuilt-=RefreshFont;
            if(lobby)Destroy(lobby);if(rooms)Destroy(rooms);
            foreach(var m in materials)if(m)Destroy(m);
            if(font)Destroy(font);
        }
    }
}
