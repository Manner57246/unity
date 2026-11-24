#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public sealed class DodgeRefereeChecks : MonoBehaviour
    {
        [Serializable] sealed class Report { public bool passed; public List<string> checks=new List<string>(); public string failure; }
        readonly Report report=new Report();DodgeApp App=>DodgeApp.Instance;DodgeReferee Ref=>App.Referee;
        bool keepActive=true,allowBot;
        [MenuItem("Super Dodgeball/Run referee checks")]
        public static void Begin()
        {
            if(!EditorApplication.isPlaying || !DodgeApp.Instance || !DodgeApp.Instance.Initialized)throw new Exception("Enter Play mode first.");
            new GameObject("Referee verification").AddComponent<DodgeRefereeChecks>().StartCoroutine("Run");
        }
        void Update()
        {
            if(!App || !App.Initialized || !App.Walkable)return;
            if(keepActive)App.SetPaused(false);
            App.Controllers[0].RemoteControlled=true;
            for(int i=1;i<5;i++){var bot=App.Actors[i].GetComponent<DodgeBot>();if(bot && !(allowBot && i==1))bot.enabled=false;}
        }
        void Check(bool condition,string name){if(!condition)throw new Exception(name);report.checks.Add(name);}
        void Phase(DodgeRefereePhase phase,float seconds)
        {
            typeof(DodgeReferee).GetProperty("Phase").SetValue(Ref,phase,null);
            typeof(DodgeReferee).GetProperty("Remaining").SetValue(Ref,seconds,null);
        }
        IEnumerator Run()
        {
            var tests=Checks();
            while(true)
            {
                object next;try{if(!tests.MoveNext()){report.passed=true;break;}next=tests.Current;}
                catch(Exception e){report.failure=e.ToString();break;}yield return next;
            }
            App.ReturnHome("");Directory.CreateDirectory("work");File.WriteAllText("work/referee-check.json",JsonUtility.ToJson(report,true));
            Debug.Log("[Referee] "+report.passed+" / "+report.checks.Count+" / "+report.failure);Destroy(gameObject);
        }
        IEnumerator Checks()
        {
            App.ReturnHome("");App.BeginParty();App.ChooseSide(0,0);App.Room.crowd=App.Room.drops=false;
            yield return new WaitForSeconds(.2f);
            Check(Ref.Whistles==0 && Ref.Clock==0,"Referee does not police the clubhouse");
            App.StartMatch();yield return new WaitForSeconds(.3f);
            Check(Ref.Whistles==0 && Ref.Clock==0,"Rest rooms do not consume the slacking window");
            for(int i=0;i<5;i++)App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),App.Room.seats[i].team==0?0:180);
            yield return new WaitForSeconds(3.2f);
            Check(App.Page==DodgePage.Playing && Ref.Phase==DodgeRefereePhase.Slacking,"Every match opens with a clear slacking opportunity");
            App.PlaceActor(0,new Vector3(-5,.05f,2),0);yield return new WaitForSeconds(.55f);
            Check(Ref.Strikes(0)==0 && Ref.Whistles==0,"Actual enemy-half trespass is permitted while the referee slacks");
            Phase(DodgeRefereePhase.Slacking,.08f);yield return new WaitForSeconds(.15f);
            Check(Ref.Phase==DodgeRefereePhase.Warning && Ref.Strikes(0)==0,"Slacking transitions to a warning before judging fouls");
            Check(Ref.MustRetreat(App.Actors[0]),"Bots can recognize the public retreat cue");
            App.PlaceActor(0,new Vector3(-5,.05f,-3),0);yield return new WaitForSeconds(2.3f);
            Check(Ref.Phase==DodgeRefereePhase.Watching && Ref.Strikes(0)==0,"Retreating during the warning avoids punishment");
            Check(!Ref.IsAcross(new Vector3(0,0,.5f),0) && Ref.IsAcross(new Vector3(0,0,-1),1),"Both teams use a tolerant center-line rule");
            Phase(DodgeRefereePhase.Watching,50);
            App.PlaceActor(0,new Vector3(-5,.05f,-1.3f),0);
            App.Controllers[0].RemoteInput=new DodgeInput{move=Vector2.up,yaw=0};
            float until=Time.time+2f;while(Ref.Strikes(0)==0 && Time.time<until)yield return null;
            App.Controllers[0].RemoteInput=default;
            Check(Ref.Strikes(0)==1 && Ref.Whistles==1,"Real controller movement across center is caught once during supervision");
            yield return new WaitForSeconds(.9f);
            Check(App.Actors[0].transform.position.z< -3 && !Ref.IsRestricted(0),"The first warning returns the offender to their own half");
            yield return new WaitForSeconds(3.1f);
            App.PlaceActor(0,new Vector3(-5,.05f,2),0);yield return new WaitForSeconds(.45f);
            Check(Ref.Strikes(0)==2 && Ref.GuardCalls==0,"A second separate offence warns without summoning security");
            yield return new WaitForSeconds(4f);
            App.PlaceActor(0,new Vector3(-5,.05f,2),0);yield return new WaitForSeconds(.45f);
            Check(Ref.Strikes(0)==3 && Ref.GuardCalls==1 && Ref.GuardRemaining(0)>4,"The third offence summons two guards for a bounded detention");
            Check(!App.Controllers[0].TryShoot() && !App.CanHurt(App.Actors[0],App.Actors[1]),"Detained offenders cannot fire or earn points with owned balls");
            Check(!App.Actors[0].ReceiveHit(App.Actors[0].HandPosition),"Guard detention cannot be farmed for hits");
            float remaining=Ref.GuardRemaining(0),clock=Ref.Clock;
            keepActive=false;App.SetPaused(true);yield return new WaitForSeconds(.35f);
            Check(Mathf.Abs(Ref.GuardRemaining(0)-remaining)<.03f && Mathf.Abs(Ref.Clock-clock)<.03f,"Offline pause freezes the judge and escort together");
            keepActive=true;App.SetPaused(false);yield return new WaitForSeconds(2.85f);
            Check(Ref.IsRestricted(0) && App.Actors[0].transform.position.z< -20,"Security visibly carries the offender into their own rest room");
            yield return new WaitForSeconds(1.8f);
            Check(!Ref.IsRestricted(0) && Ref.Strikes(0)==0 && App.Actors[0].transform.position.z< -3 && App.Actors[0].transform.position.z> -12,"After a short detention the player automatically returns with cleared warnings");
            Check(App.Actors[0].GetComponent<CharacterController>().enabled,"Release restores normal character movement");
            yield return new WaitForSeconds(3.1f);
            App.Actors[0].ReceiveHit(App.Actors[0].HandPosition,true,Vector3.forward*3);
            App.PlaceActor(0,new Vector3(-5,.05f,2),0);yield return new WaitForSeconds(.7f);
            Check(Ref.Strikes(0)==0,"Getting knocked across center during ragdoll recovery is not judged deliberate cheating");
            App.PlaceActor(0,new Vector3(-5,.05f,-4),0);
            Phase(DodgeRefereePhase.Watching,.1f);yield return new WaitForSeconds(.2f);
            Check(Ref.Phase==DodgeRefereePhase.Slacking,"Supervision eventually returns to a new opportunity window");
            Phase(DodgeRefereePhase.Watching,50);yield return new WaitForSeconds(3f);
            int botSlot=1;int team=App.Room.seats[botSlot].team;
            App.PlaceActor(botSlot,new Vector3(5,.05f,team==0?2:-2),0);yield return new WaitForSeconds(.45f);
            Check(Ref.Strikes(botSlot)==1,"The same referee rules apply to bots and humans");
            yield return new WaitForSeconds(.9f);Phase(DodgeRefereePhase.Slacking,50);
            var cleanKit=App.Actors[botSlot].Kit;cleanKit.Earn(3);cleanKit.Upgrade(DodgeSkill.GoodBehaviour);
            yield return new WaitForSeconds(15.2f);
            Check(Ref.Strikes(botSlot)==0,"Good Behaviour removes a real warning after fifteen clean seconds");
            var raider=App.Actors[1].GetComponent<DodgeBot>();
            Phase(DodgeRefereePhase.Slacking,8);
            Vector3 raid=Ref.BotDestination(App.Actors[1],new Vector3(0,0,team==0?-4:4),true);
            Check(Ref.IsAcross(raid,team),"A mischievous bot plans an actual raid during the public slacking window");
            Phase(DodgeRefereePhase.Warning,2.2f);allowBot=true;raider.enabled=true;
            App.PlaceActor(1,new Vector3(6,.05f,team==0?1.7f:-1.7f),0);
            yield return new WaitForSeconds(2.55f);
            Check(!Ref.IsAcross(App.Actors[1].transform.position,team) && Ref.Strikes(1)==0,"A running bot physically retreats before supervision catches it");
            allowBot=false;raider.enabled=false;
            App.BackToLobby();Check(Ref.Whistles==0 && Ref.GuardCalls==0 && Ref.Strikes(0)==0,"Rematch resets every penalty and event without leaving movement locked");
        }
    }
}
#endif
