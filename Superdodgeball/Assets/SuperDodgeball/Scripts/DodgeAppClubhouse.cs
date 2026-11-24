using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeApp
    {
        readonly float[] sideDwell = new float[5];
        public int TeamScore(int team) => Room.seats.Where(s=>s.kind!=DodgeSeatKind.Empty && s.team==team).Sum(s=>s.score);
        public bool AllOnCourt => Room.seats.Any(s=>s.kind!=DodgeSeatKind.Empty && s.team==0)
            && Room.seats.Any(s=>s.kind!=DodgeSeatKind.Empty && s.team==1)
            && Room.seats.Where(s=>s.kind!=DodgeSeatKind.Empty).All(s=>s.inCourt);
        public bool CanHurt(DodgeActor attacker, DodgeActor target) => Page == DodgePage.Playing && !SameTeam(attacker,target)
            && !DodgeReferee.Restricted(attacker) && !DodgeReferee.Restricted(target);
        public bool SameTeam(DodgeActor a, DodgeActor b)
        {
            int x=SeatOf(a), y=SeatOf(b);
            return x>=0 && y>=0 && Room.seats[x].team>=0 && Room.seats[x].team==Room.seats[y].team;
        }
        public int SmallerTeam() => Room.seats.Count(s=>s.kind!=DodgeSeatKind.Empty && s.team==0)
            <= Room.seats.Count(s=>s.kind!=DodgeSeatKind.Empty && s.team==1) ? 0 : 1;
        public void ChooseSide(int slot,int team)
        {
            if(!IsHost || Page!=DodgePage.Lobby || slot<0 || slot>=5 || team<0 || team>1 || Room.seats[slot].kind==DodgeSeatKind.Empty) return;
            if(Room.seats[slot].team==team) return;
            if(Room.seats.Count(s=>s.kind==DodgeSeatKind.Human && s.team==team)>=3) { Message="这边已经有三位真人了，去另一边看看。"; return; }
            Room.seats[slot].team=team; Room.seats[slot].ready=true;
            // Fill around the human choices; bots never force the last human off a side.
            foreach(var seat in Room.seats.Where(s=>s.kind==DodgeSeatKind.Bot)) seat.team=-1;
            foreach(var seat in Room.seats.Where(s=>s.kind==DodgeSeatKind.Bot)) seat.team=SmallerTeam();
            foreach(int i in Enumerable.Range(0,5).Where(i=>Room.seats[i].kind==DodgeSeatKind.Bot))
                PlaceActor(i,Clubhouse.LobbySpawn(i),0f);
            InvalidateReady(); Message="已加入"+(team==0?"薄荷队":"蜜桃队"); Net.BroadcastLobby();
        }
        public void PlaceActor(int slot,Vector3 position,float yaw)
        {
            var actor=Actors[slot]; var motor=actor.GetComponent<CharacterController>();
            bool enabledBefore=motor.enabled; motor.enabled=false;
            actor.transform.SetPositionAndRotation(position,Quaternion.Euler(0f,yaw,0f));
            actor.Puppet.SnapToPose(); motor.enabled=enabledBefore;
            if(slot==LocalSlot) game.view.SetView(yaw,9f);
        }
        public void BeginParty() => CreateRoom(false);
        public void OpenJoin()
        {
            if(!IsHost || Online) { Message="已在房间中；先回到自己的大厅再加入朋友。";return; }
            Page=DodgePage.Join; Paused=false; Message=""; game.ClearBalls(); game.UnlockInput();
        }
        public void CancelJoin()
        {
            if(IsClient || Connecting) Net.Close();
            Connecting=false; Page=DodgePage.Lobby; Paused=false; Message="";
            ConfigureActors(); game.CaptureInput();
        }
        public void OpenRoomInLobby()
        {
            if(!IsHost || Page!=DodgePage.Lobby) return;
            if(Online) { Message="房间已经开放，把大屏上的地址告诉朋友。";return; }
            if(!int.TryParse(PortText,out int port)||port<1024||port>65535) { Message="请在设置中填写有效端口。";return; }
            try { Net.Host(port); Message="房间已开放！砸减人机按钮给朋友留空位。";Net.BroadcastLobby(); }
            catch(System.Exception) { Net.Close();Message="开房失败，请在设置中更换端口后重试。"; }
        }
        public bool SetOutfit(int slot,int style)
        {
            if(!IsHost || Page!=DodgePage.Lobby || slot<0 || slot>=5 || Room.seats[slot].kind!=DodgeSeatKind.Human || !Clubhouse.InWardrobe(Actors[slot].transform.position)) return false;
            style=((style%3)+3)%3;
            Room.seats[slot].outfit=style; Actors[slot].Puppet.Wardrobe.SetStyle(style); Net.BroadcastLobby();
            return true;
        }
        public void PersistLocalOutfit()
        {
            if(!initialized || Room.seats[LocalSlot].kind!=DodgeSeatKind.Human) return;
            int style=Room.seats[LocalSlot].outfit;
            if(PlayerPrefs.GetInt("MVP.Outfit",-1)==style)return;
            PlayerPrefs.SetInt("MVP.Outfit",style);PlayerPrefs.Save();
        }
        public bool HandleLobbyHit(DodgeLobbyAction action,DodgeActor owner)
        {
            int slot=SeatOf(owner);
            if(!IsHost || Page!=DodgePage.Lobby || slot<0 || Room.seats[slot].kind!=DodgeSeatKind.Human) return false;
            if(action>=DodgeLobbyAction.OutfitBody && action<=DodgeLobbyAction.OutfitSuit)
                return SetOutfit(slot,action-DodgeLobbyAction.OutfitBody);
            if(slot!=0) { Message="比赛规则由房主砸屏幕选择，你可以先选队或换装。"; return false; }
            switch(action)
            {
                case DodgeLobbyAction.Start: return StartMatch();
                case DodgeLobbyAction.AddBot:
                    int free=System.Array.FindIndex(Room.seats,1,s=>s.kind==DodgeSeatKind.Empty);
                    if(free>0)SetBot(free,true);else Message="已经满员了。";
                    break;
                case DodgeLobbyAction.RemoveBot:
                    int bot=System.Array.FindLastIndex(Room.seats,s=>s.kind==DodgeSeatKind.Bot);
                    if(bot>0)SetBot(bot,false);else Message="没有人机可以移除。";
                    break;
                case DodgeLobbyAction.Duration: Room.duration=Room.duration>=300f?60f:Room.duration+60f;RulesChanged();break;
                case DodgeLobbyAction.Difficulty: Room.difficulty=(Room.difficulty+1)%3;RulesChanged();break;
                case DodgeLobbyAction.Crowd: Room.crowd=!Room.crowd;RulesChanged();break;
                case DodgeLobbyAction.Drops: Room.drops=!Room.drops;RulesChanged();break;
                case DodgeLobbyAction.Host: OpenRoomInLobby();break;
                case DodgeLobbyAction.Join: OpenJoin();break;
            }
            return true;
        }
        void UpdateClubhouse()
        {
            PersistLocalOutfit();
            if(!SimulationRunning)return;
            if(Page==DodgePage.Lobby)
            {
                for(int i=0;i<5;i++)
                {
                    if(Room.seats[i].kind!=DodgeSeatKind.Human)continue;
                    int side=Clubhouse.SideAt(Actors[i].transform.position);
                    sideDwell[i]=side<0?0f:sideDwell[i]+Time.deltaTime;
                    if(side>=0 && sideDwell[i]>.25f) ChooseSide(i,side);
                }
            }
            if(Page==DodgePage.Staging || Page==DodgePage.Countdown)
            {
                bool changed=false;
                for(int i=0;i<5;i++)
                {
                    var seat=Room.seats[i];if(seat.kind==DodgeSeatKind.Empty)continue;
                    bool inside=Clubhouse.InCourt(Actors[i].transform.position,seat.team);
                    if(seat.inCourt!=inside) { seat.inCourt=inside; changed=true; }
                }
                if(Page==DodgePage.Staging && AllOnCourt)
                { game.ClearBalls(); Page=DodgePage.Countdown;Countdown=3f;changed=true; }
                if(changed)Net.BroadcastLobby();
            }
        }
        public bool MoveBotBeforeMatch(DodgeBot bot,CharacterController motor)
        {
            if(!Walkable || Page==DodgePage.Playing)return false;
            int slot=SeatOf(bot.Actor);if(slot<0)return true;
            var actor=bot.Actor;var seat=Room.seats[slot];
            Vector3 target=Clubhouse.LobbySpawn(slot);
            if(Page!=DodgePage.Lobby)
            {
                if(Mathf.Abs(actor.transform.position.z)>DodgeCourt.RestZ(20))DodgeRestTarget.BotChoose(bot);
                bool follow=Room.seats.Where(s=>s.kind==DodgeSeatKind.Human && s.team==seat.team).All(s=>s.inCourt);
                target=follow?Clubhouse.CourtSpawn(seat.team,slot):Clubhouse.RestSpawn(seat.team,slot);
                // Walk straight through the doorway first, then fan out on court.
                if(follow && Mathf.Abs(actor.transform.position.z)>DodgeCourt.PlayDepth+.1f)target=new Vector3(0f,.05f,seat.team==0?-13.75f:13.75f);
            }
            var delta=Vector3.ProjectOnPlane(target-actor.transform.position,Vector3.up);
            var velocity=delta.magnitude>.3f?delta.normalized*Mathf.Min(3.4f,delta.magnitude*4f):Vector3.zero;
            motor.height=1.9f;motor.center=Vector3.up*.95f;motor.stepOffset=.2f;
            motor.Move((velocity+Vector3.down*2f)*Time.deltaTime);
            if(velocity.sqrMagnitude>.01f)actor.transform.rotation=Quaternion.RotateTowards(actor.transform.rotation,Quaternion.LookRotation(velocity),360f*Time.deltaTime);
            actor.SetLocomotion(velocity.magnitude/4f);actor.SetMovement(Vector2.up);actor.SetDive(0);actor.SetCharge(0);actor.SetLean(0);actor.SetShape(0,0);
            return true;
        }
    }
}
