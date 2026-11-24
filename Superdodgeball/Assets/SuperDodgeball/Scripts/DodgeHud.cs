using UnityEngine;

namespace SuperDodgeball
{
    // Only the reticle and temporary egg obstruction are drawn over the world.
    public sealed class DodgeHud : MonoBehaviour
    {
        public DodgeGame game;
        Texture2D eggSplat;
        static readonly Vector4[] SplatBlobs = { new Vector4(.26f,.55f,.19f,.27f), new Vector4(.15f,.43f,.13f,.16f),
            new Vector4(.34f,.7f,.12f,.16f), new Vector4(.72f,.76f,.15f,.17f),
            new Vector4(.83f,.61f,.1f,.12f), new Vector4(.1f,.8f,.035f,.05f),
            new Vector4(.43f,.82f,.025f,.035f), new Vector4(.63f,.63f,.035f,.05f),
            new Vector4(.23f,.29f,.026f,.18f), new Vector4(.31f,.37f,.018f,.15f),
            new Vector4(.74f,.55f,.02f,.17f) };
        void Start() => BuildSplat();

        void OnGUI()
        {
            if (!game) return;
            if (DodgeApp.Active && !DodgeApp.Instance.ShowReticle) return;
            float x = Screen.width * 0.5f, y = Screen.height * 0.5f;
            float scale = Mathf.Max(0.8f, Screen.height / 900f);
            Color old = GUI.color;
            var status = game.player.GetComponent<DodgeActor>().CrowdStatus;
            if (status.Obscured)
            {
                if (!eggSplat) BuildSplat();
                GUI.color = new Color(1f, 1f, 1f, status.Opacity);
                float slide = (1f - status.EggRemaining / DodgeCrowdStatus.EggDuration) * Screen.height * 0.035f;
                GUI.DrawTexture(new Rect(0f, slide, Screen.width, Screen.height), eggSplat);
            }
            GUI.color = new Color(0.13f, 0.2f, 0.23f, 0.55f);
            Mark(x, y, scale, 3f);
            GUI.color = game.view.AimedAtTarget ? new Color(0.75f, 1f, 0.83f) : new Color(1f, 0.98f, 0.9f, 0.95f);
            Mark(x, y, scale, 1.5f);
            GUI.color = old;
        }

        void BuildSplat()
        {
            const int width = 512, height = 288;
            eggSplat = new Texture2D(width, height, TextureFormat.RGBA32, false);
            eggSplat.name = "Temporary cartoon egg splatter";
            eggSplat.wrapMode = TextureWrapMode.Clamp;
            // Elliptical white, offset yolk, small splash flecks and hanging drips.
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = SplatColor(new Vector2(x / (float)width, y / (float)height));
            eggSplat.SetPixels(pixels); eggSplat.Apply(false, true);
        }

        public static Color SplatColor(Vector2 uv)
        {
            float d = 9f;
            foreach (var b in SplatBlobs)
                d = Mathf.Min(d, new Vector2((uv.x - b.x) / b.z, (uv.y - b.y) / b.w).magnitude);
            float edge = 1f + (Mathf.PerlinNoise(uv.x * 24f, uv.y * 18f) - .5f) * .13f;
            float alpha = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - .045f, edge, d))) * .86f;
            float yolk = new Vector2((uv.x - .265f) / .085f, (uv.y - .55f) / .13f).magnitude;
            Color color = Color.Lerp(new Color(1f,.94f,.7f), new Color(1f,.65f,.1f),
                1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.88f, 1f, yolk)));
            color.a = alpha;
            return color;
        }

        void OnDestroy() { if (eggSplat) Destroy(eggSplat); }

        static void Mark(float x, float y, float scale, float width)
        {
            GUI.DrawTexture(new Rect(x - 9f * scale, y - width * scale * 0.5f, 5f * scale, width * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + 4f * scale, y - width * scale * 0.5f, 5f * scale, width * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - width * scale * 0.5f, y - 9f * scale, width * scale, 5f * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - width * scale * 0.5f, y + 4f * scale, width * scale, 5f * scale), Texture2D.whiteTexture);
        }
    }
}
