using UnityEngine;
using System.Collections.Generic;

namespace SuperDodgeball
{
    public enum DodgeGiftKind { Weapon, Soap, Item }
    // A trigger can be claimed only by an owned, still-dangerous projectile.
    // Walking into the box or an ownerless practice-cannon shot grants nothing.
    public sealed class DodgeAirdrop : MonoBehaviour
    {
        static readonly HashSet<DodgeAirdrop> active = new HashSet<DodgeAirdrop>();
        public static IReadOnlyCollection<DodgeAirdrop> Active => active;
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);
        public DodgeGiftKind Kind { get; private set; }
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
        Collider hitbox;
        Transform canopy, marker, emblem;
        Vector3 landing;
        float descent, age;
        DodgeGame game;
        int round;
        Vector3 tossOrigin;
        float tossTime;

        public static DodgeAirdrop Spawn(DodgeGame game, Vector3 point, DodgeWeapon reward, bool landed = false, DodgeGiftKind kind = DodgeGiftKind.Weapon, DodgeItem item = DodgeItem.None)
        {
            var go = new GameObject("Shoot to claim - " + reward);
            var drop = go.AddComponent<DodgeAirdrop>();
            drop.game = game;
            drop.round = game.RoundNumber;
            drop.Reward = reward;
            drop.Kind = kind;drop.Item=item;
            drop.landing = new Vector3(point.x, 0.82f, point.z);
            drop.descent = landed ? 1f : 0f;
            go.transform.position = drop.landing + Vector3.up * (landed ? 0f : 7f);
            var hitbox = go.AddComponent<BoxCollider>();
            hitbox.size = new Vector3(1.18f, 1.1f, 1.18f);
            hitbox.isTrigger = true;
            drop.hitbox = hitbox;
            var color = reward == DodgeWeapon.Bowling ? DodgeArsenal.BowlingColor : DodgeArsenal.BurstColor;
            if (kind == DodgeGiftKind.Soap) { color = DodgeCrowd.SoapColor; go.name = "Shoot to claim - Bubble soap"; }
            Part(go.transform, PrimitiveType.Cube, "Cream crate", Vector3.zero, new Vector3(1.1f, 0.8f, 1.1f), game, new Color(1f, 0.89f, 0.68f));
            Part(go.transform, PrimitiveType.Cube, "Ribbon", Vector3.zero, new Vector3(1.13f, 0.84f, 0.18f), game, color);
            Part(go.transform, PrimitiveType.Cube, "Cross ribbon", Vector3.zero, new Vector3(0.18f, 0.85f, 1.13f), game, color);
            drop.emblem = new GameObject("Weapon preview").transform;
            drop.emblem.SetParent(go.transform, false);
            drop.emblem.localPosition = Vector3.up * 0.78f;
            if(kind==DodgeGiftKind.Item) {var icon=DodgePartyVisuals.Item(drop.emblem,item);icon.localScale=Vector3.one*1.2f;}
            else if(kind==DodgeGiftKind.Weapon && reward>=DodgeWeapon.Bat){var icon=DodgePartyVisuals.Weapon(drop.emblem,reward);icon.localScale=Vector3.one*.3f;}
            else if (kind == DodgeGiftKind.Soap)
            {
                Part(drop.emblem, PrimitiveType.Sphere, "Mint soap", Vector3.zero, new Vector3(0.68f, 0.25f, 0.45f), game, color);
                for (int i = 0; i < 3; i++)
                    Part(drop.emblem, PrimitiveType.Sphere, "Soap bubble", new Vector3(-0.2f + i * 0.18f, 0.16f + i * 0.1f, 0f), Vector3.one * (0.13f + i * 0.035f), game, Color.white);
            }
            else if (reward == DodgeWeapon.Bowling)
            {
                var ball = Part(drop.emblem, PrimitiveType.Sphere, "Bowling reward", Vector3.zero, Vector3.one * 0.53f, game, color);
                DodgeArsenal.AddFingerHoles(ball, game.popMaterial);
            }
            else
                for (int i = 0; i < 5; i++)
                    Part(drop.emblem, PrimitiveType.Sphere, "Five-shot reward", new Vector3((i - 2) * 0.18f, Mathf.Abs(i - 2) * 0.06f, 0f), Vector3.one * 0.23f, game, color);
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
            drop.canopy.gameObject.SetActive(!landed);
            return drop;
        }

        public static DodgeAirdrop Toss(DodgeGame game, Vector3 origin, Vector3 point, DodgeGiftKind kind, DodgeWeapon weapon, DodgeItem item=DodgeItem.None)
        {
            var drop = Spawn(game, point, weapon, true, kind,item);
            drop.FromCrowd = true;
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
            if (Replica) return;
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
            if (!wasLanded && Landed)
            {
                canopy.gameObject.SetActive(false);
                game.EmitFoam(landing - Vector3.up * 0.65f, Vector3.zero, 10, 1.2f);
                game.PlayTone(220f, 0.1f, 0.08f);
            }
        }

        public bool TryClaim(DodgeBall ball)
        {
            if (!isActiveAndEnabled || Claimed || !game || !DodgeGame.SimulationActive || game.RoundOver || round != game.RoundNumber
                || !ball || !ball.enabled || ball.SpawnRound != round || !ball.IsDangerous || ball.IsEgg || !ball.owner) return false;
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
