#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    // Explicit development-build command-line exercise of the real LAN flow.
    public sealed partial class DodgeMvpSmoke : MonoBehaviour
    {
        [Serializable] sealed class Report {public bool passed;public string role,failure;public List<string> checks=new List<string>();}
        readonly Report report=new Report();
        DodgeApp App=>DodgeApp.Instance;
        readonly HashSet<int> stadiumCounts=new HashSet<int>();
        bool seriesCheck, sawClientHit, sawVestReturn, sawSidelineCoil, sawPaidPill, sawPaidShot, sawBulk, sawSprint;
        bool sawRedHalf,sawPursuit,sawEscort,sawReturn,sawGuardMove;
        Vector3 firstGuardPosition;
        bool zeusTossed, sawZeusGift, sawZeusHover, sawZeusImpact, sawZeusShock;
        int checkedLeg, lastResult, previousTactics, previousSkills;
        bool refereePrepared,spectatorSeen;
        bool partyItemSent,partyUsed,partyItemSeen,partyDecoySeen,partyBombSeen,partyBlastSeen,partyUpgradeSeen;
        string folder,role;int step,foulAttempts;float began,playingAt,nextFire,phaseAt,nextState,nextItemUse;bool done,departed,stagingSeen,countdownSeen,eggSent,soapSent,eggSeen,soapSeen,outfitSeen,diveSent,diveSeen,resultsSeen,refWarningSeen,refWatchingSeen,refFoulSeen,guardsSeen,releaseSeen;
        public void Initialize(string value,string[] args)
        {
            venueCheck=Array.IndexOf(args,"--check-venues")>=0;
            armoryCheck=Array.IndexOf(args,"--check-armory")>=0;role=value;report.role=role;seriesCheck=Array.IndexOf(args,"--check-series")>=0;
            string path=Array.Find(args,a=>a.StartsWith("--dodge-report="));
            folder=path==null?"work/mvp-smoke":path.Substring("--dodge-report=".Length);
            Directory.CreateDirectory(folder);began=Time.realtimeSinceStartup;
            App.PortText="17877";App.PlayerName=role=="host"?"派对房主":role=="client1"?"薄荷朋友":"蜜桃朋友";
            if(role=="host")
            {
                Check(App.Page==DodgePage.Home,"Independent executable starts on the minimal home");
                Check(App.CreateRoom(true),"Host opens a real network clubhouse");
                App.SetBot(3,true);App.SetBot(4,true);App.Room.duration=armoryCheck?90f:seriesCheck?5f:30f;
                App.Room.crowd=false;App.Room.drops=false;
                App.PlaceActor(0,new Vector3(-9,.05f,DodgeClubhouse.LobbyZ),0);
                File.WriteAllText(Path.Combine(folder,"host-ready.txt"),"ready");
            }
            else {PlayerPrefs.SetInt("MVP.Outfit",0);App.JoinAddress="127.0.0.1";App.JoinRoom();App.Net.AutomationInput=Input;}
        }
        DodgeInput Toward(Vector3 target)
        {
            var position=App.Actors[App.LocalSlot].transform.position;
            Vector3 delta=Vector3.ProjectOnPlane(target-position,Vector3.up);
            float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            return new DodgeInput{move=delta.magnitude>.4f?new Vector2(0,Mathf.Min(1,delta.magnitude)):Vector2.zero,yaw=yaw,pitch=8,aim=position+Quaternion.Euler(0,yaw,0)*Vector3.forward*10f};
        }
        DodgeInput AimAtSign(DodgeActor actor,Vector3 point)
        {
            Vector3 delta=point-actor.transform.position;
            var shot=new DodgeInput{yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,aim=point};
            if(Time.unscaledTime>nextFire){nextFire=Time.unscaledTime+1f;shot.pressed=DodgeButtons.Fire;}
            return shot;
        }
        DodgeInput Input()
        {
            if(!App.Initialized || App.LocalSlot<0)return default;
            var actor=App.Actors[App.LocalSlot];
            if(venueCheck && App.Page!=DodgePage.Lobby)return VenueInput(actor);
            if(armoryCheck && App.Page!=DodgePage.Lobby) return ArmoryInput();
            if(App.Page==DodgePage.Lobby)
            {
                if(role=="client2" && App.Room.seats[App.LocalSlot].outfit!=2)
                {
                    Vector3 changing=new Vector3(-11f,.05f,DodgeClubhouse.LobbyZ-8f);
                    if(Vector3.Distance(actor.transform.position,changing)>.6f)return Toward(changing);
                    var point=App.Clubhouse.Target(DodgeLobbyAction.OutfitSuit).transform.position;
                    Vector3 delta=point-actor.transform.position;
                    var input=new DodgeInput{yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,pitch=0,aim=point};
                    if(Time.unscaledTime>nextFire){nextFire=Time.unscaledTime+1f;input.pressed=DodgeButtons.Fire;}
                    return input;
                }
                return Toward(new Vector3(role=="client1"?-9:9,.05f,DodgeClubhouse.LobbyZ));
            }
            if(App.Page==DodgePage.Staging)
            {
                int team=App.Room.seats[App.LocalSlot].team;
                var seat=App.Room.seats[App.LocalSlot];
                if(App.Drafting && seat.pick<0)
                {
                    int slot=App.LocalSlot%3;
                    var card=FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None).First(t=>t.Team==team && t.DraftSlot==slot);
                    return AimAtSign(actor,card.transform.position);
                }
                if(seriesCheck && role!="host" && App.Room.leg==3 && actor.Kit.Item==DodgeItem.None)
                {
                    var wanted=role=="client1"?DodgeItem.ThunderPill:DodgeItem.SprintShot;
                    var shelf=FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None).First(t=>t.Team==team && t.Item==wanted);
                    return AimAtSign(actor,shelf.transform.position);
                }
                if(Mathf.Abs(actor.transform.position.z)>13f && Mathf.Abs(actor.transform.position.x)>.5f)
                    return Toward(new Vector3(0,.05f,actor.transform.position.z));
                return Toward(new Vector3(0,.05f,team==0?-10:10));
            }
            if(App.Page!=DodgePage.Playing)return default;
            if(seriesCheck && App.Room.leg==1)
            {
                if(role!="client1" || !actor.Zeus.Transformed) return default;
                int victim=Array.FindIndex(App.Room.seats,s=>s.name=="蜜桃朋友");
                Vector3 target=App.Actors[victim].Puppet.BodyCenter;
                Vector3 delta=target-actor.transform.position;
                return new DodgeInput {aim=target,yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,held=DodgeButtons.Aim,
                    pressed=App.Controllers[App.LocalSlot].ReplicaCharge>.82f?DodgeButtons.Fire:0};
            }
            if(seriesCheck && App.Room.leg==3)
                return new DodgeInput{pressed=DodgePartyKit.Regulated(actor.Kit.Item)?DodgeButtons.UseItem:0};
            if(seriesCheck && App.Room.leg==2)
            {
                var buttons=role=="client2"?DodgeButtons.Collect:
                    actor.Arsenal.WearingVest?DodgeButtons.Detonate:0;
                return new DodgeInput{pressed=buttons,aim=actor.transform.position+actor.transform.forward*6+Vector3.up};
            }
            var enemy=App.Actors.FirstOrDefault(a=>a.gameObject.activeSelf && !App.SameTeam(actor,a));
            if(!enemy)return default;
            Vector3 aim=enemy.transform.position+Vector3.up*1.2f;Vector3 difference=aim-actor.transform.position;
            var frame=new DodgeInput{yaw=Mathf.Atan2(difference.x,difference.z)*Mathf.Rad2Deg,pitch=8,aim=aim,held=DodgeButtons.Aim};
            float elapsed=Time.unscaledTime-playingAt;
            if(actor.Kit.Item==DodgeItem.Decoy && Time.unscaledTime>=nextItemUse){frame.pressed|=DodgeButtons.UseItem;nextItemUse=Time.unscaledTime+.25f;}
            if(elapsed>9.05f && elapsed<9.65f && App.LocalSlot==2)
            {
                if(actor.Arsenal.Selected==DodgeWeapon.Bomb && Time.unscaledTime>=nextFire){nextFire=Time.unscaledTime+1f;frame.pressed|=DodgeButtons.Fire;}
                return frame;
            }
            if(elapsed>9.7f && elapsed<11.2f && App.LocalSlot==2)
            {
                var audience=FindFirstObjectByType<DodgeCrowd>();var point=audience.Spectators[App.Room.seats[App.LocalSlot].team==0?0:11].AimPosition;var d=point-actor.transform.position;
                var shot=new DodgeInput{yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,pitch=0,aim=point,held=DodgeButtons.Aim};
                if(Time.unscaledTime>=nextFire){nextFire=Time.unscaledTime+1;shot.pressed=DodgeButtons.Standard|DodgeButtons.Fire;}
                return shot;
            }
            if(elapsed>8f)return new DodgeInput{pressed=frame.pressed&DodgeButtons.UseItem};
            if(role=="client1" && elapsed>1.4f && !diveSent){diveSent=true;frame.pressed=DodgeButtons.Dive;frame.move=Vector2.right;frame.held=0;}
            if(Time.unscaledTime>=nextFire && (role!="client1" || elapsed<1f || elapsed>2.7f)){nextFire=Time.unscaledTime+.95f;frame.pressed|=DodgeButtons.Fire;}
            return frame;
        }
        void Update()
        {
            if(done)return;
            try
            {
                if(Time.realtimeSinceStartup-began>80f)throw new Exception("Timed out in clubhouse lifecycle, page "+App.Page+", step "+step);
                if(venueCheck){CheckVenues();return;}
                if(armoryCheck){CheckArmory();return;}
                if(seriesCheck){CheckSeries();return;}
                if(App.Stadium && App.Page==DodgePage.Countdown && App.Stadium.CountdownVisible)stadiumCounts.Add(App.Stadium.CountdownNumber);
                var audience=FindFirstObjectByType<DodgeCrowd>();
                if(audience && audience.SpectatorHits>0 && audience.Spectators.Any(f=>f.Reacting && Vector3.Distance(f.BodyPosition,f.transform.TransformPoint(Vector3.up*.98f))>.04f))spectatorSeen=true;
                if(role=="host")HostStep();else ClientStep();
            }
            catch(Exception e){report.failure=e.ToString();Finish();}
        }
        void HostStep()
        {
            if(App.Walkable){App.SetPaused(false);App.Controllers[0].RemoteControlled=true;}
            if(App.Page==DodgePage.Staging)App.Controllers[0].RemoteInput=Input();
            if(Time.unscaledTime>nextState)
            {
                nextState=Time.unscaledTime+3f;
                File.WriteAllText(Path.Combine(folder,"host-state.txt"),App.Page+" / "+step+" / "+Time.realtimeSinceStartup+"\n"+string.Join("\n",App.Room.seats.Select((s,i)=>i+" "+s.name+" team="+s.team+" court="+s.inCourt+" pos="+App.Actors[i].transform.position+" input="+App.Controllers[i].RemoteInput.move+" yaw="+App.Controllers[i].RemoteInput.yaw).ToArray()));
            }
            if(step==0 && App.Net.ConnectedHumans==3 && App.CanStart && App.Room.seats.Any(s=>s.name=="蜜桃朋友" && s.outfit==2))
            {
                Check(App.Room.seats.Count(s=>s.kind==DodgeSeatKind.Human)==3,"Two independent clients join the walkable clubhouse");
                Check(App.Room.seats.Where(s=>s.kind==DodgeSeatKind.Human).All(s=>s.team>=0),"Both clients choose sides using real remote movement");
                Check(App.Room.seats.Any(s=>s.name=="蜜桃朋友" && s.outfit==2),"A remote ball selects clothing inside the dressing room");
                var target=App.Clubhouse.Target(DodgeLobbyAction.Start);
                DodgeBall.Spawn(target.transform.position+Vector3.back,Vector3.forward*15,App.Actors[0],false,DodgeGame.Instance.ballMaterial);
                step=1;
            }
            if(App.Page==DodgePage.Staging && step==1)
            {
                Check(App.Room.seats.Where(s=>s.kind==DodgeSeatKind.Human).All(s=>Mathf.Abs(App.Actors[Array.IndexOf(App.Room.seats,s)].transform.position.z)>20f),"Depart sends every human to their team rest room");
                Check(App.Countdown==3f,"Rest rooms hold the countdown until players actually enter court");step=2;
            }
            if(App.Page==DodgePage.Countdown && step==2){Check(App.AllOnCourt,"Humans and bots walk through both doors before 3-2-1");step=3;}
            if(App.Page==DodgePage.Playing && step==3){playingAt=Time.unscaledTime;step=4;}
            if(step==4)
            {
                if(!partyItemSent)
                {
                    partyItemSent=true;
                    var receiver=App.Actors[1];
                    DodgeAirdrop.Spawn(DodgeGame.Instance,receiver.transform.position,DodgeWeapon.Standard,true,DodgeGiftKind.Skill,DodgeItem.None,DodgeSkill.QuickPitch);
                    var gift=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(2,0,-4),DodgeWeapon.Standard,true,DodgeGiftKind.Item,DodgeItem.Decoy);
                    DodgeBall.Spawn(gift.AimPosition,Vector3.forward,App.Actors[1],false,DodgeGame.Instance.ballMaterial);
                    var weaponGift=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(-2,0,4),DodgeWeapon.Bomb,true);
                    DodgeBall.Spawn(weaponGift.AimPosition,Vector3.forward,App.Actors[2],false,DodgeGame.Instance.ballMaterial);
                }
                partyDecoySeen|=DodgePartyEffects.Instance.Active.Any(e=>e.kind==DodgeEffect.Decoy && e.owner==1);
                partyBombSeen|=DodgePartyEffects.Instance.Active.Any(e=>e.kind==DodgeEffect.Bomb && e.owner==2);
                partyBlastSeen|=DodgePartyEffects.Instance.Explosions>0;
                float refereeElapsed=Time.unscaledTime-playingAt;
                // Keep the later referee exercise free of incidental ragdoll hits / grace periods.
                // The opening eight seconds above still exercise real combat, items and bombs.
                if(refereeElapsed>9f && !refereePrepared)
                {
                    refereePrepared=true;DodgeGame.Instance.ClearBalls();DodgePartyEffects.Instance.ResetEffects();
                    for(int i=0;i<5;i++)
                    {
                        var bot=App.Actors[i].GetComponent<DodgeBot>();if(bot)bot.enabled=false;
                        App.Controllers[i].CancelForReferee();App.Actors[i].Puppet.ResetPuppet();
                        App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),App.Room.seats[i].team==0?0:180);
                    }
                    // Acquire again through the real gift path after random combat, then exercise input in a quiet window.
                    var item=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(2,0,-4),DodgeWeapon.Standard,true,DodgeGiftKind.Item,DodgeItem.Decoy);
                    DodgeBall.Spawn(item.AimPosition,Vector3.forward,App.Actors[1],false,DodgeGame.Instance.ballMaterial);
                    var weapon=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(-2,0,4),DodgeWeapon.Bomb,true);
                    DodgeBall.Spawn(weapon.AimPosition,Vector3.forward,App.Actors[2],false,DodgeGame.Instance.ballMaterial);
                }
                if(refereeElapsed>11.5f && foulAttempts==0 && !App.Referee.IsRestricted(1))
                {
                    typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Watching,null);
                    typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,25f,null);
                    App.PlaceActor(1,new Vector3(-5,.05f,App.Room.seats[1].team==0?2:-2),0);
                    foulAttempts++;
                }
                guardsSeen|=App.Referee.GuardRemaining(1)>0;
                releaseSeen|=guardsSeen && App.Referee.GuardRemaining(1)<=0 && !App.Referee.IsRestricted(1);
                diveSeen|=App.Actors.Any(a=>a.transform.position.y>.35f && a.DiveAmount>.5f);
                if(!eggSent && Time.unscaledTime-playingAt>3f){eggSent=true;DodgeBall.SpawnEgg(App.Actors[1].Puppet.BodyPath[3],Vector3.forward,App.Actors[1]);}
                if(!soapSent && Time.unscaledTime-playingAt>5f)
                {
                    soapSent=true;App.Actors[1].CrowdStatus.Wash();
                    var drop=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(2,0,0),DodgeWeapon.Bowling,true);
                    DodgeBall.Spawn(drop.AimPosition,Vector3.forward,App.Actors[2],false,DodgeGame.Instance.ballMaterial);
                }
                if(App.Page==DodgePage.Results)
                {
                    Check(spectatorSeen,"Remote player hits a spectator with a real owned ball and activates host physics");
                    Check(App.Stadium && stadiumCounts.Count==3 && App.Stadium.ScoresMatch,"Host stadium screens show 3-2-1 and authoritative scores");
                    Check(partyDecoySeen && partyBombSeen && partyBlastSeen,"Remote F deploys a decoy and remote fire throws a timed bomb with authoritative detonation");
                    Check(App.Actors[1].Kit.Skills!=0 && App.Actors[2].Kit.Skills!=0,"Remote players purchase real rest-room upgrades");
                    Check(App.Controllers[1].ShotsFired>0 && App.Controllers[2].ShotsFired>0,"Both remote players throw owned balls after the court countdown");
                    Check(diveSeen,"Authoritative simulation retains the airborne dive");
                    Check(App.TeamScore(0)+App.TeamScore(1)>0,"Team collisions generate shared scores");
                    Check(App.Referee.GuardCalls>0 && guardsSeen && releaseSeen,"Host judges sustained remote trespass and completes a security escort");
                    Check(!string.IsNullOrEmpty(App.WinnerText),"The match ends at team results");phaseAt=Time.unscaledTime;step=5;
                }
            }
            if(step==5 && Time.unscaledTime-phaseAt>2f){App.BackToLobby();Check(App.Page==DodgePage.Lobby,"Rematch returns to the same physical clubhouse");phaseAt=Time.unscaledTime;step=6;}
            if(step==6 && Time.unscaledTime-phaseAt>3f){App.ReturnHome("");Check(!App.Net.Online,"Host exit closes the network room");report.passed=true;Finish();}
        }
        void CheckSeries()
        {
            bool host=role=="host";
            if(host && App.Walkable)
            {
                App.SetPaused(false);
                App.Controllers[0].RemoteControlled=true;
                if(App.Page==DodgePage.Staging)App.Controllers[0].RemoteInput=Input();
            }
            if(host && App.Page==DodgePage.Lobby && lastResult==0)
            {
                if(Time.unscaledTime>nextState)
                {
                    nextState=Time.unscaledTime+.5f;
                    File.WriteAllText(Path.Combine(folder,"host-state.txt"),string.Join("\n",App.Room.seats.Select((s,i)=>i+" "+s.name+" slot")));
                }
                if(App.Net.ConnectedHumans==3 && App.CanStart)Check(App.StartMatch(),"Host starts a three-round match");
            }
            // Exercise one confirmed client-to-client hit before unrelated combat begins.
            if(App.Page==DodgePage.Playing && App.Room.leg==1)
            {
                int victim=Array.FindIndex(App.Room.seats,s=>s.name=="蜜桃朋友");
                var hud=DodgeGame.Instance.GetComponent<DodgeHud>();
                if(host && checkedLeg==0)
                {
                    // Extend this round so both peers can watch the live capture and release.
                    App.ApplyClock(19f,0);
                    typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Watching,null);
                    typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,30f,null);
                    App.PlaceActor(victim,new Vector3(0,.05f,App.Room.seats[victim].team==0?4:-4),0);
                    for(int i=3;i<5;i++)App.Actors[i].GetComponent<DodgeBot>().enabled=false;
                    int shooter=Array.FindIndex(App.Room.seats,s=>s.name=="薄荷朋友");
                    App.CreditHit(App.Actors[shooter],App.Actors[victim]);
                    Check(!hud.HasHit(victim),"An uninvolved host sees no hit bubble for a client-to-client hit");
                }
                if(!host && hud.HasHit(victim))sawClientHit=true;
                int home=App.Room.seats[victim].team;
                sawRedHalf|=App.Referee.HalfAlert(1-home)>0 && App.Referee.HalfAlert(home)==0;
                if(App.Referee.Chasing(victim))
                {
                    var guard=GameObject.Find("Security "+victim+" / 0");
                    if(guard)
                    {
                        if(!sawPursuit)firstGuardPosition=guard.transform.position;
                        sawGuardMove|=Vector3.Distance(firstGuardPosition,guard.transform.position)>1;
                        sawPursuit|=!App.Referee.IsRestricted(victim);
                    }
                }
                int zeusSlot=Array.FindIndex(App.Room.seats,s=>s.name=="薄荷朋友");
                var caster=App.Actors[zeusSlot];
                if(host && sawReturn && !zeusTossed)
                {
                    zeusTossed=true;
                    var spot=new Vector3(4,.05f,-5);
                    App.PlaceActor(zeusSlot,spot,0);
                    App.PlaceActor(victim,new Vector3(4,.05f,5),180);
                    DodgeAirdrop.Toss(DodgeGame.Instance,spot+new Vector3(6,4,0),spot,DodgeGiftKind.Zeus,DodgeWeapon.Standard);
                }
                sawZeusGift |= DodgeAirdrop.Active.Any(g=>g.Kind==DodgeGiftKind.Zeus && g.FromCrowd);
                sawZeusHover |= caster.Zeus.Transformed && caster.transform.position.y>1.5f;
                sawZeusImpact |= DodgePartyEffects.Instance.Active.Any(e=>e.kind==DodgeEffect.Thunderstrike);
                sawZeusShock |= App.Actors[victim].Zeus.ShockLeft>0;
                sawEscort|=App.Referee.GuardRemaining(victim)>0;
                sawReturn|=sawEscort && !App.Referee.IsRestricted(victim) && App.Actors[victim].transform.position.z*(home==0?-1:1)>3;
            }
            if(App.Page==DodgePage.Playing && App.Room.leg==2)
            {
                int wearer=Array.FindIndex(App.Room.seats,s=>s.name=="薄荷朋友");
                int collector=Array.FindIndex(App.Room.seats,s=>s.name=="蜜桃朋友");
                if(host && checkedLeg<2)
                {
                    App.Actors[wearer].Arsenal.Equip(DodgeWeapon.Vest);
                    int team=App.Room.seats[collector].team;
                    Vector3 spot=new Vector3((team==0?-1:1)*(DodgeCourt.PlayWidth-2f),.05f,team==0?-5.4f:5.4f);
                    App.PlaceActor(collector,spot,0);
                    App.PlaceActor(0,spot-Vector3.forward*1.45f,0);
                    App.Actors[0].Arsenal.Equip(DodgeWeapon.Coil);
                    Check(DodgePartyEffects.Instance.DropWeapon(App.Actors[0]),"Host drops the physical coil");
                    App.PlaceActor(0,new Vector3(0,.05f,-8),0);
                }
                sawVestReturn|=App.Actors[wearer].Kit.ReturnTime>0 && DodgePartyEffects.Instance.Active.Any(e=>e.kind==DodgeEffect.VestBurst && e.owner==wearer);
                sawSidelineCoil|=App.Actors[collector].Arsenal.Selected==DodgeWeapon.Coil;
            }
            if(App.Room.leg==3 && (App.Page==DodgePage.Staging || App.Page==DodgePage.Countdown || App.Page==DodgePage.Playing))
            {
                int one=Array.FindIndex(App.Room.seats,s=>s.name=="薄荷朋友");
                int two=Array.FindIndex(App.Room.seats,s=>s.name=="蜜桃朋友");
                if(one>=0 && two>=0)
                {
                    var pill=App.Actors[one].Kit;var shot=App.Actors[two].Kit;
                    sawPaidPill|=App.Room.funds[App.Room.seats[one].team]<=5 && pill.Item==DodgeItem.ThunderPill;
                    sawPaidShot|=App.Room.funds[App.Room.seats[two].team]<=5 && shot.Item==DodgeItem.SprintShot;
                    sawBulk|=pill.BulkLeft>0;sawSprint|=shot.SprintLeft>0;
                }
            }
            if(App.Page==DodgePage.Playing && checkedLeg<App.Room.leg)
            {
                int leg=App.Room.leg;
                var seat=App.Room.seats[App.LocalSlot];
                int skills=App.Actors[App.LocalSlot].Kit.Skills;
                Check((skills&previousSkills)==previousSkills,"Spectator skills persist into round "+leg);
                Check(seat.offer!=null && seat.offer.Distinct().Count()==3 && seat.pick>=0 &&
                    seat.tactics==(1<<seat.offer[seat.pick]),"One current-round card from three distinct offers");
                Check(App.Room.seats.Where(s=>s.kind!=DodgeSeatKind.Empty && s.team==seat.team).All(s=>s.tactics==seat.tactics),"Team shares its tactical benefit");
                checkedLeg=leg;previousSkills=skills;previousTactics=seat.tactics;
            }
            if(App.Page==DodgePage.Results && lastResult<App.Room.leg)
            {
                lastResult=App.Room.leg;phaseAt=Time.unscaledTime;
                if(lastResult==1)Check(sawRedHalf && sawPursuit && sawGuardMove && sawEscort && sawReturn,"The invaded floor, free-moving chase, guard travel, physical capture and home release replicate");
                if(lastResult==1)
                {
                    Check(sawZeusGift && sawZeusHover,"Tossed thundercloud, remote pickup and charged levitation replicate");
                    Check(sawZeusImpact && sawZeusShock,"A remote thunderbolt creates the strike and electrifies its actual target");
                }
                if(lastResult==1 && !host)Check(sawClientHit,"The remote scorer and victim receive hit feedback at the victim");
                if(lastResult==2)
                {
                    Check(sawVestReturn,"Remote R triggers the vest and synchronizes its recovery and scatter");
                    Check(sawSidelineCoil,"Remote G claims and synchronizes the sideline coil");
                }
                if(lastResult==3)
                {
                    Check(sawPaidPill && sawPaidShot,"Both clients purchased their selected locker items with a voucher");
                    Check(sawBulk && sawSprint,"The host and clients observe both remotely activated consumable buffs");
                }
                Check(checkedLeg==lastResult,"Played to the end of round "+lastResult);
                if(lastResult==DodgeApp.SeriesLength && host)Check(!App.ContinueSeries(),"Series stops after three rounds");
            }
            if(host && App.Page==DodgePage.Results && Time.unscaledTime-phaseAt>1.2f)
            {
                if(lastResult<DodgeApp.SeriesLength)Check(App.ContinueSeries(),"Host opens next intermission");
                else App.BackToLobby();
            }
            if(lastResult==DodgeApp.SeriesLength && App.Page==DodgePage.Lobby && step==0)
            {
                Check(App.Room.leg==0 && App.Room.seats.All(s=>s.tactics==0 && s.pick<0),"New series clears all tactical builds");
                step=1;phaseAt=Time.unscaledTime;
            }
            if(host && step==1 && Time.unscaledTime-phaseAt>1.5f)App.ReturnHome("");
            if(step==1 && App.Page==DodgePage.Home){report.passed=true;Finish();}
        }
        void ClientStep()
        {
            if(App.Page==DodgePage.Lobby && step==0){Check(App.LocalSlot>0,"Client receives a unique server-assigned human slot");step=1;}
            if(App.Page==DodgePage.Staging && !stagingSeen){stagingSeen=true;Check(Mathf.Abs(App.Actors[App.LocalSlot].transform.position.z)>18f,"Client enters its team rest room");}
            if(App.Page==DodgePage.Countdown)countdownSeen=true;
            if(App.Page==DodgePage.Playing)
            {
                partyUpgradeSeen|=App.Actors[1].Kit.Skills!=0;
                partyItemSeen|=App.Actors[1].Kit.Item==DodgeItem.Decoy;
                partyDecoySeen|=DodgePartyEffects.Instance.Active.Any(e=>e.kind==DodgeEffect.Decoy && e.owner==1);
                partyBombSeen|=DodgePartyEffects.Instance.Active.Any(e=>e.kind==DodgeEffect.Bomb && e.owner==2);
                partyBlastSeen|=DodgePartyEffects.Instance.Explosions>0;
                refWarningSeen|=App.Referee.Phase==DodgeRefereePhase.Warning;
                refWatchingSeen|=App.Referee.Phase==DodgeRefereePhase.Watching;
                refFoulSeen|=App.Referee.Strikes(1)>0;
                guardsSeen|=App.Referee.GuardRemaining(1)>0;
                releaseSeen|=guardsSeen && App.Referee.GuardRemaining(1)<=0 && !App.Referee.IsRestricted(1);
                if(!departed){departed=true;playingAt=Time.unscaledTime;Check(stagingSeen && countdownSeen,"Client sees rest room then shared 3-2-1 before play");}
                eggSeen|=App.Actors[1].CrowdStatus.Obscured;soapSeen|=App.Actors[1].CrowdStatus.SoapRemaining>0;
                outfitSeen|=App.Room.seats.Any(s=>s.outfit==2);diveSeen|=App.Actors.Any(a=>a.transform.position.y>.35f && a.DiveAmount>.5f);
            }
            if(App.Page==DodgePage.Results && !resultsSeen)
            {
                resultsSeen=true;Check(spectatorSeen,"Client receives the spectator hit event and authoritative ragdoll poses");
                Check(App.Stadium && stadiumCounts.Count==3 && App.Stadium.ScoresMatch,"Client stadium screens display the shared countdown and synchronized team scores");
                Check(partyUpgradeSeen && partyDecoySeen && partyBombSeen && partyBlastSeen,"Independent client receives skills, decoy, bomb fuse and explosion snapshots");
                Check(App.Net.SnapshotsReceived>60,"World positions and body poses synchronize across all phases");
                Check(refWarningSeen && refWatchingSeen && refFoulSeen,"Client receives the real referee warning, supervision and foul decisions");
                Check(guardsSeen && releaseSeen,"Client sees security detention and the offender returning to play");
                Check(outfitSeen,"Clothing remains synchronized after team-room transitions");
                Check(diveSeen,"The airborne dive is visible to independent clients");
                Check(eggSeen && soapSeen,"Egg obstruction and washing still synchronize");
                Check(App.TeamScore(0)+App.TeamScore(1)>0 && !string.IsNullOrEmpty(App.WinnerText),"Clients receive team scores and winner");
            }
            if(resultsSeen && App.Page==DodgePage.Lobby && step==1){Check(App.Room.seats[App.LocalSlot].team>=0,"Returning to clubhouse preserves the selected team");step=2;}
            if(step==2 && App.Page==DodgePage.Home){Check(!App.Net.Online && !string.IsNullOrEmpty(App.Message),"A closing host cleanly returns clients home");report.passed=true;Finish();}
        }
        void Check(bool value,string name){if(!value)throw new Exception(name);report.checks.Add(name);}
        void Finish(){if(done)return;done=true;File.WriteAllText(Path.Combine(folder,role+".json"),JsonUtility.ToJson(report,true));if(!Application.isEditor)Application.Quit(report.passed?0:2);}
    }
}
#endif
