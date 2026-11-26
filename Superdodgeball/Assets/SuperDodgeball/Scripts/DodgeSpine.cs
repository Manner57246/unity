using UnityEngine;

namespace SuperDodgeball
{
    // Equal-parameter sections share one curve for pose targets, colliders and clothing.
    public sealed class DodgeSpine : MonoBehaviour
    {
        public const int Segments = 8;
        public Transform[] sections = new Transform[Segments];
        public Transform chest;
        public float LAmount { get; private set; }
        public float OAmount { get; private set; }
        public float Amount => Mathf.Max(LAmount, OAmount);
        public Vector3 RingCenter => transform.TransformPoint(new Vector3(0f, 1.38f, 0f));
        float muscle;
        public float Radius(float t) => transform.lossyScale.x * (1+muscle*Mathf.SmoothStep(.17f,.90f,t))*Mathf.Lerp(
            Mathf.Lerp(0.27f + Mathf.Sin(Mathf.PI * t) * 0.095f, 0.245f + Mathf.Sin(Mathf.PI * t) * 0.015f, LAmount),
            0.175f, OAmount);
        public float Length(int index) => Vector3.Distance(sections[index].position, index < Segments - 1 ? sections[index + 1].position : chest.position);

        public void Pose(DodgeActor actor, float l, float o)
        {
            if (!chest || sections.Length != Segments || !sections[0]) return;
            muscle=actor.Kit.MuscleBlend;
            LAmount = l;
            OAmount = o;
            Quaternion baseRotation = actor.upperBody.rotation;
            Vector3 basePosition = actor.upperBody.position;
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                // Grow the curl's angle progressively. Blending points on a straight
                // line with points on a ring would briefly collapse/flip upper links.
                float arc = Mathf.PI * (5f / 3f) * o;
                float startAngle = Mathf.PI * 0.5f * (1f - o);
                float length = Mathf.Lerp(0.86f, 0.66f * Mathf.PI * (5f / 3f), o * o);
                Vector3 curl = arc < 0.001f ? Vector3.up * (length * t)
                    : new Vector3(Mathf.Sin(startAngle + arc * t) - Mathf.Sin(startAngle),
                        Mathf.Cos(startAngle) - Mathf.Cos(startAngle + arc * t), 0f) * (length / arc);
                Quaternion curlRotation = Quaternion.Slerp(baseRotation, transform.rotation, o);
                Vector3 curlBase = Vector3.Lerp(basePosition, transform.TransformPoint(new Vector3(0f, 0.72f, 0f)), o);
                // A rounded right angle, with its bend distributed through the waist.
                float angle = Mathf.Min(t * 2f, 1f) * Mathf.PI * 0.5f;
                // Keep the inner bend wider than the body's cross section, avoiding
                // the self-intersection produced by folding a thick waist in place.
                const float lLength = 1.16f;
                float bendRadius = lLength / Mathf.PI;
                Vector3 lp = new Vector3(bendRadius * (1f - Mathf.Cos(angle)) + Mathf.Max(0f, t - 0.5f) * lLength,
                    0.72f + bendRadius * Mathf.Sin(angle), 0f);
                Vector3 position = Vector3.Lerp(curlBase + curlRotation * (curl * transform.lossyScale.x), transform.TransformPoint(lp), l * (1f - o));
                Quaternion lr = transform.rotation * Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                // Physical links follow the chord between samples. Using the analytic
                // tangent here would accumulate positional error around a closed bend.
                float tangentAngle = startAngle + arc * t - Mathf.PI * 0.5f;
                Quaternion or = curlRotation * Quaternion.Euler(0f, 0f, tangentAngle * Mathf.Rad2Deg);
                Transform node = i == Segments ? chest : sections[i];
                position.y=transform.position.y+(position.y-transform.position.y)*Mathf.Lerp(1f,.48f-.19f*actor.Kit.HeavySquash,actor.Kit.FlatAmount);
                node.SetPositionAndRotation(position, Quaternion.Slerp(or, lr, l * (1f - o)));
                node.localScale = Vector3.one;
            }
            for (int i = 0; i < Segments; i++)
            {
                Vector3 next = i == Segments - 1 ? chest.position : sections[i + 1].position;
                Vector3 direction = (next - sections[i].position).normalized;
                if (direction.sqrMagnitude > 0.01f) sections[i].rotation = Quaternion.FromToRotation(sections[i].up, direction) * sections[i].rotation;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = side < 0 ? actor.leftArm : actor.rightArm;
                Vector3 normal = chest.TransformPoint(new Vector3(side * (.37f+muscle*.18f), -0.18f, 0f));
                Vector3 folded = transform.TransformPoint(new Vector3(-0.57f + side * 0.17f, 0.85f, 0f));
                shoulder.position = Vector3.Lerp(normal, folded, o);
            }
        }
    }
}
