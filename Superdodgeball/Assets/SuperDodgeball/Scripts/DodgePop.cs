using UnityEngine;

namespace SuperDodgeball
{
    public sealed class DodgePop : MonoBehaviour
    {
        public Vector3 velocity;
        public float lifetime = 0.35f;
        float age;
        Vector3 initialScale;
        void Start() => initialScale = transform.localScale;
        void Update()
        {
            age += Time.deltaTime;
            velocity += Vector3.down * (8f * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;
            transform.localScale = initialScale * Mathf.Max(0f, 1f - age / lifetime);
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
