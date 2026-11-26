using UnityEngine;

namespace SuperDodgeball
{
    public sealed partial class DodgeReferee
    {
        const float TrespassLimit=3f, ChaseLimit=10f;
        internal bool SlipGuard(Vector3 peel)
        {
            foreach(var guard in guards) if(guard && guard.Slip(peel))return true;
            return false;
        }
        readonly DodgeSecurityGuard[,] guards=new DodgeSecurityGuard[5,2];
        readonly float[] chase=new float[5],escortTotal=new float[5];
        readonly Vector3[] lastTarget=new Vector3[5],destination=new Vector3[5];
        readonly bool[] warned=new bool[5],itemChase=new bool[5];
        public bool Chasing(int slot)=>chase[slot]>0;
        public float SeenSeconds(int slot)=>trespass[slot];
        void BeginChase(int slot,bool forItem)
        {
            if(Chasing(slot) || detention[slot]>0)return;
            chase[slot]=ChaseLimit;itemChase[slot]=forItem;GuardCalls++;
            var p=app.Actors[slot].transform.position;lastTarget[slot]=p;
            float sign=p.z>=0?1:-1;
            for(int s=0;s<2;s++)
            {
                var entry=new Vector3((s==0?-1:1)*(DodgeCourt.PlayWidth-1.3f),.04f,sign*(DodgeCourt.PlayDepth-2.2f));
                guards[slot,s].Enter(entry,p);
            }
        }
        void Dismiss(int slot)
        {
            chase[slot]=0;trespass[slot]=0;warned[slot]=false;
            for(int s=0;s<2;s++)guards[slot,s].Leave();
        }
        void Pursue(int slot,float dt)
        {
            var actor=app.Actors[slot];var p=actor.transform.position;
            float home=p.z*(app.Room.seats[slot].team==0?-1:1);
            if((!itemChase[slot] && home>=1f) || actor.IsStunned || chase[slot]<=dt)
            {Dismiss(slot);grace[slot]=1.8f;return;}
            chase[slot]-=dt;
            var velocity=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(p-lastTarget[slot],Vector3.up)/Mathf.Max(dt,.001f),12f);
            lastTarget[slot]=p;
            for(int s=0;s<2;s++)
            {
                if(!guards[slot,s].Chase(actor,velocity,dt))continue;
                chase[slot]=0;trespass[slot]=0;warned[slot]=false;
                caughtAt[slot]=p;
                destination[slot]=new Vector3(Mathf.Clamp(p.x,-8f,8f),.04f,app.Room.seats[slot].team==0?-4.2f:4.2f);
                detention[slot]=escortTotal[slot]=Mathf.Max(1.35f,Vector3.Distance(p,destination[slot])/5.8f+.48f);
                app.Controllers[slot].CancelForReferee();
                var bot=actor.GetComponent<DodgeBot>();if(bot)bot.CancelForReferee();
                actor.GetComponent<CharacterController>().enabled=false;
                break;
            }
        }
        void Escort(int slot,float dt)
        {
            var actor=app.Actors[slot];detention[slot]=Mathf.Max(0,detention[slot]-dt);
            float t=1-detention[slot]/escortTotal[slot];
            Vector3 p=Vector3.Lerp(caughtAt[slot],destination[slot],Mathf.SmoothStep(0,1,t));
            float lift=Mathf.Clamp01(t/.15f)*Mathf.Clamp01((1-t)/.16f);
            p.y=Mathf.Max(p.y,DodgeVenueDirector.WindHeight(p));
            p.y+=lift*(.66f+.045f*Mathf.Sin(20*Clock));actor.transform.position=p;
            actor.SetDive(lift*.65f);actor.SetMovement(Vector2.zero);actor.SetLocomotion(0);
            Vector3 forward=Vector3.ProjectOnPlane(destination[slot]-caughtAt[slot],Vector3.up).normalized;
            if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            var side=Vector3.Cross(Vector3.up,forward);
            for(int s=0;s<2;s++)guards[slot,s].Carry(new Vector3(p.x,.04f,p.z)+side*(s==0?-.62f:.62f),forward,dt);
            if(detention[slot]>0)return;
            actor.SetDive(0);actor.GetComponent<CharacterController>().enabled=true;
            strikes[slot]=0;grace[slot]=1.8f;Dismiss(slot);
            game.EmitFoam(p,Vector3.up,6,.85f);
        }
    }
}
