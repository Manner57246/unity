#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SuperDodgeball.Editor
{
    public sealed class DodgeMvpChecks : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public List<string> checks=new List<string>(); public string failure; }
        readonly Report report=new Report();
        DodgeApp App => DodgeApp.Instance;
        [MenuItem("Super Dodgeball/Run MVP flow checks")]
        public static void Begin()
        {
            if(!EditorApplication.isPlaying || !DodgeApp.Instance || !DodgeApp.Instance.Initialized) throw new Exception("Enter Play mode and wait for the menu.");
            new GameObject("MVP lifecycle verification").AddComponent<DodgeMvpChecks>().StartCoroutine("Guarded");
        }
        IEnumerator Guarded()
        {
            App.ReturnHome("");
            var tests=Checks();
            while(true)
            {
                object next;
                try { if(!tests.MoveNext()){report.passed=true;break;} next=tests.Current; }
                catch(Exception error){report.failure=error.ToString();break;}
                yield return next;
            }
            App.ReturnHome(""); Directory.CreateDirectory("work");
            File.WriteAllText("work/mvp-flow-check.json",JsonUtility.ToJson(report,true));
            Debug.Log("[Dodgeball MVP] Flow checks "+report.passed+": "+report.checks.Count+" "+report.failure);
            Destroy(gameObject);
        }
        void Check(bool value,string name) {if(!value)throw new Exception(name);report.checks.Add(name);}
        void Update()
        {
            if(!App || !App.Initialized || !App.Walkable)return;
            App.SetPaused(false); App.Controllers[0].RemoteControlled=true;
        }
        bool ScreenText(string name,string value)
        {
            var labels=App.Stadium.GetComponentsInChildren<TextMesh>(true).Where(t=>t.name==name).ToArray();
            return labels.Length==6 && labels.All(t=>t.text==value);
        }
        void BallAt(DodgeLobbyAction action,int owner=0)
        {
            var target=App.Clubhouse.Target(action);
            Vector3 direction=action>=DodgeLobbyAction.Host?Vector3.back:Vector3.forward;
            Vector3 from=target.transform.position-direction*.85f;
            DodgeBall.Spawn(from,direction*12f,App.Actors[owner],false,DodgeGame.Instance.ballMaterial);
        }
        IEnumerator Checks()
        {
            Check(App.Page==DodgePage.Home && !DodgeGame.InputCaptured,"Home starts frozen with a free cursor");
            Physics.SyncTransforms();
            var floor=GameObject.Find("Court floor").GetComponent<Collider>();
            Check(Mathf.Abs(floor.bounds.size.x-27.5f)<.01f && Mathf.Abs(floor.bounds.size.z-35f)<.01f,"The physical court is 27.5 by 35 metres");
            RaycastHit ground;
            Check(Physics.Raycast(new Vector3(12,3,15),Vector3.down,out ground,5) && ground.collider==floor,"The newly added corner has solid playable floor");
            var side=GameObject.Find("01 - Playground").GetComponentsInChildren<Collider>().First(c=>c.name=="Side bumper" && c.transform.position.x>0);
            Check(side.bounds.min.x>13.8f,"Side collision barriers move out with the enlarged floor");
            Check(App.Clubhouse.InCourt(new Vector3(12,.05f,15),1) && !App.Clubhouse.InCourt(new Vector3(13,.05f,16),1),"Court entry accepts the new play area and rejects outside positions");
            Check(App.Stadium && App.Stadium.ScreenCount==6,"Indoor gym has four suspended displays and two end-wall screens");
            RaycastHit shell;
            Check(Physics.Raycast(new Vector3(0,1,0),Vector3.up,out shell,20) && shell.collider.GetComponent<DodgeSurface>(),"The indoor arena has a solid overhead enclosure");
            Check(!GameObject.Find("01 - Playground").transform.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Cotton cloud" && t.gameObject.activeSelf),"Outdoor sky decorations are removed from the indoor court");
            App.BeginParty();
            yield return new WaitForSeconds(.15f);
            Check(App.Page==DodgePage.Lobby && DodgeGame.SimulationActive && App.Actors[0].transform.position.z< -40f,"Start enters a real walkable clubhouse");
            Check(App.Room.seats.Count(s=>s.kind==DodgeSeatKind.Bot)==4 && !App.CanStart,"Bots fill the clubhouse but every human must first choose a side");
            Check(!DodgeAirdrop.Active.Any() && DodgeBall.Active.Count==0,"The clubhouse has no automatic hazards or airdrops");
            App.Controllers[0].RemoteInput=new DodgeInput {move=Vector2.up,yaw=0};
            float initialZ=App.Actors[0].transform.position.z;
            yield return new WaitForSeconds(.25f);
            App.Controllers[0].RemoteInput=default;
            Check(App.Actors[0].transform.position.z>initialZ+.6f,"Normal player movement works before the match");
            App.PlaceActor(0,new Vector3(-9f,.05f,DodgeClubhouse.LobbyZ),0f);
            yield return new WaitForSeconds(.4f);
            Check(App.Room.seats[0].team==0 && App.CanStart,"Walking onto the mint side chooses a team and balances bots");
            App.PlaceActor(0,new Vector3(9f,.05f,DodgeClubhouse.LobbyZ),0f);
            yield return new WaitForSeconds(.4f);
            Check(App.Room.seats[0].team==1 && Mathf.Abs(App.Room.seats.Count(s=>s.team==1)-App.Room.seats.Count(s=>s.team==0))==1,"Walking across switches teams without a menu");
            BallAt(DodgeLobbyAction.RemoveBot);yield return new WaitForSeconds(.2f);
            Check(App.Room.seats.Count(s=>s.kind==DodgeSeatKind.Bot)==3,"An owned ball really collides with the remove-bot tile");
            BallAt(DodgeLobbyAction.AddBot);yield return new WaitForSeconds(.2f);
            Check(App.Room.seats.Count(s=>s.kind==DodgeSeatKind.Bot)==4,"An owned ball adds a bot through the physical screen");
            BallAt(DodgeLobbyAction.Crowd);yield return new WaitForSeconds(.2f);
            BallAt(DodgeLobbyAction.Drops);yield return new WaitForSeconds(.2f);
            BallAt(DodgeLobbyAction.Duration);yield return new WaitForSeconds(.2f);
            Check(!App.Room.crowd && !App.Room.drops && App.Room.duration==240f,"Screen hits change the displayed authoritative match rules");
            Check(!App.SetOutfit(0,2),"Clothes cannot change from outside the dressing room");
            App.PlaceActor(0,new Vector3(-11f,.05f,DodgeClubhouse.LobbyZ-8f),180f);
            BallAt(DodgeLobbyAction.OutfitSuit);yield return new WaitForSeconds(.2f);
            Check(App.Room.seats[0].outfit==2 && App.Actors[0].Puppet.Wardrobe.Style==2,"A ball selects a real deformable outfit inside the dressing room");
            App.PersistLocalOutfit();Check(PlayerPrefs.GetInt("MVP.Outfit")==2,"The chosen outfit is saved locally");
            var target=App.Clubhouse.Target(DodgeLobbyAction.Start);
            App.PlaceActor(0,new Vector3(-5.3f,.05f,DodgeClubhouse.LobbyZ+2f),0f);
            var player=App.Controllers[0];
            player.RemoteInput=new DodgeInput {yaw=0,aim=target.transform.position};
            Check(player.TryShoot(),"The real throw controller can target the lobby screen");
            float deadline=Time.time+2f;
            while(App.Page==DodgePage.Lobby && Time.time<deadline)yield return null;
            Check(App.Page==DodgePage.Staging && App.Countdown==3f,"Hitting depart sends both teams to rest rooms without starting countdown");
            Check(App.Actors[0].transform.position.z>20f && App.Room.seats.All(s=>!s.inCourt),"Players arrive in their own rest room with no premature readiness");
            Check(App.Actors[0].Puppet.Wardrobe.Style==2,"The selected clothes survive the transition into the rest room");
            yield return new WaitForSeconds(.4f);
            Check(App.Page==DodgePage.Staging && !App.Controllers[0].TryShoot(),"Waiting in a rest room neither starts a match nor permits combat");
            App.PlaceActor(0,new Vector3(0f,.05f,DodgeCourt.RestZ(22.2f)),180f);
            player.RemoteInput=new DodgeInput {move=Vector2.up,yaw=180};
            deadline=Time.time+5f;
            while(!App.Room.seats[0].inCourt && Time.time<deadline)yield return null;
            player.RemoteInput=default;
            Check(App.Room.seats[0].inCourt,"A real CharacterController walks through the team doorway into court");
            deadline=Time.time+8f;
            while(App.Page==DodgePage.Staging && Time.time<deadline)yield return null;
            Check(App.Page==DodgePage.Countdown && App.AllOnCourt,"Bots follow their team into court and all participants trigger 3-2-1");
            yield return new WaitForEndOfFrame();
            Check(App.Stadium.CountdownVisible && ScreenText("Opening countdown","3"),"All six physical screens display the opening 3");
            yield return new WaitForSeconds(1.03f);Check(ScreenText("Opening countdown","2"),"All six screens advance to 2");
            yield return new WaitForSeconds(1.03f);Check(ScreenText("Opening countdown","1"),"All six screens advance to 1");
            App.PlaceActor(0,new Vector3(0f,.05f,DodgeCourt.RestZ(22f)),180f);yield return new WaitForSeconds(.1f);
            Check(App.Page==DodgePage.Staging && App.Countdown==3f,"Leaving court during countdown cancels and resets it");
            App.PlaceActor(0,App.Clubhouse.CourtSpawn(1,0),180f);
            yield return new WaitForSeconds(.15f);
            Check(App.Page==DodgePage.Countdown,"Returning to court restarts the full countdown");
            yield return new WaitForSeconds(3.1f);
            Check(App.Page==DodgePage.Playing && App.TimeLeft>239f,"Only after 3-2-1 does the timed match begin");
            yield return new WaitForEndOfFrame();
            Check(!App.Stadium.CountdownVisible && ScreenText("Mint score","0") && ScreenText("Peach score","0"),"Screens switch from countdown to the new match scores");
            int seconds=Mathf.CeilToInt(App.TimeLeft);
            Check(ScreenText("Match time",(seconds/60).ToString("00")+":"+(seconds%60).ToString("00")),"Every screen renders the authoritative match clock");
            int ally=Array.FindIndex(App.Room.seats,1,s=>s.kind!=DodgeSeatKind.Empty && s.team==1);
            int enemy=Array.FindIndex(App.Room.seats,1,s=>s.kind!=DodgeSeatKind.Empty && s.team==0);
            App.CreditHit(App.Actors[0],App.Actors[ally]);
            Check(App.Room.seats[0].score==0 && App.SameTeam(App.Actors[0],App.Actors[ally]),"Friendly hits do not score");
            App.CreditHit(App.Actors[0],App.Actors[enemy]);
            Check(App.TeamScore(1)==1 && App.Room.seats[enemy].hits==1,"Enemy hits credit the shooter and the shared team score");
            yield return new WaitForEndOfFrame();
            Check(ScreenText("Peach score","1") && ScreenText("Mint score","0"),"A credited hit updates the team score on every screen");
            App.FinishMatch();yield return new WaitForEndOfFrame();
            Check(ScreenText("Scoreboard state",App.WinnerText),"Physical screens show the actual result after the match");
            Check(App.Page==DodgePage.Results && App.WinnerText.Contains("蜜桃队"),"Results declare the winning team");
            App.BackToLobby();yield return new WaitForSeconds(.1f);
            Check(ScreenText("Mint score","0") && ScreenText("Peach score","0"),"Returning to the lobby clears all previous scores on the screens");
            Check(App.Page==DodgePage.Lobby && App.Actors[0].transform.position.z< -40f && App.Room.seats[0].outfit==2,"Rematch returns to the clubhouse with clothing and teams intact");
            App.PortText="17878";App.OpenRoomInLobby();
            Check(App.Net.Online && App.Page==DodgePage.Lobby,"The physical friends corner can open the existing clubhouse as a room");
            App.ReturnHome("");Check(!App.Net.Online && !DodgeGame.InputCaptured,"Leaving releases networking and restores the three-button home");
        }
    }
}
#endif
