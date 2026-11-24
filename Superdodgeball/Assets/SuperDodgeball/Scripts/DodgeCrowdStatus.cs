using UnityEngine;

namespace SuperDodgeball
{
    // Egg interference is separate from damage, equipment and ragdoll recovery.
    public sealed class DodgeCrowdStatus : MonoBehaviour
    {
        public const float EggDuration = 3.6f;
        public float EggRemaining { get; private set; }
        public float SoapRemaining { get; private set; }
        public bool Obscured => EggRemaining > 0f;
        public float Opacity => Mathf.Clamp01(EggRemaining / 0.9f);
        public float ReactionPenalty => Obscured ? 0.2f : 0f;
        public float AimMultiplier => Obscured ? 2.5f : 1f;
        DodgeActor actor;
        Transform smear;
        float bubbleClock;

        public bool ApplyEgg()
        {
            if (SoapRemaining > 0f || EggRemaining > 0f) return false;
            EggRemaining = EggDuration;
            return true;
        }

        public void Wash()
        {
            EggRemaining = 0f;
            SoapRemaining = 8f;
            if (DodgeGame.Instance) DodgeGame.Instance.EmitFoam(transform.position + Vector3.up, Vector3.up, 16, 1.3f);
        }
        public void ApplyReplica(float egg, float soap) { EggRemaining = egg; SoapRemaining = soap; }

        public void ResetStatus()
        {
            EggRemaining = SoapRemaining = bubbleClock = 0f;
            if (smear) smear.gameObject.SetActive(false);
        }

        void Update()
        {
            var game = DodgeGame.Instance;
            if (DodgeGame.IsReplica) return;
            if (!game || !DodgeGame.SimulationActive || !game.RoundStarted || game.RoundOver) return;
            EggRemaining = Mathf.Max(0f, EggRemaining - Time.deltaTime);
            SoapRemaining = Mathf.Max(0f, SoapRemaining - Time.deltaTime);
            if (SoapRemaining > 0f && (bubbleClock -= Time.deltaTime) <= 0f)
            {
                bubbleClock = 0.55f;
                game.EmitFoam(transform.position + Vector3.up * 1.4f, Vector3.up * 0.3f, 1, 0.45f);
            }
        }

        void LateUpdate()
        {
            if (!actor) actor = GetComponent<DodgeActor>();
            if (!actor || !actor.Puppet || !actor.Puppet.Ready) return;
            if (!smear && Obscured)
            {
                smear = new GameObject("Egg on face").transform;
                smear.SetParent(actor.Puppet.HeadSocket, false);
                DodgeCrowd.Part(smear, PrimitiveType.Sphere, "Egg white", new Vector3(0f, -0.32f, 0.235f), new Vector3(0.42f, 0.3f, 0.065f), new Color(0.96f, 0.94f, 0.66f));
                DodgeCrowd.Part(smear, PrimitiveType.Sphere, "Yolk", new Vector3(0.05f, -0.34f, 0.275f), new Vector3(0.18f, 0.16f, 0.05f), new Color(1f, 0.67f, 0.12f));
            }
            if (smear) smear.gameObject.SetActive(Obscured);
        }
    }
}
