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
            var camera = DodgeGame.Instance.view.GetComponent<Camera>();
            var eyes = GameObject.Find("Lazy referee").transform.Find("Head");
            var camTransform = camera.transform;
            var oldPosition = camTransform.position;
            var oldRotation = camTransform.rotation;
            float oldVolume = AudioListener.volume;
            try
            {
                AudioListener.volume = 0;
                camTransform.SetPositionAndRotation(eyes.position + Vector3.right * 6, Quaternion.identity);
                Check(Ref.TryGetCoughCue(camera,out var towards,out int beat,out _) && beat == 1 && towards.x < -.99f,
                    "A muted trespasser sees the first cough on the referee's left edge");
                camTransform.rotation = Quaternion.Euler(0,180,0);
                Check(Ref.TryGetCoughCue(camera,out towards,out _,out _) && towards.x > .99f,"Turning around moves the same cue to the right");
                camTransform.rotation = Quaternion.LookRotation(Vector3.right);
                Check(Ref.TryGetCoughCue(camera,out towards,out _,out _) && towards.y > .99f,"A referee directly behind points to the bottom edge");
                camTransform.LookAt(eyes);
                Check(!Ref.TryGetCoughCue(camera,out _,out _,out _),"The cue disappears as the referee enters the view");
                camTransform.rotation = Quaternion.identity;
                Phase(DodgeRefereePhase.Warning, DodgeReferee.WarningSeconds - .53f);
                Check(Ref.TryGetCoughCue(camera,out _,out beat,out float age) && beat == 2 && Mathf.Abs(age-.05f)<.001f,"The second letter follows the second audio beat");
                Phase(DodgeRefereePhase.Warning, DodgeReferee.WarningSeconds - 1.12f);
                Check(Ref.TryGetCoughCue(camera,out _,out beat,out age) && beat == 3 && Mathf.Abs(age-.06f)<.001f,"The third letter follows the third audio beat");
                App.PlaceActor(0,new Vector3(-5,.05f,-3),0);
                Check(!Ref.TryGetCoughCue(camera,out _,out _,out _),"Retreat immediately clears the local player's cough bubble");
            }
            finally
            {
                AudioListener.volume = oldVolume;
                camTransform.SetPositionAndRotation(oldPosition, oldRotation);
            }
            yield return new WaitForSeconds(1.3f);
            Check(Ref.Phase==DodgeRefereePhase.Watching && Ref.Strikes(0)==0,"Retreating during the warning avoids punishment");
            Check(!Ref.IsAcross(new Vector3(0,0,.5f),0) && Ref.IsAcross(new Vector3(0,0,-1),1),"Both teams use a tolerant center-line rule");
            Phase(DodgeRefereePhase.Watching,50);
            App.PlaceActor(0,new Vector3(-5,.05f,-1.3f),0);
            App.Controllers[0].RemoteInput=new DodgeInput{move=Vector2.up,yaw=0};
            float until=Time.time+2f;while(Ref.Strikes(0)==0 && Time.time<until)yield return null;
            App.Controllers[0].RemoteInput=default;
            Check(Ref.Strikes(0)==1 && Ref.Whistles==1,"Real controller movement across center is caught once during supervision");
            var actor=App.Actors[0];var driver=App.Controllers[0];var official=GameObject.Find("Lazy referee").transform;
            Check(!GameObject.Find("Referee world sign") && !GameObject.Find("Referee call"),"Referee feedback leaves the view clear of the old floating board");
            yield return new WaitForSeconds(.26f);
            Check(official.localPosition.y>2.45f && Ref.HalfAlert(1)>0 && Ref.HalfAlert(0)==0,"Whistle launches the official and only the invaded half flashes red");
            Check(!Ref.IsRestricted(0) && actor.transform.position.z>0,"A warning leaves the offender in control on the opponent's side");
            driver.RemoteInput=new DodgeInput{move=Vector2.down,yaw=0};
            yield return new WaitForSeconds(.82f);driver.RemoteInput=default;
            Check(!Ref.Chasing(0) && Ref.GuardCalls==0 && Ref.HalfAlert(1)==0,"Crossing back in time clears the floor without a guard call");
            App.PlaceActor(0,new Vector3(0,.05f,4),0);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Referee sight test wall";
            wall.transform.position=(eyes.position+actor.Puppet.BodyCenter)*.5f;wall.transform.localScale=new Vector3(.5f,8,7);wall.AddComponent<DodgeSurface>();Physics.SyncTransforms();
            yield return new WaitForSeconds(.34f);
            Check(Ref.SeenSeconds(0)==0 && Ref.Whistles==1,"A wall between the official and offender blocks trespass detection");
            Destroy(wall);yield return null;
            yield return new WaitForSeconds(2.62f);
            Check(Ref.SeenSeconds(0)>2.35f && Ref.GuardCalls==0 && !Ref.IsRestricted(0),"Remaining across for less than three visible seconds permits retreat");
            until=Time.time+.65f;while(!Ref.Chasing(0) && Time.time<until)yield return null;
            Check(Ref.Chasing(0) && Ref.GuardCalls==1 && !Ref.IsRestricted(0),"Three consecutive visible seconds dispatch a pair without locking movement");
            var guard=GameObject.Find("Security 0 / 0").GetComponent<DodgeSecurityGuard>();
            var partner=GameObject.Find("Security 0 / 1").GetComponent<DodgeSecurityGuard>();
            Check(guard.GetComponent<CharacterController>().enabled && partner.GetComponent<CharacterController>().enabled,"Both officers enter with solid capsule bodies");
            var start=guard.Position;float clock=Ref.Clock;
            keepActive=false;App.SetPaused(true);yield return new WaitForSeconds(.28f);
            Check(Vector3.Distance(start,guard.Position)<.015f && Ref.Clock==clock,"Offline pause freezes pursuit and the referee clock");
            keepActive=true;App.SetPaused(false);
            // Stage a frontal grab while the partner still has to run in from the sideline.
            guard.Enter(actor.transform.position+Vector3.forward*1.65f,actor.transform.position);
            yield return null;
            until=Time.time+.25f;while(guard.Action!=DodgeSecurityGuard.Move.Windup && Time.time<until)yield return null;
            Check(guard.Action==DodgeSecurityGuard.Move.Windup,"A nearby guard visibly commits to a grab before contact");
            driver.RemoteInput=new DodgeInput{move=Vector2.right,pressed=DodgeButtons.Dive,yaw=0};
            yield return new WaitForSeconds(.70f);driver.RemoteInput=default;
            Check(Ref.Chasing(0) && !Ref.IsRestricted(0) && guard.Action==DodgeSecurityGuard.Move.Miss,"A real sideways dive evades the committed lunge and leaves the guard recovering");
            driver.RemoteInput=new DodgeInput{move=Vector2.down,yaw=0};
            until=Time.time+2f;while(Ref.Chasing(0) && Time.time<until)yield return null;
            driver.RemoteInput=default;
            Check(!Ref.IsRestricted(0) && !Ref.Chasing(0) && actor.transform.position.z<0,"A successful retreat ends the pursuit");
            yield return new WaitForSeconds(1.95f);App.PlaceActor(0,new Vector3(0,.05f,3.5f),0);
            until=Time.time+11;while(!Ref.IsRestricted(0) && Time.time<until)yield return null;
            Check(Ref.IsRestricted(0) && Ref.GuardRemaining(0)>0,"Guards crossing the court naturally catch a stationary offender through a physical grab");
            Check(!driver.TryShoot() && !App.CanHurt(actor,App.Actors[1]) && !actor.ReceiveHit(actor.HandPosition),"Only a caught player is locked and protected during the escort");
            var previous=actor.transform.position;
            until=Time.time+5;float largest=0;
            while(Ref.IsRestricted(0) && Time.time<until)
            {
                yield return null;var next=actor.transform.position;
                largest=Mathf.Max(largest,Vector3.Distance(previous,next)/Mathf.Max(.001f,Time.deltaTime));previous=next;
            }
            Check(!Ref.IsRestricted(0) && largest<12f && actor.transform.position.z< -3 && actor.GetComponent<CharacterController>().enabled,"The escort and release are continuous all the way home, with controls restored");
            Check(Ref.Strikes(0)==0,"The completed escort clears accumulated warnings");
            yield return new WaitForSeconds(3.1f);
            App.Actors[0].ReceiveHit(App.Actors[0].HandPosition,true,Vector3.forward*3);
            App.PlaceActor(0,new Vector3(-5,.05f,2),0);yield return new WaitForSeconds(.7f);
            Check(Ref.Strikes(0)==0,"Getting knocked across center during ragdoll recovery is not judged deliberate cheating");
            App.PlaceActor(0,new Vector3(-5,.05f,-4),0);
            Phase(DodgeRefereePhase.Watching,.1f);yield return new WaitForSeconds(.2f);
            Check(Ref.Phase==DodgeRefereePhase.Slacking,"Supervision eventually returns to a new opportunity window");
            Phase(DodgeRefereePhase.Watching,50);yield return new WaitForSeconds(3f);
            int botSlot=1;int team=App.Room.seats[botSlot].team;
            App.PlaceActor(botSlot,new Vector3(1,.05f,team==0?2:-2),0);yield return new WaitForSeconds(.45f);
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
