using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    // Only the host moves the solid motor and resolves a grab. Replicas pose the
    // same guard from snapshots, including the tell before a committed lunge.
    internal sealed class DodgeSecurityGuard : MonoBehaviour
    {
        internal enum Move { Run, Windup, Lunge, Miss, Carry }
        const float Windup=.31f,Lunge=.32f,Miss=.66f;
        CharacterController motor;
        Transform leftArm,rightArm,leftFoot,rightFoot;
        int side;
        float left,heading,speed;
        Vector3 aim,netPoint;
        float netHeading;
        bool replica,received;
        internal Move Action { get; private set; }
        internal Vector3 Position => transform.position;
        readonly Collider[] contacts=new Collider[64];

        internal void Initialize(int flank)
        {
            side=flank;
            leftArm=transform.Find("Left arm");rightArm=transform.Find("Right arm");
            leftFoot=transform.Find("Left foot");rightFoot=transform.Find("Right foot");
            motor=gameObject.AddComponent<CharacterController>();
            motor.enabled=false;motor.height=1.92f;motor.radius=.42f;motor.center=Vector3.up*.98f;
            motor.skinWidth=.025f;motor.stepOffset=.16f;motor.minMoveDistance=0;
            gameObject.SetActive(false);
        }
        internal void Enter(Vector3 pos,Vector3 target)
        {
            replica=false;received=false;motor.enabled=false;transform.position=pos;
            var delta=target-pos;heading=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;
            Face(heading);gameObject.SetActive(true);motor.enabled=true;
            Action=Move.Run;left=speed=0;
        }
        internal void Leave()
        {
            motor.enabled=false;gameObject.SetActive(false);received=false;
        }
        void Face(float yaw){transform.rotation=Quaternion.Euler(0,yaw+180,0);}
        static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
        internal bool Slip(Vector3 peel)
        {
            if(!gameObject.activeInHierarchy || !motor.enabled || Action==Move.Carry || Action==Move.Miss ||
                Vector3.ProjectOnPlane(Position-peel,Vector3.up).sqrMagnitude>.72f*.72f)return false;
            Action=Move.Miss; left=1.35f; speed=0; return true;
        }
        internal bool Chase(DodgeActor target,Vector3 targetVelocity,float dt)
        {
            var delta=Flat(target.transform.position-Position);
            left-=dt;
            if(Action==Move.Windup && left<=0){Action=Move.Lunge;left=Lunge;}
            else if(Action==Move.Lunge && left<=0){Action=Move.Miss;left=Miss;}
            else if(Action==Move.Miss && left<=0)Action=Move.Run;
            Vector3 step=Vector3.zero;
            if(Action==Move.Run)
            {
                float lead=side==0?.09f:.38f;
                var wanted=delta+targetVelocity*lead;
                if(side==1 && delta.magnitude>3f)
                    wanted+=Vector3.Cross(Vector3.up,delta.normalized)*Mathf.Min(2.2f,delta.magnitude*.24f);
                heading=Mathf.MoveTowardsAngle(heading,Mathf.Atan2(wanted.x,wanted.z)*Mathf.Rad2Deg,370*dt);
                var forward=Quaternion.Euler(0,heading,0)*Vector3.forward;
                speed=Mathf.MoveTowards(speed,side==0?6.35f:6.7f,14f*dt);
                if(delta.magnitude<1.95f && Vector3.Dot(forward,delta.normalized)>.63f)
                {
                    aim=(delta+targetVelocity*.12f).normalized;
                    if(aim.sqrMagnitude<.01f)aim=forward;
                    heading=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;
                    Action=Move.Windup;left=Windup;speed=0;
                }
                else step=forward*speed*dt;
            }
            else if(Action==Move.Lunge)step=aim*9.0f*dt;
            var before=Position;
            Face(heading);
            motor.Move(step+Vector3.down*2*dt);
            return Action==Move.Lunge && Touches(target,before,Position);
        }
        bool Touches(DodgeActor target,Vector3 from,Vector3 to)
        {
            // Chest-height reach sweeps with the guards' arms. A low dive can pass
            // underneath, a sidestep can leave the line after the wind-up.
            Vector3 hand=aim*.57f+Vector3.up*1.26f;
            int count=Physics.OverlapCapsuleNonAlloc(from+hand,to+hand,.38f,contacts,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var body=contacts[i].GetComponent<DodgeHitbox>();
                if(body && body.actor==target && DodgePartyEffects.LineClear(to+hand,body.transform.position))return true;
            }
            return false;
        }
        internal void Carry(Vector3 next,Vector3 forward,float dt)
        {
            motor.enabled=false;Action=Move.Carry;
            transform.position=Vector3.MoveTowards(Position,next,10.5f*dt);
            if(forward.sqrMagnitude>.001f)heading=Mathf.MoveTowardsAngle(heading,Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,460*dt);
            Face(heading);
        }
        internal void Pose(float clock,float dt)
        {
            if(!gameObject.activeSelf)return;
            if(replica)
            {
                float blend=1-Mathf.Exp(-32*dt);
                transform.position=Vector3.Lerp(Position,netPoint,blend);
                heading=Mathf.LerpAngle(heading,netHeading,blend);Face(heading);
            }
            float stride=Mathf.Sin(clock*13+side*2.7f);
            bool run=Action==Move.Run || Action==Move.Carry;
            float arm=Action==Move.Windup?-122:Action==Move.Lunge?82:Action==Move.Carry?60:Action==Move.Miss?18:0;
            leftArm.localRotation=Quaternion.Euler(arm+(run?stride*32:0),0,Action==Move.Windup?-45:-14);
            rightArm.localRotation=Quaternion.Euler(arm-(run?stride*32:0),0,Action==Move.Windup?45:14);
            leftFoot.localRotation=Quaternion.Euler(run?stride*34:0,0,0);
            rightFoot.localRotation=Quaternion.Euler(run?-stride*34:0,0,0);
        }
        internal void Write(BinaryWriter w)
        {
            DodgeNet.WriteVector(w,Position);w.Write(heading);w.Write((byte)Action);
        }
        internal void Read(BinaryReader r)
        {
            replica=true;netPoint=DodgeNet.ReadVector(r);netHeading=r.ReadSingle();Action=(Move)r.ReadByte();
            if(received)return;
            gameObject.SetActive(true);motor.enabled=false;transform.position=netPoint;heading=netHeading;
            Face(heading);received=true;
        }
    }
}
