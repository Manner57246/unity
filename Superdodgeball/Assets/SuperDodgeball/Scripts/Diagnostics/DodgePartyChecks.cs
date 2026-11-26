#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace SuperDodgeball.Editor
{
    public sealed partial class DodgePartyChecks:MonoBehaviour
    {
        [Serializable] sealed class Report{public bool passed;public List<string> checks=new List<string>();public string failure;}
        readonly Report report=new Report();DodgeApp App=>DodgeApp.Instance;DodgeActor A=>App.Actors[0];DodgeActor B=>App.Actors[1];
        bool maintain=true;
        [MenuItem("Super Dodgeball/Run party arsenal checks")]
        public static void Begin(){new GameObject("Party arsenal verification").AddComponent<DodgePartyChecks>().StartCoroutine("Run");}
        void Check(bool ok,string name){if(!ok)throw new Exception(name);report.checks.Add(name);}
        void Update()
        {
            if(!App.Initialized || !App.Walkable)return;
            if(maintain)App.SetPaused(false);
            App.Controllers[0].RemoteControlled=true;
            for(int i=1;i<5;i++){App.Actors[i].GetComponent<DodgeBot>().enabled=false;App.Controllers[i].enabled=false;}
        }
        void Place(int slot,Vector3 p){App.Actors[slot].ResetActor();App.PlaceActor(slot,p,slot==0?0:180);}
        void Clean(){DodgeGame.Instance.ClearBalls();DodgePartyEffects.Instance.ResetEffects();Place(0,new Vector3(0,.05f,-4));Place(1,new Vector3(0,.05f,-2));for(int i=2;i<5;i++)Place(i,new Vector3(7,.05f,8+i));}
        IEnumerator Run()
        {
            var tests=Checks();while(true){object next;try{if(!tests.MoveNext()){report.passed=true;break;}next=tests.Current;}catch(Exception e){report.failure=e.ToString();break;}yield return next;}
            App.ReturnHome("");File.WriteAllText("work/party-check.json",JsonUtility.ToJson(report,true));Debug.Log("[Party] "+report.passed+" / "+report.checks.Count+" / "+report.failure);Destroy(gameObject);
        }
        IEnumerator Checks()
        {
            while(!App.Initialized)yield return null;App.ReturnHome("");App.BeginParty();App.ChooseSide(0,0);App.Room.crowd=App.Room.drops=false;Check(App.StartMatch(),"Start a complete team match");
            yield return new WaitForSeconds(.15f);
            Check(A.Kit.Skills==0 && DodgePartyEffects.Instance.LooseCount==4,"Changing rooms hold four free weapons and no pre-learned skills");
            var shelf=FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None).First(t=>t.Team==0 && t.Item==DodgeItem.ThunderPill);
            DodgeBall.Spawn(shelf.transform.position-shelf.transform.forward*.7f,shelf.transform.forward*12,A,false,DodgeGame.Instance.ballMaterial);
            yield return new WaitForSeconds(.15f);
            Check(A.Kit.Item==DodgeItem.ThunderPill && App.Room.funds[0]==4,"A physical shot buys the pill with two team coins");
            for(int i=0;i<5;i++)App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),0);
            yield return new WaitForSeconds(3.3f);
            Check(App.Page==DodgePage.Playing,"All players enter court before effects can be used");
            typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,1000f,null);
            App.Room.seats[1].team=1;App.Room.seats[2].team=0;
            foreach(var weapon in new[]{DodgeWeapon.Bat,DodgeWeapon.Hammer})
            {
                Clean();yield return new WaitForSeconds(.12f);int hits=B.HitCount;
                A.Arsenal.Equip(weapon);Check(A.Arsenal.ConsumeAttack()==weapon && A.Arsenal.Charges==3,weapon+" can be used repeatedly");
                DodgeCombat.Release(A,weapon,A.HandPosition,B.transform.position,Vector3.zero,1,false,B);
                Check(B.HitCount==hits+1 && B.IsStunned,weapon+" strikes real body colliders and scores");
                if(weapon==DodgeWeapon.Hammer)Check(B.Kit.Flat>0,"Hammer applies temporary flattening");
                yield return new WaitForSeconds(.1f);
                if(weapon==DodgeWeapon.Bat)Check(B.Puppet.BodyCenter.y>1.2f,"Bat launches the ragdoll upward");
            }
            Clean();yield return new WaitForSeconds(.12f);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary melee wall";wall.transform.position=new Vector3(0,1,-3);wall.transform.localScale=new Vector3(4,3,.2f);wall.AddComponent<DodgeSurface>();Physics.SyncTransforms();
            DodgePartyEffects.Instance.Melee(A,DodgeWeapon.Bat,B.transform.position,1);Check(B.HitCount==0,"Melee cannot strike through a wall");Destroy(wall);yield return null;
            Clean();App.Room.seats[1].team=0;yield return new WaitForSeconds(.1f);DodgePartyEffects.Instance.Melee(A,DodgeWeapon.Hammer,B.transform.position,1);Check(B.HitCount==0,"Melee respects team rules");App.Room.seats[1].team=1;
            Clean();yield return new WaitForSeconds(.12f);
            var glove=DodgeBall.Spawn(B.Puppet.BodyPath[3],Vector3.forward*22,A,false,DodgeGame.Instance.ballMaterial,DodgeWeapon.Rocket);
            yield return new WaitForSeconds(.1f);Check(B.HitCount==1 && (!glove || !glove.IsDangerous),"Rocket projectile hits once and becomes harmless");
            Clean();yield return new WaitForSeconds(.12f);
            A.Kit.BeginSwing(DodgeWeapon.Bat,new Vector3(0,1,8));var reflected=DodgeBall.Spawn(A.transform.position+Vector3.up*1.2f+Vector3.forward*1.6f,Vector3.back*20,B,true,DodgeGame.Instance.ballMaterial);
            yield return new WaitForSeconds(.08f);Check(reflected.owner==A && A.HitCount==0,"Active bat swing returns an incoming ball with new ownership");
            Clean();yield return new WaitForSeconds(.12f);
            var bomb=DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb,B.transform.position+Vector3.up*.32f,Vector3.zero,A,.4f);
            maintain=false;App.SetPaused(true);float left=bomb.left;yield return new WaitForSeconds(.2f);Check(Mathf.Abs(bomb.left-left)<.001f,"Offline pause freezes the bomb fuse");App.SetPaused(false);maintain=true;
            yield return new WaitForSeconds(.55f);Check(DodgePartyEffects.Instance.Explosions==1 && B.HitCount==1,"Timed bomb detonates once and hits within blast radius");
            Clean();yield return new WaitForSeconds(.1f);
            var blastWall=GameObject.CreatePrimitive(PrimitiveType.Cube);blastWall.transform.position=new Vector3(0,1,-3);blastWall.transform.localScale=new Vector3(4,3,.25f);blastWall.AddComponent<DodgeSurface>();Physics.SyncTransforms();
            DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb,new Vector3(0,.5f,-3.5f),Vector3.zero,A,.15f);yield return new WaitForSeconds(.25f);Check(B.HitCount==0,"Solid walls block bomb blast damage");Destroy(blastWall);yield return null;
            Clean();yield return new WaitForSeconds(.1f);bomb=DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb,A.transform.position+Vector3.up+Vector3.forward,Vector3.zero,B,2);
            DodgePartyEffects.Instance.Melee(A,DodgeWeapon.Bat,bomb.position,1);Check(bomb.owner==0 && bomb.velocity.z>10 && bomb.left==2,"Bat punts a bomb without resetting its fuse");
            Clean();yield return new WaitForSeconds(.1f);A.Kit.Grant(DodgeItem.Decoy);Check(A.Kit.Use(A.transform.position+Vector3.forward*2),"Decoy consumes its carried item");Check(A.Kit.Item==DodgeItem.None,"Using an item frees its slot");
            var decoy=DodgePartyEffects.Instance.Active.First(e=>e.kind==DodgeEffect.Decoy);Check(DodgePartyEffects.Instance.DecoyTarget(B,A.transform.position+Vector3.back*10)==decoy.position+Vector3.up,"Enemy bots can be fooled by a visible decoy");
            Check(!DodgePartyEffects.Instance.Intercept(decoy.position-Vector3.forward+Vector3.up,decoy.position+Vector3.forward+Vector3.up,A),"Decoy lets friendly throws through");
            Check(DodgePartyEffects.Instance.Intercept(decoy.position-Vector3.forward+Vector3.up,decoy.position+Vector3.forward+Vector3.up,B),"Decoy absorbs an enemy throw and deflates");
            Clean();yield return new WaitForSeconds(.1f);A.Kit.Grant(DodgeItem.Banana);Check(A.Kit.Use(B.transform.position),"Banana can be placed toward the aim point");yield return new WaitForSeconds(.8f);Check(B.HitCount==1 && DodgePartyEffects.Instance.ActiveCount==0,"Armed banana trips a grounded player once");
            A.CrowdStatus.ApplyEgg();A.Kit.Grant(DodgeItem.Soap);Check(A.Kit.Use(A.transform.position) && !A.CrowdStatus.Obscured,"Carried soap washes egg obstruction");
            A.Kit.Grant(DodgeItem.Tea);Check(A.Kit.Use(Vector3.zero) && App.Referee.PendingTea==6,"Tea queues extra shared slacking time");A.Kit.Grant(DodgeItem.Tea);Check(!A.Kit.Use(Vector3.zero) && A.Kit.Item==DodgeItem.Tea,"Tea cannot stack indefinitely and is retained when refused");
            var phase=App.Referee.Phase;typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Watching,null);typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,.01f,null);yield return new WaitForSeconds(.05f);Check(App.Referee.Phase==DodgeRefereePhase.Slacking && App.Referee.Remaining>12 && App.Referee.PendingTea==0,"Tea extends the next slacking phase for both teams");typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,1000f,null);
            Clean();yield return new WaitForSeconds(.1f);A.Kit.Grant(DodgeItem.SpringShoes);Check(A.Kit.Use(Vector3.zero) && A.Kit.SpringReady>0,"Spring shoes arm the next dive");
            App.Controllers[0].RemoteInput=new DodgeInput{pressed=DodgeButtons.Dive,move=Vector2.right};yield return new WaitForSeconds(.25f);Check(A.transform.position.y>.5f && A.Kit.SpringReady==0,"Spring shoes produce a real airborne dive and consume their boost");A.Kit.Earn(3);A.Kit.Upgrade(DodgeSkill.DiveRelay);A.Kit.Dodged(true);yield return new WaitForSeconds(1f);
            Check(!App.Controllers[0].IsDiving && App.Controllers[0].DiveReady>.85f,"Dive relay survives flight and shortens the actual landing cooldown");yield return new WaitForSeconds(.3f);
            Clean();yield return new WaitForSeconds(.1f);
            for(int i=0;i<6;i++){A.Kit.Earn(3);Check(A.Kit.Upgrade((DodgeSkill)i),"Earn and purchase "+DodgePartyKit.SkillNames[i]);}
            Check(A.Kit.ReleaseDelay(DodgeWeapon.Standard)<.22f,"Quick pitch reduces actual release delay");A.Kit.Land();Check(A.Kit.CounterReady>0 && A.Kit.ReleaseDelay(DodgeWeapon.Standard)<.1f,"Landing prepares a faster counter throw");A.Kit.ConsumeCounter();Check(A.Kit.CounterReady==0,"Counter throw buff is single use");A.Kit.Dodged(true);Check(A.Kit.ConsumeRelay(),"A close dive dodge refreshes dive recovery");
            A.SetLeanImmediate(1);yield return new WaitForSeconds(.05f);Check(A.Kit.LeanScale>1,"Rubber waist increases physical pose bending");
            App.PlaceActor(0,new Vector3(0,.05f,2),0);typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Warning,null);Check(A.Kit.RetreatSpeed(Vector3.back)>1 && A.Kit.RetreatSpeed(Vector3.forward)==1,"Guilty sprint only boosts retreat toward the home half");
            using(var stream=new MemoryStream()){var w=new BinaryWriter(stream);A.Kit.Grant(DodgeItem.Tea);A.Kit.Write(w);A.Kit.ResetKit(false);stream.Position=0;A.Kit.Read(new BinaryReader(stream));Check(A.Kit.Skills==63 && A.Kit.Item==DodgeItem.Tea,"Skills, items and action timers survive their network serialization");}
            Clean();yield return new WaitForSeconds(.12f);
            foreach(var item in new[]{DodgeItem.Banana,DodgeItem.Decoy,DodgeItem.Tea,DodgeItem.SpringShoes})
            {
                var drop=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(3,0,-5),DodgeWeapon.Standard,true,DodgeGiftKind.Item,item);DodgeBall.Spawn(drop.AimPosition,Vector3.forward,A,false,DodgeGame.Instance.ballMaterial);yield return new WaitForSeconds(.06f);Check(A.Kit.Item==item,"Owned ball claims "+item+" from a random gift");
            }
            foreach(var weapon in new[]{DodgeWeapon.Bat,DodgeWeapon.Hammer,DodgeWeapon.Rocket,DodgeWeapon.Bomb})
            {
                var drop=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(3,0,-5),weapon,true);DodgeBall.Spawn(drop.AimPosition,Vector3.forward,A,false,DodgeGame.Instance.ballMaterial);yield return new WaitForSeconds(.06f);Check(A.Arsenal.Weapon==weapon && A.Arsenal.Charges==3,"Owned ball claims "+weapon+" with three uses");
            }
            var driver=App.Controllers[0];
            var fx=DodgePartyEffects.Instance;
            Clean();
            typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Slacking,null);
            driver.RemoteInput=new DodgeInput{aim=B.transform.position+Vector3.up};
            A.Arsenal.Equip(DodgeWeapon.Bat,1);
            yield return new WaitForSeconds(.14f);
            driver.RemoteInput=new DodgeInput{aim=B.transform.position+Vector3.up,pressed=DodgeButtons.Fire};
            yield return new WaitForSeconds(.07f);
            var stick=A.Puppet.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Weapon Bat" && t.gameObject.activeInHierarchy);
            // The puppet can be built under the court root rather than under the player.
            if(!stick)stick=FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Weapon Bat");
            var windupAxis=stick.up;
            Check(driver.VisualWeapon==DodgeWeapon.Bat && A.Kit.Swing>0 && B.HitCount==0,"Last bat charge starts a visible wind-up before contact");
            yield return new WaitForSeconds(.19f);
            Check(B.HitCount==1 && Vector3.Angle(windupAxis,stick.up)>48 && driver.VisualWeapon==DodgeWeapon.Bat,"Bat sweeps across the target, connects once and follows through");
            yield return new WaitForSeconds(.55f);
            Check(driver.VisualWeapon==DodgeWeapon.Standard,"Last bat charge returns to the ball only after the swing");
            Clean();A.Arsenal.Equip(DodgeWeapon.Burst,1);
            driver.RemoteInput=new DodgeInput{aim=new Vector3(0,3,12)};
            yield return new WaitForSeconds(.08f);
            int shots=driver.ShotsFired;
            Check(driver.TryShoot() && driver.VisualWeapon==DodgeWeapon.Burst,"Last cannon charge keeps the weapon raised");
            yield return new WaitForSeconds(.94f);
            Check(driver.ShotsFired==shots+5 && A.Arsenal.Charges==0,"A single cannon trigger sends exactly five rounds");
            Clean();
            App.PlaceActor(0,new Vector3(-DodgeCourt.PlayWidth+.95f,.05f,-5.4f),0);
            A.Arsenal.Equip(DodgeWeapon.Coil);
            Check(fx.DropWeapon(A),"Physical coil released");
            yield return new WaitForSeconds(.38f);
            var loose=FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).First(b=>b.name=="落地 · 五吨钢卷");
            App.PlaceActor(0,Vector3.ProjectOnPlane(loose.position,Vector3.up)+Vector3.up*.05f,0);
            driver.RemoteInput=new DodgeInput{pressed=DodgeButtons.Collect,aim=Vector3.forward*8};
            yield return new WaitForSeconds(.12f);
            Check(A.Arsenal.Selected==DodgeWeapon.Coil && !fx.CollectWeapon(A),"G takes the dropped coil exactly once");
            App.PlaceActor(0,new Vector3(0,.05f,-4),0);
            DodgeBall.Spawn(B.Puppet.BodyPath[3],Vector3.forward*18,A,false,DodgeGame.Instance.ballMaterial,DodgeWeapon.Coil);
            yield return new WaitForSeconds(.55f);
            Check(B.HitCount==1 && B.Kit.Flat>5 && B.Kit.HeavySquash>.9f,"Heavy coil compresses its target and holds the pose");
            yield return new WaitForSeconds(5.92f);
            Check(B.Kit.Flat==0 && B.Kit.HeavySquash>0 && B.Kit.HeavySquash<1,"Coil target gradually springs back after its six-second hold");
            yield return new WaitForSeconds(1.45f);
            Check(B.Kit.FlatAmount==0,"Coil restores the original body proportions");
            Clean();A.Arsenal.Equip(DodgeWeapon.Vest);
            typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Watching,null);
            Check(!A.Kit.TryPopVest() && A.Arsenal.WearingVest,"A watchful referee blocks the vest and preserves the charge");
            typeof(DodgeReferee).GetProperty("Phase").SetValue(App.Referee,DodgeRefereePhase.Slacking,null);
            App.PlaceActor(0,new Vector3(0,.05f,2),0);App.PlaceActor(1,new Vector3(0,.05f,4),180);
            yield return new WaitForSeconds(.15f);
            var before=B.Puppet.BodyCenter;
            driver.RemoteInput=new DodgeInput{pressed=DodgeButtons.Detonate};
            yield return new WaitForSeconds(.22f);
            Check(A.Kit.ArmedTime>0 && !A.IsStunned && fx.Explosions==0,"Arming gives the wearer a live approach window before detonation");
            var approach=A.transform.position;
            driver.RemoteInput=new DodgeInput{move=Vector2.up,yaw=0,aim=B.transform.position+Vector3.up};
            yield return new WaitForSeconds(.28f);
            Check(A.transform.position.z>approach.z+.05f,"The wearer can run during the vest fuse");
            driver.RemoteInput=default;
            yield return new WaitForSeconds(.92f);
            var costume=fx.Active.Single(e=>e.kind==DodgeEffect.VestBurst);
            Check(B.HitCount==1 && A.HitCount==0 && A.IsStunned && A.Kit.ReturnTime>2 && !A.Arsenal.WearingVest,"R while the referee is slacking launches opponents and starts the wearer's recovery");
            Check(costume.scatter && costume.scatter.GetComponentsInChildren<Rigidbody>().Length==0 && costume.scatter.GetComponentsInChildren<Collider>().Length==0,"Costume scatter is entirely cosmetic");
            yield return new WaitForSeconds(.86f);
            Check(A.transform.position.z< -6 && A.IsStunned && Vector3.Distance(before,B.Puppet.BodyCenter)>4,"Vest sends the wearer home and throws the opponent several metres");
            yield return new WaitForSeconds(2.3f);
            Check(!A.IsStunned && A.Kit.ReturnTime==0,"Vest recovery ends and the wearer regains control");
            Clean();
            DodgeCombat.Release(A,DodgeWeapon.Bomb,A.HandPosition,B.transform.position,Vector3.zero,1,false,B);
            var thrown=fx.Active.Single(e=>e.kind==DodgeEffect.Bomb);
            Check(Mathf.Approximately(thrown.left,3) && thrown.label.text=="03","Thrown timer starts at three seconds on the prop display");
            yield return new WaitForSeconds(2.18f);
            Check(thrown.label.text=="01" && fx.Explosions==0,"Timer remains live with one second displayed");
            yield return new WaitForSeconds(1f);
            Check(fx.Explosions==1,"Three-second fuse produces exactly one explosion");
            var locker=LockerChecks();while(locker.MoveNext())yield return locker.Current;
            App.BackToLobby();Check(A.Kit.Skills==0 && A.Kit.Item==DodgeItem.None && DodgePartyEffects.Instance.ActiveCount==0,"Rematch clears match-local skills, items and effects");
        }
    }
}
#endif
