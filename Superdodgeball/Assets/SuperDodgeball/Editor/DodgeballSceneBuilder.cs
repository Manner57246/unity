using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SuperDodgeball.Editor
{
    public static class DodgeballSceneBuilder
    {
        const string Root = "Assets/SuperDodgeball";
        public const string ScenePath = Root + "/Scenes/SoftPlayground.unity";
        const string MaterialRoot = Root + "/Materials/Soft";
        static readonly Color Cyan = new Color(0.32f, 0.79f, 0.94f);
        static Material dark, white, court, purple, orange, yellow, cyan, pink, lilac, mint, metal, trail, ball;

        [MenuItem("Super Dodgeball/Build Soft Playground")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the playground.");
            var previous = EditorSceneManager.GetActiveScene();
            if (previous.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(MaterialRoot);
            EnsureFolder(Root + "/Scenes");
            SetupMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SoftPlayground";
            var arena = new GameObject("01 - Playground").transform;
            BuildCourt(arena);
            arena.localScale=new Vector3(DodgeCourt.Scale,1,DodgeCourt.Scale);
            BuildLighting();

            var player = CreateActor("Player - Blue Bean", new Vector3(0f, 0.05f, -6.5f), cyan, Cyan, true, false);
            var motor = player.gameObject.AddComponent<CharacterController>();
            motor.height = 1.9f;
            motor.radius = 0.29f;
            motor.center = new Vector3(0f, 0.95f, 0f);
            motor.skinWidth = 0.035f;
            motor.stepOffset = 0.2f;
            motor.slopeLimit = 45f;
            var controller = player.gameObject.AddComponent<DodgePlayer>();

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData), typeof(DodgeCamera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 150f;
            camera.fieldOfView = 66f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.91f, 0.88f, 0.84f);
            camera.allowHDR = true;
            var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var view = cameraObject.GetComponent<DodgeCamera>();
            view.player = controller;
            controller.view = view;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0.8f, 2.85f, -11.5f), Quaternion.Euler(9f, 0f, 0f));

            var targets = new GameObject("02 - Practice targets").transform;
            var positions = new[] { new Vector3(-6f, 0.05f, 4.5f), new Vector3(-3f, 0.05f, 8f), new Vector3(3f, 0.05f, 8f), new Vector3(6f, 0.05f, 4.5f) };
            var mats = new[] { pink, yellow, lilac, orange };
            var colors = new[] { new Color(0.95f, 0.59f, 0.66f), new Color(0.98f, 0.8f, 0.4f), new Color(0.7f, 0.64f, 0.87f), new Color(0.96f, 0.65f, 0.45f) };
            for (int i = 0; i < 4; i++)
            {
                var target = CreateActor("Target " + (i + 1), positions[i], mats[i], colors[i], false, i == 1 || i == 2);
                target.transform.SetParent(targets);
                target.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                target.targetPhase = i * 2f;
                // The cap makes the non-player silhouettes easy to recognize.
                var cap = Shape(PrimitiveType.Sphere, "Target cap", target.upperBody, new Vector3(0f, 1.29f, 0f), new Vector3(0.48f, 0.17f, 0.48f), white);
                cap.SetParent(target.head, true);
                DodgeBotUpgrade.Apply(target, ball, (DodgeBotStyle)i);
            }

            var launcher = new GameObject("03 - Ball launcher").transform;
            launcher.position = new Vector3(0f, 0f, 11f);
            launcher.rotation = Quaternion.Euler(0f, 180f, 0f);
            Shape(PrimitiveType.Cylinder, "Launcher base", launcher, new Vector3(0f, 0.16f, 0f), new Vector3(1.6f, 0.16f, 1.6f), dark, true);
            Shape(PrimitiveType.Cylinder, "Launcher stand", launcher, new Vector3(0f, 0.8f, 0f), new Vector3(0.35f, 0.6f, 0.35f), metal);
            Shape(PrimitiveType.Sphere, "Launcher body", launcher, new Vector3(0f, 1.65f, 0f), new Vector3(1.2f, 1.05f, 1.15f), orange);
            var barrel = Shape(PrimitiveType.Cylinder, "Launcher barrel", launcher, new Vector3(0f, 1.72f, 0.65f), new Vector3(0.65f, 0.45f, 0.65f), dark);
            barrel.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var bore = Shape(PrimitiveType.Cylinder, "Launcher muzzle", launcher, new Vector3(0f, 1.72f, 1.12f), new Vector3(0.43f, 0.015f, 0.43f), yellow);
            bore.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var muzzle = new GameObject("Ball spawn point").transform;
            muzzle.SetParent(launcher, false);
            muzzle.localPosition = new Vector3(0f, 1.72f, 1.22f);

            var gameObject = new GameObject("04 - Game and HUD", typeof(AudioSource), typeof(DodgeGame), typeof(DodgeHud));
            var game = gameObject.GetComponent<DodgeGame>();
            game.player = controller;
            game.view = view;
            game.launcher = launcher;
            game.muzzle = muzzle;
            game.ballMaterial = ball;
            game.trailMaterial = trail;
            game.popMaterial = white;
            game.foamMaterial = Mat("Foam cream", new Color(0.99f, 0.97f, 0.9f));
            game.hitboxMaterial = Mat("Hitbox lime", new Color(0.57f, 1f, 0.1f), true);
            game.practiceLauncher = false;
            gameObject.GetComponent<DodgeHud>().game = game;
            var audience=new GameObject("05 - Interactive stadium spectators").AddComponent<DodgeCrowd>();
            audience.BuildAudience(game);DodgeCrowdUpgrade.OptimizePalette(audience,game);

            DodgeStadiumUpgrade.Apply();

            PlayerSettings.productName = "Super Dodgeball - Play Lab";
            PlayerSettings.companyName = "Super Dodgeball";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            Time.fixedDeltaTime = 1f / 60f;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0f, 0f, 1f), Quaternion.Euler(40f, 0f, 0f), 23f);
            Debug.Log("[Dodgeball] Soft Playground built. Original playground scene preserved.");
        }

        static void SetupMaterials()
        {
            dark = Mat("Ink", new Color(0.22f, 0.29f, 0.37f));
            white = Mat("Chalk", new Color(0.98f, 0.95f, 0.86f));
            court = Mat("Court teal", new Color(0.5f, 0.72f, 0.7f));
            purple = Mat("Surround lavender", new Color(0.76f, 0.72f, 0.82f));
            orange = Mat("Peach", new Color(0.96f, 0.65f, 0.45f));
            yellow = Mat("Golden yellow", new Color(0.98f, 0.8f, 0.4f));
            cyan = Mat("Player cyan", Cyan);
            pink = Mat("Target pink", new Color(0.95f, 0.59f, 0.66f));
            lilac = Mat("Target lilac", new Color(0.7f, 0.64f, 0.87f));
            mint = Mat("Mint", new Color(0.7f, 0.85f, 0.68f));
            metal = Mat("Blue grey", new Color(0.62f, 0.7f, 0.76f));
            trail = Mat("Trail unlit", Color.white, true);
            trail.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            trail.SetFloat("_Surface", 1f);
            trail.SetFloat("_Blend", 0f);
            trail.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            trail.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            trail.SetFloat("_ZWrite", 0f);
            trail.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            trail.renderQueue = (int)RenderQueue.Transparent;
            ball = Mat("Peach dodgeball", new Color(1f, 0.72f, 0.51f));
            SetupBallPattern(ball);
        }

        static Material Mat(string name, Color color, bool unlit = false)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.16f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void SetupBallPattern(Material material)
        {
            string path = MaterialRoot + "/BallPattern.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture)
            {
                texture = new Texture2D(128, 64, TextureFormat.RGBA32, true);
                texture.name = "Soft dodgeball stripes";
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = x / 128f, v = y / 64f;
                    float band = Mathf.Min(Mathf.Abs(u - 0.25f), Mathf.Abs(u - 0.75f));
                    bool stripe = band < 0.035f || Mathf.Abs(v - 0.5f) < 0.045f;
                    texture.SetPixel(x, y, stripe ? new Color(1f, 0.97f, 0.84f) : new Color(0.97f, 0.58f, 0.38f));
                }
                texture.Apply();
                AssetDatabase.CreateAsset(texture, path);
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(parent + "/" + parts[i])) AssetDatabase.CreateFolder(parent, parts[i]);
                parent += "/" + parts[i];
            }
        }

        static void BuildCourt(Transform arena)
        {
            Shape(PrimitiveType.Cube, "Raised foundation", arena, new Vector3(0f, -0.9f, 0f), new Vector3(32f, 1.5f, 38f), dark);
            Shape(PrimitiveType.Cube, "Lavender runoff", arena, new Vector3(0f, -0.2f, 0f), new Vector3(30f, 0.35f, 36f), purple, true);
            Shape(PrimitiveType.Cube, "Court floor", arena, new Vector3(0f, -0.04f, 0f), new Vector3(22f, 0.08f, 28f), court, true);
            var lighterCourt = Mat("Court light", new Color(0.55f, 0.76f, 0.74f));
            for (int i = 0; i < 7; i++)
                Shape(PrimitiveType.Cube, "Court panel", arena, new Vector3(0f, 0.001f, -12f + i * 4f), new Vector3(21.8f, 0.005f, 1.95f), lighterCourt);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape(PrimitiveType.Cube, "Sideline", arena, new Vector3(side * 10.3f, 0.012f, 0f), new Vector3(0.12f, 0.015f, 26.8f), white);
                Shape(PrimitiveType.Cube, "End line", arena, new Vector3(0f, 0.012f, side * 13.3f), new Vector3(20.7f, 0.015f, 0.12f), white);
                Shape(PrimitiveType.Cube, "Side bumper", arena, new Vector3(side * 11.5f, 0.5f, 0f), new Vector3(0.7f, 1.1f, 29f), orange, true);
                Shape(PrimitiveType.Cube, "Side bumper stripe", arena, new Vector3(side * 11.48f, 1.075f, 0f), new Vector3(0.63f, 0.08f, 29.1f), yellow);
                Shape(PrimitiveType.Cube, "End bumper", arena, new Vector3(0f, 0.5f, side * 14.5f), new Vector3(23.6f, 1.1f, 0.7f), orange, true);
                for (int i = 0; i < 6; i++)
                {
                    Shape(PrimitiveType.Cube, "Side lane dash", arena, new Vector3(side * 8.8f, 0.02f, -10f + i * 4f), new Vector3(0.13f, 0.02f, 1.1f), yellow);
                    var post = Shape(PrimitiveType.Cylinder, "Arena post", arena, new Vector3(side * 11.5f, 1.7f, -12f + i * 5f), new Vector3(0.18f, 1.2f, 0.18f), metal);
                    Shape(PrimitiveType.Sphere, "Post cap", arena, post.localPosition + Vector3.up * 1.23f, Vector3.one * 0.24f, yellow);
                }
                for (int row = 0; row < 3; row++)
                    Shape(PrimitiveType.Cube, "Bleacher step", arena, new Vector3(side * (13.1f + row * 0.7f), 0.25f + row * 0.4f, 2f),
                        new Vector3(0.65f, 0.25f + row * 0.8f, 17f), row % 2 == 0 ? lilac : pink);
            }
            Shape(PrimitiveType.Cube, "Half court stripe", arena, new Vector3(0f, 0.015f, 0f), new Vector3(20.6f, 0.02f, 0.12f), white);
            Ring("Center circle", arena, new Vector3(0f, 0.025f, 0f), 2.6f, 0.075f, white);
            Ring("Center dot", arena, new Vector3(0f, 0.027f, 0f), 0.3f, 0.08f, yellow);
            Shape(PrimitiveType.Cube, "Back billboard", arena, new Vector3(0f, 4.2f, 14.3f), new Vector3(18.8f, 4.4f, 0.35f), dark);
            Shape(PrimitiveType.Cube, "Billboard left accent", arena, new Vector3(-9.2f, 4.2f, 14.05f), new Vector3(0.3f, 4.2f, 0.1f), mint);
            WorldText("Game title", arena, "SUPER DODGEBALL", new Vector3(0f, 4.75f, 14.02f), 0.52f, white, TextAnchor.MiddleCenter);
            WorldText("Game subtitle", arena, "BOUNCE.  BEND.  BE SILLY.", new Vector3(0f, 3.65f, 14f), 0.22f, mint, TextAnchor.MiddleCenter);
            foreach (float x in new[] { -10f, 10f })
            {
                Shape(PrimitiveType.Cube, "Billboard support", arena, new Vector3(x, 3f, 14.5f), new Vector3(0.3f, 6f, 0.3f), metal);
                var floodlight = new GameObject("Floodlight", typeof(Light)).GetComponent<Light>();
                floodlight.transform.SetParent(arena);
                floodlight.transform.SetPositionAndRotation(new Vector3(x, 6.8f, 12f), Quaternion.Euler(40f, 180f, 0f));
                floodlight.type = LightType.Spot;
                floodlight.range = 35f;
                floodlight.spotAngle = 85f;
                floodlight.intensity = 3f;
                floodlight.color = new Color(0.7f, 0.85f, 1f);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (18f + i * 1.2f);
                Shape(PrimitiveType.Sphere, "Soft garden hill", arena, new Vector3(x, 0.6f, 16f + i * 2f),
                    new Vector3(9f, 5f + i % 3, 9f), i % 3 == 0 ? mint : i % 3 == 1 ? lilac : pink);
            }
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                var cloud = Shape(PrimitiveType.Sphere, "Cotton cloud", arena,
                    new Vector3(side * (11f + i * 1.7f), 7f + (i == 1 ? 0.5f : 0f), 20f),
                    new Vector3(4.5f, 1.7f + (i == 1 ? 0.5f : 0f), 2f), white);
                cloud.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        public static DodgeActor CreateActor(string name, Vector3 position, Material material, Color color, bool isPlayer, bool wiggle)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var actor = root.AddComponent<DodgeActor>();
            actor.isPlayer = isPlayer;
            actor.animatedTarget = wiggle;
            actor.bodyColor = color;
            var coloredParts = new List<Renderer>();
            var upper = new GameObject("Upper body pivot - dodge").transform;
            upper.SetParent(root.transform, false);
            upper.localPosition = new Vector3(0f, 0.72f, 0f);
            actor.upperBody = upper;
            Body(PrimitiveType.Capsule, "Torso hitbox", upper, new Vector3(0f, 0.48f, 0f), new Vector3(0.84f, 0.5f, 0.7f), material, actor, coloredParts);
            var head = Body(PrimitiveType.Sphere, "Head hitbox", upper, new Vector3(0f, 1.04f, 0.025f), new Vector3(0.85f, 0.7f, 0.77f), material, actor, coloredParts);
            actor.head = head;
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new GameObject(side < 0 ? "Left shoulder" : "Right shoulder").transform;
                shoulder.SetParent(upper, false);
                shoulder.localPosition = new Vector3(side * 0.37f, 0.68f, 0f);
                Body(PrimitiveType.Capsule, "Arm hitbox", shoulder, new Vector3(side * 0.03f, -0.2f, 0f), new Vector3(0.27f, 0.25f, 0.27f), material, actor, coloredParts);
                if (side < 0) actor.leftArm = shoulder; else actor.rightArm = shoulder;
                var hip = new GameObject(side < 0 ? "Left hip" : "Right hip").transform;
                hip.SetParent(root.transform, false);
                hip.localPosition = new Vector3(side * 0.22f, 0.7f, 0f);
                Body(PrimitiveType.Capsule, "Leg hitbox", hip, new Vector3(0f, -0.27f, 0f), new Vector3(0.27f, 0.28f, 0.27f), material, actor, coloredParts);
                Body(PrimitiveType.Sphere, "Foot hitbox", hip, new Vector3(0f, -0.58f, 0.08f), new Vector3(0.36f, 0.23f, 0.46f), white, actor, null);
                if (side < 0) actor.leftLeg = hip; else actor.rightLeg = hip;
            }
            var belt = Shape(PrimitiveType.Cube, "Bean number", upper, new Vector3(0f, 0.47f, 0.295f), new Vector3(0.24f, 0.23f, 0.025f), white);
            if (isPlayer)
                actor.heldBall = Shape(PrimitiveType.Sphere, "Held dodgeball", actor.rightArm, new Vector3(0f, -0.42f, 0.16f), Vector3.one * 0.34f, ball);
            actor.bodyRenderers = coloredParts.ToArray();
            Articulate(actor);
            return actor;
        }

        [MenuItem("Super Dodgeball/Upgrade articulated characters")]
        public static void UpgradeCharacters()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            foreach (var actor in UnityEngine.Object.FindObjectsByType<DodgeActor>(FindObjectsSortMode.None)) Articulate(actor);
            foreach (var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if (label.name == "Target label" || label.name == "Launcher instruction") UnityEngine.Object.DestroyImmediate(label.gameObject);
            var player = UnityEngine.Object.FindFirstObjectByType<DodgePlayer>();
            if (player) player.chargeSeconds = 1.25f;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        static void Articulate(DodgeActor actor)
        {
            if (actor.leftForearm) { ElasticCharacterUpgrade.Apply(actor); return; }
            var parts = new List<Renderer>(actor.bodyRenderers);
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = side < 0 ? actor.leftArm : actor.rightArm;
                var arm = shoulder.Find("Arm hitbox");
                arm.localPosition = new Vector3(0f, -0.16f, 0f);
                arm.localScale = new Vector3(0.27f, 0.18f, 0.27f);
                var material = arm.GetComponent<Renderer>().sharedMaterial;
                var elbow = new GameObject(side < 0 ? "Left elbow" : "Right elbow").transform;
                elbow.SetParent(shoulder, false);
                elbow.localPosition = new Vector3(0f, -0.32f, 0f);
                Body(PrimitiveType.Capsule, "Forearm hitbox", elbow, new Vector3(0f, -0.14f, 0f), new Vector3(0.25f, 0.16f, 0.25f), material, actor, parts);
                Body(PrimitiveType.Sphere, "Hand hitbox", elbow, new Vector3(0f, -0.29f, 0f), Vector3.one * 0.26f, material, actor, parts);
                var hip = side < 0 ? actor.leftLeg : actor.rightLeg;
                var thigh = hip.Find("Leg hitbox");
                thigh.localPosition = new Vector3(0f, -0.16f, 0f);
                thigh.localScale = new Vector3(0.28f, 0.18f, 0.28f);
                var knee = new GameObject(side < 0 ? "Left knee" : "Right knee").transform;
                knee.SetParent(hip, false);
                knee.localPosition = new Vector3(0f, -0.34f, 0f);
                Body(PrimitiveType.Capsule, "Shin hitbox", knee, new Vector3(0f, -0.14f, 0f), new Vector3(0.26f, 0.17f, 0.26f), material, actor, parts);
                var foot = hip.Find("Foot hitbox");
                foot.SetParent(knee, false);
                foot.localPosition = new Vector3(0f, -0.28f, 0.08f);
                if (side < 0) { actor.leftForearm = elbow; actor.leftShin = knee; }
                else { actor.rightForearm = elbow; actor.rightShin = knee; }
            }
            if (actor.heldBall)
            {
                actor.heldBall.SetParent(actor.rightForearm, false);
                actor.heldBall.localPosition = new Vector3(0f, -0.29f, 0.12f);
            }
            var cap = actor.upperBody.Find("Target cap");
            if (cap) cap.SetParent(actor.head, true);
            actor.bodyRenderers = parts.ToArray();
            if (!actor.GetComponent<DodgeRagdoll>()) actor.gameObject.AddComponent<DodgeRagdoll>();
            ElasticCharacterUpgrade.Apply(actor);
            EditorUtility.SetDirty(actor);
        }

        static Transform Body(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, DodgeActor actor, List<Renderer> renderers)
        {
            var part = Shape(type, name, parent, position, scale, material);
            var collider = type == PrimitiveType.Sphere ? (Collider)part.gameObject.AddComponent<SphereCollider>() : part.gameObject.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            part.gameObject.AddComponent<DodgeHitbox>().actor = actor;
            renderers?.Add(part.GetComponent<Renderer>());
            return part;
        }

        static Transform Shape(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            if (type == PrimitiveType.Cube)
            {
                var rounded = SoftShapes.Box(name, scale, material, solid);
                rounded.transform.SetParent(parent, false);
                rounded.transform.localPosition = position;
                return rounded.transform;
            }
            var shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            var collider = shape.GetComponent<Collider>();
            if (solid) shape.AddComponent<DodgeSurface>();
            else UnityEngine.Object.DestroyImmediate(collider);
            return shape.transform;
        }

        static void Ring(string name, Transform parent, Vector3 position, float radius, float width, Material material)
        {
            var line = new GameObject(name, typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(parent);
            line.transform.localPosition = position;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 96;
            line.startWidth = line.endWidth = width;
            line.sharedMaterial = trail;
            line.startColor = line.endColor = material ? material.GetColor("_BaseColor") : Color.white;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius);
            }
        }

        static void WorldText(string name, Transform parent, string content, Vector3 position, float size, Material colorMaterial, TextAnchor anchor)
        {
            var label = new GameObject(name, typeof(TextMesh)).GetComponent<TextMesh>();
            label.transform.SetParent(parent);
            label.transform.position = position;
            label.text = content;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 96;
            label.characterSize = size * 0.25f;
            label.anchor = anchor;
            label.alignment = TextAlignment.Center;
            label.color = colorMaterial.GetColor("_BaseColor");
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
        }

        static void BuildLighting()
        {
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1f, 0.92f, 0.8f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.45f;
            sun.transform.rotation = Quaternion.Euler(43f, -32f, 0f);
            RenderSettings.sun = sun;
            string skyPath = MaterialRoot + "/SoftSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (!sky)
            {
                sky = new Material(Shader.Find("SuperDodgeball/SoftSky"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.SetColor("_TopColor", new Color(0.32f, 0.65f, 0.88f));
            sky.SetColor("_HorizonColor", new Color(0.98f, 0.93f, 0.82f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.8f, 0.82f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.7f, 0.72f, 0.73f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.48f, 0.5f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.91f, 0.88f, 0.84f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 100f;
            var volume = new GameObject("Color and bloom", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.Neutral);
            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.1f);
            color.contrast.Override(-5f);
            color.saturation.Override(-3f);
            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.15f);
            bloom.threshold.Override(1.2f);
            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.08f);
            string path = MaterialRoot + "/PlaygroundVolume.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!existing)
            {
                AssetDatabase.CreateAsset(profile, path);
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
                volume.sharedProfile = profile;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(profile);
                volume.sharedProfile = existing;
            }
        }

        [MenuItem("Super Dodgeball/Validate collision geometry")]
        public static void ValidateGeometry()
        {
            var temporary = new GameObject("Temporary geometry verification");
            try
            {
                var actor = CreateActor("Test actor", new Vector3(1000f, 0f, 1000f),
                    AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "/Player cyan.mat"), Cyan, false, false);
                actor.transform.SetParent(temporary.transform);
                var buffer = new RaycastHit[64];
                Vector3 from = actor.transform.position + new Vector3(0f, 1.72f, -4f);
                Vector3 to = from + Vector3.forward * 8f;
                actor.SetLeanImmediate(0f);
                Physics.SyncTransforms();
                bool standingHit = DodgeBall.Trace(from, to, DodgeBall.Radius, null, buffer, out _, out var hit) && hit == actor;
                actor.SetLeanImmediate(1f);
                Physics.SyncTransforms();
                bool rightMiss = !DodgeBall.Trace(from, to, DodgeBall.Radius, null, buffer, out _, out _);
                actor.SetLeanImmediate(-1f);
                Physics.SyncTransforms();
                bool leftMiss = !DodgeBall.Trace(from, to, DodgeBall.Radius, null, buffer, out _, out _);
                actor.SetLeanImmediate(0f);
                Physics.SyncTransforms();
                bool ownerIgnored = !DodgeBall.Trace(from, to, DodgeBall.Radius, actor, buffer, out _, out _);
                var wall = Shape(PrimitiveType.Cube, "Test wall", temporary.transform,
                    actor.transform.position + new Vector3(0f, 1.72f, -2f), new Vector3(2f, 3f, 0.15f), dark, true);
                Physics.SyncTransforms();
                bool wallFirst = DodgeBall.Trace(from, to, DodgeBall.Radius, null, buffer, out var wallHit, out var wallActor)
                    && wallActor == null && wallHit.collider.gameObject == wall.gameObject;
                string report = JsonUtility.ToJson(new GeometryReport { standingHit = standingHit, leftLeanDodges = leftMiss,
                    rightLeanDodges = rightMiss, shooterIgnored = ownerIgnored, wallBlocksFirst = wallFirst,
                    passed = standingHit && leftMiss && rightMiss && ownerIgnored && wallFirst }, true);
                System.IO.Directory.CreateDirectory("work");
                System.IO.File.WriteAllText("work/geometry-check.json", report);
                if (!standingHit || !leftMiss || !rightMiss || !ownerIgnored || !wallFirst)
                    throw new InvalidOperationException("Collision verification failed: " + report);
                Debug.Log("[Dodgeball] Collision verification passed: standing hit, both lean directions evade, shooter ignored, wall blocks.");
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        [Serializable]
        public sealed class GeometryReport
        {
            public bool standingHit, leftLeanDodges, rightLeanDodges, shooterIgnored, wallBlocksFirst, passed;
        }
    }
}
