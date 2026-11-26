using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperDodgeball
{
    public enum DodgeCrowdThrow { RottenEgg, Soap, Burst, Bowling, PartyGift, Peel, Coaching, Gag, Ammo, Zeus }

    // Spectators share the authoritative ball simulation, while retaining their own seats and scoring rules.
    public sealed partial class DodgeCrowd : MonoBehaviour
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
            if (Pending) { w.Write((byte)throwingSeat); w.Write((byte)selectedItem); w.Write((byte)selectedSkill); }
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
            if (pending)
            {
                int sender=r.ReadByte();var gag=(DodgeItem)r.ReadByte();var skill=(DodgeSkill)r.ReadByte();
                if(sender>=count)throw new System.IO.InvalidDataException("Invalid crowd thrower");
                if(!prop || throwingSeat!=sender || kind!=item || selectedItem!=gag || selectedSkill!=skill)
                {
                    if(prop)Destroy(prop.gameObject);
                    throwingSeat=sender;kind=item;selectedItem=gag;selectedSkill=skill;
                    prop=MakeHandProp();
                }
            }
            else if (prop) { Destroy(prop.gameObject); prop=null; }
            Pending=pending;LockedTargetPoint=point;
            if (!ring)
            {
                ring = new GameObject("Replicated crowd warning").AddComponent<LineRenderer>();
                ring.transform.SetParent(transform, false); ring.sharedMaterial = game.trailMaterial;
                ring.loop = true; ring.positionCount = 48; ring.widthMultiplier = .075f;
            }
            ring.enabled = pending;
            ring.startColor = ring.endColor = WarningColour;
            if (pending) for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48;
                ring.SetPosition(i, new Vector3(point.x + Mathf.Cos(a)*.9f,.09f,point.z+Mathf.Sin(a)*.9f));
            }
        }
        DodgeGame game;
        DodgeActor victim, lastVictim;
        DodgeCrowdThrow kind;
        int throwingSeat, nextAmmoTeam;
        float nextAmmo;
        DodgeItem selectedItem;
        DodgeSkill selectedSkill;
        DodgeGiftKind GiftKind => kind switch
        {
            DodgeCrowdThrow.Ammo=>DodgeGiftKind.Ammo, DodgeCrowdThrow.Zeus=>DodgeGiftKind.Zeus,
            DodgeCrowdThrow.Soap=>DodgeGiftKind.Soap, DodgeCrowdThrow.Peel=>DodgeGiftKind.Peel,
            DodgeCrowdThrow.Coaching=>DodgeGiftKind.Skill, DodgeCrowdThrow.Gag or DodgeCrowdThrow.PartyGift=>DodgeGiftKind.Item,
            _=>DodgeGiftKind.Weapon
        };
        DodgeWeapon GiftWeapon => kind==DodgeCrowdThrow.Bowling?DodgeWeapon.Bowling:kind==DodgeCrowdThrow.Burst?DodgeWeapon.Burst:DodgeWeapon.Standard;
        Color WarningColour => kind==DodgeCrowdThrow.RottenEgg?new Color(1,.68f,.18f):DodgeFanGifts.Colour(GiftKind,GiftWeapon,selectedItem);
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
            DressSupporters();
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
            if(!game.crowdEnabled && Pending && kind!=DodgeCrowdThrow.Ammo && kind!=DodgeCrowdThrow.Zeus){Pending=false;if(prop)Destroy(prop.gameObject);if(ring)ring.enabled=false;}
            if (Pending && clock >= releaseAt) Release();
            if (!Pending && Automatic && clock>=nextAmmo && DodgeApp.Active && DodgeApp.Instance.MatchRunning) ScheduleAmmo();
            if (game.crowdEnabled && !Pending && Automatic && clock >= nextThrow)
            {
                var actors = FindObjectsByType<DodgeActor>(FindObjectsSortMode.InstanceID);
                var candidates = new List<DodgeActor>();
                foreach (var actor in actors)
                    if (actor.isActiveAndEnabled && !actor.IsStunned && actor != lastVictim && (!lastTargeted.TryGetValue(actor, out float t) || clock - t > 8f)) candidates.Add(actor);
                if (candidates.Count > 0)
                {
                    var target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    float roll = UnityEngine.Random.value;
                    DodgeCrowdThrow item = roll < .25f ? DodgeCrowdThrow.RottenEgg : roll < .39f ? DodgeCrowdThrow.Soap
                        : roll < .57f ? DodgeCrowdThrow.Peel : roll < .72f ? DodgeCrowdThrow.Coaching : DodgeCrowdThrow.Gag;
                    int gifts = 0;
                    foreach (var drop in DodgeAirdrop.Active) if (drop.FromCrowd && drop.Kind!=DodgeGiftKind.Ammo && drop.Kind!=DodgeGiftKind.Zeus) gifts++;
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

        void ScheduleAmmo()
        {
            var app=DodgeApp.Instance;
            nextAmmo=clock+.65f;
            for(int offset=0;offset<2;offset++)
            {
                int team=(nextAmmoTeam+offset)%2;
                bool inPlay=false;
                foreach(var drop in DodgeAirdrop.Active)
                    if(drop.Kind==DodgeGiftKind.Ammo && (drop.LandingPoint.z<0?0:1)==team){inPlay=true;break;}
                if(inPlay)continue;
                for(int i=0;i<app.Room.seats.Length;i++)
                {
                    if(app.Room.seats[i].kind==DodgeSeatKind.Empty || app.Room.seats[i].team!=team)continue;
                    var target=app.Actors[i];
                    if(!target || !target.isActiveAndEnabled || target.IsStunned || !target.Arsenal.CanRefill || DodgeReferee.Restricted(target))continue;
                    if(!ScheduleThrow(target,DodgeCrowdThrow.Ammo))continue;
                    nextAmmoTeam=1-team;nextAmmo=clock+7f;return;
                }
            }
        }

        public bool ScheduleThrow(DodgeActor target, DodgeCrowdThrow item, int seat = -1)
        {
            if (!game) game = DodgeGame.Instance;
            if (Pending || !target || !game || (!game.crowdEnabled && item!=DodgeCrowdThrow.Ammo && item!=DodgeCrowdThrow.Zeus) || !game.RoundStarted || !DodgeGame.SimulationActive || game.RoundOver || SpectatorCount == 0) return false;
            throwingSeat = seat >= 0 ? Mathf.Clamp(seat, 0, SpectatorCount - 1) : UnityEngine.Random.Range(0, SpectatorCount);
            if(spectators.Count>throwingSeat && spectators[throwingSeat].Reacting)return false;
            victim=target;kind=item;
            if(item!=DodgeCrowdThrow.Ammo && item!=DodgeCrowdThrow.Zeus){lastVictim=target;lastTargeted[target]=clock;}
            selectedItem=GiftKind==DodgeGiftKind.Item?DodgeFanGifts.Gags[UnityEngine.Random.Range(0,DodgeFanGifts.Gags.Length)]:DodgeItem.None;
            selectedSkill=GiftKind==DodgeGiftKind.Skill?(DodgeSkill)UnityEngine.Random.Range(0,DodgePartyKit.SkillNames.Length):default;
            LockedTargetPoint = item == DodgeCrowdThrow.RottenEgg ? target.transform.position + Vector3.up * 1.35f
                : new Vector3(UnityEngine.Random.Range(-6f, 6f), 0.82f, UnityEngine.Random.Range(-5f, 7f));
            if(item==DodgeCrowdThrow.Ammo)
            {
                int slot=DodgeApp.Instance.SeatOf(target);
                float side=DodgeApp.Instance.Room.seats[slot].team==0?-1:1;
                float x=target.transform.position.x;
                LockedTargetPoint=new Vector3(Mathf.Clamp(x+(x>0?-2.6f:2.6f),-6,6),.82f,side*Mathf.Clamp(Mathf.Abs(target.transform.position.z),3.8f,DodgeCourt.PlayDepth-2));
            }
            else if(item==DodgeCrowdThrow.Zeus)
                LockedTargetPoint=new Vector3(UnityEngine.Random.Range(-2.1f,2.1f),.82f,(game.RoundNumber%2==0?-1:1)*1.25f);
            Pending = true; releaseAt = clock + 0.9f;
            Color color=WarningColour;
            prop=MakeHandProp();
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

        Transform MakeHandProp()
        {
            var hand=seats[throwingSeat].hand;
            if(kind==DodgeCrowdThrow.RottenEgg)
                return Part(hand,PrimitiveType.Sphere,"Rotten egg",Vector3.zero,new Vector3(.27f,.36f,.27f),new Color(.97f,.94f,.69f));
            var gift=DodgeFanGifts.Model(hand,GiftKind,GiftWeapon,selectedItem,selectedSkill);
            gift.localScale=Vector3.one*.52f;
            return gift;
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
                LastGift=DodgeAirdrop.Toss(game,origin,LockedTargetPoint,GiftKind,GiftWeapon,selectedItem,selectedSkill);
                if(kind==DodgeCrowdThrow.Zeus)game.RegisterThundercloud(LastGift);
                GiftsThrown++;
            }
            if (ring) ring.enabled = false;
            nextThrow = clock + UnityEngine.Random.Range(3.8f, 5.5f);
        }

        void OnDestroy(){if(spectatorMaterial)Destroy(spectatorMaterial);foreach(var material in supporterMaterials.Values)Destroy(material);}
        public void ResetCrowd()
        {
            SpectatorHits=0;foreach(var spectator in spectators)spectator.ResetPose(true);
            Pending = false; clock = followUntil = 0f; nextThrow = 6f; nextAmmo=0;nextAmmoTeam=game && game.RoundNumber%2==0?1:0;
            victim = lastVictim = null; lastTargeted.Clear();
            EggsThrown = GiftsThrown = 0; LastEgg = null; LastGift = null;
            if (prop) Destroy(prop.gameObject);
            if (ring) ring.enabled = false;
        }
    }
}
