using UnityEngine;
using System.Collections.Generic;

namespace SuperDodgeball
{
    public sealed class DodgeBall : MonoBehaviour
    {
        static readonly HashSet<DodgeBall> active = new HashSet<DodgeBall>();
        public static IReadOnlyCollection<DodgeBall> Active => active;
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);
        public const float Radius = 0.16f;
        public const float Gravity = 9.5f;
        public DodgeActor owner;
        public bool hostile;
        public Vector3 velocity;
        public Vector3 TargetSnapshot { get; private set; }
        public int SpawnRound { get; private set; }
        public int BounceCount { get; private set; }
        public bool IsDangerous { get; private set; } = true;
        public float Power { get; private set; }
        public DodgeWeapon Weapon { get; private set; }
        public bool IsEgg { get; private set; }
        DodgeSpectator audienceSource;
        public bool Replica;
        public int NetworkId;
        public float CollisionRadius => Weapon == DodgeWeapon.Bowling ? 0.265f : Radius;
        readonly HashSet<DodgeActor> nearDodges=new HashSet<DodgeActor>();
        bool countedDodge;
        bool tracksPlayerDodge;
        Vector3 initialDirection;
        Vector3 spawnPoint;
        float lifetime = 6f;
        float squash;
        bool settled;
        TrailRenderer trail;
        readonly RaycastHit[] hits = new RaycastHit[64];
        readonly Collider[] overlaps = new Collider[32];

        public static DodgeBall Spawn(Vector3 origin, Vector3 speed, DodgeActor source, bool enemy, Material material,
            DodgeWeapon weapon = DodgeWeapon.Standard, DodgeActor intendedTarget = null)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = enemy ? "Incoming dodgeball" : "Player dodgeball";
            var primitiveCollider = ball.GetComponent<Collider>();
            primitiveCollider.enabled = false;
            Destroy(primitiveCollider);
            ball.transform.SetPositionAndRotation(origin, Quaternion.identity);
            ball.transform.localScale = Vector3.one * Radius * 2f;
            ball.GetComponent<Renderer>().sharedMaterial = material;
            var projectile = ball.AddComponent<DodgeBall>();
            projectile.Weapon = weapon;
            ball.transform.localScale = Vector3.one * projectile.CollisionRadius * 2f;
            if (weapon != DodgeWeapon.Standard)
            {
                ball.name = weapon == DodgeWeapon.Bowling ? "Flying bowling ball" : "Five-shot dodgeball";
                ball.GetComponent<Renderer>().sharedMaterial = DodgeGame.Instance.popMaterial;
                DodgeWardrobe.Tint(ball.GetComponent<Renderer>(), weapon == DodgeWeapon.Bowling ? DodgeArsenal.BowlingColor : DodgeArsenal.BurstColor);
                if (weapon == DodgeWeapon.Bowling) DodgeArsenal.AddFingerHoles(ball.transform, DodgeGame.Instance.popMaterial);
            }
            if(weapon==DodgeWeapon.Glove){ball.GetComponent<Renderer>().enabled=false;DodgePartyVisuals.Glove(ball.transform,Vector3.zero,1.8f);}
            projectile.owner = source;
            projectile.hostile = enemy;
            projectile.tracksPlayerDodge = enemy && (!source || (intendedTarget && intendedTarget.isPlayer));
            projectile.velocity = speed;
            projectile.initialDirection = speed.normalized;
            projectile.spawnPoint = origin;
            projectile.SpawnRound = DodgeGame.Instance ? DodgeGame.Instance.RoundNumber : 0;
            if (DodgeGame.Instance) projectile.TargetSnapshot = DodgeGame.Instance.player.transform.position;
            var trail = ball.AddComponent<TrailRenderer>();
            projectile.trail = trail;
            trail.sharedMaterial = DodgeGame.Instance.trailMaterial;
            trail.startColor = enemy ? new Color(1f, 0.3f, 0.16f, 0.85f) : new Color(1f, 0.87f, 0.35f, 0.8f);
            trail.endColor = new Color(1f, 0.6f, 0.1f, 0f);
            trail.startWidth = 0.17f;
            trail.endWidth = 0.015f;
            trail.time = 0.14f;
            trail.minVertexDistance = 0.08f;
            trail.numCapVertices = 3;
            return projectile;
        }

        public void SetPower(float power)
        {
            Power = Mathf.Clamp01(power);
            if (!trail) return;
            trail.time = Mathf.Lerp(0.11f, 0.24f, Power);
            trail.startWidth = Mathf.Lerp(0.12f, 0.2f, Power);
            trail.startColor = Color.Lerp(new Color(1f, 0.82f, 0.62f), new Color(1f, 0.72f, 0.32f), Power);
        }

        public static DodgeBall SpawnEgg(Vector3 origin, Vector3 velocity, DodgeActor target, DodgeSpectator sourceSpectator=null)
        {
            var egg = Spawn(origin, velocity, null, target && target.isPlayer, DodgeGame.Instance.popMaterial, DodgeWeapon.Standard, target);
            egg.IsEgg = true;egg.audienceSource=sourceSpectator;
            egg.tracksPlayerDodge = target && target.isPlayer;
            egg.name = "Crowd rotten egg";
            DodgeWardrobe.Tint(egg.GetComponent<Renderer>(), new Color(0.97f, 0.94f, 0.69f));
            egg.trail.startColor = new Color(1f, 0.8f, 0.3f, 0.7f);
            egg.trail.endColor = new Color(1f, 0.9f, 0.5f, 0f);
            return egg;
        }

        public static Vector3 SolveLaunchVelocity(Vector3 origin, Vector3 target, float speed)
        {
            Vector3 difference = target - origin;
            Vector3 planar = Vector3.ProjectOnPlane(difference, Vector3.up);
            float range = planar.magnitude;
            if (range < 0.01f) return difference.normalized * speed;
            float square = speed * speed;
            float discriminant = square * square - Gravity * (Gravity * range * range + 2f * difference.y * square);
            if (discriminant < 0f) return difference.normalized * speed;
            float angle = Mathf.Atan((square - Mathf.Sqrt(discriminant)) / (Gravity * range));
            return planar.normalized * (Mathf.Cos(angle) * speed) + Vector3.up * (Mathf.Sin(angle) * speed);
        }

        public static bool Trace(Vector3 from, Vector3 to, float radius, DodgeActor ignore, RaycastHit[] buffer,
            out RaycastHit nearestHit, out DodgeActor hitActor, bool includeActors = true, bool includeDrops = true, DodgeSpectator ignoreAudience=null)
        {
            Vector3 motion = to - from;
            nearestHit = default;
            hitActor = null;
            if (motion.sqrMagnitude < 0.000001f) return false;
            int count = Physics.SphereCastNonAlloc(from, radius, motion.normalized, buffer, motion.magnitude, ~0, QueryTriggerInteraction.Collide);
            float nearest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var body = buffer[i].collider.GetComponent<DodgeHitbox>();
                var drop = buffer[i].collider.GetComponent<DodgeAirdrop>();
                var spectator=buffer[i].collider.GetComponent<DodgeSpectatorHitbox>();
                if(spectator && (!includeActors || spectator.spectator==ignoreAudience))continue;
                if (drop && !includeDrops) continue;
                if (body && !includeActors) continue;
                if (body && body.actor == ignore) continue;
                if (!body && !drop && !spectator && !buffer[i].collider.GetComponent<DodgeSurface>()) continue;
                if (buffer[i].distance >= nearest) continue;
                nearest = buffer[i].distance;
                nearestHit = buffer[i];
                hitActor = body ? body.actor : null;
                found = true;
            }
            return found;
        }

        void FixedUpdate()
        {
            if (Replica) return;
            var game = DodgeGame.Instance;
            if (!game || game.RoundOver || SpawnRound != game.RoundNumber) { Destroy(gameObject); return; }
            if (game.RoundStarted && !DodgeGame.SimulationActive) return;
            lifetime -= Time.fixedDeltaTime;
            if (lifetime <= 0f) { Destroy(gameObject); return; }
            squash = Mathf.MoveTowards(squash, 0f, Time.fixedDeltaTime * 5f);
            float radius = CollisionRadius;
            transform.localScale = Vector3.Scale(Vector3.one * radius * 2f, new Vector3(1f + squash * 0.22f, 1f - squash * 0.28f, 1f + squash * 0.22f));
            if (IsEgg) transform.localScale = new Vector3(0.27f, 0.37f, 0.27f);
            if(Weapon==DodgeWeapon.Glove && velocity.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(velocity);
            if (settled) return;
            // Pose transforms are driven in Update, so synchronize before querying hitboxes.
            Physics.SyncTransforms();
            Vector3 position = transform.position;
            Vector3 reflectionEnd=position+velocity*Time.fixedDeltaTime;
            if(Trace(position,reflectionEnd,radius,owner,hits,out RaycastHit initialStop,out DodgeActor initialActor,IsDangerous,IsDangerous && owner,audienceSource))reflectionEnd=position+(reflectionEnd-position).normalized*initialStop.distance;
            if(IsDangerous && TryReflect(position,reflectionEnd))return;
            // Sphere casts do not report colliders already overlapping the starting sphere.
            int count = Physics.OverlapSphereNonAlloc(position, radius, overlaps, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var rest=overlaps[i].GetComponent<DodgeRestTarget>();if(rest && HitRest(rest))return;
                var menu = overlaps[i].GetComponent<DodgeLobbyTarget>();
                if(menu && HitMenu(menu))return;
                var drop = overlaps[i].GetComponent<DodgeAirdrop>();
                if (drop && ClaimDrop(drop)) return;
                var spectator=overlaps[i].GetComponent<DodgeSpectatorHitbox>();
                if(spectator && spectator.spectator==audienceSource)continue;
                if(spectator && IsDangerous){HitSpectator(position,spectator.spectator);return;}
                var part = overlaps[i].GetComponent<DodgeHitbox>();
                if (part && part.actor != owner && IsDangerous)
                {
                    HitBody(position, part.actor);
                    transform.position += velocity.normalized * 0.08f;
                    return;
                }
                if (overlaps[i].GetComponent<DodgeSurface>())
                {
                    Vector3 point = overlaps[i].ClosestPoint(position);
                    Vector3 normal = position - point;
                    if (normal.sqrMagnitude < 0.00001f) normal = Vector3.up;
                    normal.Normalize();
                    position = point + normal * (radius + 0.015f);
                    if (Vector3.Dot(velocity, normal) < 0f) Bounce(point, normal);
                    if (!enabled) return;
                }
            }
            float remaining = Time.fixedDeltaTime;
            for (int step = 0; step < 4 && remaining > 0.0001f; step++)
            {
                Vector3 acceleration = Vector3.down * Gravity;
                Vector3 next = position + velocity * remaining + acceleration * (0.5f * remaining * remaining);
                Vector3 motion = next - position;
                Vector3 clearEnd=next;
                if(Trace(position,next,radius,owner,hits,out RaycastHit stop,out DodgeActor stopActor,IsDangerous,IsDangerous && owner,audienceSource))clearEnd=position+motion.normalized*stop.distance;
                if(IsDangerous && TryReflect(position,clearEnd)){transform.position=position;return;}
                if(IsDangerous && DodgePartyEffects.Instance && DodgePartyEffects.Instance.Intercept(position,clearEnd,owner)){IsDangerous=false;velocity*=.2f;lifetime=.8f;}
                if(IsDangerous)CheckNearDodge(position,clearEnd);
                if (Trace(position, next, radius, owner, hits, out RaycastHit hit, out DodgeActor actor, IsDangerous, IsDangerous && owner,audienceSource))
                {
                    var rest=hit.collider.GetComponent<DodgeRestTarget>();if(rest && HitRest(rest))return;
                    var menu=hit.collider.GetComponent<DodgeLobbyTarget>();
                    if(menu && HitMenu(menu))return;
                    var drop = hit.collider.GetComponent<DodgeAirdrop>();
                    if (drop && ClaimDrop(drop)) return;
                    float fraction = Mathf.Clamp01(hit.distance / Mathf.Max(0.0001f, motion.magnitude));
                    float elapsed = Mathf.Max(0.00005f, remaining * fraction);
                    velocity += acceleration * elapsed;
                    position = hit.point + hit.normal * (radius + 0.012f);
                    var spectator=hit.collider.GetComponent<DodgeSpectatorHitbox>();
                    if(spectator)HitSpectator(hit.point,spectator.spectator);
                    else if (actor) HitBody(hit.point, actor);
                    else Bounce(hit.point, hit.normal);
                    if (!enabled) return;
                    remaining = Mathf.Max(0f, remaining - elapsed);
                    if (settled) break;
                }
                else
                {
                    position = next;
                    velocity += acceleration * remaining;
                    remaining = 0f;
                }
            }
            transform.position = position;
            transform.Rotate(new Vector3(200f, 350f, 80f) * Time.fixedDeltaTime);
            if (tracksPlayerDodge && !countedDodge && Vector3.Dot(position - TargetSnapshot, initialDirection) > 0.8f)
            {
                countedDodge = true;
                if (Vector3.Distance(game.player.transform.position, TargetSnapshot) < 2.5f) game.RegisterDodge();
            }
            if ((position - spawnPoint).sqrMagnitude > 6400f) Destroy(gameObject);
        }

        void HitSpectator(Vector3 point,DodgeSpectator spectator)
        {
            if(!IsDangerous)return;
            if(IsEgg){BreakEgg(point);return;}
            float force=Weapon==DodgeWeapon.Bowling?5.2f:Weapon==DodgeWeapon.Glove?6.2f:Mathf.Lerp(2.4f,4.2f,Power);
            spectator.Hit(point,velocity.normalized*force+Vector3.up*(Weapon==DodgeWeapon.Bowling?4.5f:Weapon==DodgeWeapon.Glove?3.2f:2.4f),owner);
            IsDangerous=false;countedDodge=true;velocity=-velocity*.25f+Vector3.up*2;lifetime=Mathf.Min(lifetime,1.5f);squash=1;
        }
        void HitBody(Vector3 point, DodgeActor actor)
        {
            if (!IsDangerous) return;
            if(DodgeApp.Active && !DodgeApp.Instance.CanHurt(owner,actor))
            { IsDangerous=false;velocity=-velocity*.2f+Vector3.up*1.5f;lifetime=Mathf.Min(lifetime,1f);return; }
            if (IsEgg)
            {
                bool affected = actor.CrowdStatus.ApplyEgg();
                if (affected && actor.isPlayer) DodgeGame.Instance.view.Kick(0.025f);
                BreakEgg(point);
                return;
            }
            bool launched = actor.ReceiveHit(point, owner && owner.isPlayer, velocity * (Weapon==DodgeWeapon.Glove?.55f:.23f) + Vector3.up * 1.2f,
                Weapon == DodgeWeapon.Bowling ? 8.2f + Power * 0.8f : Weapon==DodgeWeapon.Glove?5.5f:0f);
            if (launched && DodgeApp.Active) DodgeApp.Instance.CreditHit(owner, actor);
            var bot = actor.GetComponent<DodgeBot>();
            if (launched && bot) bot.RememberAttacker(owner);
            if (launched && Weapon == DodgeWeapon.Bowling)
            {
                DodgeGame.Instance.EmitFoam(point, Vector3.up * 2f, 16, 1.6f);
                DodgeGame.Instance.PlayTone(90f, 0.18f, 0.16f);
                if (owner && owner.isPlayer) DodgeGame.Instance.view.Kick(0.045f);
            }
            IsDangerous = false;
            countedDodge = true;
            velocity = -velocity * 0.24f + Vector3.up * 2.1f;
            lifetime = Mathf.Min(lifetime, 1.6f);
            squash = 1f;
            DodgeGame.Instance?.SpawnPop(point, hostile ? new Color(1f, 0.34f, 0.2f) : new Color(1f, 0.84f, 0.2f));
        }

        bool ClaimDrop(DodgeAirdrop drop)
        {
            if (IsEgg) return false;
            if (!drop.TryClaim(this)) return false;
            IsDangerous = false;
            enabled = false;
            Destroy(gameObject);
            return true;
        }

        bool HitRest(DodgeRestTarget target)
        {if(!target.Hit(this))return false;IsDangerous=false;enabled=false;Destroy(gameObject);return true;}
        bool TryReflect(Vector3 from,Vector3 to)
        {
            if(IsEgg || !DodgeApp.Active)return false;
            foreach(var actor in DodgeApp.Instance.Actors)
            {
                if(!actor.gameObject.activeSelf || actor==owner || actor.Kit.SwingWeapon!=DodgeWeapon.Bat || actor.Kit.Swing<.12f || actor.IsStunned || DodgeReferee.Restricted(actor))continue;
                if(DodgeApp.Instance.SameTeam(owner,actor))continue;
                Vector3 center=actor.transform.position+Vector3.up*1.2f,delta=to-from;
                float t=delta.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(center-from,delta)/delta.sqrMagnitude);
                if(Vector3.Distance(center,from+delta*t)>1.65f || Vector3.Dot(velocity,center-from)<=0 || !DodgePartyEffects.LineClear(from,center))continue;
                owner=actor;hostile=!actor.isPlayer;velocity=DodgeBall.SolveLaunchVelocity(from,actor.Kit.SwingAim,Mathf.Max(22,velocity.magnitude));initialDirection=velocity.normalized;countedDodge=true;
                DodgeGame.Instance.EmitFoam(from,velocity.normalized,6,1);return true;
            }
            return false;
        }
        void CheckNearDodge(Vector3 from,Vector3 to)
        {
            if(!DodgeApp.Active || DodgeApp.Instance.Page!=DodgePage.Playing)return;
            foreach(var actor in DodgeApp.Instance.Actors)
            {
                if(!actor.gameObject.activeSelf || actor==owner || nearDodges.Contains(actor) || actor.IsStunned || !DodgeApp.Instance.CanHurt(owner,actor))continue;
                if(actor.DiveAmount<.3f && Mathf.Abs(actor.Lean)<.45f && actor.ShapeAmount<.4f)continue;
                Vector3 point=actor.transform.position+Vector3.up*(actor.DiveAmount>.3f?.65f:1.2f),delta=to-from;
                float t=delta.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(point-from,delta)/delta.sqrMagnitude);
                if(Vector3.Distance(point,from+delta*t)>1.35f || Vector3.Dot(to-point,velocity)<=0)continue;
                // A swept hit on any real body part is a hit, not a successful dodge.
                if(Trace(from,to,CollisionRadius,owner,hits,out RaycastHit hit,out DodgeActor body) && body==actor)continue;
                nearDodges.Add(actor);actor.Kit.Dodged(actor.DiveAmount>.3f);
            }
        }
        bool HitMenu(DodgeLobbyTarget target)
        {
            if(!target.Hit(this))return false;
            IsDangerous=false;enabled=false;Destroy(gameObject);return true;
        }

        void Bounce(Vector3 point, Vector3 normal)
        {
            if (IsEgg) { BreakEgg(point); return; }
            float impactSpeed = Mathf.Abs(Vector3.Dot(velocity, normal));
            velocity = Vector3.Reflect(velocity, normal) * (normal.y > 0.6f ? 0.62f : 0.76f);
            BounceCount++;
            squash = Mathf.Clamp01(impactSpeed / 8f);
            if (BounceCount >= 3) IsDangerous = false;
            if (impactSpeed > 2.2f)
            {
                DodgeGame.Instance?.EmitFoam(point, normal, 2, 0.5f);
                DodgeGame.Instance?.PlayTone(180f + Mathf.Min(impactSpeed, 12f) * 10f, 0.05f, 0.025f);
            }
            if (normal.y > 0.6f && velocity.sqrMagnitude < 2.5f)
            {
                velocity = Vector3.zero;
                settled = true;
                IsDangerous = false;
                lifetime = Mathf.Min(lifetime, 1.2f);
                if (trail) trail.emitting = false;
            }
        }

        void BreakEgg(Vector3 point)
        {
            IsDangerous = false;
            countedDodge = true;
            DodgeGame.Instance.SpawnPop(point, new Color(1f, 0.73f, 0.19f));
            DodgeGame.Instance.EmitFoam(point, Vector3.up, 5, 0.6f);
            DodgeGame.Instance.PlayTone(135f, 0.1f, 0.08f);
            enabled = false;
            Destroy(gameObject);
        }
    }
}
