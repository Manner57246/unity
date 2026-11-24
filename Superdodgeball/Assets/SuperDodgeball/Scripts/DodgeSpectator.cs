using System.IO;
using UnityEngine;
namespace SuperDodgeball
{
    public sealed class DodgeSpectatorHitbox:MonoBehaviour {public DodgeSpectator spectator;}

    // Six real rigid bodies are released only while this spectator is reacting.
    public sealed class DodgeSpectator:MonoBehaviour
    {
        public const int BoneCount=6;
        public bool Reacting=>Remaining>0;
        public float Remaining {get;private set;}
        public int Hits {get;private set;}
        public int Index {get;private set;}
        public Vector3 AimPosition=>bones[1]?bones[1].position:transform.position+Vector3.up*1.5f;
        public Vector3 BodyPosition=>bones[0]?bones[0].position:transform.position+Vector3.up;
        public Rigidbody Body=>bodies[0];
        public bool Ready=>bodies[0];
        public bool Recovering=>Reacting && Remaining<=.75f;
        readonly Transform[] bones=new Transform[BoneCount];
        readonly Rigidbody[] bodies=new Rigidbody[BoneCount];
        readonly Vector3[] restPosition=new Vector3[BoneCount],savedVelocity=new Vector3[BoneCount],savedAngular=new Vector3[BoneCount],risePosition=new Vector3[BoneCount],replicaPosition=new Vector3[BoneCount];
        readonly Quaternion[] restRotation=new Quaternion[BoneCount],riseRotation=new Quaternion[BoneCount],replicaRotation=new Quaternion[BoneCount];
        DodgeCrowd crowd;bool frozen,recovering,replicaReceived;float cooldown;
        public void Initialize(DodgeCrowd owner,int index,DodgeCrowd.Seat seat,PhysicsMaterial material)
        {
            if(Ready)return;
            crowd=owner;Index=index;
            bones[0]=seat.body;bones[1]=seat.head;bones[2]=seat.arm;bones[3]=seat.otherArm;bones[4]=seat.leftLeg;bones[5]=seat.rightLeg;
            for(int i=0;i<BoneCount;i++)
            {
                var bone=bones[i];restPosition[i]=transform.InverseTransformPoint(bone.position);restRotation[i]=Quaternion.Inverse(transform.rotation)*bone.rotation;
                var body=bone.gameObject.AddComponent<Rigidbody>();bodies[i]=body;body.isKinematic=true;body.useGravity=true;
                body.mass=i==0?.9f:i==1?.3f:.18f;body.linearDamping=.45f;body.angularDamping=.8f;body.maxAngularVelocity=12;body.solverIterations=6;
                Collider collider;
                if(i==1){var sphere=bone.gameObject.AddComponent<SphereCollider>();sphere.radius=.27f;collider=sphere;}
                else
                {
                    var capsule=bone.gameObject.AddComponent<CapsuleCollider>();capsule.radius=i==0?.27f:i<4?.095f:.115f;capsule.height=i==0?.67f:.58f;
                    capsule.center=i==0?Vector3.zero:i==2?Vector3.up*.22f:i==3?Vector3.down*.22f:new Vector3(0,-.16f,.27f);collider=capsule;
                }
                collider.sharedMaterial=material;bone.gameObject.AddComponent<DodgeSpectatorHitbox>().spectator=this;
                if(i>0)
                {
                    var joint=bone.gameObject.AddComponent<ConfigurableJoint>();joint.connectedBody=bodies[0];joint.autoConfigureConnectedAnchor=false;
                    joint.anchor=Vector3.zero;joint.connectedAnchor=bones[0].InverseTransformPoint(bone.position);
                    joint.xMotion=joint.yMotion=joint.zMotion=ConfigurableJointMotion.Locked;
                    joint.angularXMotion=joint.angularYMotion=joint.angularZMotion=ConfigurableJointMotion.Limited;
                    joint.lowAngularXLimit=new SoftJointLimit{limit=-55};joint.highAngularXLimit=new SoftJointLimit{limit=65};
                    joint.angularYLimit=new SoftJointLimit{limit=60};joint.angularZLimit=new SoftJointLimit{limit=65};
                    joint.projectionMode=JointProjectionMode.PositionAndRotation;joint.projectionDistance=.1f;joint.projectionAngle=25;
                    joint.enablePreprocessing=false;
                }
            }
            for(int i=0;i<BoneCount;i++)for(int j=i+1;j<BoneCount;j++)Physics.IgnoreCollision(bones[i].GetComponent<Collider>(),bones[j].GetComponent<Collider>());
        }
        public bool Hit(Vector3 point,Vector3 impulse,DodgeActor source=null)
        {
            if(!Ready || DodgeGame.IsReplica || !DodgeGame.SimulationActive || !DodgeGame.MatchActive || cooldown>0 || DodgeReferee.Restricted(source))return false;
            if(DodgeApp.Active && DodgeApp.Instance.Page!=DodgePage.Playing)return false;
            crowd.BeforeSpectatorHit(this);Hits++;crowd.RecordSpectatorHit(Index);Remaining=3.7f;cooldown=.65f;recovering=frozen=false;
            Physics.SyncTransforms();
            Vector3 push=Vector3.ClampMagnitude(impulse,9f);
            for(int i=0;i<BoneCount;i++)
            {
                bodies[i].isKinematic=false;bodies[i].collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                bodies[i].linearVelocity=Vector3.zero;bodies[i].angularVelocity=Vector3.zero;bodies[i].AddForce(push,ForceMode.VelocityChange);
            }
            int nearest=0;for(int i=1;i<BoneCount;i++)if((bones[i].position-point).sqrMagnitude<(bones[nearest].position-point).sqrMagnitude)nearest=i;
            bodies[nearest].AddTorque(Vector3.Cross(point-BodyPosition,push)*2f+transform.forward*(Index%2==0?1.8f:-1.8f),ForceMode.VelocityChange);
            DodgeGame.Instance.EmitFoam(point,Vector3.up,7,1);DodgeGame.Instance.PlayTone(180+Index%5*45,.09f,.035f);return true;
        }
        public void Step(float dt,bool running)
        {
            if(!Ready || DodgeGame.IsReplica)return;
            if(!Reacting){if(running)cooldown=Mathf.Max(0,cooldown-dt);return;}
            if(!running)
            {
                if(!frozen)
                {
                    for(int i=0;i<BoneCount;i++){savedVelocity[i]=bodies[i].isKinematic?Vector3.zero:bodies[i].linearVelocity;savedAngular[i]=bodies[i].isKinematic?Vector3.zero:bodies[i].angularVelocity;bodies[i].isKinematic=true;}
                    frozen=true;
                }
                return;
            }
            if(frozen)
            {
                frozen=false;
                if(!recovering)for(int i=0;i<BoneCount;i++){bodies[i].isKinematic=false;bodies[i].linearVelocity=savedVelocity[i];bodies[i].angularVelocity=savedAngular[i];}
            }
            cooldown=Mathf.Max(0,cooldown-dt);Remaining=Mathf.Max(0,Remaining-dt);
            if(Remaining<=.75f)
            {
                if(!recovering)
                {
                    recovering=true;
                    for(int i=0;i<BoneCount;i++){risePosition[i]=bones[i].position;riseRotation[i]=bones[i].rotation;bodies[i].isKinematic=true;}
                }
                float t=Mathf.SmoothStep(0,1,1-Remaining/.75f);
                for(int i=0;i<BoneCount;i++)bones[i].SetPositionAndRotation(Vector3.Lerp(risePosition[i],transform.TransformPoint(restPosition[i]),t),Quaternion.Slerp(riseRotation[i],transform.rotation*restRotation[i],t));
            }
            if(Remaining<=0)ResetPose(false);
        }
        public void ResetPose(bool clearHits)
        {
            Remaining=cooldown=0;recovering=frozen=replicaReceived=false;if(clearHits)Hits=0;
            if(!Ready)return;
            for(int i=0;i<BoneCount;i++)
            {
                bodies[i].isKinematic=true;bodies[i].collisionDetectionMode=CollisionDetectionMode.Discrete;
                bones[i].SetPositionAndRotation(transform.TransformPoint(restPosition[i]),transform.rotation*restRotation[i]);bones[i].localScale=Vector3.one;
            }
        }
        public void WritePose(BinaryWriter w)
        {
            w.Write((byte)Index);w.Write(Hits);w.Write(Remaining);
            for(int i=0;i<BoneCount;i++){DodgeNet.WriteVector(w,transform.InverseTransformPoint(bones[i].position));DodgeNet.WriteRotation(w,Quaternion.Inverse(transform.rotation)*bones[i].rotation);}
        }
        public void ReadPose(BinaryReader r)
        {
            Hits=r.ReadInt32();Remaining=r.ReadSingle();
            if(Remaining<=0 || Remaining>4 || Hits<0)throw new InvalidDataException("Invalid spectator state");
            for(int i=0;i<BoneCount;i++)
            {
                replicaPosition[i]=transform.TransformPoint(DodgeNet.ReadVector(r));replicaRotation[i]=transform.rotation*DodgeNet.ReadRotation(r);
                bodies[i].isKinematic=true;
                if(!replicaReceived)bones[i].SetPositionAndRotation(replicaPosition[i],replicaRotation[i]);
            }
            replicaReceived=true;
        }
        void LateUpdate()
        {
            if(!DodgeGame.IsReplica || !replicaReceived || !Reacting)return;
            float t=1-Mathf.Exp(-28*Time.deltaTime);
            for(int i=0;i<BoneCount;i++)bones[i].SetPositionAndRotation(Vector3.Lerp(bones[i].position,replicaPosition[i],t),Quaternion.Slerp(bones[i].rotation,replicaRotation[i],t));
        }
    }
}
