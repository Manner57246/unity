#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public sealed partial class DodgePartyChecks
    {
        void RefPhase(DodgeRefereePhase phase)
        {
            typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,phase);
            typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,160f);
        }
        IEnumerator LockerChecks()
        {
            var driver=App.Controllers[0];
            var mat=DodgeGame.Instance.ballMaterial;
            Clean();App.Referee.ResetRound();RefPhase(DodgeRefereePhase.Slacking);
            var shelves=FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None).Where(t=>t.Item!=DodgeItem.None).ToArray();
            Check(shelves.Length==4,"Two purchasable drugs per team");
            Check(!App.Shop(A,DodgeWeapon.Standard,DodgeItem.ThunderPill),"Medicine cannot be bought during play");
            A.Kit.Grant(DodgeItem.ThunderPill);
            App.PlaceActor(0,new Vector3(0,.05f,-4),0);App.PlaceActor(1,new Vector3(6,.05f,6),180);
            var run=Vector2.right;
            driver.RemoteInput=new DodgeInput{move=run,aim=Vector3.forward*10};
            yield return new WaitForSeconds(.72f);
            float normalSpeed=driver.MovementAmount*driver.moveSpeed;
            driver.RemoteInput=default;yield return new WaitForSeconds(.6f);
            driver.RemoteInput=new DodgeInput{move=run,aim=Vector3.forward*10};
            yield return new WaitForSeconds(.09f);float normalStart=driver.MovementAmount;
            driver.RemoteInput=default;yield return new WaitForSeconds(.6f);
            App.PlaceActor(0,new Vector3(0,.05f,-4),0);driver.RemoteInput=new DodgeInput{aim=new Vector3(0,1,9)};
            yield return null;
            Check(driver.TryShoot(),"Baseline throw starts");
            yield return new WaitForSeconds(.8f);float baselineThrow=driver.LastShotSpeed;
            driver.RemoteInput=new DodgeInput{aim=new Vector3(0,1,9),pressed=DodgeButtons.UseItem};
            yield return new WaitForSeconds(.14f);
            Check(A.Kit.BiteLeft>0 && A.Kit.BulkLeft==0 && A.Kit.Item==DodgeItem.None,"F raises the oversized pill before the bite grants strength");
            yield return new WaitForSeconds(.9f);
            Check(A.Kit.MuscleBlend>.95f && A.Kit.BulkLeft>13 && driver.TryShoot(),"The bite grows the body and permits a stronger throw");
            yield return new WaitForSeconds(.8f);
            Check(driver.LastShotSpeed>baselineThrow*1.26f,"The real uncharged throw leaves the hand about thirty percent faster");
            driver.RemoteInput=new DodgeInput{move=run,aim=Vector3.forward*10};
            yield return new WaitForSeconds(.09f);Check(driver.MovementAmount<normalStart*.9f,"Bulky player accelerates more slowly");
            yield return new WaitForSeconds(.94f);
            Check(Mathf.Abs(driver.MovementAmount*driver.moveSpeed-normalSpeed)<.06f,"Bulky player reaches the same maximum running speed");
            driver.RemoteInput=default;yield return new WaitForSeconds(.6f);
            var shoulder=FindObjectsByType<DodgeHitbox>(FindObjectsSortMode.None).First(h=>h.actor==A && h.name=="Thunder deltoid 1");
            var edge=shoulder.GetComponent<BoxCollider>().bounds.center;
            DodgeBall.Spawn(edge,Vector3.back*22,B,true,mat);
            yield return new WaitForSeconds(.09f);
            Check(A.HitCount==1,"The added shoulder is a real active ball-hit volume");
            Clean();driver.RemoteInput=default;yield return new WaitForSeconds(.15f);
            RefPhase(DodgeRefereePhase.Watching);
            yield return new WaitForSeconds(.10f);
            Check(App.Referee.SeesItem(A),"Referee has a clear view of the test player");
            int strikes=App.Referee.Strikes(0);A.Kit.Grant(DodgeItem.SprintShot);
            Check(!A.Kit.Use(Vector3.zero) && A.Kit.SprintLeft==0 && App.Referee.Strikes(0)==strikes+1,"Using a visible injector is confiscated and records one real foul");
            yield return new WaitForSeconds(.9f);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Locker item sight blocker";
            var eyes=GameObject.Find("Lazy referee").transform.Find("Head").position;
            wall.transform.position=(eyes+A.Puppet.BodyCenter)*.5f;wall.transform.localScale=new Vector3(.75f,6,7);wall.AddComponent<DodgeSurface>();Physics.SyncTransforms();
            Check(!App.Referee.SeesItem(A),"Court geometry blocks the official's line of sight");
            A.Kit.Grant(DodgeItem.SprintShot);driver.RemoteInput=new DodgeInput{pressed=DodgeButtons.UseItem};
            yield return new WaitForSeconds(.10f);
            Check(A.Kit.SprintLeft>9.6f && A.Kit.ThrowStrength==1 && A.Kit.MuscleBlend==0,"Hidden injector starts a ten-second speed-only effect");
            Destroy(wall);RefPhase(DodgeRefereePhase.Slacking);
            App.PlaceActor(0,new Vector3(-6,.05f,-6),0);
            driver.RemoteInput=new DodgeInput{move=run};yield return new WaitForSeconds(.58f);
            float x=A.transform.position.x;float t=Time.time;
            yield return new WaitForSeconds(.24f);
            float measured=(A.transform.position.x-x)/(Time.time-t);
            Check(Mathf.Abs(measured-normalSpeed*2)<normalSpeed*.12f,"Actual running displacement doubles during the injection ("+measured.ToString("F2")+" vs "+normalSpeed.ToString("F2")+" m/s)");
            driver.RemoteInput=default;
            while(A.Kit.SprintLeft>0)yield return null;
            var frozen=A.transform.position;
            Check(A.Kit.PantLeft>1.85f && A.Kit.RunMultiplier==1,"Sprint expires into two seconds of panting");
            driver.RemoteInput=new DodgeInput{move=run,pressed=DodgeButtons.Fire|DodgeButtons.Dive};
            yield return new WaitForSeconds(1.65f);
            Check(Vector3.ProjectOnPlane(A.transform.position-frozen,Vector3.up).magnitude<.02f && !driver.IsDiving && !driver.TryShoot(),"The winded player cannot run, dive or throw");
            yield return new WaitForSeconds(.65f);
            Check(A.Kit.PantLeft==0 && A.transform.position.x>frozen.x+.2f,"Normal controls return automatically after panting");
            driver.RemoteInput=default;
        }
    }
}
#endif
