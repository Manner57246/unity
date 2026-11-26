using UnityEngine;
namespace SuperDodgeball
{
    // Physical signs work through the same swept ball collisions as the clubhouse.
    public sealed class DodgeRestTarget:MonoBehaviour
    {
        public int Team,Skill=-1,DraftSlot=-1;
        public DodgeItem Item;
        public DodgeWeapon StockWeapon;
        public bool Refresh;
        public static bool CanAim(DodgeActor actor,Vector3 aim)
        {
            if(!actor || !DodgeCourt.InRest(actor.transform.position))return false;
            foreach(var target in FindObjectsByType<DodgeRestTarget>(FindObjectsSortMode.None))
                if(target.Eligible(actor) && Vector3.Distance(aim,target.transform.position)<1.7f)return true;
            return false;
        }
        bool Eligible(DodgeActor actor)
        {
            var app=DodgeApp.Instance;int slot=app.SeatOf(actor);
            if (slot < 0 || app.Room.seats[slot].team != Team) return false;
            Vector3 p = actor.transform.position;
            return app.Drafting && DodgeCourt.InRest(p) && (p.z > 0 ? 1 : 0) == Team;
        }
        public bool Hit(DodgeBall ball)
        {
            if(DodgeGame.IsReplica || !ball.owner || ball.IsEgg || !ball.IsDangerous || !Eligible(ball.owner))return false;
            if(ball.Weapon!=DodgeWeapon.Standard)return false;
            var app=DodgeApp.Instance;
            bool success=Refresh ? app.Reroll(ball.owner) :
                DraftSlot>=0 ? app.PickTactic(ball.owner,DraftSlot) : app.Shop(ball.owner,StockWeapon,Item);
            if(success){DodgeGame.Instance.EmitFoam(transform.position,Vector3.up,10,1);DodgeGame.Instance.PlayTone(880,.12f,.08f);}
            else DodgeGame.Instance.PlayTone(180,.08f,.04f);
            return true;
        }
        public static void ResetBots(){}
    }
}
