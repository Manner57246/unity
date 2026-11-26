using UnityEngine;

namespace SuperDodgeball
{
    [DefaultExecutionOrder(220)]
    public sealed partial class DodgeHud : MonoBehaviour
    {
        public DodgeGame game;
        Texture2D eggSplat;
        Texture2D coughBubble;
        GUIStyle olympianTime, kitStyle,venueStyle;
        readonly GUIContent kitText = new GUIContent();
        Camera matchCamera;
        static readonly Vector4[] SplatBlobs = { new Vector4(.26f,.55f,.19f,.27f), new Vector4(.15f,.43f,.13f,.16f),
            new Vector4(.34f,.7f,.12f,.16f), new Vector4(.72f,.76f,.15f,.17f),
            new Vector4(.83f,.61f,.1f,.12f), new Vector4(.1f,.8f,.035f,.05f),
            new Vector4(.43f,.82f,.025f,.035f), new Vector4(.63f,.63f,.035f,.05f),
            new Vector4(.23f,.29f,.026f,.18f), new Vector4(.31f,.37f,.018f,.15f),
            new Vector4(.74f,.55f,.02f,.17f) };
        void Start()
        {
            BuildSplat();
            coughBubble = Resources.Load<Texture2D>("Court/CoughBubble");
            matchCamera = game.view.GetComponent<Camera>();
            PrepareHitClouds();
        }

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
            if (Event.current.type == EventType.Repaint)
            {
                DrawCough();
                if (DodgeApp.Active && DodgeApp.Instance.MatchRunning)
                {
                    var venue=DodgeApp.Instance.Venue;
                    if(venue && venue.Cue!=null)DrawVenueCue(venue);
                    var local=DodgeApp.Instance.Actors[DodgeApp.Instance.LocalSlot];
                    if (local.Zeus.Transformed) DrawOlympianTime(local.Zeus.Remaining);
                    else if (!string.IsNullOrEmpty(local.Kit.CourtCaption))
                        DrawKitCard(local.Kit.CourtCaption, DodgeApp.Instance.Room.seats[DodgeApp.Instance.LocalSlot].team);
                }
            }
        }

        void DrawVenueCue(DodgeVenueDirector venue)
        {
            venueStyle??=new GUIStyle(GUI.skin.label){font=DodgeApp.CreateUiFont(20),alignment=TextAnchor.MiddleCenter};
            float unit=Mathf.Min(Screen.height/720f,Screen.width/960f);
            venueStyle.fontSize=Mathf.Max(12,Mathf.RoundToInt(19*unit));
            float width=270f*unit;var safe=Screen.safeArea;
            var box=new Rect(safe.center.x-width*.5f,Screen.height-safe.yMax+16f*unit,width,35f*unit);
            var old=GUI.color;GUI.color=new Color(.12f,.17f,.19f,.9f);GUI.DrawTexture(box,Texture2D.whiteTexture);
            GUI.color=venue.Displayed==DodgeVenue.Warehouse?new Color(1,.72f,.31f):new Color(.60f,.94f,.92f);
            GUI.DrawTexture(new Rect(box.x,box.y,4*unit,box.height),Texture2D.whiteTexture);
            GUI.color=Color.white;GUI.Label(box,venue.Cue,venueStyle);GUI.color=old;
        }

        void DrawKitCard(string caption, int team)
        {
            kitStyle ??= new GUIStyle(GUI.skin.label) {
                font = DodgeApp.CreateUiFont(16), alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0,0,0,0), wordWrap = true
            };
            float unit = Mathf.Min(Screen.height/720f, Screen.width/960f);
            var safe = Screen.safeArea;
            kitStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(16f*unit));
            kitText.text = caption;
            float width = Mathf.Min(safe.width*.61f, kitStyle.CalcSize(kitText).x + 24f*unit);
            float height = kitStyle.CalcHeight(kitText, width - 24f*unit) + 12f*unit;
            var box = new Rect(safe.xMin + 22f*unit, Screen.height-safe.yMin-22f*unit-height, width, height);
            var old = GUI.color;
            GUI.color = new Color(.09f,.14f,.17f,.9f);
            GUI.DrawTexture(box,Texture2D.whiteTexture);
            GUI.color = team==0 ? new Color32(122,198,178,255) : new Color32(232,147,106,255);
            GUI.DrawTexture(new Rect(box.x,box.y,3f*unit,box.height),Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(box.x+12f*unit,box.y,box.width-24f*unit,box.height),kitText,kitStyle);
            GUI.color = old;
        }

        void DrawOlympianTime(float seconds)
        {
            olympianTime ??= new GUIStyle(GUI.skin.label) {font=DodgeApp.CreateUiFont(18),alignment=TextAnchor.MiddleCenter};
            float unit=Mathf.Min(Screen.height/720f,Screen.width/960f);
            var safe=Screen.safeArea;
            var box=new Rect(safe.xMax-126*unit,Screen.height-safe.yMin-52*unit,104*unit,30*unit);
            var old=GUI.color;
            GUI.color=new Color(.12f,.18f,.23f,.88f);GUI.DrawTexture(box,Texture2D.whiteTexture);
            GUI.color=DodgeStorm.Gold;
            GUI.DrawTexture(new Rect(box.x,box.yMax-2*unit,box.width*seconds/DodgeZeus.Duration,2*unit),Texture2D.whiteTexture);
            olympianTime.fontSize=Mathf.RoundToInt(17*unit);
            GUI.color=Color.white;GUI.Label(box,"宙斯  "+Mathf.CeilToInt(seconds)+" 秒",olympianTime);
            GUI.color=old;
        }

        void DrawCough()
        {
            if (!coughBubble || !DodgeApp.Active) return;
            var referee = DodgeApp.Instance.Referee;
            if (!referee || !referee.TryGetCoughCue(matchCamera, out var direction, out int beat, out float age)) return;

            Rect view = matchCamera.pixelRect;
            Rect safe = Screen.safeArea;
            Rect bounds = Rect.MinMaxRect(Mathf.Max(view.xMin, safe.xMin), Screen.height - Mathf.Min(view.yMax, safe.yMax),
                Mathf.Min(view.xMax, safe.xMax), Screen.height - Mathf.Max(view.yMin, safe.yMin));
            if (bounds.width <= 0 || bounds.height <= 0) return;
            float unit = Mathf.Min(bounds.height / 720f, bounds.width / 960f);
            float pop = age < .095f ? Mathf.Lerp(.91f, 1.12f, age / .095f)
                                    : Mathf.SmoothStep(1.12f, 1f, (age - .095f) / .14f);
            Vector2 size = new Vector2(132, 76) * (unit * pop);
            Vector2 origin = new Vector2(view.center.x, Screen.height - view.center.y);
            float pad = 22f * unit;
            Rect inset = Rect.MinMaxRect(bounds.xMin + size.x / 2 + pad, bounds.yMin + size.y / 2 + pad,
                                         bounds.xMax - size.x / 2 - pad, bounds.yMax - size.y / 2 - pad);
            origin = new Vector2(Mathf.Clamp(origin.x, inset.xMin, inset.xMax), Mathf.Clamp(origin.y, inset.yMin, inset.yMax));
            float tx = Mathf.Abs(direction.x) < .0001f ? float.PositiveInfinity :
                ((direction.x > 0 ? inset.xMax : inset.xMin) - origin.x) / direction.x;
            float ty = Mathf.Abs(direction.y) < .0001f ? float.PositiveInfinity :
                ((direction.y > 0 ? inset.yMax : inset.yMin) - origin.y) / direction.y;
            Vector2 center = origin + direction * Mathf.Min(tx, ty);

            Color color = GUI.color;
            var matrix = GUI.matrix;
            float fade = beat == 3 ? 1f - Mathf.SmoothStep(0, 1, (age - .40f) / .24f) : 1;
            GUI.color = new Color(1, 1, 1, fade);
            // Rotate just the tail. Lettering stays upright even when the referee is behind us.
            float radius = 1f / new Vector2(direction.x / (size.x * .42f), direction.y / (size.y * .39f)).magnitude;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, center);
            GUI.DrawTextureWithTexCoords(new Rect(center.x + radius - 12 * unit, center.y - 10 * unit, 34 * unit, 20 * unit),
                coughBubble, new Rect(.75f, 0, .25f, 1));
            GUI.matrix = matrix;
            GUI.DrawTextureWithTexCoords(new Rect(center - size / 2, size), coughBubble, new Rect((beat - 1) * .25f, 0, .25f, 1));
            GUI.color = color;
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

        void OnDestroy() { if (eggSplat) Destroy(eggSplat); if (cloudMaterial) Destroy(cloudMaterial); }

        static void Mark(float x, float y, float scale, float width)
        {
            GUI.DrawTexture(new Rect(x - 9f * scale, y - width * scale * 0.5f, 5f * scale, width * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + 4f * scale, y - width * scale * 0.5f, 5f * scale, width * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - width * scale * 0.5f, y - 9f * scale, width * scale, 5f * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - width * scale * 0.5f, y + 4f * scale, width * scale, 5f * scale), Texture2D.whiteTexture);
        }
    }
}
