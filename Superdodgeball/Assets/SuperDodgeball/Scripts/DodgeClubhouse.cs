using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeLobbyAction { Start,AddBot,RemoveBot,Duration,Difficulty,Crowd,Drops,Host,Join,OutfitBody,OutfitHat,OutfitSuit,Venue }

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

    public sealed partial class DodgeClubhouse : MonoBehaviour
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
        readonly GameObject[] trainingWalls=new GameObject[2], draftWalls=new GameObject[2];
        readonly TextMesh[,] rarityLabels=new TextMesh[2,3];
        readonly TextMesh[,] draftNames=new TextMesh[2,3], draftDetails=new TextMesh[2,3];
        static readonly Color draftInk=new Color(.018f,.03f,.035f);
        readonly TextMesh[] draftHeaders=new TextMesh[2];
        GameObject[] gates=new GameObject[2];
        Collider[] originalBumpers; Renderer[] originalBumperRenderers;
        public const float LobbyZ=-49f;
        public Vector3 LobbySpawn(int slot)
        {
            int team=app.Room.seats[slot].team;
            if(app.Room.seats[slot].kind==DodgeSeatKind.Bot && team>=0)
            {
                int queue=0;
                for(int i=0;i<slot;i++)
                    if(app.Room.seats[i].kind==DodgeSeatKind.Bot && app.Room.seats[i].team==team)queue++;
                return new Vector3(team==0?-12.3f:12.3f,.05f,LobbyZ-2.20f+queue*1.46f);
            }
            int lane = (slot+1)/2;
            return new Vector3((slot%2==1?-1:1)*lane*1.65f,.05f,LobbyZ-5f);
        }
        public Vector3 RestSpawn(int team,int slot)
        {
            bool bot=app.Room.seats[slot].kind==DodgeSeatKind.Bot;
            int rank=app.Room.seats.Take(slot).Count(s=>s.team==team && s.kind==app.Room.seats[slot].kind);
            float sign=team==1?1f:-1f;
            if(!bot && app.Drafting)
            {
                int people=app.Room.seats.Count(s=>s.team==team && s.kind==DodgeSeatKind.Human);
                return new Vector3((rank-(people-1)*.5f)*1.85f,.05f,sign*DodgeCourt.RestZ(23.65f));
            }
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
            font=DodgeApp.CreateUiFont(64);
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
        void AddTarget(Transform root,DodgeLobbyAction action,Vector3 position,Vector2 size,Material material,string label,float yaw=0)
        {
            float front=yaw==0?-1f:1f;
            Part("Enamel plate rim",root,position-Vector3.forward*front*.058f,new Vector3(size.x+.105f,size.y+.095f,.18f),RoomFinish(Rail));
            var part=Part("Ball menu - "+action,root,position,new Vector3(size.x,size.y,.22f),material,true);
            var target=part.gameObject.AddComponent<DodgeLobbyTarget>();target.Action=action;
            target.Label=Text("Option label - "+action,root,position+Vector3.forward*(yaw==0?-.14f:.14f),label,action==DodgeLobbyAction.Start?.107f:.080f,draftInk,yaw);
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
                Part("Rest room floor",root,new Vector3(0,-.06f,sign*21),new Vector3(2*DodgeCourt.RestHalfWidth,.12f,12),floor,true);
                Part("Court entrance walkway",root,new Vector3(0,-.06f,sign*15),new Vector3(4,.12f,5),c,true);
                Part("Rest room far wall",root,new Vector3(0,2,sign*26.9f),new Vector3(2*DodgeCourt.RestHalfWidth,4,.3f),cream,true);
                Part("Rest room left wall",root,new Vector3(-DodgeCourt.RestHalfWidth,1.7f,sign*21),new Vector3(.3f,3.4f,12),cream,true);
                Part("Rest room right wall",root,new Vector3(DodgeCourt.RestHalfWidth,1.7f,sign*21),new Vector3(.3f,3.4f,12),cream,true);
                for(int side=-1;side<=1;side+=2)
                {
                    Part("End bumper with doorway",root,new Vector3(side*8.3375f,.5f,sign*14.5f),new Vector3(12.275f,1.1f,.7f),peach,true);
                    Part("Rest room doorway wall",root,new Vector3(side*(4.2f+DodgeCourt.RestSideExtension*.5f),1.6f,sign*17.2f),new Vector3(3.6f+DodgeCourt.RestSideExtension,3.2f,.25f),cream,true);
                    Pillar(root,new Vector3(side*2.25f,1.6f,sign*17.2f),c);

                }
                FurnishRestRoom(root,team,sign);
                Part("Doorway arch",root,new Vector3(0,3.15f,sign*17.2f),new Vector3(5.2f,.5f,.65f),c,true);
                Text("Court doorway sign",root,new Vector3(0,3.22f,sign*17.55f),"球  场",.064f,Color.white,team==0?0f:180f);
                Text("Rest room title",root,new Vector3(0,4.65f,sign*26.65f),(DodgeColleges.Name(team))+" · 休息间",.10f,draftInk,yaw);
                Part("Doorway noticeboard",root,new Vector3(0,3.88f,sign*17.81f),new Vector3(4.50f,.54f,.09f),dark);
                var status=Text("Rest room readiness",root,new Vector3(0,3.88f,sign*17.88f),"",.042f,Color.white,team==0?0f:180f);
                if(team==0)blueRest=status;else redRest=status;
                trainingWalls[team]=new GameObject("Training signs");
                trainingWalls[team].transform.SetParent(root,false);
                BuildEquipmentBoard(root,trainingWalls[team].transform,team);
                BuildDraftWall(root,team,sign,yaw);
                var court=Text("Court countdown sign",root,new Vector3(0,3.2f,sign*14.75f),"",.12f,ink.color,yaw);
                if(team==0)blueCourt=court;else redCourt=court;
                gates[team]=Part("Closed during match",root,new Vector3(0,1.6f,sign*14.5f),new Vector3(4.4f,3.2f,.5f),c,true).gameObject;
            }
        }
        void ShowTrainingWalls()
        {
            for(int team=0;team<2;team++)
            {
                bool drafting=app.Drafting;
                trainingWalls[team].SetActive(!drafting);
                draftWalls[team].SetActive(drafting);
                if(!drafting)continue;
                var seat=app.Room.seats[app.LocalSlot];
                bool own=seat.team==team && seat.offer!=null && seat.offer.Length==3;
                string prompt=!own?"球队战术":seat.pick<0?"瞄准墙牌，投球选一张":"已选「"+DodgeTactics.Names[seat.offer[seat.pick]]+"」 · 转身进球场";
                draftHeaders[team].text="队费 "+app.Room.funds[team]+" 币  ·  "+prompt+(app.Room.rerolled[team]?" · 已刷新":"");
                for(int i=0;i<3;i++)
                {
                    draftNames[team,i].text=own?DodgeTactics.Names[seat.offer[i]]:"战术牌";
                    rarityLabels[team,i].text=own?DodgeTactics.Rarity[seat.offer[i]]:"";
                    rarityLabels[team,i].color=own?DodgeTactics.Ink[seat.offer[i]]:draftInk;
                    draftDetails[team,i].text=own?DodgeTactics.Details[seat.offer[i]]:"对方球队";
                    for(int k=0;k<DodgeTactics.Names.Length;k++)
                        tacticSketches[team,i,k].SetActive(own && seat.offer[i]==k);
                    Color lettering=own && seat.pick==i ? new Color(.07f,.29f,.18f) : draftInk;
                    draftNames[team,i].color=lettering;
                }
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
            ShowTrainingWalls();
            foreach(var gate in gates)if(gate)gate.SetActive(page==DodgePage.Results);
        }
        void Update()
        {
            if(!app || !app.Initialized)return;
            if(DodgeApp.BypassForChecks){OriginalBarriers(true);if(lobby)lobby.SetActive(false);if(rooms)rooms.SetActive(false);return;}
            if(app.Page==DodgePage.Lobby)
            {
                targets[DodgeLobbyAction.Duration].Label.text="时长  "+Mathf.RoundToInt(app.Room.duration/60f)+" 分钟";
                targets[DodgeLobbyAction.Difficulty].Label.text="人机  "+new[]{"轻松","标准","热闹"}[app.Room.difficulty];
                targets[DodgeLobbyAction.Crowd].Label.text="观众干扰  "+(app.Room.crowd?"开启":"关闭");
                targets[DodgeLobbyAction.Venue].Label.text="地图  "+DodgeVenueDirector.Title(app.Room.venue);
                targets[DodgeLobbyAction.Drops].Label.text="雷云补给  "+(app.Room.drops?"开启":"关闭");
                boardStatus.text=(app.Online?"房主地址 "+app.Net.LocalAddress+" : "+app.Net.Port:"自己的大厅 · 可以先和人机玩")+"\n"+
                    (string.IsNullOrEmpty(app.Message)?"走向两侧选队，更衣间在后方左侧":app.Message);
                RefreshRoster(blueRoster,0);
                RefreshRoster(redRoster,1);
                int localTeam=app.Room.seats[app.LocalSlot].team;
                if(indicatedTeam!=localTeam)
                {
                    indicatedTeam=localTeam;
                    for(int i=0;i<2;i++)checkInLamps[i].sharedMaterial=RoomFinish(i==localTeam?Sun:Steel);
                }
            }
            if(app.Page==DodgePage.Staging || app.Page==DodgePage.Countdown || app.Page==DodgePage.Playing)
            {
                string status=app.Page==DodgePage.Playing?"比赛中！":app.Page==DodgePage.Countdown?"所有人已入场  "+Mathf.CeilToInt(app.Countdown):"全员进入本方半场后开赛";
                if(app.Page==DodgePage.Staging)status=DodgeVenueDirector.Title(app.CurrentVenue)+" · "+status;
                blueRest.text=status;redRest.text=status;
                string count=app.Page==DodgePage.Countdown?Mathf.CeilToInt(app.Countdown).ToString():app.Page==DodgePage.Playing?"GO!":
                    "已入场 "+app.Room.seats.Count(s=>s.kind!=DodgeSeatKind.Empty && s.inCourt)+" / "+app.Room.seats.Count(s=>s.kind!=DodgeSeatKind.Empty);
                blueCourt.text=count;redCourt.text=count;
            }
            app.PersistLocalOutfit();
        }
        void RefreshRoster(TextMesh label,int team)
        {
            string names=Roster(team);
            if(label.text==names)return;
            label.text=names;
            label.characterSize=.061f;
            var bounds=label.GetComponent<MeshRenderer>().localBounds;
            label.characterSize*=Mathf.Min(1f,4.18f/Mathf.Max(.01f,bounds.size.x),1.59f/Mathf.Max(.01f,bounds.size.y));
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
            foreach(var mesh in roomMeshes)if(mesh)Destroy(mesh);
            if(font)Destroy(font);
        }
    }
}
