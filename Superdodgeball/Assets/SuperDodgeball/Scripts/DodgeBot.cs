using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeBotStyle { Brawler, Collector, Acrobat, Pitcher }
    public enum DodgeBotState { Practice, Paused, Recovering, Moving, Charging, Throwing, Evading }

    // Local game AI: coarse observations, reaction delay, imperfect prediction and
    // physical actions. It never reads input, moves a projectile or grants equipment.
    [DefaultExecutionOrder(-90)]
    [RequireComponent(typeof(DodgeActor), typeof(CharacterController))]
    public sealed class DodgeBot : MonoBehaviour
    {
        public DodgeBotStyle style;
        [Min(0.15f)] public float reactionSeconds = 0.28f;
        [Min(0.1f)] public float aimError = 0.55f;
        public float moveSpeed = 4.2f;
        public DodgeBotState State { get; private set; }
        public DodgeActor Opponent { get; private set; }
        public DodgeAirdrop WantedDrop { get; private set; }
        public int ShotsFired { get; private set; }
        public int DodgesAttempted { get; private set; }
        public int CratesClaimed { get; private set; }
        public DodgeWeapon LastWeaponFired { get; private set; }
        public bool IsCharging => charging;
        public float EffectiveReaction => reactionSeconds + Actor.CrowdStatus.ReactionPenalty;
        public float EffectiveAimError => aimError * Actor.CrowdStatus.AimMultiplier;
        public float Charge { get; private set; }
        public Vector3 Velocity => velocity;
        public DodgeWeapon VisualWeapon => Actor.Kit.Swing>0 && DodgeCombat.IsMelee(Actor.Kit.SwingWeapon) ? Actor.Kit.SwingWeapon : burstLeft > 0 ? queuedWeapon : Actor.Arsenal.Selected;
        public DodgeActor Actor { get { Initialize(); return actor; } }

        static readonly List<DodgeBot> bots = new List<DodgeBot>();
        public static IReadOnlyList<DodgeBot> All => bots;
        readonly Dictionary<DodgeBall, float> noticed = new Dictionary<DodgeBall, float>();
        readonly List<DodgeBall> stale = new List<DodgeBall>();
        readonly RaycastHit[] visibilityHits = new RaycastHit[64];
        DodgeActor actor;
        CharacterController motor;
        System.Random random;
        bool initialized, originalWiggle, charging, firstRelease;
        DodgeDiveMotion dive;
        Vector3 home, velocity, movementSpring, destination, observedPoint, observedVelocity, lockedAim;
        Vector3 evadeDirection;
        float nextItem,springScale=1;
        float clock, nextThink, nextRetarget, nextAttack, chargeGoal, attackPower, releaseAt, releaseDeadline;
        float evadeUntil, nextEvade, fallSpeed, bubbleDistance, lastObservation, strafeSign;
        int burstLeft, evadeMode;
        DodgeWeapon queuedWeapon;
        DodgeActor recentAttacker;
        float rememberUntil;

        void Awake() => Initialize();
        void OnEnable() { if (!bots.Contains(this)) bots.Add(this); }
        void OnDisable() { bots.Remove(this); if (initialized) ClearActions(); }

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            actor = GetComponent<DodgeActor>();
            motor = GetComponent<CharacterController>();
            originalWiggle = actor.animatedTarget;
            home = transform.position;
            ResetBrain();
        }

        public void ResetBrain()
        {
            if (!initialized) { Initialize(); return; }
            int seed = Mathf.RoundToInt(home.x * 43f + home.z * 97f) + (DodgeGame.Instance ? DodgeGame.Instance.RoundNumber * 31 : 0);
            random = new System.Random(seed);
            nextItem=0;springScale=1;clock = nextThink = nextRetarget = nextEvade = 0f;
            nextAttack = Range(1.5f, 2.8f);
            strafeSign = random.Next(2) == 0 ? -1f : 1f;
            ShotsFired = DodgesAttempted = CratesClaimed = 0;
            LastWeaponFired = DodgeWeapon.Standard;
            Opponent = null; WantedDrop = null;
            recentAttacker = null;
            destination = home;
            velocity = movementSpring = observedVelocity = Vector3.zero;
            fallSpeed = bubbleDistance = 0f;
            noticed.Clear();
            ClearActions();
            actor.animatedTarget = !DodgeGame.Instance || !DodgeGame.Instance.botsEnabled ? originalWiggle : false;
            State = DodgeGame.Instance && DodgeGame.Instance.botsEnabled ? DodgeBotState.Moving : DodgeBotState.Practice;
            motor.height = 1.9f;
            motor.center = Vector3.up * 0.95f;
            motor.stepOffset = 0.2f;
        }

        public void ClaimedCrate() { CratesClaimed++; WantedDrop = null; nextAttack = Mathf.Min(nextAttack, clock + 0.7f); }
        public void RememberAttacker(DodgeActor attacker)
        {
            if (!attacker || attacker == actor) return;
            recentAttacker = attacker;
            rememberUntil = clock + 5f;
            nextRetarget = clock;
        }
        float Range(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());

        void ClearActions(bool preserveDive = false)
        {
            charging = false;
            burstLeft = 0;
            Charge = 0f;
            if (!preserveDive) { evadeUntil = 0f; dive = default; }
            actor.SetCharge(0f); actor.SetLean(0f); actor.SetDive(dive.Active ? 1f : 0f); actor.SetShape(0f, 0f);
            actor.SetLocomotion(0f); actor.SetMovement(Vector2.zero);
        }

        void Update()
        {
            var game = DodgeGame.Instance;
            if (!game || !actor.Puppet || !actor.Puppet.Ready) return;
            if (DodgeReferee.Restricted(actor)) { CancelForReferee(); return; }
            if (!game.botsEnabled)
            {
                if (State != DodgeBotState.Practice) ClearActions();
                actor.animatedTarget = originalWiggle;
                State = DodgeBotState.Practice;
                return;
            }
            actor.animatedTarget = false;
            if (!DodgeGame.SimulationActive || !game.RoundStarted || game.RoundOver)
            {
                if (State != DodgeBotState.Paused) { ClearActions(true); nextAttack = Mathf.Max(nextAttack, clock + 0.6f); }
                State = DodgeBotState.Paused;
                return;
            }
            clock += Time.deltaTime;
            if (actor.IsStunned)
            {
                ClearActions();
                velocity = movementSpring = Vector3.zero;
                nextAttack = Mathf.Max(nextAttack, clock + 0.8f);
                State = DodgeBotState.Recovering;
                return;
            }
            if (!motor.enabled) return;
            if(DodgeApp.Active && DodgeApp.Instance.MoveBotBeforeMatch(this,motor))return;
            if(!dive.Active && actor.Kit.ConsumeRelay())nextEvade=Mathf.Min(nextEvade,clock+.15f);
            if(clock>=nextItem && actor.Kit.Item!=DodgeItem.None && Opponent)
            {
                bool use=actor.Kit.Item!=DodgeItem.Soap || actor.CrowdStatus.Obscured;
                if(use && actor.Kit.Use(Opponent.transform.position))nextItem=clock+3f;
            }
            if(DodgeRestTarget.BotVisit(this,motor))return;
            if (clock >= nextThink)
            {
                nextThink = clock + 0.12f;
                Think(game);
            }

            bool evading = clock < evadeUntil || dive.Active;
            bool diving = evading && evadeMode == 1;
            if (diving && !dive.Active)
            {
                if (motor.isGrounded) {bool spring=actor.Kit.TakeSpring();springScale=spring?1.35f:1;dive.Begin(spring?.72f:.62f, ref fallSpeed);}
                else { evadeMode = 0; diving = false; }
            }
            actor.SetDive(diving ? 1f : 0f);
            actor.SetLean(evading && evadeMode == 0 ? strafeSign : 0f);
            actor.SetShape(evading && evadeMode == 2 ? 1f : 0f, evading && evadeMode == 3 ? 1f : 0f);
            if (DodgeApp.Active && DodgeApp.Instance.Referee)
            {
                var referee = DodgeApp.Instance.Referee;
                destination = referee.BotDestination(actor, destination, style == DodgeBotStyle.Brawler);
                if (referee.MustRetreat(actor))
                {
                    ClearActions(); nextAttack = Mathf.Max(nextAttack,clock+.5f); evading = diving = false;
                    actor.SetDive(0f); actor.SetShape(0f,0f); actor.SetLean(0f);
                }
            }
            Move(game, evading, diving);
            if (evading || actor.ShapeAmount > 0.1f)
            {
                State = DodgeBotState.Evading;
                return;
            }
            if (burstLeft > 0)
            {
                State = DodgeBotState.Throwing;
                if (clock >= releaseAt && (!firstRelease || (queuedWeapon < DodgeWeapon.Bat && transform.InverseTransformPoint(actor.HandPosition).z > 0.05f) || clock >= releaseDeadline))
                    Release(game);
                return;
            }
            if (charging)
            {
                if (!HasGoal() || !HasSight(observedPoint)) { CancelAttack(); return; }
                State = DodgeBotState.Charging;
                Charge = Mathf.MoveTowards(Charge, chargeGoal, Time.deltaTime / 1.25f);
                actor.SetCharge(Charge);
                if (Charge >= chargeGoal - 0.001f) BeginThrow();
            }
            else
            {
                State = DodgeBotState.Moving;
                if (clock >= nextAttack && HasGoal() && Vector3.Distance(transform.position, observedPoint) < 25f
                    && Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(observedPoint - transform.position, Vector3.up)) < 18f
                    && HasSight(observedPoint))
                {
                    charging = true;
                    Charge = 0f;
                    chargeGoal = WantedDrop ? 0.12f : style == DodgeBotStyle.Pitcher ? Range(0.65f, 0.95f) : Range(0.22f, 0.62f);
                }
            }
        }

        bool HasGoal() => WantedDrop ? !WantedDrop.Claimed : Opponent && Opponent.isActiveAndEnabled && !Opponent.IsStunned;

        void Think(DodgeGame game)
        {
            ObserveThreats();
            if (clock < evadeUntil || burstLeft > 0) return;
            DodgeAirdrop drop = null;
            bool wantsWeapon = actor.Arsenal.Charges == 0 || style == DodgeBotStyle.Collector;
            float nearest = 20f;
            foreach (var candidate in DodgeAirdrop.Active)
            {
                if (!candidate || candidate.Claimed) continue;
                bool wanted = candidate.Kind == DodgeGiftKind.Soap ? actor.CrowdStatus.Obscured : candidate.Kind==DodgeGiftKind.Item ? actor.Kit.Item==DodgeItem.None : wantsWeapon;
                float crateDistance = Vector3.Distance(transform.position, candidate.AimPosition);
                if (!wanted || crateDistance >= nearest || !HasSight(candidate.AimPosition)) continue;
                drop = candidate; nearest = crateDistance;
            }
            if (drop)
            {
                if (WantedDrop != drop) { WantedDrop = drop; charging = false; Charge = 0f; observedVelocity = Vector3.zero; lastObservation = 0f; }
                Opponent = null;
            }
            else
            {
                if (WantedDrop) { WantedDrop = null; CancelAttack(); }
                if (clock >= nextRetarget || !Opponent || Opponent.IsStunned || !HasSight(Opponent.transform.position + Vector3.up))
                    ChooseOpponent();
            }
            if (!HasGoal()) { destination = home; return; }
            Vector3 point = WantedDrop ? WantedDrop.AimPosition : Opponent.transform.position + Vector3.up * 1.2f;
            if (!WantedDrop && DodgePartyEffects.Instance)point=DodgePartyEffects.Instance.DecoyTarget(actor,point);
            if (!HasSight(point)) { destination = point; return; }
            if (lastObservation > 0f)
                observedVelocity = Vector3.ClampMagnitude((point - observedPoint) / Mathf.Max(0.05f, clock - lastObservation), 7f);
            observedPoint = point;
            lastObservation = clock;
            Vector3 away = Vector3.ProjectOnPlane(transform.position - point, Vector3.up);
            float distance = away.magnitude;
            away = distance > 0.01f ? away / distance : transform.forward;
            bool melee=!WantedDrop && DodgeCombat.IsMelee(actor.Arsenal.Weapon) && actor.Arsenal.Charges>0;
            actor.Arsenal.SelectSpecial(!WantedDrop && (!melee || Vector3.Distance(transform.position,point)<3.2f));
            float preferred = melee ? 1.25f : WantedDrop ? 5f : style == DodgeBotStyle.Brawler ? 5.5f : style == DodgeBotStyle.Pitcher ? 10.5f : 8f;
            Vector3 lateral = Vector3.Cross(Vector3.up, away) * strafeSign;
            destination = point + away * preferred + lateral * (WantedDrop || melee ? 0f : 2f);
            destination.x = Mathf.Clamp(destination.x, -8.5f*DodgeCourt.Scale, 8.5f*DodgeCourt.Scale);
            destination.z = Mathf.Clamp(destination.z, -11.5f*DodgeCourt.Scale, 11.5f*DodgeCourt.Scale);
            destination.y = transform.position.y;
            // Do not blindly shoot at a high, rapidly descending target. Move into
            // position and shoot when it is low enough to judge the drop reliably.
            if (WantedDrop && WantedDrop.AimPosition.y > 2f) nextAttack = Mathf.Max(nextAttack, clock + 0.35f);
        }

        void ChooseOpponent()
        {
            nextRetarget = clock + Range(1.4f, 2.5f);
            DodgeActor chosen = null;
            float best = float.NegativeInfinity;
            foreach (var other in FindObjectsByType<DodgeActor>(FindObjectsSortMode.None))
            {
                if(DodgeApp.Active && DodgeApp.Instance.SameTeam(actor,other))continue;
                if (other == actor || !other.isActiveAndEnabled || other.IsStunned || other.transform.position.y > 1f) continue;
                Vector3 difference = other.transform.position - transform.position;
                if (difference.sqrMagnitude > 30f * 30f || !HasSight(other.transform.position + Vector3.up * 1.2f)) continue;
                int playerAttackers = 0;
                if (other.isPlayer)
                    foreach (var bot in bots) if (bot != this && bot.Opponent == other && bot.State != DodgeBotState.Practice) playerAttackers++;
                if (playerAttackers >= 2) continue;
                float playerInterest = other.isPlayer ? style == DodgeBotStyle.Pitcher ? 10f : style == DodgeBotStyle.Brawler ? 8f : 5f : 0f;
                float score = -difference.magnitude + (other == Opponent ? 2f : 0f) + playerInterest
                    + (other == recentAttacker && clock < rememberUntil ? 8f : 0f) + Range(0f, 3f);
                if (score > best) { chosen = other; best = score; }
            }
            if (chosen != Opponent)
            {
                Opponent = chosen;
                charging = false; Charge = 0f; actor.SetCharge(0f);
                lastObservation = 0f; observedVelocity = Vector3.zero;
            }
        }

        bool HasSight(Vector3 point)
        {
            Vector3 eye = transform.position + Vector3.up * 1.45f;
            Vector3 ray = point - eye;
            int count = Physics.RaycastNonAlloc(eye, ray.normalized, visibilityHits, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (visibilityHits[i].collider.GetComponent<DodgeSurface>() && visibilityHits[i].distance < ray.magnitude - 0.08f) return false;
            return true;
        }

        void ObserveThreats()
        {
            DodgeBall urgent = null;
            float soonest = 1.2f;
            float predictedHeight = 0f;
            foreach (var ball in DodgeBall.Active)
            {
                if (!ball || !ball.enabled || !ball.IsDangerous || ball.owner == actor || (DodgeApp.Active && DodgeApp.Instance.SameTeam(actor,ball.owner))) continue;
                Vector3 offset = ball.transform.position - transform.position;
                if (offset.sqrMagnitude > 18f * 18f || (offset.sqrMagnitude > 9f && Vector3.Dot(transform.forward, offset.normalized) < -0.5f)
                    || !HasSight(ball.transform.position)) continue;
                if (!noticed.ContainsKey(ball)) noticed.Add(ball, clock);
                Vector3 motion = Vector3.ProjectOnPlane(ball.velocity - velocity, Vector3.up);
                Vector3 toUs = Vector3.ProjectOnPlane(transform.position - ball.transform.position, Vector3.up);
                if (motion.sqrMagnitude < 1f) continue;
                float time = Vector3.Dot(toUs, motion) / motion.sqrMagnitude;
                if (time < 0.06f || time > soonest || (motion * time - toUs).magnitude > 0.65f + ball.CollisionRadius) continue;
                float height = ball.transform.position.y + ball.velocity.y * time - DodgeBall.Gravity * 0.5f * time * time - transform.position.y;
                if (height < 0.1f || height > 2.1f) continue;
                if (clock - noticed[ball] < EffectiveReaction) continue;
                urgent = ball; soonest = time; predictedHeight = height;
            }
            stale.Clear();
            foreach (var pair in noticed) if (!pair.Key || !pair.Key.IsDangerous || clock - pair.Value > 5f) stale.Add(pair.Key);
            foreach (var item in stale) noticed.Remove(item);
            if(DodgePartyEffects.Instance && clock>=nextEvade)
                foreach(var effect in DodgePartyEffects.Instance.Active)
                    if(effect.kind==DodgeEffect.Bomb && effect.left<1.5f && Vector3.Distance(effect.position,transform.position)<4f && HasSight(effect.position))
                    {
                        CancelAttack();evadeMode=1;evadeDirection=Vector3.ProjectOnPlane(transform.position-effect.position,Vector3.up).normalized;
                        if(evadeDirection.sqrMagnitude<.01f)evadeDirection=transform.right;
                        evadeUntil=clock+.72f;nextEvade=clock+2;DodgesAttempted++;return;
                    }
            if (!urgent || soonest > 0.65f || clock < nextEvade || clock < evadeUntil || random.NextDouble() > 0.85) return;
            RememberAttacker(urgent.owner);
            CancelAttack();
            Vector3 sideways = Vector3.Cross(Vector3.up, urgent.velocity).normalized;
            if (Vector3.Dot(sideways, transform.position) < 0f) sideways = -sideways;
            if (Mathf.Abs((transform.position + sideways * 2f).x) > 9.5f*DodgeCourt.Scale || Mathf.Abs((transform.position + sideways * 2f).z) > 12f*DodgeCourt.Scale) sideways = -sideways;
            evadeDirection = sideways;
            strafeSign = Vector3.Dot(sideways, transform.right) >= 0f ? 1f : -1f;
            evadeMode = predictedHeight > 1.25f && soonest > 0.25f ? 1 : 0;
            if (style == DodgeBotStyle.Acrobat && soonest > 0.48f) evadeMode = random.NextDouble() > 0.5 ? 2 : 3;
            evadeUntil = clock + (evadeMode >= 2 ? 1f : evadeMode == 1 ? 0.62f : 0.72f);
            nextEvade = evadeUntil + (evadeMode == 1 ? 1.3f : 0.55f);
            DodgesAttempted++;
            DodgeGame.Instance.EmitFoam(transform.position, -sideways, 5, 0.8f);
        }

        void Move(DodgeGame game, bool evading, bool diving)
        {
            Vector3 direction = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            if (direction.magnitude < 0.65f) direction = Vector3.zero;
            else direction.Normalize();
            if (evading) direction = evadeDirection;
            direction = Steer(direction);
            float speed = Mathf.Min(5.8f, moveSpeed) * (charging || burstLeft > 0 ? 0.38f : 1f);
            if (evading && evadeMode >= 2) speed *= 0.3f;
            speed*=actor.Kit.RetreatSpeed(direction)*(actor.Kit.Flat>0?.55f:1);
            if (diving) speed = dive.Speed*springScale;
            velocity = Vector3.SmoothDamp(velocity, direction * speed, ref movementSpring, diving ? 0.04f : 0.13f);
            motor.height = diving || actor.Kit.Flat>0 ? 0.95f : 1.9f;
            motor.center = Vector3.up * motor.height * 0.5f;
            motor.stepOffset = diving ? 0.1f : 0.2f;
            bool divingBeforeMove = dive.Active;
            if (dive.Active) dive.Step(Time.deltaTime, ref fallSpeed);
            else fallSpeed = motor.isGrounded ? -2f : Mathf.Max(-25f, fallSpeed - DodgeDiveMotion.Gravity * Time.deltaTime);
            Vector3 before = transform.position;
            var collision = motor.Move((velocity + Vector3.up * fallSpeed) * Time.deltaTime);
            if (dive.Resolve(collision, ref fallSpeed)) { actor.Land(); game.EmitFoam(transform.position, -evadeDirection, 7, 1f); }
            if (divingBeforeMove && !dive.Active) { evadeUntil = clock; nextEvade = clock + 1.3f; }
            Vector3 face = HasGoal() ? Vector3.ProjectOnPlane(observedPoint - transform.position, Vector3.up) : direction;
            if (diving) face = direction;
            if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 220f * Time.deltaTime);
            Vector3 localMove = transform.InverseTransformDirection(velocity / Mathf.Max(1f, moveSpeed));
            actor.SetMovement(new Vector2(localMove.x, localMove.z));
            actor.SetLocomotion(diving ? 0f : Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.01f, moveSpeed)));
            bubbleDistance += Vector3.Distance(before, transform.position);
            if (bubbleDistance > 0.9f && motor.isGrounded)
            {
                bubbleDistance = 0f;
                game.EmitFoam(transform.position - velocity.normalized * 0.15f, -velocity, 2, 0.65f);
            }
            if (transform.position.y < -5f) { actor.ResetActor(); ResetBrain(); }
        }

        Vector3 Steer(Vector3 direction)
        {
            Vector3 force = Vector3.zero;
            foreach (var bot in bots)
            {
                if (bot == this) continue;
                Vector3 away = Vector3.ProjectOnPlane(transform.position - bot.transform.position, Vector3.up);
                float distance = away.magnitude;
                if (distance > 0.01f && distance < 1.4f) force += away.normalized * (1.4f - distance) * 2f;
            }
            Vector3 pos = transform.position;
            force.x += Mathf.Max(0f, -9f*DodgeCourt.Scale - pos.x) - Mathf.Max(0f, pos.x - 9f*DodgeCourt.Scale);
            force.z += Mathf.Max(0f, -12f*DodgeCourt.Scale - pos.z) - Mathf.Max(0f, pos.z - 12f*DodgeCourt.Scale);
            int count = direction.sqrMagnitude > 0.001f
                ? Physics.SphereCastNonAlloc(pos + Vector3.up * 0.65f, 0.35f, direction, visibilityHits, 1.4f, ~0, QueryTriggerInteraction.Ignore) : 0;
            for (int i = 0; i < count; i++)
            {
                var hit = visibilityHits[i];
                if (!hit.collider.GetComponent<DodgeSurface>() || Mathf.Abs(hit.normal.y) > 0.6f) continue;
                Vector3 tangent = Vector3.Cross(Vector3.up, hit.normal) * strafeSign;
                force += (hit.normal + tangent) * (1.5f - hit.distance) * 2f;
            }
            return Vector3.ClampMagnitude(direction + force, 1f);
        }

        public void CancelForReferee()
        {
            ClearActions(); velocity = movementSpring = Vector3.zero; fallSpeed = 0f;
            nextAttack = Mathf.Max(nextAttack,clock+.5f);
        }
        void CancelAttack()
        {
            charging = false; burstLeft = 0; Charge = 0f; actor.SetCharge(0f);
            nextAttack = Mathf.Max(nextAttack, clock + 0.4f);
        }

        void BeginThrow()
        {
            charging = false;
            attackPower = Charge;
            queuedWeapon = WantedDrop?DodgeWeapon.Standard:actor.Arsenal.ConsumeAttack();
            burstLeft = queuedWeapon == DodgeWeapon.Burst ? 5 : 1;
            firstRelease = true;
            releaseAt = clock + actor.Kit.ReleaseDelay(queuedWeapon);
            actor.Kit.ConsumeCounter();
            releaseDeadline = releaseAt + .12f;
            float speed = queuedWeapon == DodgeWeapon.Bowling ? Mathf.Lerp(14f, 24f, attackPower) : Mathf.Lerp(queuedWeapon == DodgeWeapon.Burst ? 20f : 17f, 34f, attackPower);
            float flight = Vector3.Distance(transform.position, observedPoint) / speed;
            // Aim is committed before the arm releases. Partial lead is estimated
            // from past observations, with a bounded error that remains for the volley.
            float error = WantedDrop ? 0.06f : EffectiveAimError;
            lockedAim = observedPoint + observedVelocity * Mathf.Min(0.6f, (flight + 0.27f) * 0.65f)
                + new Vector3(Range(-error, error), Range(-error, error) * 0.5f, Range(-error, error));
            actor.Throw(attackPower,releaseAt-clock);
            actor.SetCharge(0f);
            Charge = 0f;
            nextAttack = clock + (queuedWeapon == DodgeWeapon.Burst ? 3.7f : Range(2.5f, 3.4f));
        }

        void Release(DodgeGame game)
        {
            Vector3 origin = actor.HandPosition + transform.forward * 0.2f;
            float speed = queuedWeapon == DodgeWeapon.Bowling ? Mathf.Lerp(14f, 24f, attackPower)
                : Mathf.Lerp(queuedWeapon == DodgeWeapon.Burst ? 20f : 17f, 34f, attackPower);
            DodgeCombat.Release(actor,queuedWeapon,origin,lockedAim,DodgeBall.SolveLaunchVelocity(origin,lockedAim,speed),attackPower,!WantedDrop,Opponent);
            if(!DodgeCombat.IsMelee(queuedWeapon))actor.ReleaseBall();
            ShotsFired++;
            LastWeaponFired = queuedWeapon;
            firstRelease = false;
            burstLeft--;
            releaseAt = clock + 0.1f;
            game.PlayTone(queuedWeapon == DodgeWeapon.Bowling ? 150f : 350f, 0.07f, 0.035f);
        }
    }
}
