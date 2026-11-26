using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeMvpSmoke
    {
        bool armoryCheck, heldEmpty, returnedGun, sawStock, sawGrant, sawTransfer, firedForRefill;
        bool sawAmmoWindup, sawMarkedFlight;
        Vector3 warnedLanding;
        DodgeCrowd ammoCrowd;
        int firstCarrier=-1;
        float staged, handoff;
        DodgeInput ArmoryInput()
        {
            if(role=="host" || App.Room.leg!=1 || App.Page!=DodgePage.Playing)return default;
            var actor=App.Actors[App.LocalSlot];
            var gear=actor.Arsenal;
            if(gear.Weapon==DodgeWeapon.Rocket)
            {
                if(gear.Charges==0)
                {
                    heldEmpty=true;
                    var ammo=DodgeAirdrop.Active.FirstOrDefault(drop=>drop.Kind==DodgeGiftKind.Ammo && drop.Landed);
                    if(!ammo)return default;
                    var shot=AimAtSign(actor,ammo.AimPosition);
                    shot.pressed |= DodgeButtons.Standard;
                    firedForRefill |= shot.Down(DodgeButtons.Fire);
                    return shot;
                }
                if(heldEmpty && gear.Charges>0)
                {
                    if(handoff==0)handoff=Time.unscaledTime+1.4f;
                    if(Time.unscaledTime>handoff){returnedGun=true;return new DodgeInput{pressed=DodgeButtons.Drop};}
                }
                return default;
            }
            if(returnedGun)return default;
            var gun=FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="落地 · 毒刺火箭筒" && Mathf.Abs(b.position.z)<10f);
            if(!gun)return default;
            var frame=Toward(gun.position);
            frame.pressed=DodgeButtons.Collect;
            return frame;
        }

        void CheckArmory()
        {
            bool host=role=="host";
            var fx=DodgePartyEffects.Instance;
            if(host && App.Walkable){App.SetPaused(false);App.Controllers[0].RemoteControlled=true;}
            if(host && App.Page==DodgePage.Lobby && step==0)
            {
                if(Time.unscaledTime>nextState)
                {
                    nextState=Time.unscaledTime+.4f;
                    File.WriteAllText(Path.Combine(folder,"host-state.txt"),string.Join("\n",App.Room.seats.Select((s,i)=>i+" "+s.name+" slot")));
                }
                if(App.Net.ConnectedHumans==3 && App.CanStart)
                {Check(App.StartMatch(),"Start a real LAN round");step=1;}
                return;
            }
            if(App.Page==DodgePage.Staging && App.Room.leg==1)
            {
                sawStock |= fx.LooseCount==4;
                if(host && step==1)
                {
                    Check(fx.LooseCount==4,"Two physical weapons per changing room");
                    var drops=FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("落地 · ")).ToList();
                    Check(drops.Count==4 && drops.All(b=>!b.isKinematic && b.GetComponent<Collider>().enabled) &&
                        drops.Where(b=>b.position.z<0).Select(b=>b.name).OrderBy(x=>x).SequenceEqual(drops.Where(b=>b.position.z>0).Select(b=>b.name).OrderBy(x=>x)),
                        "Both teams receive identical stocks with active rigidbodies and colliders");
                    int team=App.Room.seats[0].team;
                    Check(App.Room.funds.All(f=>f==6),"Each team opens with six coins");
                    Check(App.Reroll(App.Actors[0]) && !App.Reroll(App.Actors[0]) && App.Room.funds[team]==5,"Only one paid refresh");
                    Check(App.Shop(App.Actors[0],DodgeWeapon.Rocket,DodgeItem.None) && App.Room.funds[team]==1 &&
                        !App.Shop(App.Actors[0],DodgeWeapon.Standard,DodgeItem.SprintShot),"Shared balance debits atomically and rejects overspending");
                    int other=Array.FindIndex(App.Room.seats,s=>s.team==1-team && s.kind==DodgeSeatKind.Human);
                    Check(App.Shop(App.Actors[other],DodgeWeapon.Standard,DodgeItem.ThunderPill) && !App.Shop(App.Actors[other],DodgeWeapon.Standard,DodgeItem.SprintShot),"One drug slot, no charge for a rejected second purchase");
                    Check(App.PickTactic(App.Actors[0],1) && App.Room.seats.Where(s=>s.team==team && s.kind!=DodgeSeatKind.Empty).All(s=>s.pick==1),"Team members share the selected card");
                    Check(App.PickTactic(App.Actors[other],0),"Other team makes its own choice");
                    staged=Time.unscaledTime;step=2;
                }
                if(host && step==2 && Time.unscaledTime-staged>1.2f)
                {
                    for(int i=0;i<5;i++)App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),0);
                    step=3;
                }
            }
            if(App.Page==DodgePage.Playing && App.Room.leg==1)
            {
                if(!ammoCrowd)ammoCrowd=FindFirstObjectByType<DodgeCrowd>();
                if(!sawAmmoWindup && ammoCrowd && ammoCrowd.Pending && ammoCrowd.GetComponentsInChildren<MeshFilter>().Any(m=>m.name=="FoamAmmo"))
                {
                    sawAmmoWindup=true;warnedLanding=ammoCrowd.LockedTargetPoint;
                }
                var flying=DodgeAirdrop.Active.FirstOrDefault(g=>g.FromCrowd && !g.Landed && g.Kind==DodgeGiftKind.Ammo);
                if(sawAmmoWindup && !sawMarkedFlight && flying && Vector3.Distance(warnedLanding,flying.LandingPoint)<.03f)
                {
                    var rim=new Vector3(warnedLanding.x+1.05f,.075f,warnedLanding.z);
                    sawMarkedFlight=flying.GetComponentsInChildren<MeshFilter>().Any(m=>m.name=="FoamAmmo") &&
                        FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Any(line=>line.name=="Airdrop landing ring" && Vector3.Distance(line.GetPosition(0),rim)<.03f);
                }
                if(host && step==3)
                {
                    for(int i=3;i<5;i++)App.Actors[i].GetComponent<DodgeBot>().enabled=false;
                    App.PlaceActor(0,new Vector3(1,.05f,-1),0);
                    App.PlaceActor(1,new Vector3(-.4f,.05f,-.8f),0);
                    App.PlaceActor(2,new Vector3(1.8f,.05f,.8f),180);
                    Check(App.Actors[0].Arsenal.ConsumeAttack()==DodgeWeapon.Rocket && fx.DropWeapon(App.Actors[0]),"An empty purchased launcher can be dropped");
                    App.PlaceActor(0,new Vector3(-7,.05f,-8),0);step=4;
                }
                int[] carriers=Enumerable.Range(1,2).Where(i=>App.Actors[i].Arsenal.Weapon==DodgeWeapon.Rocket).ToArray();
                if(carriers.Length>1)Check(false,"A contested gun was duplicated");
                if(carriers.Length==1)
                {
                    int slot=carriers[0];var gun=App.Actors[slot].Arsenal;
                    if(firstCarrier<0 && gun.Charges==0)firstCarrier=slot;
                    sawGrant |= slot==firstCarrier && gun.Charges==1 && !gun.CanRefill;
                    sawTransfer |= firstCarrier>=0 && slot!=firstCarrier && gun.Charges==1;
                }
                if(host && sawTransfer && step==4)
                {
                    Check(sawGrant,"A remote carrier shot the crowd's foam ammo and gained one charge");
                    Check(!App.Actors[firstCarrier].Arsenal.CanRefill,"An empty-handed player cannot consume crowd ammo");
                    Check(App.Room.seats[1].team!=App.Room.seats[2].team,"The second carrier stole the loaded gun across teams");
                    staged=Time.unscaledTime;step=5;
                }
                if(host && step==5 && Time.unscaledTime-staged>1.3f)App.FinishMatch();
            }
            if(host && App.Page==DodgePage.Results && step==5)
            {Check(App.ContinueSeries(),"Next round reopens the changing room");step=6;}
            if(step!=7 && App.Page==DodgePage.Staging && App.Room.leg==2 && fx.LooseCount==4)
            {
                Check(sawAmmoWindup && sawMarkedFlight,"The fan held the ammo model, threw it and marked the same landing point throughout flight");
                Check(sawStock && sawGrant && sawTransfer,"Free stock, empty pickup, refill and enemy transfer replicated");
                Check(App.Room.funds.All(n=>n==7) && App.Room.rerolled.All(r=>!r),"Carry capped at one and reroll reset");
                Check(App.Actors.All(a=>a.Arsenal.Weapon==DodgeWeapon.Standard),"No equipped weapon carried into the next round");
                if(!host && heldEmpty)Check(firedForRefill && returnedGun,"Remote client fired and dropped using network input");
                if(!host){report.passed=true;Finish();}
                else {step=7;staged=Time.unscaledTime;}
            }
            if(host && step==7 && Time.unscaledTime-staged>2){report.passed=true;Finish();}
        }
    }
}
