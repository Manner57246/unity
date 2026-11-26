using UnityEngine;
using System.Collections.Generic;

namespace SuperDodgeball
{
    public enum DodgeGiftKind { Weapon, Soap, Item, Zeus, Skill, Peel, Ammo }
    public sealed class DodgeAirdrop : MonoBehaviour
    {
        static readonly HashSet<DodgeAirdrop> active = new HashSet<DodgeAirdrop>();
        public static IReadOnlyCollection<DodgeAirdrop> Active => active;
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);
        public DodgeGiftKind Kind { get; private set; }
        public DodgeSkill Skill {get;private set;}
        public DodgeItem Item {get;private set;}
        public bool FromCrowd { get; private set; }
        public bool Replica;
        public int NetworkId;
        public void ApplyReplica(bool landed)
        {
            descent = landed ? 1f : 0f;
            if (canopy) canopy.gameObject.SetActive(!landed && !FromCrowd);
        }
        public DodgeWeapon Reward { get; private set; }
        public DodgeActor ClaimedBy { get; private set; }
        public bool Claimed { get; private set; }
        public bool Landed => descent >= 1f;
        public Vector3 AimPosition => transform.position;
        internal Vector3 LandingPoint => landing;
        internal bool IsSoapBar => Kind==DodgeGiftKind.Soap || (Kind==DodgeGiftKind.Item && Item==DodgeItem.Soap);
        Collider hitbox;
        Transform canopy, marker, emblem;
        Vector3 landing;
        float descent, age;
        DodgeGame game;
        int round;
        Vector3 tossOrigin;
        float tossTime;

        public static DodgeAirdrop Spawn(DodgeGame game, Vector3 point, DodgeWeapon reward, bool landed = false, DodgeGiftKind kind = DodgeGiftKind.Weapon, DodgeItem item = DodgeItem.None, DodgeSkill skill=default, bool fromCrowd=false)
        {
            var go = new GameObject("Shoot to claim - " + reward);
            var drop = go.AddComponent<DodgeAirdrop>();
            drop.game = game;
            drop.round = game.RoundNumber;
            drop.Reward = reward;
            drop.Kind = kind;drop.Item=item;drop.Skill=skill;drop.FromCrowd=fromCrowd;
            go.name = "Court pickup - "+(kind==DodgeGiftKind.Item?item.ToString():kind.ToString());
            drop.landing = new Vector3(point.x, kind==DodgeGiftKind.Peel?.13f:.82f, point.z);
            drop.descent = landed ? 1f : 0f;
            go.transform.position = drop.landing + Vector3.up * (landed ? 0f : 7f);
            var hitbox = go.AddComponent<BoxCollider>();
            hitbox.size = new Vector3(1.18f, 1.1f, 1.18f);
            hitbox.isTrigger = true;
            drop.hitbox = hitbox;
            var color = DodgeFanGifts.Colour(kind,reward,item);
            bool boxed=kind==DodgeGiftKind.Weapon && !fromCrowd;
            if (drop.IsSoapBar)
            {
                color = DodgeCrowd.SoapColor;
                go.name = "Bubble soap - shoot to claim";
                hitbox.size = new Vector3(1.27f,.80f,.91f);
            }
            if (kind == DodgeGiftKind.Zeus)
            {
                go.name = "Olympian thundercloud - walk to collect";
                hitbox.enabled = false;
                color = DodgeStorm.Gold;
            }
            else if(kind==DodgeGiftKind.Skill || kind==DodgeGiftKind.Peel) hitbox.enabled=false;
            else if(boxed)
            {
                Part(go.transform, PrimitiveType.Cube, "Cream crate", Vector3.zero, new Vector3(1.1f, 0.8f, 1.1f), game, new Color(1f, 0.89f, 0.68f));
            Part(go.transform, PrimitiveType.Cube, "Ribbon", Vector3.zero, new Vector3(1.13f, 0.84f, 0.18f), game, color);
            Part(go.transform, PrimitiveType.Cube, "Cross ribbon", Vector3.zero, new Vector3(0.18f, 0.85f, 1.13f), game, color);
            }
            drop.emblem = DodgeFanGifts.Model(go.transform,kind,reward,item,skill);
            if(boxed)drop.emblem.localPosition=Vector3.up*.78f;
            if(boxed)
            {
                drop.canopy = new GameObject("Marshmallow parachute").transform;
                drop.canopy.SetParent(go.transform, false);
                Part(drop.canopy, PrimitiveType.Sphere, "Canopy", Vector3.up * 2f, new Vector3(2.3f, 0.65f, 2f), game, color);
                for (int i = 0; i < 4; i++)
                {
                    var line = new GameObject("Parachute cord").AddComponent<LineRenderer>();
                    line.transform.SetParent(drop.canopy, false);
                    line.useWorldSpace = false;
                    line.sharedMaterial = game.trailMaterial;
                    line.startWidth = line.endWidth = 0.025f;
                    line.positionCount = 2;
                    line.SetPosition(0, new Vector3(i % 2 == 0 ? -0.45f : 0.45f, 0.35f, i < 2 ? -0.4f : 0.4f));
                    line.SetPosition(1, new Vector3(i % 2 == 0 ? -0.8f : 0.8f, 1.95f, i < 2 ? -0.65f : 0.65f));
                    line.startColor = line.endColor = new Color(1f, 0.97f, 0.87f);
                }
            }
            drop.marker = new GameObject("Airdrop landing ring").transform;
            var ring = drop.marker.gameObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = game.trailMaterial;
            ring.loop = true;
            ring.startWidth = ring.endWidth = 0.075f;
            ring.startColor = ring.endColor = color;
            ring.positionCount = 48;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48;
                ring.SetPosition(i, new Vector3(point.x + Mathf.Cos(angle) * 1.05f, 0.075f, point.z + Mathf.Sin(angle) * 1.05f));
            }
            if (drop.canopy) drop.canopy.gameObject.SetActive(!landed);
            return drop;
        }

        public static DodgeAirdrop Toss(DodgeGame game, Vector3 origin, Vector3 point, DodgeGiftKind kind, DodgeWeapon weapon, DodgeItem item=DodgeItem.None, DodgeSkill skill=default)
        {
            var drop = Spawn(game, point, weapon, true, kind,item,skill,true);
            drop.tossOrigin = origin;
            drop.tossTime = 1.5f;
            drop.descent = 0f;
            drop.transform.position = origin;
            return drop;
        }

        static Transform Part(Transform parent, PrimitiveType shape, string name, Vector3 pos, Vector3 size, DodgeGame game, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial = game.popMaterial;
            DodgeWardrobe.Tint(go.GetComponent<Renderer>(), color);
            return go.transform;
        }

        void Update()
        {
            if (Replica)
            {
                if (DodgeGame.SimulationActive)
                    emblem.Rotate(0,65f*Time.deltaTime,0);
                return;
            }
            if (!game || game.RoundNumber != round) { Destroy(gameObject); return; }
            if (!DodgeGame.SimulationActive || game.RoundOver) return;
            age += Time.deltaTime;
            if (age > 23f) { Destroy(gameObject); return; }
            bool wasLanded = Landed;
            descent = Mathf.Min(1f, descent + Time.deltaTime / (FromCrowd ? tossTime : 3f));
            transform.position = FromCrowd ? Vector3.Lerp(tossOrigin, landing, descent) + Vector3.up * (4f * descent * (1f - descent) * 4f)
                : landing + Vector3.up * ((1f - Mathf.SmoothStep(0f, 1f, descent)) * 7f);
            transform.rotation = Quaternion.Euler(0f, Mathf.Sin(age * 1.2f) * 8f, Mathf.Sin(age * 2.1f) * (Landed ? 0f : 4f));
            emblem.localRotation = Quaternion.Euler(0f, age * 65f, 0f);
            if (Kind == DodgeGiftKind.Peel && Landed)
            {
                DodgePartyEffects.Instance.Spawn(DodgeEffect.Banana,new Vector3(landing.x,.10f,landing.z),Vector3.zero,null,15f);
                gameObject.SetActive(false); Destroy(gameObject); return;
            }
            if ((Kind == DodgeGiftKind.Zeus || Kind == DodgeGiftKind.Skill || Kind==DodgeGiftKind.Ammo) && Landed && DodgeApp.Active && DodgeApp.Instance.MatchRunning)
            {
                DodgeActor nearest = null;
                float best = 1.02f * 1.02f;
                foreach (var contender in DodgeApp.Instance.Actors)
                {
                    if (Kind == DodgeGiftKind.Skill && contender.Kit.Has(Skill) || Kind==DodgeGiftKind.Ammo && !CanTakeAmmo(contender)) continue;
                    if (!contender.isActiveAndEnabled || contender.IsStunned || DodgeReferee.Restricted(contender)) continue;
                    float distance = (contender.transform.position + Vector3.up * .82f - transform.position).sqrMagnitude;
                    if (distance < best) { nearest = contender; best = distance; }
                }
                if (nearest)
                {
                    if(Kind==DodgeGiftKind.Ammo){GiveAmmo(nearest);return;}
                    Claimed = true; ClaimedBy = nearest;
                    if (Kind == DodgeGiftKind.Zeus) nearest.Zeus.TransformForMatch();
                    else nearest.Kit.LearnFromCrowd(Skill);
                    gameObject.SetActive(false); Destroy(gameObject);
                    return;
                }
            }
            if (!wasLanded && Landed)
            {
                if (canopy) canopy.gameObject.SetActive(false);
                game.EmitFoam(landing - Vector3.up * 0.65f, Vector3.zero, 10, 1.2f);
                game.PlayTone(220f, 0.1f, 0.08f);
            }
        }

        static bool CanTakeAmmo(DodgeActor actor)
        {
            return actor && actor.isActiveAndEnabled && !actor.IsStunned && actor.Arsenal.CanRefill && !DodgeReferee.Restricted(actor);
        }

        bool GiveAmmo(DodgeActor actor)
        {
            if(Replica || DodgeGame.IsReplica || Claimed || !DodgeApp.Active || !DodgeApp.Instance.MatchRunning || !CanTakeAmmo(actor) || !actor.Arsenal.Refill())return false;
            Claimed=true;ClaimedBy=actor;
            DodgePartyEffects.Instance.Spawn(DodgeEffect.Refill,AimPosition,Vector3.zero,actor,.65f);
            game.PlayTone(820,.14f,.10f);
            var bot=actor.GetComponent<DodgeBot>();if(bot)bot.ClaimedCrate();
            gameObject.SetActive(false);Destroy(gameObject);
            return true;
        }

        public bool TryClaim(DodgeBall ball)
        {
            if (Replica || DodgeGame.IsReplica || Kind == DodgeGiftKind.Peel || Kind == DodgeGiftKind.Skill || Kind == DodgeGiftKind.Zeus || !isActiveAndEnabled || Claimed || !game || !DodgeGame.SimulationActive || game.RoundOver || round != game.RoundNumber
                || !ball || !ball.enabled || ball.SpawnRound != round || !ball.IsDangerous || ball.IsEgg || !ball.owner) return false;
            if(Kind==DodgeGiftKind.Ammo)return ball.Weapon==DodgeWeapon.Standard && GiveAmmo(ball.owner);
            if (Kind == DodgeGiftKind.Item && ball.owner.Kit.Item != DodgeItem.None) return false;
            // Resolve synchronously: further hits in this physics step cannot award twice.
            Claimed = true;
            ClaimedBy = ball.owner;
            hitbox.enabled = false;
            if (Kind == DodgeGiftKind.Soap) ClaimedBy.CrowdStatus.Wash();
            else if(Kind==DodgeGiftKind.Item)ClaimedBy.Kit.Grant(Item);
            else ClaimedBy.Arsenal.Equip(Reward);
            var bot = ClaimedBy.GetComponent<DodgeBot>();
            if (bot) bot.ClaimedCrate();
            game.EmitFoam(transform.position, Vector3.up, 14, 1.4f);
            game.SpawnPop(transform.position, ClaimedBy.bodyColor);
            game.PlayTone(980f, 0.13f, 0.12f);
            if (ClaimedBy.isPlayer) game.view.Kick(0.035f);
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }

        void OnDestroy() { if (marker) Destroy(marker.gameObject); }
    }
}
