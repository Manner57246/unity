using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public static class DodgeCrowdUpgrade
    {
        [MenuItem("Super Dodgeball/Upgrade stadium seating and spectator physics")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
            var game=Object.FindFirstObjectByType<DodgeGame>();if(!game)throw new System.InvalidOperationException("Open the dodgeball scene first.");
            var previous=Object.FindFirstObjectByType<DodgeCrowd>();if(previous)Object.DestroyImmediate(previous.gameObject);
            var crowd=new GameObject("05 - Interactive stadium audience").AddComponent<DodgeCrowd>();crowd.BuildAudience(game);OptimizePalette(crowd,game);RemoveFaces();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
        }
        [MenuItem("Super Dodgeball/Remove character facial features")]
        public static void RemoveFaces()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
            foreach(var part in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(part && (part.name=="Eye" || part.name=="Eye glint" || part.name=="Smile" || part.name=="Blush cheek"))Object.DestroyImmediate(part.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        public static void OptimizePalette(DodgeCrowd crowd,DodgeGame game)
        {
            const string folder="Assets/SuperDodgeball/Materials/Crowd";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/SuperDodgeball/Materials","Crowd");
            foreach(var entry in crowd.PaletteEntries)
            {
                string key=ColorUtility.ToHtmlStringRGBA(entry.color),path=folder+"/Crowd "+key+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material){material=new Material(game.popMaterial);material.name="Crowd "+key;material.mainTexture=null;material.SetColor("_BaseColor",entry.color);material.enableInstancing=true;AssetDatabase.CreateAsset(material,path);}
                entry.renderer.sharedMaterial=material;entry.renderer.SetPropertyBlock(null);
            }
            crowd.FinishPalette();EditorUtility.SetDirty(crowd);AssetDatabase.SaveAssets();
        }
        [MenuItem("Super Dodgeball/Add sideline spectators")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before adding spectators.");
            var game = Object.FindFirstObjectByType<DodgeGame>();
            if (!game) throw new System.InvalidOperationException("Open the playground scene first.");
            var crowd = Object.FindFirstObjectByType<DodgeCrowd>();
            if (!crowd)
            {
                var root = new GameObject("05 - Mischievous sideline spectators");
                Undo.RegisterCreatedObjectUndo(root, "Add spectators");
                crowd = root.AddComponent<DodgeCrowd>();
                crowd.BuildAudience(game);OptimizePalette(crowd,game);
            }
            game.crowdEnabled = true;
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        }
    }
}
