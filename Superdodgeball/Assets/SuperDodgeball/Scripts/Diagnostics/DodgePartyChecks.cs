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
    public sealed class DodgePartyChecks:MonoBehaviour
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
            Check(A.Kit.Points==1 && A.Kit.Skills==0,"Each match begins with one unspent skill point");
            var skill=FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None).First(t=>t.Team==0 && t.Skill==(int)DodgeSkill.QuickPitch);
            var from=skill.transform.position+Vector3.forward*.7f;
            DodgeBall.Spawn(from,Vector3.back*12,A,false,DodgeGame.Instance.ballMaterial);
            yield return new WaitForSeconds(.15f);
            Check(A.Kit.Has(DodgeSkill.QuickPitch) && A.Kit.Points==0,"A real ball buys a rest-room skill once");
            Check(!A.Kit.Upgrade(DodgeSkill.QuickPitch),"Duplicate skill cannot consume a point");
            var itemTarget=FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None).First(t=>t.Team==0 && t.Item==DodgeItem.Banana);
            DodgeBall.Spawn(itemTarget.transform.position+Vector3.forward*.6f,Vector3.back*12,A,false,DodgeGame.Instance.ballMaterial);
            yield return new WaitForSeconds(.15f);
            Check(A.Kit.Item==DodgeItem.Banana && A.Kit.StarterClaimed,"A real ball claims a carried starter item");
            Check(!A.Kit.ClaimStarter(DodgeItem.Tea),"Starter counter cannot provide unlimited items");
            for(int i=0;i<5;i++)App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),0);
            yield return new WaitForSeconds(3.3f);
            Check(App.Page==DodgePage.Playing,"All players enter court before effects can be used");
            typeof(DodgeReferee).GetProperty("Remaining").SetValue(App.Referee,1000f,null);
            App.Room.seats[1].team=1;App.Room.seats[2].team=0;
            foreach(var weapon in new[]{DodgeWeapon.Bat,DodgeWeapon.Hammer})
            {
                Clean();yield return new WaitForSeconds(.12f);int hits=B.HitCount;
                A.Arsenal.Equip(weapon);Check(A.Arsenal.ConsumeAttack()==weapon && A.Arsenal.Charges==2,weapon+" consumes exactly one charge");
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
            var glove=DodgeBall.Spawn(B.Puppet.BodyPath[3],Vector3.forward*22,A,false,DodgeGame.Instance.ballMaterial,DodgeWeapon.Glove);
            yield return new WaitForSeconds(.1f);Check(B.HitCount==1 && !glove.IsDangerous,"Glove projectile hits once and becomes harmless");
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
            foreach(var weapon in new[]{DodgeWeapon.Bat,DodgeWeapon.Hammer,DodgeWeapon.Glove,DodgeWeapon.Bomb})
            {
                var drop=DodgeAirdrop.Spawn(DodgeGame.Instance,new Vector3(3,0,-5),weapon,true);DodgeBall.Spawn(drop.AimPosition,Vector3.forward,A,false,DodgeGame.Instance.ballMaterial);yield return new WaitForSeconds(.06f);Check(A.Arsenal.Weapon==weapon && A.Arsenal.Charges==3,"Owned ball claims "+weapon+" with three uses");
            }
            App.BackToLobby();Check(A.Kit.Skills==0 && A.Kit.Item==DodgeItem.None && DodgePartyEffects.Instance.ActiveCount==0,"Rematch clears match-local skills, items and effects");
        }
    }
}
#endif
