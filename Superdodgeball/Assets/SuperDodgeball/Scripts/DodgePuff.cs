using UnityEngine;
using UnityEngine.Rendering;

namespace SuperDodgeball
{
    // Tiny, unlit-looking foam beads; no colliders or lights are created.
    public sealed class DodgePuff : MonoBehaviour
    {
        Vector3 velocity;
        float age;
        float lifetime;
        float size;

        public static void Emit(Vector3 position, Vector3 drift, Material material, int count = 3, float strength = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Soft footstep bubble";
                var collider = puff.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                puff.transform.position = position + new Vector3(Random.Range(-0.16f, 0.16f), 0.04f, Random.Range(-0.13f, 0.13f));
                var renderer = puff.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var animation = puff.AddComponent<DodgePuff>();
                animation.velocity = drift * 0.12f + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.3f, 0.7f), Random.Range(-0.35f, 0.35f));
                animation.size = Random.Range(0.13f, 0.24f) * strength;
                animation.lifetime = Random.Range(0.36f, 0.55f);
                puff.transform.localScale = Vector3.one * animation.size * 0.3f;
            }
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / lifetime;
            transform.position += velocity * Time.deltaTime;
            velocity *= Mathf.Exp(-2f * Time.deltaTime);
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            transform.localScale = Vector3.one * (size * envelope);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
