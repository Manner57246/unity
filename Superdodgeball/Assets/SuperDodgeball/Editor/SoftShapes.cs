using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public static class SoftShapes
    {
        const string Folder = "Assets/SuperDodgeball/Meshes";

        public static GameObject Box(string name, Vector3 dimensions, Material material, bool solid)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/SuperDodgeball", "Meshes");
            string id = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.###}_{1:0.###}_{2:0.###}", dimensions.x, dimensions.y, dimensions.z);
            string path = Folder + "/SoftBox_" + id + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = BuildMesh(dimensions);
                mesh.name = "Soft box " + id;
                AssetDatabase.CreateAsset(mesh, path);
            }
            var result = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            result.GetComponent<MeshFilter>().sharedMesh = mesh;
            result.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (solid)
            {
                result.AddComponent<BoxCollider>().size = dimensions;
                result.AddComponent<DodgeSurface>();
            }
            return result;
        }

        static Mesh BuildMesh(Vector3 size)
        {
            Vector3 half = size * 0.5f;
            float radius = Mathf.Min(0.22f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.32f);
            Vector3 coreHalf = half - Vector3.one * radius;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            var faceNormals = new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
            var faceRight = new[] { Vector3.right, Vector3.left, Vector3.back, Vector3.forward, Vector3.right, Vector3.right };
            var faceUp = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.back, Vector3.forward };
            for (int face = 0; face < 6; face++)
            {
                int first = vertices.Count;
                Vector3 n = faceNormals[face], u = faceRight[face], v = faceUp[face];
                float hx = Mathf.Abs(Vector3.Dot(half, u)), hy = Mathf.Abs(Vector3.Dot(half, v));
                float hn = Mathf.Abs(Vector3.Dot(half, n));
                for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                {
                    Vector3 point = n * hn + u * Coordinate(x, hx, radius) + v * Coordinate(y, hy, radius);
                    Vector3 core = new Vector3(Mathf.Clamp(point.x, -coreHalf.x, coreHalf.x), Mathf.Clamp(point.y, -coreHalf.y, coreHalf.y), Mathf.Clamp(point.z, -coreHalf.z, coreHalf.z));
                    Vector3 normal = (point - core).normalized;
                    vertices.Add(core + normal * radius);
                    normals.Add(normal);
                    uv.Add(new Vector2(x / 8f, y / 8f));
                    if (x < 8 && y < 8)
                    {
                        int i = first + y * 9 + x;
                        triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 10);
                        triangles.Add(i); triangles.Add(i + 10); triangles.Add(i + 9);
                    }
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static float Coordinate(int i, float half, float radius)
        {
            switch (i)
            {
                case 0: return -half;
                case 1: return -half + radius * 0.14f;
                case 2: return -half + radius * 0.47f;
                case 3: return -half + radius;
                case 4: return 0f;
                case 5: return half - radius;
                case 6: return half - radius * 0.47f;
                case 7: return half - radius * 0.14f;
                default: return half;
            }
        }
    }
}
