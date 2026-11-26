using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SuperDodgeball.Editor
{
    public static class DodgeHitCloudBaker
    {
        // The field has isotropic voxels. X is the reading direction and -Z is the front.
        const int Width = 144, Height = 68, Depth = 44;
        const float Cell = .022f;
        const string AssetPath = "Assets/SuperDodgeball/Resources/Court/HitCloudVolume.asset";

        readonly struct Puff
        {
            public readonly Vector3 center, radius;
            public Puff(float x, float y, float z, float rx, float ry, float rz)
            {
                center = new Vector3(x, y, z);
                radius = new Vector3(rx, ry, rz);
            }
        }

        static readonly Puff[] Puffs = {
            // H. The front lobes overlap off-axis so the face and flank have distinct silhouettes.
            new(-1.14f,-.32f,.03f,.175f,.19f,.175f), new(-1.075f,-.11f,-.035f,.145f,.17f,.20f),
            new(-1.065f,.13f,.035f,.14f,.175f,.17f), new(-.99f,.34f,-.03f,.18f,.18f,.20f),
            new(-.86f,-.01f,.015f,.22f,.135f,.17f), new(-.83f,.025f,-.11f,.13f,.11f,.13f),
            new(-.64f,-.33f,.01f,.17f,.18f,.18f), new(-.57f,-.12f,-.05f,.145f,.18f,.19f),
            new(-.56f,.13f,.02f,.145f,.19f,.17f), new(-.49f,.36f,-.025f,.17f,.17f,.18f),
            new(-1.17f,-.27f,-.145f,.12f,.13f,.12f), new(-.61f,.16f,-.145f,.12f,.12f,.13f),
            new(-1.07f,.33f,-.16f,.13f,.10f,.105f), new(-.66f,-.29f,-.155f,.115f,.105f,.115f),
            // I. A short top and foot make it read from a three-quarter view.
            new(-.10f,-.34f,.015f,.205f,.148f,.17f), new(-.07f,-.17f,-.02f,.14f,.21f,.20f),
            new(-.04f,.06f,.025f,.145f,.21f,.18f), new(.005f,.27f,-.035f,.15f,.20f,.20f),
            new(.035f,.40f,.015f,.195f,.14f,.15f), new(-.13f,-.09f,-.17f,.115f,.12f,.12f),
            new(-.04f,.31f,-.165f,.12f,.125f,.10f),
            // T. The top bar is three banks of vapour rather than a single rounded bar.
            new(.47f,.38f,.04f,.19f,.15f,.15f), new(.69f,.43f,-.02f,.21f,.155f,.195f),
            new(.91f,.455f,.03f,.19f,.16f,.18f), new(1.10f,.47f,-.015f,.17f,.15f,.155f),
            new(.80f,.25f,-.06f,.16f,.20f,.20f), new(.77f,.055f,.025f,.15f,.21f,.19f),
            new(.73f,-.14f,-.045f,.155f,.21f,.20f), new(.69f,-.31f,.025f,.16f,.19f,.17f),
            new(.89f,.46f,-.165f,.14f,.12f,.125f), new(.70f,-.27f,-.16f,.12f,.11f,.13f),
            new(.54f,.415f,-.11f,.115f,.10f,.105f)
        };

        [MenuItem("Super Dodgeball/Art/Bake HIT cloud volume")]
        public static void Bake()
        {
            int count = Width * Height * Depth;
            var distance = new float[count];
            System.Array.Fill(distance, 1f);
            Vector3 half = new Vector3(Width, Height, Depth) * (.5f * Cell);
            foreach (var puff in Puffs)
            {
                var pad = Vector3.one * .13f;
                var lo = Vector3Int.FloorToInt((puff.center - puff.radius - pad + half) / Cell);
                var hi = Vector3Int.CeilToInt((puff.center + puff.radius + pad + half) / Cell);
                float r = Mathf.Min(puff.radius.x, Mathf.Min(puff.radius.y, puff.radius.z));
                for (int z = Mathf.Max(0,lo.z); z <= Mathf.Min(Depth-1,hi.z); z++)
                for (int y = Mathf.Max(0,lo.y); y <= Mathf.Min(Height-1,hi.y); y++)
                for (int x = Mathf.Max(0,lo.x); x <= Mathf.Min(Width-1,hi.x); x++)
                {
                    Vector3 p = new Vector3(x+.5f,y+.5f,z+.5f)*Cell - half - puff.center;
                    float d = (new Vector3(p.x/puff.radius.x,p.y/puff.radius.y,p.z/puff.radius.z).magnitude-1f)*r;
                    int i = x + Width*(y + Height*z);
                    float blend = Mathf.Max(.045f - Mathf.Abs(distance[i]-d), 0f);
                    distance[i] = Mathf.Min(distance[i],d) - blend*blend/(4f*.045f);
                }
            }

            var voxels = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                int x = i % Width, y = i / Width % Height, z = i / (Width * Height);
                float grain = Mathf.Sin(.17f*x+.11f*z)*Mathf.Cos(.21f*y-.07f*z)
                            + .48f*Mathf.Sin(.31f*z+.12f*y-.18f*x);
                float density = Mathf.SmoothStep(0,1, (.057f-distance[i]+.011f*grain)/.095f);
                voxels[i] = new Color32(Byte(density),0,Byte(.5f+.32f*grain),255);
            }
            // Bake light transmittance towards the upper-left/front key through the same density field.
            Vector3 key = new Vector3(-1f,1.4f,-.5f).normalized;
            for (int z = 0; z < Depth; z++)
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int index = x + Width*(y + Height*z);
                if (voxels[index].r == 0) continue;
                float opticalDepth = 0f;
                for (int s = 1; s <= 26; s++)
                {
                    int lx=x-s, ly=y+Mathf.RoundToInt(1.4f*s), lz=z-Mathf.RoundToInt(.5f*s);
                    if (lx<0 || ly>=Height || lz<0) break;
                    opticalDepth += voxels[lx+Width*(ly+Height*lz)].r / 255f;
                }
                voxels[index].g = Byte(Mathf.Exp(-opticalDepth*.25f));
                // Density vanishes before the texture boundary, so occupied cells have all six neighbours.
                Vector3 outward = new Vector3(distance[index+1]-distance[index-1],
                    distance[index+Width]-distance[index-Width],
                    distance[index+Width*Height]-distance[index-Width*Height]).normalized;
                voxels[index].a = Byte(.5f+.5f*Vector3.Dot(outward,key));
            }
            var volume = AssetDatabase.LoadAssetAtPath<Texture3D>(AssetPath);
            if (!volume)
            {
                volume = new Texture3D(Width,Height,Depth,GraphicsFormat.R8G8B8A8_UNorm,TextureCreationFlags.None);
                AssetDatabase.CreateAsset(volume,AssetPath);
            }
            volume.name = "HIT cloud density, light and billows";
            volume.wrapMode = TextureWrapMode.Clamp;
            volume.filterMode = FilterMode.Bilinear;
            volume.SetPixels32(voxels);
            volume.Apply(false);
            EditorUtility.SetDirty(volume);
            AssetDatabase.SaveAssets();
        }

        static byte Byte(float value) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);
    }
}
