using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    // A physical puppet follows an invisible pose rig. No Transform writes are made to
    // dynamic bodies: joints keep the skeleton together and their drives act as muscles.
    [DefaultExecutionOrder(30)]
    public sealed class DodgeRagdoll : MonoBehaviour
    {
        sealed class Bone
        {
            public Rigidbody body;
            public Transform target;
            public float strength;
            public Transform parentTarget;
            public Quaternion bindRotation;
            public ConfigurableJoint joint;
            public Vector3 restAnchor, restConnected;
            public int spineIndex = -1;
            public Vector3 previousTargetPosition, targetVelocity;
            public Vector3 pausedVelocity, pausedAngularVelocity;
        }
        sealed class Skin
        {
            public Transform source, copy;
        }
        readonly List<Bone> bones = new List<Bone>();
        readonly List<Skin> skins = new List<Skin>();
        readonly List<Collider> colliders = new List<Collider>();
        readonly List<Renderer> colored = new List<Renderer>();
        DodgeActor actor;
        GameObject assembly;
        Rigidbody pelvis, throwingHand;
        Transform handTarget, heldCopy;
        CharacterController motor;
        Vector3 initialPosition;
        Quaternion initialRotation;
        float limpUntil, standUntil;
        float riseStarted = -10f;
        Vector3 riseFrom;
        Quaternion riseRotation;
        bool loose, paused;
        bool launched;
        float airRecoveryDeadline;
        float pausedAt;
        Vector3 pausedPelvisVelocity, pausedPelvisAngularVelocity;
        DodgeSpine spine;
        Rigidbody[] spineBodies;
        DodgeTube torsoSurface;
        readonly List<System.Action> drawSurfaces = new List<System.Action>();
        public DodgeWardrobe Wardrobe { get; private set; }
        public Vector3[] BodyPath { get; private set; }
        public float[] BodyRadii { get; private set; }
        public Transform HeadSocket { get; private set; }
        public float JointError
        {
            get
            {
                float error = 0f;
                foreach (var bone in bones)
                    error = Mathf.Max(error, Vector3.Distance(bone.body.transform.TransformPoint(bone.joint.anchor),
                        bone.joint.connectedBody.transform.TransformPoint(bone.joint.connectedAnchor)));
                return error;
            }
        }
        public bool Ready => pelvis;
        public bool NetworkReplica { get; private set; }
        Vector3[] netPositions;
        Quaternion[] netRotations;
        public void SetNetworkReplica(bool value)
        {
            if (NetworkReplica == value) return;
            NetworkReplica = value;
            if (!Ready) return;
            pelvis.isKinematic = value || !loose;
            foreach (var bone in bones) bone.body.isKinematic = value;
        }
        public void SetVisible(bool value) { if (assembly) assembly.SetActive(value); }
        public void WritePose(System.IO.BinaryWriter writer)
        {
            writer.Write((byte)BodyCount);
            WriteBody(writer, pelvis);
            foreach (var bone in bones) WriteBody(writer, bone.body);
        }
        static void WriteBody(System.IO.BinaryWriter w, Rigidbody body)
        {
            DodgeNet.WriteVector(w, body.position); DodgeNet.WriteRotation(w, body.rotation);
        }
        public void ReadPose(System.IO.BinaryReader r)
        {
            int count = r.ReadByte();
            if (count != BodyCount || count > 24) throw new System.IO.InvalidDataException("Incompatible character skeleton");
            if (netPositions == null || netPositions.Length != count) { netPositions = new Vector3[count]; netRotations = new Quaternion[count]; }
            for (int i = 0; i < count; i++) { netPositions[i] = DodgeNet.ReadVector(r); netRotations[i] = DodgeNet.ReadRotation(r); }
            SetNetworkReplica(true);
        }
        public bool IsStunned => loose || Time.time < standUntil;
        public bool IsLoose => loose;
        public int BodyCount => bones.Count + (pelvis ? 1 : 0);
        internal bool BlastContact(Vector3 origin,float radius,out Vector3 nearest)
        {
            nearest=Vector3.zero;
            float best=radius*radius;
            bool exposed=false;
            foreach(var shape in colliders)
            {
                if(!shape || !shape.enabled || !shape.gameObject.activeInHierarchy ||
                    !shape.TryGetComponent<DodgeHitbox>(out var zone) || zone.actor!=actor)continue;
                Vector3 p=shape.ClosestPoint(origin);
                float distance=(p-origin).sqrMagnitude;
                if(distance>best || (distance>.0001f && !DodgePartyEffects.LineClear(origin,p)))continue;
                nearest=p; best=distance; exposed=true;
            }
            return exposed;
        }
        public Vector3 BodyCenter => bones.Count > 0 ? bones[0].body.position : transform.position + Vector3.up;
        public Vector3 HandPosition => throwingHand
            ? throwingHand.position + throwingHand.rotation * Quaternion.Inverse(handTarget.rotation) * (actor.heldBall.position - handTarget.position)
            : actor.heldBall.position;
        public float PoseError
        {
            get
            {
                float result = 0f;
                foreach (var bone in bones) result = Mathf.Max(result, Vector3.Distance(bone.body.position, bone.target.position));
                return result;
            }
        }

        public void Initialize(DodgeActor owner)
        {
            if (Ready || !owner.leftForearm) return;
            actor = owner;
            motor = GetComponent<CharacterController>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            assembly = new GameObject(name + " - physical puppet");
            pelvis = NewBody("Pelvis", actor.upperBody.position, transform.rotation, 2f);
            pelvis.isKinematic = true;
            var hipCollider = pelvis.gameObject.AddComponent<SphereCollider>();
            hipCollider.radius = 0.23f;
            pelvis.gameObject.AddComponent<DodgeHitbox>().actor = actor;
            colliders.Add(hipCollider);
            spine = actor.Spine;
            Rigidbody torso;
            if (spine)
            {
                spine.Pose(actor, 0f, 0f);
                spineBodies = new Rigidbody[DodgeSpine.Segments];
                torso = pelvis;
                for (int i = 0; i < spineBodies.Length; i++)
                {
                    torso = AddBone(spine.sections[i], torso, spine.sections[i].position, 0.28f, 550f, 100f, false);
                    spineBodies[i] = torso;
                    bones[bones.Count - 1].spineIndex = i;
                    var capsule = torso.gameObject.AddComponent<CapsuleCollider>();
                    capsule.direction = 1;
                    capsule.radius = spine.Radius(i / (float)spineBodies.Length) * 0.94f;
                    capsule.height = spine.Length(i) + capsule.radius * 1.5f;
                    capsule.center = Vector3.up * (spine.Length(i) * 0.5f);
                    torso.gameObject.AddComponent<DodgeHitbox>().actor = actor;
                    colliders.Add(capsule);
                }
            }
            else torso = AddBone(actor.upperBody.Find("Torso hitbox"), pelvis, actor.upperBody.position, 3f, 125f, 100f);
            var physicalHead = AddBone(actor.head, torso, actor.head.position - actor.head.up * 0.18f, 0.9f, 70f, 85f);
            HeadSocket = new GameObject("Hat socket").transform;
            HeadSocket.SetParent(physicalHead.transform, false);
            HeadSocket.localPosition = Vector3.up * 0.31f;
            foreach (int side in new[] { -1, 1 })
            {
                Transform shoulder = side < 0 ? actor.leftArm : actor.rightArm;
                Transform elbow = side < 0 ? actor.leftForearm : actor.rightForearm;
                Transform hip = side < 0 ? actor.leftLeg : actor.rightLeg;
                Transform knee = side < 0 ? actor.leftShin : actor.rightShin;
                var upperArm = AddBone(shoulder.Find("Arm hitbox"), torso, shoulder.position, 0.4f, 120f, 160f);
                if(spine)
                {
                    Muscle(torso,"Thunder pec "+side,new Vector3(side*.235f,-.15f,.23f),new Vector3(.59f,.39f,.40f),side*10);
                    Muscle(upperArm,"Thunder deltoid "+side,new Vector3(side*.075f,.115f,0),new Vector3(.50f,.46f,.49f),side*12);
                    Muscle(upperArm,"Thunder biceps "+side,new Vector3(side*.085f,-.12f,.045f),new Vector3(.39f,.51f,.39f),-side*10);
                }
                var forearm = AddBone(elbow.Find("Forearm hitbox"), upperArm, elbow.position, 0.3f, 115f, 145f);
                CopySkin(elbow.Find("Hand hitbox"), forearm, true);
                var thigh = AddBone(hip.Find("Leg hitbox"), pelvis, hip.position, 0.65f, 120f, 125f);
                var shin = AddBone(knee.Find("Shin hitbox"), thigh, knee.position, 0.45f, 100f, 125f);
                var foot=CopySkin(knee.Find("Foot hitbox"), shin, false);
                if(spine)DodgeWardrobe.Sneaker(foot,side,actor.bodyRenderers[0].sharedMaterial);
                if (spine)
                {
                    BuildLimbSurface(upperArm, forearm, 0.16f, 0.14f, 0.29f, 0.14f);
                    BuildLimbSurface(thigh, shin, 0.16f, 0.14f, 0.28f, 0.15f, true);
                }
                if (side > 0)
                {
                    throwingHand = forearm;
                    handTarget = elbow.Find("Forearm hitbox");
                }
            }
            var badge = actor.upperBody.Find("Bean number");
            if (badge && !spine) CopySkin(badge, torso, false);
            if (actor.heldBall) heldCopy = CopySkin(actor.heldBall, throwingHand, false);
            foreach (var collider in colliders)
            {
                foreach (var other in colliders) if (other != collider) Physics.IgnoreCollision(collider, other);
                foreach (var controller in FindObjectsByType<CharacterController>(FindObjectsSortMode.None)) Physics.IgnoreCollision(collider, controller);
            }
            // Only the physical meshes and their colliders remain visible / hittable.
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var hitbox in actor.GetComponentsInChildren<DodgeHitbox>(true)) hitbox.GetComponent<Collider>().enabled = false;
            if (spine)
            {
                BodyPath = new Vector3[DodgeSpine.Segments + 1];
                BodyRadii = new float[BodyPath.Length];
                torsoSurface = NewTube("Continuous pear body", actor.bodyRenderers[0].sharedMaterial);
                colored.Add(torsoSurface.Surface);
                Wardrobe = assembly.AddComponent<DodgeWardrobe>();
                Wardrobe.Initialize(this, actor.bodyRenderers[0].sharedMaterial);
            }
            SnapToPose();
        }

        DodgeTube NewTube(string label, Material material)
        {
            var go = new GameObject(label);
            go.transform.SetParent(assembly.transform, false);
            var tube = go.AddComponent<DodgeTube>();
            tube.Initialize(material);
            return tube;
        }

        void BuildLimbSurface(Rigidbody upper, Rigidbody lower, float upperHalf, float lowerHalf, float end, float radius, bool shorts = false)
        {
            foreach (var body in new[] { upper, lower })
            {
                var renderer = body.transform.GetChild(0).GetComponent<Renderer>();
                if (renderer) renderer.enabled = false;
            }
            var tube = NewTube("Continuous limb", actor.bodyRenderers[0].sharedMaterial);
            colored.Add(tube.Surface);
            DodgeTube garment=NewTube(shorts?"Track shorts":"Shooting sleeve",actor.bodyRenderers[0].sharedMaterial);
            DodgeWardrobe.Tint(garment.Surface,shorts?DodgeColleges.Navy:DodgeColleges.Cream);
            var path = new Vector3[3];
            var sizes = new[] { radius, radius * 0.94f, radius * 0.86f };
            drawSurfaces.Add(() =>
            {
                path[0] = upper.position + upper.rotation * Vector3.up * upperHalf;
                path[1] = lower.position + lower.rotation * Vector3.up * lowerHalf;
                path[2] = lower.position - lower.rotation * Vector3.up * end;
                tube.Draw(path, sizes, transform.forward, 1f);
                if (garment)
                {
                    garment.gameObject.SetActive(Wardrobe && !Wardrobe.Olympian && (shorts || Wardrobe.Style==2));
                    if (garment.gameObject.activeSelf) garment.Draw(path, sizes, transform.forward, 1f, 0f, shorts ? 0.60f : .33f, 0.027f);
                }
            });
        }

        readonly List<(Transform mesh,Vector3 size)> muscles=new List<(Transform,Vector3)>();
        float drawnMuscle=-1;
        void Muscle(Rigidbody bone,string label,Vector3 offset,Vector3 size,float roll)
        {
            var shape=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shape.name=label;
            shape.transform.SetParent(bone.transform,false);
            shape.transform.localPosition=offset;shape.transform.localRotation=Quaternion.Euler(0,0,roll);
            var collider=shape.GetComponent<SphereCollider>();
            collider.enabled=false;Destroy(collider);
            var zone=shape.AddComponent<BoxCollider>();zone.isTrigger=true;zone.size=Vector3.one*.79f;
            shape.AddComponent<DodgeHitbox>().actor=actor;
            var surface=shape.GetComponent<Renderer>();surface.sharedMaterial=actor.bodyRenderers[0].sharedMaterial;
            colored.Add(surface);muscles.Add((shape.transform,size));shape.SetActive(false);
        }
        void DrawMuscles()
        {
            float blend=actor.Kit.MuscleBlend * transform.localScale.x;
            if(Mathf.Approximately(drawnMuscle,blend))return;
            drawnMuscle=blend;
            foreach(var muscle in muscles)
            {
                muscle.mesh.localScale=muscle.size*blend;
                muscle.mesh.gameObject.SetActive(blend>.02f);
            }
        }
        Rigidbody NewBody(string label, Vector3 position, Quaternion rotation, float mass)
        {
            var go = new GameObject(label);
            go.transform.SetParent(assembly.transform);
            go.transform.SetPositionAndRotation(position, rotation);
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.linearDamping = 0.4f;
            body.angularDamping = 0.7f;
            body.maxAngularVelocity = 20f;
            body.maxDepenetrationVelocity = 3f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations = 12;
            body.solverVelocityIterations = 6;
            return body;
        }

        Rigidbody AddBone(Transform source, Rigidbody parent, Vector3 anchor, float mass, float strength, float limit, bool skin = true)
        {
            var body = NewBody(source.name, source.position, source.rotation, mass);
            var bone = new Bone { body = body, target = source, strength = strength, previousTargetPosition = source.position };
            bones.Add(bone);
            if (skin) CopySkin(source, body, true);
            var joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = parent;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = body.transform.InverseTransformPoint(anchor);
            joint.connectedAnchor = parent.transform.InverseTransformPoint(anchor);
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -limit, contactDistance = 3f };
            joint.highAngularXLimit = new SoftJointLimit { limit = limit, contactDistance = 3f };
            joint.angularYLimit = joint.angularZLimit = new SoftJointLimit { limit = limit, contactDistance = 3f };
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.04f;
            joint.projectionAngle = 12f;
            joint.enableCollision = false;
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            bone.joint = joint; bone.restAnchor=joint.anchor; bone.restConnected=joint.connectedAnchor;
            bone.bindRotation = Quaternion.Inverse(parent.rotation) * body.rotation;
            foreach (var possibleParent in bones)
                if (possibleParent.body == parent) bone.parentTarget = possibleParent.target;
            return body;
        }

        Transform CopySkin(Transform source, Rigidbody body, bool tint)
        {
            var copy = Instantiate(source.gameObject, body.transform).transform;
            copy.name = source.name + " - physical";
            copy.SetPositionAndRotation(source.position, source.rotation);
            copy.localScale = source.lossyScale;
            foreach (var collider in copy.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = true;
                collider.isTrigger = false;
                colliders.Add(collider);
                var marker = collider.GetComponent<DodgeHitbox>();
                if (marker) marker.actor = actor;
            }
            if (tint) colored.Add(copy.GetComponent<Renderer>());
            skins.Add(new Skin { source = source, copy = copy });
            return copy;
        }

        void FixedUpdate()
        {
            if (!Ready) return;
            if (NetworkReplica) return;
            foreach (var bone in bones)
            {
                bone.targetVelocity = Vector3.ClampMagnitude((bone.target.position - bone.previousTargetPosition) / Time.fixedDeltaTime, 25f);
                bone.previousTargetPosition = bone.target.position;
            }
            bool shouldPause = DodgeGame.Instance && DodgeGame.Instance.RoundStarted && !DodgeGame.SimulationActive;
            if (shouldPause != paused)
            {
                if (shouldPause)
                {
                    pausedAt = Time.time;
                    pausedPelvisVelocity = pelvis.linearVelocity;
                    pausedPelvisAngularVelocity = pelvis.angularVelocity;
                    foreach (var bone in bones)
                    {
                        bone.pausedVelocity = bone.body.linearVelocity;
                        bone.pausedAngularVelocity = bone.body.angularVelocity;
                    }
                }
                else
                {
                    float duration = Time.time - pausedAt;
                    limpUntil += duration;
                    standUntil += duration;
                    airRecoveryDeadline += duration;
                    riseStarted += duration;
                }
                paused = shouldPause;
                foreach (var bone in bones) bone.body.isKinematic = paused;
                pelvis.isKinematic = paused || !loose;
                if (!paused)
                {
                    foreach (var bone in bones)
                    {
                        bone.body.linearVelocity = bone.pausedVelocity;
                        bone.body.angularVelocity = bone.pausedAngularVelocity;
                    }
                    if (loose)
                    {
                        pelvis.linearVelocity = pausedPelvisVelocity;
                        pelvis.angularVelocity = pausedPelvisAngularVelocity;
                    }
                }
            }
            if (paused) return;
            if (loose)
            {
                if(DodgeVenueDirector.InUpdraft)
                {
                    float speed=pelvis.linearVelocity.y;
                    DodgeVenueDirector.ApplyWind(pelvis.position-Vector3.up*.72f,ref speed);
                    Vector3 lift=Vector3.up*(speed-pelvis.linearVelocity.y);
                    pelvis.AddForce(lift,ForceMode.VelocityChange);
                    foreach(var bone in bones)bone.body.AddForce(lift,ForceMode.VelocityChange);
                }
                if (Time.time < limpUntil || (launched && pelvis.position.y > 1.15f && Time.time < airRecoveryDeadline))
                {
                    transform.position = pelvis.position - Vector3.up * 0.72f;
                    return;
                }
                loose = false;
                launched = false;
                standUntil = Time.time + 0.65f;
                riseFrom = pelvis.position;
                riseRotation = pelvis.rotation;
                riseStarted = Time.time;
                Vector3 position = pelvis.position;
                position.x = Mathf.Clamp(position.x, -10.4f, 10.4f);
                position.z = Mathf.Clamp(position.z, -13.3f, 13.3f);
                position.y = DodgeVenueDirector.InUpdraft?Mathf.Max(.05f,pelvis.position.y-.72f):.05f;
                transform.position = position;
                pelvis.isKinematic = true;
                if (motor) motor.enabled = true;
                actor.Land();
            }
            float rise = Mathf.SmoothStep(0f, 1f, (Time.time - riseStarted) / 0.65f);
            pelvis.MovePosition(Vector3.Lerp(riseFrom, actor.upperBody.position, rise));
            pelvis.MoveRotation(Quaternion.Slerp(riseRotation, transform.rotation, rise));
            float recovery = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Max(0f, standUntil - Time.time) / 0.65f);
            foreach (var bone in bones)
            {
                float scale=transform.localScale.x;
                bone.joint.anchor=bone.restAnchor*scale;
                if (bone.spineIndex < 0) bone.joint.connectedAnchor=bone.restConnected*scale;
                if (bone.spineIndex >= 0)
                {
                    int i = bone.spineIndex;
                    // Cartoon stretch changes the length of each section, not the mesh's
                    // thickness. The same lengths place joint anchors and hit capsules.
                    bone.joint.connectedAnchor = i == 0 ? Vector3.zero : Vector3.up * spine.Length(i - 1);
                    var capsule = bone.body.GetComponent<CapsuleCollider>();
                    capsule.radius = spine.Radius((i + 0.5f) / spineBodies.Length) * 0.94f;
                    capsule.height = spine.Length(i) + capsule.radius * 1.5f;
                    capsule.center = Vector3.up * (spine.Length(i) * 0.5f);
                }
                else if (spine && bone.parentTarget == spine.sections[DodgeSpine.Segments - 1])
                {
                    Vector3 anchor = bone.body.name == actor.head.name
                        ? actor.head.position - actor.head.up * (0.18f*scale)
                        : bone.target.position + bone.target.up * (0.16f*scale);
                    bone.joint.connectedAnchor = bone.parentTarget.InverseTransformPoint(anchor)*transform.localScale.x;
                }
                // Distributed muscle support lets a curl or airborne dive reach its
                // pose inside the dodge window. Hands and head need support: their
                // inertia would otherwise pull the end of the long spine open.
                // Hit() releases all support throughout the limp phase.
                if (spine && (bone.spineIndex >= 0 || actor.ShapeAmount > 0.05f || actor.DiveAmount > .05f))
                {
                    float shape = Mathf.Max(actor.ShapeAmount, actor.DiveAmount);
                    Vector3 support = (bone.target.position - bone.body.position) * Mathf.Lerp(150f, 500f, shape)
                        + (bone.targetVelocity - bone.body.linearVelocity) * Mathf.Lerp(16f, 32f, shape);
                    bone.body.AddForce(Vector3.ClampMagnitude(support, 180f) * recovery, ForceMode.Acceleration);
                }
                Quaternion parentRotation = bone.parentTarget ? bone.parentTarget.rotation : transform.rotation;
                Quaternion relative = Quaternion.Inverse(parentRotation) * bone.target.rotation;
                // Joint axes are X / Y, so their basis is the identity rotation.
                bone.joint.targetRotation = Quaternion.Inverse(relative) * bone.bindRotation;
                bone.joint.slerpDrive = new JointDrive
                {
                    positionSpring = bone.strength * 6f * recovery,
                    positionDamper = bone.strength * 0.22f * Mathf.Lerp(0.1f, 1f, recovery),
                    maximumForce = 1500f
                };
            }
        }

        // Project the actual outfit and puppet meshes into one squash pose. Joints and
        // rigidbodies retain their normal transforms, even while the ragdoll is moving.
        readonly List<MeshFilter> flatMeshes = new List<MeshFilter>();
        readonly List<MeshRenderer> flatRenderers = new List<MeshRenderer>();
        MaterialPropertyBlock flatColor;
        DodgeWeapon flatWeapon;
        void RestoreFlatRenderers()
        {
            foreach(var renderer in flatRenderers)if(renderer)renderer.forceRenderingOff=false;
            flatRenderers.Clear();flatMeshes.Clear();
        }
        void DrawHeavySquash()
        {
            float amount=actor.Kit.HeavySquash;
            if(amount<=0)
            {
                if(flatMeshes.Count>0)RestoreFlatRenderers();
                return;
            }
            if(flatMeshes.Count==0 || flatWeapon!=actor.Arsenal.Selected)
            {
                RestoreFlatRenderers();flatWeapon=actor.Arsenal.Selected;
                foreach(var mesh in assembly.GetComponentsInChildren<MeshFilter>(true))
                    if(mesh.TryGetComponent<MeshRenderer>(out var renderer) && mesh.sharedMesh)
                    {flatMeshes.Add(mesh);flatRenderers.Add(renderer);renderer.forceRenderingOff=true;}
                if(flatColor==null)flatColor=new MaterialPropertyBlock();
            }
            var pivot=new Vector3(pelvis.position.x,transform.position.y,pelvis.position.z);
            var shape=Matrix4x4.Translate(pivot)*Matrix4x4.Scale(new Vector3(1+.4f*amount,1-.74f*amount,1+.36f*amount))*Matrix4x4.Translate(-pivot);
            for(int i=0;i<flatMeshes.Count;i++)
            {
                var renderer=flatRenderers[i];
                if(!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                renderer.GetPropertyBlock(flatColor);
                var geometry=flatMeshes[i];
                var settings=new RenderParams(renderer.sharedMaterial){matProps=flatColor,
                    layer=renderer.gameObject.layer,shadowCastingMode=renderer.shadowCastingMode,receiveShadows=renderer.receiveShadows};
                Graphics.RenderMesh(settings,geometry.sharedMesh,0,shape*renderer.localToWorldMatrix);
            }
        }
        void OnDisable(){RestoreFlatRenderers();}

        void LateUpdate()
        {
            if (!Ready) return;
            if (NetworkReplica && netPositions != null)
            {
                float blend = 1f - Mathf.Exp(-32f * Time.deltaTime);
                for (int i = 0; i < netPositions.Length; i++)
                {
                    var body = i == 0 ? pelvis : bones[i - 1].body;
                    float amount = (body.position - netPositions[i]).sqrMagnitude > 9f ? 1f : blend;
                    body.position = Vector3.Lerp(body.position, netPositions[i], amount);
                    body.rotation = Quaternion.Slerp(body.rotation, netRotations[i], amount);
                }
            }
            pelvis.GetComponent<SphereCollider>().radius=.23f*transform.localScale.x;
            DrawMuscles();
            foreach (var skin in skins)
            {
                if (!skin.source || !skin.copy) continue;
                skin.copy.localScale = skin.source.lossyScale;
            }
            if (heldCopy)
            {
                var player = actor.GetComponent<DodgePlayer>();
                var bot = actor.GetComponent<DodgeBot>();
                actor.Arsenal.DrawHeld(heldCopy, DodgeGame.Instance.popMaterial,
                    actor.Zeus.Transformed ? DodgeWeapon.Thunderbolt : DodgeGame.IsReplica ? actor.ReplicaVisualWeapon : player && player.enabled ? player.VisualWeapon : bot ? bot.VisualWeapon : actor.Arsenal.Selected);
                heldCopy.gameObject.SetActive(actor.heldBall.gameObject.activeSelf && !IsStunned);
            }
            if (torsoSurface)
            {
                for (int i = 0; i < spineBodies.Length; i++)
                {
                    BodyPath[i] = spineBodies[i].position;
                    BodyRadii[i] = spine.Radius(i / (float)spineBodies.Length)*(1f+.38f*actor.Kit.HeavySquash);
                }
                int last = spineBodies.Length - 1;
                BodyPath[last + 1] = spineBodies[last].position + spineBodies[last].rotation * Vector3.up * spine.Length(last);
                BodyRadii[last + 1] = spine.Radius(1f)*(1f+.38f*actor.Kit.HeavySquash);
                torsoSurface.Draw(BodyPath, BodyRadii, transform.forward);
                foreach (var draw in drawSurfaces) draw();
                Wardrobe.Draw();
                DrawHeavySquash();
            }
        }

        public void Hit(Vector3 point, Vector3 impulse, float launch = 0f, float carry = 3f)
        {
            if (!Ready) return;
            loose = true;
            limpUntil = Time.time + 0.85f;
            standUntil = limpUntil + 0.65f;
            launched = launch > 0f;
            airRecoveryDeadline = Time.time + 3.5f;
            if (motor) motor.enabled = false;
            pelvis.isKinematic = false;
            foreach (var bone in bones) bone.joint.slerpDrive = new JointDrive();
            if (impulse.sqrMagnitude < 0.01f) impulse = -transform.forward * 4f + Vector3.up;
            impulse = Vector3.ClampMagnitude(impulse, 10f);
            Bone nearest = bones[0];
            foreach (var bone in bones)
                if ((bone.body.position - point).sqrMagnitude < (nearest.body.position - point).sqrMagnitude) nearest = bone;
            nearest.body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            pelvis.AddForce(impulse * 0.35f + Vector3.up * 0.6f, ForceMode.Impulse);
            if (launched)
            {
                Vector3 flight = Vector3.ProjectOnPlane(impulse, Vector3.up).normalized * carry + Vector3.up * Mathf.Clamp(launch, 0f, 10f);
                pelvis.AddForce(flight, ForceMode.VelocityChange);
                foreach (var bone in bones) bone.body.AddForce(flight, ForceMode.VelocityChange);
            }
        }

        public void SetColor(MaterialPropertyBlock color)
        {
            foreach (var renderer in colored) if (renderer) renderer.SetPropertyBlock(color);
        }

        public void SnapToPose()
        {
            if (!Ready) return;
            pelvis.position = actor.upperBody.position;
            pelvis.rotation = transform.rotation;
            foreach (var bone in bones)
            {
                bone.body.position = bone.target.position;
                bone.body.rotation = bone.target.rotation;
                bone.previousTargetPosition = bone.target.position;
                if (!bone.body.isKinematic) { bone.body.linearVelocity = Vector3.zero; bone.body.angularVelocity = Vector3.zero; }
            }
            Physics.SyncTransforms();
        }

        public void ResetPuppet()
        {
            if (!Ready) return;
            loose = paused = false;
            launched = false;
            limpUntil = standUntil = 0f;
            riseStarted = -10f;
            if (motor) motor.enabled = false;
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            pelvis.isKinematic = true;
            foreach (var bone in bones) bone.body.isKinematic = false;
            if (motor) motor.enabled = true;
            SnapToPose();
        }

        void OnDestroy()
        {
            if (assembly) Destroy(assembly);
        }
    }
}
