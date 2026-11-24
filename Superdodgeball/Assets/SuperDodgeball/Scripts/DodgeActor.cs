using UnityEngine;

namespace SuperDodgeball
{
    public sealed class DodgeActor : MonoBehaviour
    {
        public bool isPlayer;
        public bool animatedTarget;
        public float targetPhase;
        public Transform upperBody;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform leftForearm, rightForearm, leftShin, rightShin;
        public Transform heldBall;
        public Transform head;
        public Renderer[] bodyRenderers;
        public Color bodyColor = new Color(0.12f, 0.8f, 0.91f);
        public float Lean { get; private set; }
        public float InvulnerableUntil { get; private set; }
        public int HitCount { get; private set; }
        public float DiveAmount { get; private set; }
        public float MotionAmount => locomotion;
        public float ThrowPulse => throwPulse;
        public float Windup => charge;
        public DodgeSpine Spine => spine ? spine : (spine = GetComponent<DodgeSpine>());
        public float ShapeAmount => Mathf.Max(lShape, oShape);
        public float LShape => lShape;
        public float OShape => oShape;
        public void ApplyReplica(float lean, float dive, float l, float o, float windup, float motion, float throwing)
        {
            animatedTarget = false;
            desiredLean = lean; desiredDive = dive; desiredL = l; desiredO = o;
            desiredCharge = windup; desiredLocomotion = motion;
            replicaThrow = throwing;
        }
        float replicaThrow;
        bool replicaHeldVisible = true;
        public DodgeWeapon ReplicaVisualWeapon;
        public void SetReplicaHeld(bool value) => replicaHeldVisible = value;
        DodgeSpine spine;
        float lShape, oShape, desiredL, desiredO;
        public void SetShape(float l, float o) { desiredL = Mathf.Clamp01(l); desiredO = Mathf.Clamp01(o); }
        public void SetShapeImmediate(float l, float o)
        {
            lShape = desiredL = Mathf.Clamp01(l); oShape = desiredO = Mathf.Clamp01(o);
            ApplyPose();
            if (puppet) puppet.SnapToPose();
        }
        bool replicaStunned;
        public bool IsStunned => DodgeGame.IsReplica ? replicaStunned : puppet && puppet.IsStunned;
        public void ApplyReplicaHits(bool stunned, int hitCount)
        {
            if (hitCount > HitCount)
            {
                hitPulse = 1f;
                DodgeGame.Instance.SpawnPop(puppet.BodyCenter, bodyColor);
                DodgeGame.Instance.PlayTone(isPlayer ? 110f : 760f, .08f, .06f);
            }
            if (hitCount > HitCount && isPlayer) DodgeGame.Instance.view.Kick(.04f);
            replicaStunned = stunned; HitCount = hitCount;
        }
        int replicaShots;
        public void ApplyReplicaShots(int value)
        {
            if (value > replicaShots)
            {
                DodgeGame.Instance.PlayTone(350f,.07f,isPlayer ? .065f : .025f);
                if (isPlayer) DodgeGame.Instance.view.Kick(.02f);
            }
            replicaShots = value;
        }
        public DodgeRagdoll Puppet => puppet;
        DodgePartyKit kit;
        public DodgePartyKit Kit => kit ? kit : (kit = GetComponent<DodgePartyKit>() ?? gameObject.AddComponent<DodgePartyKit>());
        DodgeArsenal arsenal;
        public DodgeArsenal Arsenal => arsenal ? arsenal : (arsenal = GetComponent<DodgeArsenal>() ?? gameObject.AddComponent<DodgeArsenal>());
        DodgeCrowdStatus crowdStatus;
        public DodgeCrowdStatus CrowdStatus => crowdStatus ? crowdStatus : (crowdStatus = GetComponent<DodgeCrowdStatus>() ?? gameObject.AddComponent<DodgeCrowdStatus>());
        public Vector3 HandPosition => puppet && puppet.Ready ? puppet.HandPosition : heldBall ? heldBall.position : upperBody.position;
        DodgeRagdoll puppet;
        float throwScale=1f;
        float throwStarted = -10f, throwWindup, releasedAt = -10f;
        float desiredLean;
        float leanVelocity;
        float desiredDive;
        float desiredLocomotion;
        float motionVelocity;
        float stride;
        float charge;
        float desiredCharge;
        float throwPulse;
        float landingPulse;
        Vector2 moveDirection;
        float hitPulse;
        float locomotion;
        MaterialPropertyBlock propertyBlock;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public void SetLean(float value) => desiredLean = Mathf.Clamp(value, -1f, 1f);

        public void SetLeanImmediate(float value)
        {
            Lean = desiredLean = Mathf.Clamp(value, -1f, 1f);
            ApplyPose();
            if (puppet) puppet.SnapToPose();
        }

        public void SetLocomotion(float speed) => desiredLocomotion = Mathf.Clamp01(speed);
        public void SetMovement(Vector2 direction) => moveDirection = direction;
        public void SetCharge(float value) => desiredCharge = Mathf.Clamp01(value);
        public void SetDive(float value) => desiredDive = Mathf.Clamp01(value);
        public void Throw(float power = 0f, float releaseDelay=.22f)
        {
            throwScale=Mathf.Clamp(releaseDelay/.22f,.35f,2.5f);
            throwStarted = Time.time;
            throwWindup = Mathf.Max(charge, 0.4f + power * 0.6f);
        }
        public void Land() { landingPulse = 1f; Kit.Land(); }
        public void ReleaseBall() => releasedAt = Time.time;

        void Start()
        {
            puppet = GetComponent<DodgeRagdoll>();
            if (puppet) puppet.Initialize(this);
        }

        public void SetDiveImmediate(float value)
        {
            DiveAmount = desiredDive = Mathf.Clamp01(value);
            ApplyPose();
            if (puppet) puppet.SnapToPose();
        }

        void Update()
        {
            if (!isPlayer && animatedTarget)
                desiredLean = Mathf.Sin(Time.time * 1.3f + targetPhase) * 0.88f;
            Lean = Mathf.SmoothDamp(Lean, desiredLean, ref leanVelocity, Kit.Has(DodgeSkill.RubberWaist) ? .045f : .065f);
            DiveAmount = Mathf.MoveTowards(DiveAmount, desiredDive, Time.deltaTime * 9f);
            locomotion = Mathf.SmoothDamp(locomotion, desiredLocomotion, ref motionVelocity, 0.1f);
            charge = Mathf.Lerp(charge, desiredCharge, 1f - Mathf.Exp(-14f * Time.deltaTime));
            stride += Time.deltaTime * locomotion * 13.5f;
            float blend = 1f - Mathf.Exp(-8f * Time.deltaTime);
            lShape = Mathf.Lerp(lShape, desiredL, blend);
            oShape = Mathf.Lerp(oShape, desiredO, blend);
            float throwTime = (Time.time - throwStarted)/throwScale;
            throwPulse = Mathf.SmoothStep(0f, 1f, throwTime / 0.18f) * (1f - Mathf.SmoothStep(0f, 1f, (throwTime - 0.3f) / 0.44f));
            if (DodgeGame.IsReplica) throwPulse = replicaThrow;
            landingPulse = Mathf.MoveTowards(landingPulse, 0f, Time.deltaTime * 3.5f);
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, Time.deltaTime * 3.5f);
            ApplyPose();
            ApplyColor();
        }

        void ApplyPose()
        {
            if (!upperBody) return;
            // Rotate above the hips: the real head/torso colliders move with the mesh.
            float now = Application.isPlaying ? Time.time : 0f;
            float run = locomotion * (1f - DiveAmount);
            float step = Mathf.Sin(stride);
            float bounce = Mathf.Abs(Mathf.Sin(stride)) * run;
            float breathe = Mathf.Sin(now * 2.6f + targetPhase) * 0.012f;
            float bend = -Lean * 72f * Kit.LeanScale;
            float wobble = Mathf.Sin(now * 2.8f + targetPhase) * 1.4f;
            float squash = hitPulse * 0.16f + landingPulse * 0.16f + bounce * 0.045f + charge * 0.035f;
            float wind = Mathf.SmoothStep(0f, 1f, charge);
            if ((Time.time - throwStarted)/throwScale < 0.74f) wind = throwWindup * (1f - Mathf.SmoothStep(0f, 1f, (Time.time - throwStarted)/throwScale / 0.18f));
            wind *= 1f - DiveAmount;
            float swing = throwPulse * (1f - DiveAmount);
            float throwTime = (Time.time - throwStarted)/throwScale;
            float rightPitch = -10f - step * run * 24f + wind * 130f;
            if (throwTime < 0.78f)
            {
                if (throwTime < 0.18f) rightPitch = Mathf.Lerp(-10f + throwWindup * 130f, 235f, Mathf.SmoothStep(0f, 1f, throwTime / 0.18f));
                else if (throwTime < 0.4f) rightPitch = Mathf.Lerp(235f, 310f, Mathf.SmoothStep(0f, 1f, (throwTime - 0.18f) / 0.22f));
                else rightPitch = Mathf.Lerp(310f, 350f, Mathf.SmoothStep(0f, 1f, (throwTime - 0.4f) / 0.38f));
            }
            upperBody.localPosition = new Vector3(0f, 0.72f + bounce * 0.065f + breathe - DiveAmount * 0.22f - landingPulse * 0.08f + wind * 0.04f - swing * 0.07f, -wind * 0.08f + swing * 0.15f);
            upperBody.localRotation = Quaternion.Euler(
                Mathf.Lerp(-moveDirection.y * run * 8f - wind * 25f + swing * 34f - hitPulse * 16f, 82f, DiveAmount),
                Mathf.Sin(stride) * run * 3f + wind * 38f - swing * 29f,
                (bend + wobble * (1f - Mathf.Abs(Lean)) - moveDirection.x * run * 6f + step * run * 2f) * (1f - DiveAmount));
            upperBody.localScale = new Vector3(1f + squash * 0.65f, 1f - squash - DiveAmount * 0.1f, 1f + squash * 0.6f);
            float armSwing = step * run * 24f;
            if (leftArm) leftArm.localRotation = Quaternion.Euler(Mathf.Lerp(-8f + armSwing - wind * 65f + swing * 55f, -165f, DiveAmount), -wind * 15f, -20f + Lean * 32f + DiveAmount * 9f);
            if (rightArm) rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(rightPitch, -165f, DiveAmount), -wind * 15f - swing * 20f,
                20f + Lean * 32f + wind * 25f - DiveAmount * 9f);
            if(Kit.Swing>0 && DodgeCombat.IsMelee(Kit.SwingWeapon))
            {
                float duration=Kit.SwingWeapon==DodgeWeapon.Hammer?.85f:.5f;
                float phase=1-Kit.Swing/duration;
                rightArm.localRotation=Quaternion.Euler(Kit.SwingWeapon==DodgeWeapon.Hammer?Mathf.Lerp(-160,40,Mathf.SmoothStep(0,1,phase*2)): -65,Mathf.Lerp(-100,120,Mathf.SmoothStep(0,1,phase*2)),25);
            }
            if (leftForearm) leftForearm.localRotation = Quaternion.Euler(12f + wind * 30f + swing * 20f, 0f, 0f);
            if (rightForearm) rightForearm.localRotation = Quaternion.Euler(12f + wind * 58f - swing * 4f, 0f, 0f);
            SetLeg(leftLeg, -1f, step * run * 29f);
            SetLeg(rightLeg, 1f, -step * run * 29f);
            if (leftLeg) leftLeg.localRotation *= Quaternion.Euler(-wind * 38f + swing * 10f, 0f, 0f);
            if (rightLeg) rightLeg.localRotation *= Quaternion.Euler(wind * 8f - swing * 20f, 0f, 0f);
            if (leftShin) leftShin.localRotation = Quaternion.Euler(5f + wind * 73f + DiveAmount * 29f, 0f, 0f);
            if (rightShin) rightShin.localRotation = Quaternion.Euler(5f + swing * 38f + DiveAmount * 39f, 0f, 0f);
            if (head)
                head.localRotation = Quaternion.Euler(wind * 13f - swing * 12f, -wind * 28f + swing * 20f, -Lean * 5f);
            if (Spine)
            {
                Spine.Pose(this, lShape, oShape);
                // Keep the hands below and outside the opening, rather than across it.
                if (ShapeAmount > 0f)
                {
                    leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(0f, 0f, -55f), ShapeAmount);
                    rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(0f, 0f, 55f), ShapeAmount);
                    leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.Euler(0f, 0f, -25f), ShapeAmount);
                    rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.Euler(0f, 0f, 25f), ShapeAmount);
                    leftArm.rotation = Quaternion.Slerp(leftArm.rotation, transform.rotation * Quaternion.Euler(0f, 0f, -25f), oShape);
                    rightArm.rotation = Quaternion.Slerp(rightArm.rotation, transform.rotation * Quaternion.Euler(0f, 0f, 25f), oShape);
                    leftForearm.localRotation = Quaternion.Slerp(leftForearm.localRotation, Quaternion.identity, oShape);
                    rightForearm.localRotation = Quaternion.Slerp(rightForearm.localRotation, Quaternion.identity, oShape);
                }
            }
            if (heldBall)
            {
                heldBall.localScale = Vector3.one * (0.34f + charge * 0.055f);
                heldBall.gameObject.SetActive(DodgeGame.IsReplica ? replicaHeldVisible : Time.time - releasedAt > 0.34f && DiveAmount < 0.3f && ShapeAmount < 0.1f && !IsStunned);
            }
        }

        void SetLeg(Transform leg, float side, float swing)
        {
            if (!leg) return;
            leg.localPosition = new Vector3(side * 0.22f, 0.7f - DiveAmount * 0.2f, 0f);
            leg.localRotation = Quaternion.Euler(Mathf.Lerp(swing, 100f, DiveAmount), 0f, side * (5f + DiveAmount * 5f));
        }

        void ApplyColor()
        {
            if (bodyRenderers == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            Color color = Color.Lerp(bodyColor, Color.white, hitPulse);
            if(DodgeApp.Active && DodgeApp.Instance.Initialized)
            {
                int slot=DodgeApp.Instance.SeatOf(this);
                if(slot>=0 && DodgeApp.Instance.Room.seats[slot].team>=0)
                    color=Color.Lerp(DodgeApp.Instance.Room.seats[slot].team==0?new Color(.38f,.8f,.79f):new Color(.98f,.65f,.47f),Color.white,hitPulse);
            }
            if (isPlayer && Time.time < InvulnerableUntil && Mathf.Sin(Time.time * 35f) > 0)
                color = Color.Lerp(color, Color.white, 0.65f);
            propertyBlock.SetColor(BaseColor, color);
            foreach (Renderer body in bodyRenderers)
                if (body) body.SetPropertyBlock(propertyBlock);
            if (puppet) puppet.SetColor(propertyBlock);
        }

        public bool ReceiveHit(Vector3 point, bool playerShot = true, Vector3 impulse = default, float launch = 0f)
        {
            if (DodgeReferee.Restricted(this)) return false;
            if (Time.time < InvulnerableUntil) return false;
            bool human = isPlayer || (DodgeApp.Active && DodgeApp.Instance.SeatOf(this) >= 0
                && DodgeApp.Instance.Room.seats[DodgeApp.Instance.SeatOf(this)].kind == DodgeSeatKind.Human);
            InvulnerableUntil = Time.time + (human ? 0.8f : 0.16f);
            hitPulse = 1f;
            HitCount++;
            if (puppet) puppet.Hit(point, impulse, launch);
            DodgeGame.Instance?.RegisterHit(this, point, playerShot);
            return true;
        }

        public void ResetActor()
        {
            Kit.ResetKit(DodgeApp.Active && DodgeApp.Instance.Page==DodgePage.Staging);
            CrowdStatus.ResetStatus();
            Arsenal.ResetEquipment();
            InvulnerableUntil = 0f;
            HitCount = 0;
            hitPulse = 0f;
            lShape = oShape = desiredL = desiredO = 0f;
            throwStarted = -10f;
            releasedAt = -10f;
            throwPulse = landingPulse = locomotion = desiredLocomotion = 0f;
            desiredCharge = charge = leanVelocity = motionVelocity = 0f;
            moveDirection = Vector2.zero;
            SetDiveImmediate(0f);
            SetLeanImmediate(0f);
            ApplyColor();
            if (puppet) puppet.ResetPuppet();
        }

        void OnDrawGizmos()
        {
            if (!DodgeGame.ShowHitboxes) return;
            Gizmos.color = new Color(0.5f, 1f, 0.1f, 0.9f);
            foreach (DodgeHitbox part in GetComponentsInChildren<DodgeHitbox>())
            {
                Collider collider = part.GetComponent<Collider>();
                if (!collider) continue;
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = part.transform.localToWorldMatrix;
                if (collider is SphereCollider sphere) Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                else if (collider is CapsuleCollider capsule)
                    Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f));
                Gizmos.matrix = old;
            }
        }
    }
}
