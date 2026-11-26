using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SuperDodgeball
{
    [DefaultExecutionOrder(100)]
    public sealed class DodgeCamera : MonoBehaviour
    {
        public DodgePlayer player;
        public float sensitivity = 0.1f;
        public float Yaw { get; private set; }
        public float Pitch { get; private set; } = 11f;
        public Vector3 AimPoint { get; private set; }
        public bool AimedAtTarget { get; private set; }
        Camera lens;
        float zoom;
        float kick;
        Vector3 followAnchor;
        Vector3 followVelocity;
        bool anchorInitialized;
        float motionFov;
        float focusCharge, focusDistance = 15f;
        Volume focusVolume;
        VolumeProfile focusProfile;
        DepthOfField depthOfField;
        DodgeActor actor;
        public float FocusAmount => focusCharge;
        readonly RaycastHit[] aimHits = new RaycastHit[48];
        readonly RaycastHit[] obstructionHits = new RaycastHit[24];

        void Awake()
        {
            lens = GetComponent<Camera>();
            var data = GetComponent<UniversalAdditionalCameraData>();
            if (data) data.requiresDepthTexture = true;
            var focus = new GameObject("Charge background focus", typeof(Volume));
            focus.transform.SetParent(transform, false);
            focusVolume = focus.GetComponent<Volume>();
            focusVolume.isGlobal = true;
            focusVolume.priority = 20f;
            focusVolume.weight = 0f;
            focusProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            depthOfField = focusProfile.Add<DepthOfField>(true);
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianMaxRadius.Override(0.85f);
            depthOfField.highQualitySampling.Override(false);
            focusVolume.sharedProfile = focusProfile;
        }
        public void Kick(float value) => kick = Mathf.Max(kick, value);

        void LateUpdate()
        {
            if (!player) return;
            if (DodgeGame.InputCaptured && Mouse.current != null && !DodgeGame.Instance.CapturedThisFrame)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                Yaw += delta.x * sensitivity;
                Pitch = Mathf.Clamp(Pitch - delta.y * sensitivity, -32f, 65f);
            }
            UpdateView(Time.deltaTime);
        }

        public void UpdateView(float dt)
        {
            if (!lens) lens = GetComponent<Camera>();
            if (!actor || actor.gameObject != player.gameObject) actor = player.GetComponent<DodgeActor>();
            zoom = Mathf.Lerp(zoom, player.IsAiming ? 1f : 0f, 1f - Mathf.Exp(-12f * dt));
            focusCharge = Mathf.Lerp(focusCharge, player.IsAiming ? player.Charge : 0f, 1f - Mathf.Exp(-7f * dt));
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 anchor = player.transform.position + Vector3.up * 1.45f;
            if (actor.IsStunned && actor.Puppet && actor.Puppet.Ready) anchor = actor.Puppet.BodyCenter + Vector3.up * 0.4f;
            if (!anchorInitialized || dt >= 0.5f) { followAnchor = anchor; followVelocity = Vector3.zero; anchorInitialized = true; }
            else followAnchor = Vector3.SmoothDamp(followAnchor, anchor, ref followVelocity, 0.055f, float.PositiveInfinity, dt);
            anchor = followAnchor;
            Vector3 offset = new Vector3(Mathf.Lerp(0.8f, 1.02f, zoom), Mathf.Lerp(1.20f, 0.48f, zoom),
                -Mathf.Lerp(5.55f, 4.35f - focusCharge * 0.55f, zoom) - actor.ThrowPulse * 0.14f);
            Vector3 desired = anchor + rotation * offset;
            Vector3 cameraRay = desired - anchor;
            float distance = cameraRay.magnitude;
            int count = Physics.SphereCastNonAlloc(anchor, 0.22f, cameraRay.normalized, obstructionHits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (obstructionHits[i].collider.GetComponent<DodgeSurface>())
                    distance = Mathf.Min(distance, Mathf.Max(0.35f, obstructionHits[i].distance - 0.08f));
            transform.SetPositionAndRotation(anchor + cameraRay.normalized * distance, rotation);
            kick = Mathf.MoveTowards(kick, 0f, dt * 0.2f);
            float tension = Mathf.SmoothStep(0f, 1f, (focusCharge - 0.45f) / 0.55f) * 0.012f;
            transform.position += rotation * new Vector3(Mathf.Sin(Time.time * 37f), Mathf.Cos(Time.time * 43f), 0f) * (kick + tension);
            motionFov = Mathf.Lerp(motionFov, player.MovementAmount * 3f + (player.IsDiving ? 2f : 0f), 1f - Mathf.Exp(-5f * dt));
            lens.fieldOfView = Mathf.Lerp(66f + motionFov, 54f - focusCharge * 11f, zoom) + actor.ThrowPulse * 1.7f;
            RefreshAim();
            if (focusVolume)
            {
                // Volume enum overrides switch on for any nonzero weight. Disable the
                // volume explicitly at rest so an exponential tail cannot leave blur on.
                focusVolume.enabled = focusCharge > 0.05f;
                // Blur begins behind the aimed-at surface, keeping the target and the ball clear.
                focusDistance = Mathf.Lerp(focusDistance, Mathf.Clamp(Vector3.Distance(transform.position, AimPoint), 5f, 28f), 1f - Mathf.Exp(-6f * dt));
                depthOfField.gaussianStart.Override(focusDistance + 3f);
                depthOfField.gaussianEnd.Override(focusDistance + 18f);
                depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0.5f, 0.7f, focusCharge));
                focusVolume.weight = 1f;
            }
        }

        public void SetView(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, -32f, 65f);
            UpdateView(1f);
        }

        public void LookAtPoint(Vector3 point)
        {
            Vector3 anchor = player.transform.position + Vector3.up * 1.45f;
            Vector3 direction = (point - anchor).normalized;
            Yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            // Correct for the camera's shoulder offset using the actual camera location.
            for (int i = 0; i < 5; i++)
            {
                UpdateView(1f);
                direction = (point - transform.position).normalized;
                Yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                Pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            }
            UpdateView(1f);
        }

        public void RefreshAim()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            AimPoint = origin + direction * 75f;
            AimedAtTarget = false;
            float nearest = 75f;
            int count = Physics.RaycastNonAlloc(origin, direction, aimHits, nearest, ~0, QueryTriggerInteraction.Collide);
            DodgeActor self = player.GetComponent<DodgeActor>();
            for (int i = 0; i < count; i++)
            {
                var part = aimHits[i].collider.GetComponent<DodgeHitbox>();
                var drop = aimHits[i].collider.GetComponent<DodgeAirdrop>();
                var spectator=aimHits[i].collider.GetComponent<DodgeSpectatorHitbox>();
                if (part && part.actor == self) continue;
                if (!part && !drop && !spectator && !aimHits[i].collider.GetComponent<DodgeSurface>()) continue;
                if (aimHits[i].distance >= nearest) continue;
                nearest = aimHits[i].distance;
                AimPoint = aimHits[i].point;
                AimedAtTarget = part != null || drop != null || spectator != null;
            }
        }

        void OnDestroy()
        {
            if (focusProfile)
            {
                foreach (var component in focusProfile.components) if (component) Destroy(component);
                Destroy(focusProfile);
            }
        }
    }
}
