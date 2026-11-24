using UnityEngine;

namespace SuperDodgeball
{
    // The controller moves the real root through an arc; the puppet follows it.
    // Both local/remote players and bots share the same takeoff and landing rules.
    public struct DodgeDiveMotion
    {
        public const float Gravity = 22f;
        public const float LandingSeconds = .18f;
        public bool Active { get; private set; }
        public bool Airborne { get; private set; }
        public float Elapsed { get; private set; }
        float flightSeconds, landingElapsed;

        public void Begin(float duration, ref float verticalSpeed)
        {
            Active = Airborne = true;
            Elapsed = landingElapsed = 0f;
            flightSeconds = Mathf.Clamp(duration, .56f, .72f);
            verticalSpeed = Gravity * flightSeconds * .5f;
        }

        public void Step(float dt, ref float verticalSpeed)
        {
            if (!Active) return;
            Elapsed += dt;
            if (Airborne) verticalSpeed = Mathf.Max(-25f, verticalSpeed - Gravity * dt);
            else
            {
                verticalSpeed = -2f;
                landingElapsed += dt;
                if (landingElapsed >= LandingSeconds) Active = false;
            }
        }

        public float Speed => Airborne ? Mathf.Lerp(10.5f, 6f, Mathf.Clamp01(Elapsed / flightSeconds))
            : Mathf.Lerp(4f, 1.6f, Mathf.SmoothStep(0f, 1f, landingElapsed / LandingSeconds));

        public bool Resolve(CollisionFlags flags, ref float verticalSpeed)
        {
            if (!Active || !Airborne) return false;
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if (Elapsed < .08f || verticalSpeed > 0f || (flags & CollisionFlags.Below) == 0) return false;
            Airborne = false;
            landingElapsed = 0f;
            verticalSpeed = -2f;
            return true;
        }
    }
}
