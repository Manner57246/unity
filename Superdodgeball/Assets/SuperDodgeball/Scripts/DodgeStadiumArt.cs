using UnityEngine;
using UnityEngine.Rendering;

namespace SuperDodgeball
{
    public sealed partial class DodgeStadium
    {
        static Color CourtPaint(Paint paint)
        {
            switch (paint.renderer.name)
            {
                case "Indoor side wall": return new Color(.68f, .65f, .55f);
                case "Indoor end wall upper": return new Color(.32f, .42f, .41f);
                case "End wall beside doorway": return new Color(.22f, .40f, .41f);
                case "Team colour lower wall": return new Color(.11f, .28f, .31f);
                case "Indoor pilaster": return new Color(.47f, .37f, .28f);
                case "Indoor roof": return new Color(.42f, .56f, .56f);
                case "Gym concourse floor": return new Color(.29f, .43f, .43f);
                case "Side bumper": return new Color(.15f, .32f, .34f);
                default: return paint.color;
            }
        }

        void DressCourt()
        {
            var maple = Resources.Load<Material>("Court/MapleCourt");
            foreach (var paint in paints)
            {
                var renderer = paint.renderer;
                if (!renderer) continue;
                // The indoor key represents the light fixtures below the roof.
                // Keep the enclosing walls and suspended fixtures out of its shadow map.
                if (renderer.transform.IsChildOf(transform))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (maple && (renderer.name == "Wood floor seam" || renderer.name == "Court panel"))
                    renderer.enabled = false;
            }
            app.Clubhouse.BuildCourtside(transform);
            if (maple)
                Print("Painted maple surface", new Vector3(0f, .009f, 0f), Quaternion.Euler(90f, 0f, 0f),
                    new Vector2(2f * DodgeCourt.HalfWidth - .02f, 2f * DodgeCourt.HalfDepth - .02f), maple);
            HangCollegeBanners(surface);
        }

        void HangCollegeBanners(Material canvas)
        {
            for (int team = 0; team < 2; team++)
            {
                var image = Resources.Load<Texture2D>(team == 0 ? "Court/CapeAthletics" : "Court/RidgeAthletics");
                if (!image) continue;
                var ink = new MaterialPropertyBlock();
                ink.SetTexture("_BaseMap", image);
                ink.SetColor("_BaseColor", Color.white);
                ink.SetFloat("_Smoothness", .14f);
                float z = team == 0 ? -7f : 7f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var center = new Vector3(side * 21.90f, 6.84f, z);
                    var facing = Quaternion.Euler(0f, side * 90f, 0f);
                    var print = Print(DodgeColleges.Name(team) + " · athletics banner", center, facing, new Vector2(5.95f, 2.975f), canvas);
                    print.SetPropertyBlock(ink);
                    var rail = Part("Banner hanging rail", transform, center + Vector3.up * 1.52f,
                        new Vector3(.095f, .11f, 6.22f), new Color(.45f, .33f, .21f));
                    rail.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        Renderer Print(string label, Vector3 position, Quaternion rotation, Vector2 size, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = label;
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = position;
            quad.transform.localRotation = rotation;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            var collider = quad.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return renderer;
        }
    }
}
