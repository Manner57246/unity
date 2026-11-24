using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeCrowdThrow { RottenEgg, Soap, Burst, Bowling, PartyGift }

    // Spectators share the authoritative ball simulation, while retaining their own seats and scoring rules.
    public sealed class DodgeCrowd : MonoBehaviour
    {
        [Serializable] public class Seat
        {
            public Transform body, arm, hand, head, otherArm, leftLeg, rightLeg;
            public float phase;
        }
        [SerializeField] Seat[] seats;
        [Serializable] public struct TintEntry { public Renderer renderer; public Color color; }
        [SerializeField] List<TintEntry> tints = new List<TintEntry>();
        public IReadOnlyList<TintEntry> PaletteEntries=>tints;
        public void FinishPalette(){tints.Clear();}
        public int SpectatorCount => seats == null ? 0 : seats.Length;
        public bool Pending { get; private set; }
        public Vector3 LockedTargetPoint { get; private set; }
        public int EggsThrown { get; private set; }
        public int GiftsThrown { get; private set; }
        public DodgeBall LastEgg { get; private set; }
        public DodgeAirdrop LastGift { get; private set; }
        public bool Automatic = true;
        public const int MaximumReactions=10;
        public int SpectatorHits {get;private set;}
        public IReadOnlyList<DodgeSpectator> Spectators=>spectators;
        readonly List<DodgeSpectator> spectators=new List<DodgeSpectator>();
        PhysicsMaterial spectatorMaterial;
        public void BeforeSpectatorHit(DodgeSpectator target)
        {
            int count=0;DodgeSpectator oldest=null;
            foreach(var spectator in spectators)if(spectator.Reacting){count++;if(!oldest || spectator.Remaining<oldest.Remaining)oldest=spectator;}
            if(!target.Reacting && count>=MaximumReactions && oldest)oldest.ResetPose(false);
        }
        public void RecordSpectatorHit(int index)
        {
            SpectatorHits++;
            if(Pending && throwingSeat==index)
            {
                Pending=false;if(prop)Destroy(prop.gameObject);if(ring)ring.enabled=false;nextThrow=clock+2f;
            }
        }
        void FixedUpdate()
        {
            bool running=game && game.RoundStarted && DodgeGame.SimulationActive && DodgeGame.MatchActive && !game.RoundOver;
            foreach(var spectator in spectators)spectator.Step(Time.fixedDeltaTime,running);
        }
        public void InitializeSignage(DodgeClubhouse club)=>DodgeBleachers.Labels(this,club);
        public void WritePose(System.IO.BinaryWriter w)
        {
            w.Write((byte)SpectatorCount);
            foreach (var seat in seats) DodgeNet.WriteRotation(w, seat.arm.localRotation);
            w.Write(SpectatorHits);w.Write(clock);
            int active=0;foreach(var spectator in spectators)if(spectator.Reacting)active++;
            w.Write((byte)active);foreach(var spectator in spectators)if(spectator.Reacting)spectator.WritePose(w);
            w.Write(Pending); DodgeNet.WriteVector(w, LockedTargetPoint); w.Write((byte)kind);
        }
        public void ReadPose(System.IO.BinaryReader r)
        {
            int count = r.ReadByte();
            if (count != SpectatorCount) throw new System.IO.InvalidDataException("Incompatible audience");
            foreach (var seat in seats) seat.arm.localRotation = DodgeNet.ReadRotation(r);
            SpectatorHits=r.ReadInt32();clock=r.ReadSingle();
            int reacting=r.ReadByte();if(reacting>MaximumReactions)throw new System.IO.InvalidDataException("Too many spectator reactions");
            var active=new HashSet<int>();
            for(int i=0;i<reacting;i++){int index=r.ReadByte();if(index>=spectators.Count || !active.Add(index))throw new System.IO.InvalidDataException("Invalid spectator index");spectators[index].ReadPose(r);}
            foreach(var spectator in spectators)if(spectator.Reacting && !active.Contains(spectator.Index))spectator.ResetPose(false);
            bool pending = r.ReadBoolean(); Vector3 point = DodgeNet.ReadVector(r); var item = (DodgeCrowdThrow)r.ReadByte();
            if (!ring)
            {
                ring = new GameObject("Replicated crowd warning").AddComponent<LineRenderer>();
                ring.transform.SetParent(transform, false); ring.sharedMaterial = game.trailMaterial;
                ring.loop = true; ring.positionCount = 48; ring.widthMultiplier = .075f;
            }
            ring.enabled = pending;
            ring.startColor = ring.endColor = item == DodgeCrowdThrow.RottenEgg ? new Color(1f,.68f,.18f) : SoapColor;
            if (pending) for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48;
                ring.SetPosition(i, new Vector3(point.x + Mathf.Cos(a)*.9f,.09f,point.z+Mathf.Sin(a)*.9f));
            }
        }
        DodgeGame game;
        DodgeActor victim, lastVictim;
        DodgeCrowdThrow kind;
        int throwingSeat;
        float clock, nextThrow = 6f, releaseAt, followUntil;
        Transform prop;
        LineRenderer ring;
        readonly Dictionary<DodgeActor, float> lastTargeted = new Dictionary<DodgeActor, float>();
        public static readonly Color SoapColor = new Color(0.43f, 0.9f, 0.77f);

        void Start()
        {
            game = DodgeGame.Instance;
            if (SpectatorCount == 0) BuildAudience(game);
            ApplyTints();
            DodgeBleachers.HideOldSteps();
            spectatorMaterial=new PhysicsMaterial("Spectator jelly"){dynamicFriction=.6f,staticFriction=.6f,bounciness=.12f};
            for(int i=0;i<SpectatorCount;i++)
            {
                var spectator=seats[i].body.parent.GetComponent<DodgeSpectator>();
                spectator.Initialize(this,i,seats[i],spectatorMaterial);spectators.Add(spectator);
            }
        }

        void OnEnable() => ApplyTints();
        void ApplyTints()
        {
            foreach (var tint in tints) if (tint.renderer) DodgeWardrobe.Tint(tint.renderer, tint.color);
        }

        public void BuildAudience(DodgeGame owner)
        {
            game = owner;
            if (SpectatorCount > 0 || !game) return;
            seats=DodgeBleachers.Build(this,game);
        }

        public static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 size, Color color, DodgeGame owner = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            var renderer = go.GetComponent<Renderer>();
            var g = owner ? owner : DodgeGame.Instance;
            renderer.sharedMaterial = g.popMaterial;
            DodgeWardrobe.Tint(renderer, color);
            if (!Application.isPlaying)
            {
                var crowd = parent.GetComponentInParent<DodgeCrowd>();
                if (crowd) crowd.tints.Add(new TintEntry { renderer = renderer, color = color });
            }
            return go.transform;
        }

        void Update()
        {
            if (DodgeGame.IsReplica) return;
            if (!game) game = DodgeGame.Instance;
            if (!game || !game.RoundStarted || !DodgeGame.SimulationActive || !DodgeGame.MatchActive || game.RoundOver) return;
            clock += Time.deltaTime;
            if(!game.crowdEnabled && Pending){Pending=false;if(prop)Destroy(prop.gameObject);if(ring)ring.enabled=false;}
            if (Pending && clock >= releaseAt) Release();
            if (game.crowdEnabled && !Pending && Automatic && clock >= nextThrow)
            {
                var actors = FindObjectsByType<DodgeActor>(FindObjectsSortMode.InstanceID);
                var candidates = new List<DodgeActor>();
                foreach (var actor in actors)
                    if (!actor.IsStunned && actor != lastVictim && (!lastTargeted.TryGetValue(actor, out float t) || clock - t > 8f)) candidates.Add(actor);
                if (candidates.Count > 0)
                {
                    var target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    float roll = UnityEngine.Random.value;
                    DodgeCrowdThrow item = roll < 0.55f ? DodgeCrowdThrow.RottenEgg : roll < 0.76f ? DodgeCrowdThrow.Soap
                        : UnityEngine.Random.value < 0.5f ? DodgeCrowdThrow.Bowling : DodgeCrowdThrow.PartyGift;
                    int gifts = 0;
                    foreach (var drop in DodgeAirdrop.Active) if (drop.FromCrowd) gifts++;
                    if (gifts >= 2) item = DodgeCrowdThrow.RottenEgg;
                    ScheduleThrow(target, item);
                }
                nextThrow = clock + UnityEngine.Random.Range(3.8f, 5.5f);
            }
            for (int i = 0; i < SpectatorCount; i++)
            {
                var s = seats[i];
                if(spectators[i].Reacting)continue;
                float cheer = Mathf.Sin(clock * 3f + s.phase);
                s.body.localRotation=Quaternion.Euler(0,0,cheer*2f);
                float angle = -15f + Mathf.Sin(clock * 2.8f + s.phase) * 23f;
                if (i == throwingSeat && (Pending || clock < followUntil))
                    angle = Pending ? Mathf.Lerp(-20f, -135f, Mathf.Clamp01(1f - (releaseAt - clock) / 0.9f))
                        : Mathf.Lerp(65f, -15f, 1f - (followUntil - clock) / 0.45f);
                s.arm.localRotation = Quaternion.Euler(angle, 0f, -25f);
            }
            if (Pending && ring) ring.widthMultiplier = 0.065f + Mathf.Sin(clock * 15f) * 0.018f;
        }

        public bool ScheduleThrow(DodgeActor target, DodgeCrowdThrow item, int seat = -1)
        {
            if (!game) game = DodgeGame.Instance;
            if (Pending || !target || !game || !game.crowdEnabled || !game.RoundStarted || !DodgeGame.SimulationActive || game.RoundOver || SpectatorCount == 0) return false;
            throwingSeat = seat >= 0 ? Mathf.Clamp(seat, 0, SpectatorCount - 1) : UnityEngine.Random.Range(0, SpectatorCount);
            if(spectators.Count>throwingSeat && spectators[throwingSeat].Reacting)return false;
            victim=lastVictim=target;lastTargeted[target]=clock;kind=item;
            LockedTargetPoint = item == DodgeCrowdThrow.RottenEgg ? target.transform.position + Vector3.up * 1.35f
                : new Vector3(UnityEngine.Random.Range(-6f, 6f), 0.82f, UnityEngine.Random.Range(-5f, 7f));
            Pending = true; releaseAt = clock + 0.9f;
            Color color = item == DodgeCrowdThrow.RottenEgg ? new Color(1f, 0.68f, 0.18f) : item == DodgeCrowdThrow.Soap ? SoapColor
                : item == DodgeCrowdThrow.Bowling ? DodgeArsenal.BowlingColor : DodgeArsenal.BurstColor;
            prop = Part(seats[throwingSeat].hand, item == DodgeCrowdThrow.RottenEgg ? PrimitiveType.Sphere : PrimitiveType.Cube,
                "Spectator is winding up", Vector3.zero, Vector3.one * 0.32f, color);
            if (!ring)
            {
                ring = new GameObject("Crowd throw warning").AddComponent<LineRenderer>();
                ring.transform.SetParent(transform, false); ring.sharedMaterial = game.trailMaterial;
                ring.loop = true; ring.positionCount = 48;
            }
            ring.enabled = true; ring.startColor = ring.endColor = color;
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48;
                ring.SetPosition(i, new Vector3(LockedTargetPoint.x + Mathf.Cos(a) * 0.9f, 0.09f, LockedTargetPoint.z + Mathf.Sin(a) * 0.9f));
            }
            game.PlayTone(item == DodgeCrowdThrow.RottenEgg ? 420f : 720f, 0.14f, 0.06f);
            return true;
        }

        void Release()
        {
            Pending = false; followUntil = clock + 0.45f;
            seats[throwingSeat].arm.localRotation=Quaternion.Euler(65,0,-25);
            Physics.SyncTransforms();
            Vector3 origin = seats[throwingSeat].hand.position;
            if (prop) Destroy(prop.gameObject);
            // Aim was fixed before the windup. Neither eggs nor gifts home in.
            if (kind == DodgeCrowdThrow.RottenEgg)
            {
                float flight = Vector3.Distance(origin, LockedTargetPoint) / 17f;
                flight = Mathf.Clamp(flight, 0.95f, 1.65f);
                Vector3 velocity = (LockedTargetPoint - origin) / flight + Vector3.up * (DodgeBall.Gravity * flight * 0.5f);
                LastEgg = DodgeBall.SpawnEgg(origin, velocity, victim,spectators[throwingSeat]);
                EggsThrown++;
            }
            else
            {
                bool carried=kind==DodgeCrowdThrow.PartyGift && UnityEngine.Random.value<.4f;
                LastGift = DodgeAirdrop.Toss(game, origin, LockedTargetPoint, carried?DodgeGiftKind.Item:kind == DodgeCrowdThrow.Soap ? DodgeGiftKind.Soap : DodgeGiftKind.Weapon,
                    kind == DodgeCrowdThrow.Bowling ? DodgeWeapon.Bowling : kind==DodgeCrowdThrow.Burst?DodgeWeapon.Burst:(DodgeWeapon)UnityEngine.Random.Range(1,7),carried?(DodgeItem)UnityEngine.Random.Range(1,6):DodgeItem.None);
                GiftsThrown++;
            }
            if (ring) ring.enabled = false;
            nextThrow = clock + UnityEngine.Random.Range(3.8f, 5.5f);
        }

        void OnDestroy(){if(spectatorMaterial)Destroy(spectatorMaterial);}
        public void ResetCrowd()
        {
            SpectatorHits=0;foreach(var spectator in spectators)spectator.ResetPose(true);
            Pending = false; clock = followUntil = 0f; nextThrow = 6f;
            victim = lastVictim = null; lastTargeted.Clear();
            EggsThrown = GiftsThrown = 0; LastEgg = null; LastGift = null;
            if (prop) Destroy(prop.gameObject);
            if (ring) ring.enabled = false;
        }
    }
}
