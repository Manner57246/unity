using System.IO;
using UnityEngine;

namespace SuperDodgeball
{
    [DefaultExecutionOrder(160)]
    public sealed class DodgeZeus : MonoBehaviour
    {
        public const float Duration = 25f, ChargeSeconds = 1.8f, MaxHeight = 3.15f;
        public float Remaining { get; private set; }
        public float ShockLeft { get; private set; }
        public bool Transformed => Remaining > 0;
        public bool Hovering { get; private set; }
        DodgeActor actor;
        Transform hair, robe;
        DodgeStorm aura, shock;
        bool dressed;
        void Awake() => actor = GetComponent<DodgeActor>();
        internal void TransformForMatch()
        {
            Remaining = Duration;

        }
        internal void Electrify(float seconds) => ShockLeft = Mathf.Max(ShockLeft,seconds);
        internal void ResetPower()
        {
            Remaining = ShockLeft = 0;
            Hovering = false;
            Dress(false);
            if (shock) shock.gameObject.SetActive(false);
        }

        internal void Hover(ref float verticalSpeed, bool aiming, float power)
        {
            Hovering = Transformed && aiming;
            if (!Hovering) return;
            float height = .055f + MaxHeight * Mathf.SmoothStep(0,1,power);
            verticalSpeed = Mathf.Clamp((height-transform.position.y)*9f, -6f, 4.6f);
        }

        internal void Write(BinaryWriter w) { w.Write(Remaining); w.Write(ShockLeft); }
        internal void Read(BinaryReader r) { Remaining = r.ReadSingle(); ShockLeft = r.ReadSingle(); }

        void Update()
        {
            if (!DodgeApp.Active || !DodgeApp.Instance.MatchRunning)
            {
                if (Transformed || ShockLeft > 0 || dressed) ResetPower();
                return;
            }
            if (!DodgeGame.IsReplica && DodgeGame.SimulationActive)
            {
                Remaining = Mathf.Max(0,Remaining-Time.deltaTime);
                ShockLeft = Mathf.Max(0,ShockLeft-Time.deltaTime);
                if (!Transformed || actor.IsStunned || DodgeReferee.Restricted(actor)) Hovering=false;
            }
        }

        void Dress(bool visible)
        {
            if (visible == dressed || !actor || !actor.Puppet || !actor.Puppet.Ready || !actor.Puppet.HeadSocket) return;
            if (visible && !hair)
            {
                hair = DodgeWeaponArt.Model(actor.Puppet.HeadSocket.parent,DodgeWeaponArt.Prop.Laurel);
                robe = DodgeWeaponArt.Model(transform,DodgeWeaponArt.Prop.Toga);
                aura = DodgeStorm.Around(transform,false);
            }
            if (visible) DodgeGame.Instance.PlayTone(530,.16f,.09f);
            dressed = visible;
            hair.gameObject.SetActive(visible); robe.gameObject.SetActive(visible);
            aura.gameObject.SetActive(visible);
            if(visible)aura.Arrival(transform.position);
            var wardrobe=actor.Puppet.Wardrobe;
            if (wardrobe) { wardrobe.Olympian=visible; wardrobe.SetStyle(wardrobe.Style); }
        }

        void LateUpdate()
        {
            if (!actor.Puppet || !actor.Puppet.Ready) return;
            Dress(Transformed);
            if (dressed)
            {
                var path=actor.Puppet.BodyPath;
                if (path!=null)
                {
                    var top=path[path.Length-1]; var bottom=path[0];
                    var up=(top-bottom).normalized;
                    var forward=Vector3.ProjectOnPlane(transform.forward,up);
                    if (forward.sqrMagnitude>.001f) robe.rotation=Quaternion.LookRotation(forward,up);
                    robe.position=Vector3.Lerp(bottom,top,.48f);
                    robe.localScale=new Vector3(1+.22f*actor.Kit.MuscleBlend,Mathf.Max(.48f,Vector3.Distance(top,bottom)/.9f),1+.15f*actor.Kit.MuscleBlend);
                }
                aura.PoseCharge(actor,actor.Windup);
            }
            if (ShockLeft>0 && !shock) shock=DodgeStorm.Around(transform,true);
            if (shock)
            {
                shock.gameObject.SetActive(ShockLeft>0);
                if (ShockLeft>0) shock.PoseShock(actor.Puppet.BodyCenter,ShockLeft);
            }
        }
        void OnDisable(){ if (dressed) Dress(false); }
    }
}
