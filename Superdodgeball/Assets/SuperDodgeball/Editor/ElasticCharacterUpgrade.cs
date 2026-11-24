using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public static class ElasticCharacterUpgrade
    {
        [MenuItem("Super Dodgeball/Upgrade elastic body")]
        public static void UpgradeScene()
        {
            if (EditorApplication.isPlaying) return;
            foreach (var actor in Object.FindObjectsByType<DodgeActor>(FindObjectsSortMode.None)) Apply(actor);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public static void Apply(DodgeActor actor)
        {
            if (actor.GetComponent<DodgeSpine>() || !actor.leftForearm) return;
            var spine = actor.gameObject.AddComponent<DodgeSpine>();
            var container = new GameObject("Elastic skeleton - wardrobe compatible").transform;
            container.SetParent(actor.transform, false);
            for (int i = 0; i < DodgeSpine.Segments; i++)
            {
                var section = new GameObject("Spine " + i).transform;
                section.SetParent(container, false);
                spine.sections[i] = section;
            }
            spine.chest = new GameObject("Chest clothing socket").transform;
            spine.chest.SetParent(container, false);
            spine.Pose(actor, 0f, 0f);
            actor.head.SetParent(spine.chest, true);
            actor.leftArm.SetParent(spine.chest, true);
            actor.rightArm.SetParent(spine.chest, true);
            var badge = actor.upperBody.Find("Bean number");
            if (badge) Object.DestroyImmediate(badge.gameObject);
            EditorUtility.SetDirty(spine);
            EditorUtility.SetDirty(actor);
        }
    }
}
