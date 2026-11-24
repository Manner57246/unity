using UnityEngine;

namespace SuperDodgeball
{
    // Curve skinning rebuilds rings in a transported frame. Unlike a single weighted
    // waist vertex, the inner bend retains a circular cross section at extreme angles.
    public sealed class DodgeTube : MonoBehaviour
    {
        const int Rings = 65, Sides = 20;
        Mesh mesh;
        readonly Vector3[] vertices = new Vector3[Rings * (Sides + 1)];
        readonly Vector2[] uv = new Vector2[Rings * (Sides + 1)];
        Vector3[] points;
        float[] radii;
        public Renderer Surface { get; private set; }
        public Mesh Mesh => mesh;

        public void Initialize(Material material)
        {
            mesh = new Mesh { name = "Continuous elastic surface" };
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            Surface = gameObject.AddComponent<MeshRenderer>();
            Surface.sharedMaterial = material;
            int[] triangles = new int[(Rings - 1) * Sides * 6];
            int index = 0;
            for (int r = 0; r < Rings; r++)
            for (int s = 0; s <= Sides; s++)
            {
                uv[r * (Sides + 1) + s] = new Vector2(s / (float)Sides, r / (float)(Rings - 1));
                if (r == Rings - 1 || s == Sides) continue;
                int a = r * (Sides + 1) + s, b = a + Sides + 1;
                triangles[index++] = a; triangles[index++] = b; triangles[index++] = a + 1;
                triangles[index++] = a + 1; triangles[index++] = b; triangles[index++] = b + 1;
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

        public void Draw(Vector3[] path, float[] thickness, Vector3 forward, float depth = 0.86f, float from = 0f, float to = 1f, float extra = 0f)
        {
            points = path; radii = thickness;
            for (int r = 0; r < Rings; r++)
            {
                float u = r / (float)(Rings - 1);
                float t = Mathf.Lerp(from, to, u);
                Vector3 center = Sample(t);
                Vector3 tangent = (Sample(Mathf.Min(1f, t + 0.002f)) - Sample(Mathf.Max(0f, t - 0.002f))).normalized;
                if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.up;
                Vector3 right = Vector3.Cross(tangent, forward).normalized;
                if (right.sqrMagnitude < 0.01f) right = Vector3.Cross(tangent, Vector3.right).normalized;
                Vector3 normal = Vector3.Cross(right, tangent).normalized;
                float x = t * (path.Length - 1);
                int k = Mathf.Min(Mathf.FloorToInt(x), path.Length - 2);
                float radius = Mathf.Lerp(radii[k], radii[k + 1], x - k) + extra;
                // Rounded sealed ends for body; clothing is left open at its hems.
                if (from == 0f && u < 0.045f) radius *= Mathf.Sin(u / 0.045f * Mathf.PI * 0.5f);
                if (to == 1f && u > 0.955f) radius *= Mathf.Sin((1f - u) / 0.045f * Mathf.PI * 0.5f);
                for (int s = 0; s <= Sides; s++)
                {
                    float angle = s * Mathf.PI * 2f / Sides;
                    vertices[r * (Sides + 1) + s] = transform.InverseTransformPoint(center + radius * (right * Mathf.Cos(angle) + normal * (Mathf.Sin(angle) * depth)));
                }
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        Vector3 Sample(float t)
        {
            float x = Mathf.Clamp01(t) * (points.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(x), points.Length - 2);
            float u = x - i;
            Vector3 a = points[i], b = points[i + 1];
            Vector3 ma = i > 0 ? (b - points[i - 1]) * 0.5f : b - a;
            Vector3 mb = i < points.Length - 2 ? (points[i + 2] - a) * 0.5f : b - a;
            return (2f*u*u*u - 3f*u*u + 1f)*a + (u*u*u - 2f*u*u + u)*ma
                + (-2f*u*u*u + 3f*u*u)*b + (u*u*u - u*u)*mb;
        }

        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
