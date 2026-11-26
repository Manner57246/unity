#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace SuperDodgeball.Editor
{
    public sealed class DodgeSpectatorChecks:MonoBehaviour
    {
        [Serializable] sealed class Report {public bool passed;public List<string> checks=new List<string>();public string failure;}
        readonly Report report=new Report();DodgeApp App=>DodgeApp.Instance;DodgeCrowd crowd;bool maintain=true;
        [MenuItem("Super Dodgeball/Run interactive spectator checks")]
        public static void Begin(){new GameObject("Interactive spectator verification").AddComponent<DodgeSpectatorChecks>().StartCoroutine("Run");}
        void Check(bool condition,string name){if(!condition)throw new Exception(name);report.checks.Add(name);}
        void Update()
        {
            if(!App.Initialized || !App.Walkable)return;
            if(maintain)App.SetPaused(false);
            App.Controllers[0].RemoteControlled=true;
            for(int i=1;i<5;i++){var bot=App.Actors[i].GetComponent<DodgeBot>();if(bot)bot.enabled=false;App.Controllers[i].enabled=false;}
        }
        IEnumerator Run()
        {
            var tests=Checks();while(true){object next;try{if(!tests.MoveNext()){report.passed=true;break;}next=tests.Current;}catch(Exception e){report.failure=e.ToString();break;}yield return next;}
            App.ReturnHome("");File.WriteAllText("work/spectator-check.json",JsonUtility.ToJson(report,true));Debug.Log("[Spectators] "+report.passed+" / "+report.checks.Count+" / "+report.failure);Destroy(gameObject);
        }
        DodgeBall Shoot(DodgeSpectator fan,DodgeWeapon weapon=DodgeWeapon.Standard)
        {
            Vector3 point=fan.AimPosition,from=point+fan.transform.forward*1.5f;
            return DodgeBall.Spawn(from,-fan.transform.forward*24,App.Actors[0],false,DodgeGame.Instance.ballMaterial,weapon);
        }
        IEnumerator Checks()
        {
            while(!App.Initialized)yield return null;
            App.ReturnHome("");App.BeginParty();App.ChooseSide(0,0);App.Room.crowd=App.Room.drops=false;App.StartMatch();
            crowd=FindFirstObjectByType<DodgeCrowd>();crowd.Automatic=false;
            for(int i=0;i<5;i++)App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),App.Room.seats[i].team==0?0:180);
            yield return new WaitForSeconds(3.3f);Check(App.Page==DodgePage.Playing,"Enter a real match in the indoor stadium");
            Check(crowd.SpectatorCount==DodgeBleachers.Count && crowd.Spectators.Count==96,"Both sidelines contain 96 seated interactive spectators");
            Check(crowd.GetComponentsInChildren<Transform>().Count(t=>t.name=="Individual seat cushion")==96,"Each spectator has an individual stadium chair");
            Check(crowd.GetComponentsInChildren<Transform>().Count(t=>t.name=="Concrete aisle stair")==108,"Tiered banks have real centre and end stair aisles");
            Check(crowd.GetComponentsInChildren<TextMesh>().Count(t=>t.name=="Section label")==4,"Four numbered sections label the American-style seating banks");
            Check(crowd.GetComponentsInChildren<DodgeSpectatorHitbox>().Length==96*6,"Torso, head, arms and legs have real physical hitboxes");
            Check(FindObjectsByType<DodgeActor>(FindObjectsSortMode.None).Length==5,"Audience remains separate from scoring player actors");
            Check(!FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Eye" || t.name=="Smile" || t.name=="Blush cheek"),"Players, spectators and officials all use clean faces without expressions");
            var fan=crowd.Spectators[0];Vector3 start=fan.AimPosition;
            int score=App.TeamScore(0)+App.TeamScore(1),xp=App.Actors[0].Kit.Experience;
            var ball=Shoot(fan);yield return new WaitForSeconds(.14f);
            Check(fan.Hits==1 && fan.Reacting && !fan.Body.isKinematic,"An owned swept ball releases the spectator's jointed body");
            Check(ball && !ball.IsDangerous,"A ball loses danger after hitting a spectator once");
            Check((fan.AimPosition-start).sqrMagnitude>.001f,"The head actually moves through physics after impact");
            Check(App.TeamScore(0)+App.TeamScore(1)==score && App.Actors[0].Kit.Experience==xp,"Hitting spectators cannot farm match points or skill experience");
            yield return new WaitForSeconds(.15f);Check(fan.Hits==1,"One ball cannot repeatedly hit the same spectator's different body parts");
            maintain=false;App.SetPaused(true);yield return new WaitForSeconds(.06f);Vector3 paused=fan.BodyPosition;float remaining=fan.Remaining;
            yield return new WaitForSeconds(.25f);Check((fan.BodyPosition-paused).sqrMagnitude<.00001f && Mathf.Abs(fan.Remaining-remaining)<.001f && fan.Body.isKinematic,"Offline pause freezes spectator rigid bodies and recovery timers");
            maintain=true;App.SetPaused(false);yield return new WaitForSeconds(.2f);Check(fan.Remaining<remaining && !fan.Body.isKinematic,"Unpausing restores the moving ragdoll");
            yield return new WaitForSeconds(3.8f);Check(!fan.Reacting && fan.Body.isKinematic && Vector3.Distance(fan.BodyPosition,fan.transform.TransformPoint(Vector3.up*.98f))<.06f,"The fan smoothly returns to its original seat and sleeps");
            crowd.ResetCrowd();DodgeGame.Instance.ClearBalls();yield return null;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary spectator cover";wall.transform.SetPositionAndRotation(fan.AimPosition+fan.transform.forward*.85f,fan.transform.rotation);wall.transform.localScale=new Vector3(2,3,.16f);wall.AddComponent<DodgeSurface>();Physics.SyncTransforms();
            Shoot(fan);yield return new WaitForSeconds(.2f);Check(fan.Hits==0 && !fan.Reacting,"Solid cover blocks a ball before it reaches an audience hitbox");Destroy(wall);DodgeGame.Instance.ClearBalls();yield return null;
            crowd.ResetCrowd();Shoot(fan,DodgeWeapon.Bowling);yield return new WaitForSeconds(.32f);
            Check(fan.Hits==1 && fan.BodyPosition.y>fan.transform.position.y+1.12f,"Bowling impact launches the physical spectator upward: hits="+fan.Hits+" body="+fan.BodyPosition.y+" floor="+fan.transform.position.y);
            crowd.ResetCrowd();DodgeGame.Instance.ClearBalls();yield return null;
            Shoot(fan,DodgeWeapon.Rocket);yield return new WaitForSeconds(.14f);Check(fan.Reacting && fan.Hits==1,"Rocket projectiles also knock spectators out of their seats");
            crowd.ResetCrowd();DodgeGame.Instance.ClearBalls();DodgeGame.Instance.crowdEnabled=true;
            Check(crowd.ScheduleThrow(App.Actors[0],DodgeCrowdThrow.RottenEgg,0),"A seated spectator can prepare its original egg attack");
            Shoot(fan);yield return new WaitForSeconds(.15f);Check(!crowd.Pending && crowd.EggsThrown==0,"Hitting the throwing spectator interrupts the telegraphed attack");
            Check(!crowd.ScheduleThrow(App.Actors[0],DodgeCrowdThrow.Soap,0),"A fallen spectator cannot start another gift or egg throw");
            crowd.ResetCrowd();DodgeGame.Instance.ClearBalls();DodgeGame.Instance.crowdEnabled=false;yield return null;
            App.PlaceActor(0,fan.transform.position+fan.transform.forward*1.6f,270);
            DodgePartyEffects.Instance.Melee(App.Actors[0],DodgeWeapon.Bat,fan.BodyPosition,1);Check(fan.Reacting && fan.Hits==1,"A close bat strike uses real audience hitboxes");
            crowd.ResetCrowd();App.PlaceActor(0,App.Clubhouse.CourtSpawn(0,0),0);yield return null;
            DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb,fan.BodyPosition+fan.transform.forward*.65f,Vector3.zero,App.Actors[0],.2f);yield return new WaitForSeconds(.35f);
            Check(fan.Hits==1 && fan.Reacting,"Nearby bomb blasts reach visible spectators with line-of-sight checks");
            Check(App.TeamScore(0)+App.TeamScore(1)==score,"Spectator melee and explosions still award no team points");
            crowd.ResetCrowd();DodgePartyEffects.Instance.ResetEffects();yield return null;
            for(int i=0;i<16;i++)crowd.Spectators[i].Hit(crowd.Spectators[i].BodyPosition,Vector3.up*2,App.Actors[0]);
            Check(crowd.Spectators.Count(f=>f.Reacting)==DodgeCrowd.MaximumReactions,"Simultaneous crowd physics stays within the active-body budget");
            using(var stream=new MemoryStream())
            {crowd.WritePose(new BinaryWriter(stream));Check(stream.Length<4096,"Crowd snapshots remain small with all permitted reactions active");}
            App.BackToLobby();yield return null;
            Check(crowd.SpectatorHits==0 && crowd.Spectators.All(f=>!f.Reacting && f.Hits==0 && f.Body.isKinematic),"Rematch resets every spectator and temporary physical state");
            Check(!fan.Hit(fan.BodyPosition,Vector3.up*2,App.Actors[0]),"Lobby balls cannot activate match-only spectator reactions");
        }
    }
}
#endif
