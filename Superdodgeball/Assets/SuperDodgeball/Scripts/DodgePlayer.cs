using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperDodgeball
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(DodgeActor))]
    public sealed class DodgePlayer : MonoBehaviour
    {
        public DodgeCamera view;
        public bool RemoteControlled;
        public DodgeInput RemoteInput;
        public bool Replica;
        public float ReplicaCharge, ReplicaMovement;
        DodgeInput frame;
        bool ControlsActive => RemoteControlled ? DodgeGame.SimulationActive : DodgeGame.InputCaptured && DodgeGame.SimulationActive;
        float ViewYaw => RemoteControlled ? frame.yaw : view.Yaw;
        void Kick(float value) { if (!RemoteControlled && view) view.Kick(value); }
        public float moveSpeed = 5.8f;
        public float dodgeDuration = 0.72f;
        public float dodgeCooldown = 0.3f;
        public float chargeSeconds = 1.25f;
        public float diveDuration = 0.62f;
        public float diveCooldown = 1.3f;
        public float DodgeRemaining => Mathf.Max(0f, dodgeEnd - Time.time);
        public bool Recovering => Time.time < recoverEnd && Time.time >= dodgeEnd;
        public Vector3 WeaponAim => RemoteControlled ? frame.aim : view.AimPoint;
        public bool WeaponBusy => burstLeft > 0 || IsDiving;
        public float ShotReady => Mathf.Clamp01(1f - (nextShot - Time.time) / (BurstMode ? 2.2f : 0.72f));
        public bool BurstMode => actor && actor.Arsenal.Selected == DodgeWeapon.Burst;
        public DodgeWeapon CurrentWeapon => actor && actor.Zeus.Transformed ? DodgeWeapon.Thunderbolt : actor ? actor.Arsenal.Selected : DodgeWeapon.Standard;
        public DodgeWeapon VisualWeapon => actor && actor.Kit.Swing>0 && DodgeCombat.IsMelee(actor.Kit.SwingWeapon) ? actor.Kit.SwingWeapon : burstLeft > 0 ? queuedWeapon : CurrentWeapon;
        public int ShotsFired { get; private set; }
        float charge;
        public float Charge { get => Replica ? ReplicaCharge : charge; private set => charge = value; }
        public float LastShotSpeed { get; private set; }
        public float LastShotCharge { get; private set; }
        public float MovementAmount => Replica ? ReplicaMovement : Mathf.Clamp01(planarVelocity.magnitude / moveSpeed);
        public bool IsDiving => Replica ? actor && actor.DiveAmount > .3f : dive.Active;
        public float DiveReady => IsDiving ? 0f : Mathf.Clamp01(1f - (nextDive - Time.time) / diveCooldown);
        public bool IsShaping => Time.time < shapeEnd || (actor && actor.ShapeAmount > 0.1f);
        public bool IsAiming => !IsDiving && !IsShaping && (!actor || !actor.IsStunned) && (Replica ? DodgeGame.InputCaptured && Mouse.current != null && Mouse.current.rightButton.isPressed : ControlsActive && frame.Has(DodgeButtons.Aim));
        public Vector3 ThrowOrigin => actor.heldBall ? actor.HandPosition + transform.forward * 0.2f
            : actor.upperBody.TransformPoint(new Vector3(0.33f, 0.56f, 0.53f));
        DodgeActor actor;
        CharacterController motor;
        Vector3 planarVelocity, movementSpring, diveDirection;
        float fallSpeed, dodgeEnd, recoverEnd, dodgeSign, nextShot, nextBurstShot;
        float nextDive, bubbleDistance;
        bool springDive;
        DodgeDiveMotion dive;
        float burstCharge;
        bool queuedBurst;
        DodgeWeapon queuedWeapon;
        float releaseDeadline;
        bool firstRelease;
        float shapeEnd, nextShape;
        bool shapeReleaseRequired;
        bool circleShape;
        int burstLeft;
        bool releaseDodgeRequired, chargeChimed;

        void Awake()
        {
            actor = GetComponent<DodgeActor>();
            motor = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (Replica) return;
            if (DodgeReferee.Restricted(actor)) { CancelForReferee(); return; }
            frame = RemoteControlled ? RemoteInput : DodgeInput.Read(view);
            if (RemoteControlled) RemoteInput.pressed = 0;
            if(DodgeCombat.IsLauncher(VisualWeapon))actor.Kit.LauncherAim=WeaponAim;
            var game = DodgeGame.Instance;
            bool active = ControlsActive && game && !game.RoundOver;
            // An online menu releases input, but an existing leap must keep falling
            // on the host. Only a paused offline world freezes the flight.
            if (!active && game && DodgeGame.SimulationActive && (dive.Active || actor.Zeus.Hovering || DodgeVenueDirector.InUpdraft))
            { frame = default; active = true; }
            if (!active || actor.IsStunned)
            {
                actor.SetLean(0f);
                // Pausing preserves the airborne action. A hit hands control to the ragdoll.
                if (actor.IsStunned) { dive = default; fallSpeed = 0f; }
                actor.SetDive(dive.Active ? 1f : 0f);
                actor.SetCharge(0f);
                actor.SetLocomotion(0f);
                actor.SetShape(0f, 0f);
                shapeEnd = 0f;
                planarVelocity = Vector3.zero;
                burstLeft = 0;
                Charge = 0f;
                return;
            }

            if(DodgeVenueDirector.InUpdraft && dive.Active){dive=default;actor.SetDive(0);}
            if(actor.Kit.PantLeft>0)
            {
                float down=fallSpeed;CancelForReferee();actor.SetDive(0);actor.SetMovement(Vector2.zero);
                fallSpeed=motor.isGrounded?-2:Mathf.Min(0,down)-DodgeDiveMotion.Gravity*Time.deltaTime;
                DodgeVenueDirector.ApplyWind(transform.position,ref fallSpeed);
                if(motor.enabled)motor.Move(Vector3.up*fallSpeed*Time.deltaTime);
                return;
            }
            if(frame.Down(DodgeButtons.Detonate) && actor.Kit.TryPopVest())return;
            if(frame.Down(DodgeButtons.Collect)) DodgePartyEffects.Instance.CollectWeapon(actor);
            else if(frame.Down(DodgeButtons.Drop)) DodgePartyEffects.Instance.DropWeapon(actor);
            if (frame.Down(DodgeButtons.UseItem)) { actor.Kit.Use(frame.aim); if(DodgeReferee.Restricted(actor))return; }
            if(!IsDiving && actor.Kit.ConsumeRelay())nextDive=Mathf.Min(nextDive,Time.time+.15f);
            if (frame.Down(DodgeButtons.Standard)) actor.Arsenal.SelectSpecial(false);
            if (frame.Down(DodgeButtons.Special)) actor.Arsenal.SelectSpecial(true);
            if (frame.Down(DodgeButtons.Outfit) && actor.Puppet && actor.Puppet.Wardrobe)
            {
                if(DodgeApp.Active)DodgeApp.Instance.SetOutfit(DodgeApp.Instance.SeatOf(actor),actor.Puppet.Wardrobe.Style+1);
                else actor.Puppet.Wardrobe.SetStyle(actor.Puppet.Wardrobe.Style + 1);
            }
            bool shapeL = frame.Has(DodgeButtons.L), shapeO = frame.Has(DodgeButtons.O);
            if (!shapeL && !shapeO) shapeReleaseRequired = false;
            if (!shapeReleaseRequired && Time.time >= nextShape && !IsDiving && burstLeft == 0 && shapeL != shapeO)
            {
                circleShape = shapeO;
                shapeEnd = Time.time + 1.4f;
                nextShape = shapeEnd + 0.55f;
                shapeReleaseRequired = true;
                dodgeEnd = 0f;
                Charge = 0f;
            }
            bool holdingShape = Time.time < shapeEnd && (circleShape ? shapeO : shapeL);
            if (!holdingShape) shapeEnd = 0f;
            actor.SetShape(holdingShape && !circleShape ? 1f : 0f, holdingShape && circleShape ? 1f : 0f);
            Vector2 move = frame.move;
            move = Vector2.ClampMagnitude(move, 1f);
            Quaternion heading = Quaternion.Euler(0f, ViewYaw, 0f);
            Vector3 direction = heading * new Vector3(move.x, 0f, move.y);
            if (frame.Down(DodgeButtons.Dive) && !IsDiving && !DodgeVenueDirector.InUpdraft && motor.isGrounded && Time.time >= nextDive && burstLeft == 0 && !IsShaping)
            {
                diveDirection = direction.sqrMagnitude > 0.05f ? direction.normalized : heading * Vector3.forward;
                springDive=actor.Kit.TakeSpring();
                dive.Begin(springDive?.72f:diveDuration, ref fallSpeed);
                nextDive = Time.time + diveDuration + DodgeDiveMotion.LandingSeconds + diveCooldown;
                movementSpring = Vector3.zero;
                dodgeEnd = 0f;
                burstLeft = 0;
                Charge = 0f;
                game.EmitFoam(transform.position + Vector3.up * 0.08f, -diveDirection * 2f, 7, 1.1f);
                game.PlayTone(250f, 0.09f, 0.05f);
            }

            float speed = moveSpeed * (IsShaping ? 0.3f : IsAiming ? Mathf.Lerp(0.68f, 0.36f, Charge) : 1f) * (Time.time < dodgeEnd ? 0.6f : 1f);
            speed*=actor.Kit.RetreatSpeed(direction)*Mathf.Lerp(1f,.55f,actor.Kit.FlatAmount);
            Vector3 desiredVelocity = direction * speed;
            if (IsDiving)
            {
                desiredVelocity = diveDirection * dive.Speed * (springDive?1.35f:1f);
                planarVelocity = desiredVelocity;
            }
            else planarVelocity = Vector3.SmoothDamp(planarVelocity, desiredVelocity, ref movementSpring, (move.sqrMagnitude > 0f ? 0.1f : 0.13f)*actor.Kit.ResponseTime);
            float movementHeight = IsDiving ? .95f : Mathf.Lerp(1.9f,.95f,actor.Kit.FlatAmount);
            if (!Mathf.Approximately(motor.height, movementHeight))
            {
                motor.height = movementHeight;
                motor.center = Vector3.up * (movementHeight * 0.5f);
                motor.stepOffset = IsDiving ? 0.1f : 0.2f;
            }
            bool divingBeforeMove = dive.Active;
            if (dive.Active) dive.Step(Time.deltaTime, ref fallSpeed);
            else fallSpeed = motor.isGrounded ? -2f : Mathf.Max(-25f, fallSpeed - DodgeDiveMotion.Gravity * Time.deltaTime);
            if (!dive.Active)
                actor.Zeus.Hover(ref fallSpeed, IsAiming && Time.time >= nextShot || burstLeft > 0 && queuedWeapon == DodgeWeapon.Thunderbolt,
                    burstLeft > 0 ? burstCharge : Charge);
            DodgeVenueDirector.ApplyWind(transform.position,ref fallSpeed);
            Vector3 before = transform.position;
            var collision = motor.Move((planarVelocity*actor.Kit.RunMultiplier + Vector3.up * fallSpeed) * Time.deltaTime);
            if (dive.Resolve(collision, ref fallSpeed))
            {
                actor.Land();
                game.EmitFoam(transform.position, -diveDirection, 9, 1.25f);
                Kick(0.018f);
            }
            if (divingBeforeMove && !dive.Active) nextDive = Time.time + diveCooldown;
            if (transform.position.y < -5f) ResetPlayer();
            Quaternion face = IsDiving ? Quaternion.LookRotation(diveDirection) : Quaternion.Euler(0f, ViewYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 1f - Mathf.Exp(-14f * Time.deltaTime / actor.Kit.ResponseTime));
            actor.SetLocomotion(IsDiving ? 0f : MovementAmount);
            actor.SetMovement(move);
            actor.SetDive(IsDiving ? 1f : 0f);
            bubbleDistance += Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude;
            if (bubbleDistance > (IsDiving ? 0.65f : 0.85f) && motor.isGrounded)
            {
                bubbleDistance = 0f;
                game.EmitFoam(transform.position - planarVelocity.normalized * 0.17f, -planarVelocity, IsDiving ? 4 : 3, IsDiving ? 1f : 0.8f);
            }

            bool left = frame.Has(DodgeButtons.Left);
            bool right = frame.Has(DodgeButtons.Right);
            if (!left && !right) releaseDodgeRequired = false;
            if (!IsDiving && !IsShaping && !releaseDodgeRequired && Time.time >= recoverEnd && left != right)
            {
                dodgeSign = left ? -1f : 1f;
                dodgeEnd = Time.time + dodgeDuration;
                recoverEnd = dodgeEnd + dodgeCooldown;
                releaseDodgeRequired = true;
            }
            actor.SetLean(!IsDiving && !IsShaping && Time.time < dodgeEnd ? dodgeSign : 0f);

            if (IsAiming && Time.time >= nextShot && burstLeft == 0)
            {
                Charge = Mathf.MoveTowards(Charge, 1f, Time.deltaTime / (actor.Zeus.Transformed ? DodgeZeus.ChargeSeconds : chargeSeconds));
                if (Charge >= 0.99f && !chargeChimed)
                {
                    chargeChimed = true;
                    game.PlayTone(660f, 0.08f, 0.045f);
                    game.EmitFoam(ThrowOrigin, Vector3.up, 3, 0.45f);
                }
            }
            else { Charge = 0f; chargeChimed = false; }
            actor.SetCharge(Charge);

            if (burstLeft > 0 && Time.time >= nextBurstShot)
            {
                bool handInFront = transform.InverseTransformPoint(actor.HandPosition).z > 0.05f;
                if (!firstRelease || !DodgeCombat.UsesThrowPose(queuedWeapon) || handInFront || Time.time >= releaseDeadline)
                {
                    FireSingle(queuedWeapon, burstCharge);
                    firstRelease = false;
                    burstLeft--;
                    nextBurstShot = Time.time + 0.1f;
                }
            }
            if (!IsDiving && frame.Down(DodgeButtons.Fire) && Time.time >= nextShot && (RemoteControlled || !game.CapturedThisFrame))
                TryShoot();
        }

        public Vector3 AimVelocity(Vector3 origin, float speed)
        {
            Vector3 point = RemoteControlled ? frame.aim : view.AimPoint;
            if ((point - origin).sqrMagnitude > 35f * 35f) point = origin + (RemoteControlled ? Quaternion.Euler(frame.pitch, frame.yaw, 0f) * Vector3.forward : view.transform.forward) * 22f;
            return DodgeBall.SolveLaunchVelocity(origin, point, speed);
        }

        public bool TryShoot()
        {
            if (DodgeReferee.Restricted(actor) || actor.Kit.PantLeft>0 || actor.Kit.BiteLeft>0) return false;
            if (Replica || !ControlsActive || Time.time < nextShot || burstLeft > 0 || DodgeGame.Instance.RoundOver || IsDiving || IsShaping || actor.IsStunned
                || (DodgeApp.Active && DodgeApp.Instance.Page != DodgePage.Lobby && DodgeApp.Instance.Page != DodgePage.Playing && !(DodgeApp.Instance.Page==DodgePage.Staging && DodgeRestTarget.CanAim(actor,frame.aim)))) return false;
            float power = Charge;
            queuedWeapon = DodgeApp.Active && DodgeApp.Instance.Page!=DodgePage.Playing ? DodgeWeapon.Standard : actor.Arsenal.ConsumeAttack();
            queuedBurst = queuedWeapon == DodgeWeapon.Burst;
            burstCharge = power;
            burstLeft = queuedBurst ? 5 : 1;
            firstRelease = true;
            nextBurstShot = Time.time + actor.Kit.ReleaseDelay(queuedWeapon);
            actor.Kit.ConsumeCounter();
            releaseDeadline = nextBurstShot + 0.12f;
            nextShot = Time.time + (queuedBurst ? 2.2f : queuedWeapon == DodgeWeapon.Bowling ? 1.25f : queuedWeapon == DodgeWeapon.Hammer ? 1.1f : queuedWeapon == DodgeWeapon.Bat ? .82f : .72f);
            if(queuedWeapon==DodgeWeapon.Bat)actor.Kit.BeginSwing(queuedWeapon,WeaponAim);
            else if(DodgeCombat.UsesThrowPose(queuedWeapon) || queuedWeapon==DodgeWeapon.Hammer)actor.Throw(power,nextBurstShot-Time.time);
            Charge = 0f;
            return true;
        }

        void FireSingle(DodgeWeapon weapon, float power)
        {
            bool burst = weapon == DodgeWeapon.Burst;
            float speed = weapon == DodgeWeapon.Bowling ? Mathf.Lerp(14f, 24f, power) : Mathf.Lerp(burst ? 20f : 17f, 34f, power);
            Vector3 from = ThrowOrigin;
            Vector3 aim=RemoteControlled?frame.aim:view.AimPoint;
            var ball = DodgeCombat.Release(actor,weapon,from,aim,AimVelocity(from,speed*actor.Kit.ThrowStrength),power,false,null);
            if(DodgeCombat.UsesThrowPose(weapon))actor.ReleaseBall();
            if(ball)ball.SetPower(power);
            LastShotCharge = power;
            LastShotSpeed = ball ? ball.velocity.magnitude : 0;
            ShotsFired++;
            Kick(Mathf.Lerp(0.018f, 0.055f, power));
            DodgeGame.Instance.PlayTone(weapon == DodgeWeapon.Bowling ? 150f : Mathf.Lerp(burst ? 330f : 400f, 530f, power), 0.1f, 0.075f);
        }

        public void CancelForReferee()
        {
            dive = default; fallSpeed = 0f; burstLeft = 0; Charge = 0f;
            shapeEnd = dodgeEnd = 0f; planarVelocity = movementSpring = Vector3.zero;
            actor.SetCharge(0f); actor.SetShape(0f,0f); actor.SetLocomotion(0f); actor.SetLean(0f);
        }
        public void ResetPlayer()
        {
            if (!motor) motor = GetComponent<CharacterController>();
            motor.enabled = false;
            transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -6.5f), Quaternion.identity);
            motor.height = 1.9f;
            motor.center = Vector3.up * 0.95f;
            motor.stepOffset = 0.2f;
            motor.enabled = true;
            planarVelocity = movementSpring = Vector3.zero;
            fallSpeed = 0f;
            dodgeEnd = recoverEnd = nextShot = nextDive = 0f;
            dive = default;
            shapeEnd = nextShape = 0f;
            shapeReleaseRequired = false;
            Charge = bubbleDistance = 0f;
            burstLeft = 0;
            ShotsFired = 0;
            releaseDodgeRequired = chargeChimed = false;
        }
    }
}
