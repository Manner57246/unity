using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    // Only the host simulates the props. The same ID travels in snapshots until somebody takes it.
    public sealed partial class DodgePartyEffects
    {
        sealed class LooseWeapon
        {
            public int id, ammo;
            public DodgeWeapon kind;
            public Rigidbody body;
            public float unlock;
            public DodgeNetView view;
            public Vector3 safePosition;
        }

        readonly Dictionary<int, LooseWeapon> weapons = new();
        readonly HashSet<int> seenWeapons = new();
        readonly List<int> removedWeapons = new();
        int nextWeaponId;
        public int LooseCount => weapons.Count;

        public static Transform WorldWeapon(Transform parent, DodgeWeapon kind)
        {
            if (kind == DodgeWeapon.Bowling)
            {
                var ball = DodgePartyVisuals.Part(parent, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .55f, DodgeArsenal.BowlingColor);
                DodgeArsenal.AddFingerHoles(ball, DodgeGame.Instance.popMaterial);
                return ball;
            }
            var model = DodgePartyVisuals.Weapon(parent, kind);
            model.localScale = Vector3.one * .34f;
            return model;
        }

        LooseWeapon CreateWeapon(int id, DodgeWeapon kind, int ammo, Vector3 pos, Quaternion rotation, bool replica)
        {
            var root = new GameObject("落地 · " + DodgeArsenal.Names[(int)kind]);
            root.transform.SetParent(transform, true);
            var model = WorldWeapon(root.transform, kind);
            // Measure the actual silhouette in local space before rotating the whole prop.
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.localPosition -= bounds.center - root.transform.position;
            var box = root.AddComponent<BoxCollider>();
            box.size = Vector3.Max(bounds.size, Vector3.one * .17f);
            box.enabled = !replica;
            root.transform.SetPositionAndRotation(pos, rotation);
            var body = root.AddComponent<Rigidbody>();
            body.mass = 1.8f;
            body.isKinematic = replica;
            if (!replica)
            {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.linearDamping = .4f;
                body.angularDamping = 1.8f;
            }
            var pickup = new LooseWeapon { id = id, kind = kind, ammo = ammo, body = body,
                safePosition = pos, unlock = Time.time + .28f, view = replica ? root.AddComponent<DodgeNetView>() : null };
            weapons.Add(id, pickup);
            return pickup;
        }

        public static Vector3 FreeSlot(int team, int index) =>
            new Vector3(index == 0 ? -2.0f : 2.0f, .85f, (team == 0 ? -1 : 1) * DodgeCourt.RestZ(24.5f));

        public void StockRooms()
        {
            int a = Random.Range(1, 9), b = Random.Range(1, 8);
            if (b >= a) b++;
            for (int team = 0; team < 2; team++)
                for (int i = 0; i < 2; i++)
                {
                    var kind = (DodgeWeapon)(i == 0 ? a : b);
                    CreateWeapon(++nextWeaponId, kind, DodgeArsenal.Capacity(kind), FreeSlot(team,i), Quaternion.Euler(0, 25, 74), false);
                }
        }

        static bool CanHandle(DodgeActor actor)
        {
            var app = DodgeApp.Instance;
            if (!actor || !app.IsHost || !app.SimulationRunning ||
                (app.Page != DodgePage.Staging && app.Page != DodgePage.Playing) || !actor.isActiveAndEnabled ||
                actor.IsStunned || DodgeReferee.Restricted(actor) || actor.Kit.ArmedTime > 0 || actor.Kit.Swing > 0 ||
                actor.Kit.PantLeft > 0 || actor.Kit.BiteLeft > 0) return false;
            var controller = actor.GetComponent<DodgePlayer>();
            var bot = actor.GetComponent<DodgeBot>();
            return (!controller || !controller.enabled || !controller.WeaponBusy) && (!bot || !bot.enabled || !bot.WeaponBusy);
        }

        LooseWeapon Nearest(DodgeActor actor)
        {
            float best = 1.9f * 1.9f;
            Vector3 hand = actor.transform.position + Vector3.up * .75f;
            LooseWeapon nearest = null;
            foreach (var pickup in weapons.Values)
            {
                Vector3 p = pickup.body.position;
                float distance = (p-hand).sqrMagnitude;
                if (distance >= best || Time.time < pickup.unlock || !LineClear(hand, p)) continue;
                nearest = pickup;
                best = distance;
            }
            return nearest;
        }
        internal bool FindRoomWeapon(int team, Vector3 from, out Vector3 point)
        {
            point=from;
            float best=float.MaxValue;
            foreach(var p in weapons.Values)
            {
                Vector3 pos=p.body.position;
                if(!DodgeCourt.InRest(pos) || (pos.z>0?1:0)!=team) continue;
                float distance=(from-pos).sqrMagnitude;
                if(distance<best){best=distance;point=pos;}
            }
            return best<float.MaxValue;
        }
        public string NearbyName(DodgeActor actor)
        {
            var pickup = Nearest(actor);
            return pickup == null ? null : DodgeArsenal.Names[(int)pickup.kind];
        }
        void ReleaseHeld(DodgeActor actor)
        {
            var gear = actor.Arsenal;
            Vector3 hand = actor.transform.position + Vector3.up * 1.1f;
            Vector3 forward = actor.transform.forward;
            Vector3 release = hand + forward * .85f;
            if (!LineClear(hand, release + forward * .45f)) release = hand;
            var pickup = CreateWeapon(++nextWeaponId, gear.Weapon, gear.Charges, release, actor.transform.rotation, false);
            pickup.body.linearVelocity = forward * 2.5f + Vector3.up * 1.4f;
            pickup.body.angularVelocity = actor.transform.right * 3.5f;
            gear.ResetEquipment();
        }
        public bool DropWeapon(DodgeActor actor)
        {
            if (!CanHandle(actor) || actor.Arsenal.Weapon == DodgeWeapon.Standard) return false;
            ReleaseHeld(actor);
            return true;
        }
        public bool CollectWeapon(DodgeActor actor)
        {
            if (!CanHandle(actor)) return false;
            var pickup = Nearest(actor);
            if (pickup == null) return false;
            RemoveWeapon(pickup.id);
            if (actor.Arsenal.Weapon != DodgeWeapon.Standard) ReleaseHeld(actor);
            actor.Arsenal.ApplyReplica(pickup.kind, pickup.ammo, true);
            return true;
        }
        internal bool IssueWeapon(DodgeActor actor, DodgeWeapon kind)
        {
            if (!CanHandle(actor)) return false;
            if (actor.Arsenal.Weapon != DodgeWeapon.Standard) ReleaseHeld(actor);
            actor.Arsenal.Equip(kind, DodgeArsenal.Capacity(kind));
            return true;
        }

        void RemoveWeapon(int id)
        {
            var pickup = weapons[id];
            weapons.Remove(id);
            pickup.body.gameObject.SetActive(false);
            Destroy(pickup.body.gameObject);
        }
        void ClearWeapons()
        {
            foreach (var pickup in weapons.Values) { pickup.body.gameObject.SetActive(false); Destroy(pickup.body.gameObject); }
            weapons.Clear();
        }
        void FixedUpdate()
        {
            if (!DodgeApp.Active || !DodgeApp.Instance.IsHost) return;
            foreach (var pickup in weapons.Values)
            {
                var body = pickup.body;
                if (body.position.y < -1.5f)
                {
                    body.position = pickup.safePosition;
                    if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
                }
                bool paused = !DodgeApp.Instance.SimulationRunning;
                if (body.isKinematic != paused) body.isKinematic = paused;
            }
        }
        void WriteWeapons(BinaryWriter writer)
        {
            writer.Write((ushort)weapons.Count);
            foreach (var p in weapons.Values)
            {
                writer.Write(p.id); writer.Write((byte)p.kind); writer.Write((byte)p.ammo);
                DodgeNet.WriteVector(writer, p.body.position); DodgeNet.WriteRotation(writer, p.body.rotation);
            }
        }
        void ReadWeapons(BinaryReader reader)
        {
            int count = reader.ReadUInt16();
            if (count > 128) throw new InvalidDataException("Too many loose weapons");
            seenWeapons.Clear();
            for (int i = 0; i < count; i++)
            {
                int id = reader.ReadInt32();
                var kind = (DodgeWeapon)reader.ReadByte();
                int ammo = reader.ReadByte();
                Vector3 p = DodgeNet.ReadVector(reader);
                Quaternion q = DodgeNet.ReadRotation(reader);
                if (!weapons.TryGetValue(id, out var pickup)) pickup = CreateWeapon(id, kind, ammo, p, q, true);
                pickup.view.Target(p,q); seenWeapons.Add(id);
            }
            removedWeapons.Clear();
            foreach (var id in weapons.Keys) if (!seenWeapons.Contains(id)) removedWeapons.Add(id);
            foreach (var id in removedWeapons) RemoveWeapon(id);
        }
    }
}
