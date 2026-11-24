using UnityEngine;
namespace SuperDodgeball
{
    public static class DodgeCombat
    {
        public static bool IsMelee(DodgeWeapon weapon)=>weapon==DodgeWeapon.Bat || weapon==DodgeWeapon.Hammer;
        public static DodgeBall Release(DodgeActor actor,DodgeWeapon weapon,Vector3 origin,Vector3 aim,Vector3 velocity,float power,bool hostile,DodgeActor target)
        {
            if(IsMelee(weapon))
            {
                actor.Kit.BeginSwing(weapon,aim);
                DodgePartyEffects.Instance.Melee(actor,weapon,aim,power);return null;
            }
            if(weapon==DodgeWeapon.Bomb)
            {
                // Throw from ahead of the torso; a wall still catches the full flight sweep.
                Vector3 from=actor.transform.position+Vector3.up*1.3f+actor.transform.forward*.65f;
                if(DodgeBall.Trace(actor.transform.position+Vector3.up*1.3f,from,.3f,null,new RaycastHit[64],out RaycastHit wall,out DodgeActor ignored,false,false))from=wall.point+wall.normal*.32f;
                DodgePartyEffects.Instance.Spawn(DodgeEffect.Bomb,from,DodgeBall.SolveLaunchVelocity(from,aim,14),actor,3f);return null;
            }
            var ball=DodgeBall.Spawn(origin,velocity,actor,hostile,DodgeGame.Instance.ballMaterial,weapon,target);ball.SetPower(power);return ball;
        }
    }
}
