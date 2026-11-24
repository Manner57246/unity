using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public static class DodgeBotUpgrade
    {
        [MenuItem("Super Dodgeball/Upgrade practice targets to bots")]
        public static void UpgradeScene()
        {
            if (EditorApplication.isPlaying) return;
            var player = Object.FindFirstObjectByType<DodgePlayer>();
            if (!player) return;
            var ball = player.GetComponent<DodgeActor>().heldBall.GetComponent<Renderer>().sharedMaterial;
            var actors = Object.FindObjectsByType<DodgeActor>(FindObjectsSortMode.None);
            System.Array.Sort(actors, (a, b) => string.CompareOrdinal(a.name, b.name));
            int index = 0;
            foreach (var actor in actors)
                if (!actor.isPlayer && actor.leftForearm) Apply(actor, ball, (DodgeBotStyle)(index++ % 4));
            foreach (var line in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                if (line.name == "Target floor ring") Object.DestroyImmediate(line.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public static void Apply(DodgeActor actor, Material ballMaterial, DodgeBotStyle style)
        {
            if (actor.isPlayer || actor.GetComponent<DodgeBot>()) return;
            if (!actor.heldBall)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Held dodgeball";
                Object.DestroyImmediate(ball.GetComponent<Collider>());
                ball.transform.SetParent(actor.rightForearm, false);
                ball.transform.localPosition = new Vector3(0f, -0.29f, 0.12f);
                ball.transform.localScale = Vector3.one * 0.34f;
                ball.GetComponent<Renderer>().sharedMaterial = ballMaterial;
                actor.heldBall = ball.transform;
            }
            var motor = actor.GetComponent<CharacterController>();
            if (!motor) motor = actor.gameObject.AddComponent<CharacterController>();
            motor.height = 1.9f;
            motor.radius = 0.29f;
            motor.center = Vector3.up * 0.95f;
            motor.skinWidth = 0.035f;
            motor.stepOffset = 0.2f;
            motor.slopeLimit = 45f;
            var bot = actor.gameObject.AddComponent<DodgeBot>();
            bot.style = style;
            bot.moveSpeed = style == DodgeBotStyle.Acrobat ? 4.6f : 4.2f;
            bot.reactionSeconds = style == DodgeBotStyle.Acrobat ? 0.22f : style == DodgeBotStyle.Pitcher ? 0.34f : 0.28f;
            bot.aimError = style == DodgeBotStyle.Pitcher ? 0.4f : 0.6f;
            EditorUtility.SetDirty(actor);
            EditorUtility.SetDirty(bot);
        }
    }
}
