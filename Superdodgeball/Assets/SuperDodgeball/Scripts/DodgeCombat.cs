using UnityEngine;
namespace SuperDodgeball
{
    public static class DodgeCombat
    {
        static readonly RaycastHit[] muzzleHits=new RaycastHit[32];
        public static bool IsMelee(DodgeWeapon weapon)=>weapon==DodgeWeapon.Bat || weapon==DodgeWeapon.Hammer;
        public static bool IsLauncher(DodgeWeapon w)=>w==DodgeWeapon.Burst || w==DodgeWeapon.Rocket;
        public static bool UsesThrowPose(DodgeWeapon w)=>!IsMelee(w) && !IsLauncher(w);
        static Vector3 Shoulder(DodgeActor actor)=>actor.transform.TransformPoint(new Vector3(.40f,1.33f,.08f));
        public static void PoseHeld(DodgeActor actor,Transform prop,DodgeWeapon weapon)
        {
            var heading=actor.transform.rotation;
            if(IsLauncher(weapon))
            {
                var aim=actor.Kit.LauncherAim;
                if(DodgeGame.IsReplica && actor.isPlayer)aim=DodgeGame.Instance.view.AimPoint;
                Vector3 direction=aim-Shoulder(actor);
                if(direction.sqrMagnitude<.01f || Vector3.Dot(direction,actor.transform.forward)<0)direction=actor.transform.forward;
                prop.SetPositionAndRotation(Shoulder(actor)-direction.normalized*actor.Kit.LauncherRecoil*.15f,Quaternion.LookRotation(direction));
                return;
            }
            float yaw=68f, tilt=56f;
            if(actor.Kit.Swing>0 && actor.Kit.SwingWeapon==DodgeWeapon.Bat)
            {
                Vector3 flat=Vector3.ProjectOnPlane(actor.Kit.SwingAim-actor.transform.position,Vector3.up);
                if(flat.sqrMagnitude>.01f)heading=Quaternion.LookRotation(flat);
                float age=.54f-actor.Kit.Swing;
                float wind=Mathf.SmoothStep(0,1,age/.07f);
                float slice=Mathf.SmoothStep(0,1,(age-.07f)/.23f);
                float reset=Mathf.SmoothStep(0,1,(age-.32f)/.22f);
                yaw=Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(68,102,wind),-76,slice),68,reset);
                tilt=Mathf.Lerp(Mathf.Lerp(56,10,wind),56,reset);
            }
            Vector3 axis=heading*Quaternion.Euler(-tilt,yaw,0)*Vector3.forward;
            prop.SetPositionAndRotation(actor.Puppet.HandPosition,Quaternion.FromToRotation(Vector3.up,axis));
        }
        public static DodgeBall Release(DodgeActor actor,DodgeWeapon weapon,Vector3 origin,Vector3 aim,Vector3 velocity,float power,bool hostile,DodgeActor target)
        {
            if (weapon == DodgeWeapon.Thunderbolt)
            {
                var axis = (aim-origin).normalized;
                if (axis.sqrMagnitude < .01f) axis = actor.transform.forward;
                velocity = axis * Mathf.Lerp(23f,40f,power) * actor.Kit.ThrowStrength;
            }
            if(IsMelee(weapon))
            {
                if(actor.Kit.Swing<=0)actor.Kit.BeginSwing(weapon,aim);
                DodgePartyEffects.Instance.Melee(actor,weapon,actor.Kit.SwingAim,power);return null;
            }
            if(weapon==DodgeWeapon.Bomb)
            {
                // Throw from ahead of the torso; a wall still catches the full flight sweep.
                Vector3 from=actor.transform.position+Vector3.up*1.3f+actor.transform.forward*.65f;
                if(DodgeBall.Trace(actor.transform.position+Vector3.up*1.3f,from,.3f,null,new RaycastHit[64],out RaycastHit wall,out DodgeActor ignored,false,false))from=wall.point+wall.normal*.32f;
                DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb,from,DodgeBall.SolveLaunchVelocity(from,aim,14*actor.Kit.ThrowStrength),actor,3f);return null;
            }
            if(IsLauncher(weapon))
            {
                var shoulder=Shoulder(actor);
                Vector3 axis=(aim-shoulder).normalized;
                if(axis.sqrMagnitude<.01f)axis=actor.transform.forward;
                float reach=DodgeBall.Trace(shoulder,shoulder+axis*.80f,.12f,actor,muzzleHits,out var wall,out _,false,false)
                    ? Mathf.Max(0,wall.distance-.025f) : .78f;
                origin=shoulder+axis*reach;
                velocity=weapon==DodgeWeapon.Rocket?axis*28f:DodgeBall.SolveLaunchVelocity(origin,aim,Mathf.Lerp(20,34,power));
            }
            var ball=DodgeBall.Spawn(origin,velocity,actor,hostile,DodgeGame.Instance.ballMaterial,weapon,target);ball.SetPower(power);
            if (weapon == DodgeWeapon.Standard && DodgeApp.Active && DodgeApp.Instance.MatchRunning && actor.Kit.TakeDouble())
            {
                Vector3 angled = Quaternion.AngleAxis(12f, Vector3.up) * velocity;
                var second = DodgeBall.Spawn(origin, angled, actor, hostile, DodgeGame.Instance.ballMaterial, weapon, target);
                second.SetPower(power);
            }
            return ball;
        }
    }
}
