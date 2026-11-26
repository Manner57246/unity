using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgePartyEffects
    {
        public static float BlastRadius(DodgeWeapon weapon,float charge=0f)
        {
            return weapon switch
            {
                DodgeWeapon.Thunderbolt => Mathf.Lerp(3.5f,5.5f,charge),
                DodgeWeapon.Rocket => 4.25f,
                DodgeWeapon.Bomb => 4.75f,
                DodgeWeapon.Vest => 5.75f,
                _ => 0
            };
        }
        public void RocketImpact(Vector3 point,DodgeActor source,DodgeActor contact,DodgeSpectator fan)
        {
            if(!DodgeApp.Instance.IsHost || !DodgeApp.Instance.MatchRunning)return;
            DamageBlast(point,source,DodgeWeapon.Rocket,6.4f,6f,contact,fan);
            Spawn(DodgeEffect.RocketBurst,point,Vector3.zero,source,1.40f);
        }
        internal void ThunderImpact(Vector3 point,Vector3 normal,DodgeActor source,DodgeActor contact,DodgeSpectator fan,float charge)
        {
            if(!DodgeApp.Instance.IsHost || !DodgeApp.Instance.MatchRunning)return;
            DamageBlast(point+normal*.065f,source,DodgeWeapon.Thunderbolt,1.3f,2.5f,contact,fan,charge);
            Spawn(DodgeEffect.Thunderstrike,point,normal,source,.76f+.22f*charge);
        }
        internal void VestImpact(Vector3 point,DodgeActor source)
        {
            if(!DodgeApp.Instance.IsHost || !DodgeApp.Instance.MatchRunning)return;
            DamageBlast(point,source,DodgeWeapon.Vest,8f,13.8f,null,null);
            Spawn(DodgeEffect.VestBurst,point,Vector3.zero,source,2.65f);
        }
        void DamageBlast(Vector3 point,DodgeActor source,DodgeWeapon weapon,float lift,float carry,
            DodgeActor contact,DodgeSpectator directFan,float charge=0)
        {
            var app=DodgeApp.Instance;
            bool electric=weapon==DodgeWeapon.Thunderbolt;
            if(!electric)Explosions++;
            float radius=BlastRadius(weapon,charge);
            foreach(var target in app.Actors)
            {
                if(!source || !target.isActiveAndEnabled || target==source || !app.CanHurt(source,target))continue;
                Vector3 hit=point;
                if(target!=contact && !target.Puppet.BlastContact(point,radius,out hit))continue;
                float attenuation=Mathf.Lerp(1f,.55f,Mathf.SmoothStep(0,1,Vector3.Distance(point,hit)/radius));
                Vector3 push=Vector3.ProjectOnPlane(target.Puppet.BodyCenter-point,Vector3.up);
                if(push.sqrMagnitude<.01f)push=source.transform.forward;
                if(Strike(source,target,hit,(push.normalized*7f+Vector3.up*3f)*attenuation,lift*attenuation,carry*attenuation) && electric)
                    target.Zeus.Electrify((.85f+.45f*charge)*attenuation);
            }
            var crowd=FindFirstObjectByType<DodgeCrowd>();
            if(crowd)foreach(var spectator in crowd.Spectators)
            {
                var delta=spectator.BodyPosition-point;
                if(spectator==directFan || (delta.sqrMagnitude<radius*radius && LineClear(point,spectator.BodyPosition)))
                    spectator.Hit(spectator.BodyPosition,delta.normalized*6+Vector3.up*3,source);
            }
            if(!electric)DodgeGame.Instance.PlayTone(75,.17f,.12f);
        }
    }
    public sealed partial class DodgePartyKit
    {
        public float ReturnTime {get;private set;}
        public float ArmedTime {get;private set;}
        const float FuseSeconds=1.2f, RecoverySeconds=3.2f, FlightSeconds=.95f;
        Vector3 returnFrom,returnTo;
        Transform vest;
        bool heavyFlat;
        public bool TryPopVest()
        {
            var app=DodgeApp.Instance;
            if(!app.IsHost || !app.MatchRunning || !DodgeGame.SimulationActive || actor.IsStunned ||
               DodgeReferee.Restricted(actor) || app.Referee.Phase!=DodgeRefereePhase.Slacking || ArmedTime>0 || !actor.Arsenal.WearingVest)return false;
            ArmedTime=FuseSeconds;
            DodgeGame.Instance.PlayTone(810,.12f,.11f);
            return true;
        }
        void TickFuse(float dt)
        {
            if(ArmedTime<=0)return;
            var app=DodgeApp.Instance;
            if(!actor.Arsenal.WearingVest || !app.MatchRunning || actor.IsStunned || DodgeReferee.Restricted(actor))
            {ArmedTime=0;return;}
            ArmedTime=Mathf.Max(0,ArmedTime-dt);
            if(ArmedTime>0)return;
            int slot=app.SeatOf(actor);
            if(slot<0)return;
            actor.Arsenal.ApplyReplica(DodgeWeapon.Vest,0,true);
            var upright=transform.position+Vector3.up;
            DodgePartyEffects.Instance.VestImpact(upright,actor);
            returnFrom=transform.position;
            returnTo=new Vector3(Mathf.Clamp(returnFrom.x*.6f,-8f,8f),.05f,app.Room.seats[slot].team==0?-8f:8f);
            ReturnTime=RecoverySeconds;
            app.Controllers[slot].CancelForReferee();
            var bot=actor.GetComponent<DodgeBot>();if(bot && bot.enabled)bot.CancelForReferee();
        }
        void TravelHome(float dt)
        {
            if(ReturnTime<=0)return;
            ReturnTime=Mathf.Max(0,ReturnTime-dt);
            float t=Mathf.Clamp01((RecoverySeconds-ReturnTime)/FlightSeconds);
            var p=Vector3.Lerp(returnFrom,returnTo,Mathf.SmoothStep(0,1,t))+Vector3.up*(3.25f*4*t*(1-t));
            p.y=Mathf.Max(p.y,DodgeVenueDirector.WindHeight(p));
            transform.position=p;
        }
        void PoseVest()
        {
            bool wearing=actor.Arsenal.WearingVest;
            if(wearing && !vest)vest=DodgeWeaponArt.Model(transform,DodgeWeaponArt.Prop.Vest);
            if(!vest)return;
            vest.gameObject.SetActive(wearing && actor.gameObject.activeInHierarchy);
            if(!wearing || !actor.Puppet || actor.Puppet.BodyPath==null)return;
            vest.localScale=Vector3.one*(ArmedTime>0?1.06f+.065f*Mathf.Sin(ArmedTime*30):1);
            var path=actor.Puppet.BodyPath;
            vest.position=Vector3.Lerp(path[0],path[path.Length-1],.57f);
            var up=path[path.Length-1]-path[0];
            var forward=Vector3.ProjectOnPlane(transform.forward,up);
            if(up.sqrMagnitude>.0001f && forward.sqrMagnitude>.0001f)vest.rotation=Quaternion.LookRotation(forward,up);
        }
    }
}
