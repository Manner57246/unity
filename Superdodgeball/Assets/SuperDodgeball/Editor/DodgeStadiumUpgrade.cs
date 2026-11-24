using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SuperDodgeball.Editor
{
    public static class DodgeStadiumUpgrade
    {
        [MenuItem("Super Dodgeball/Upgrade to indoor stadium")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
            var game=Object.FindFirstObjectByType<DodgeGame>();
            if(!game)throw new System.InvalidOperationException("Open the dodgeball scene first.");
            const string path="Assets/SuperDodgeball/Materials/Soft/Stadium display.mat";
            var screen=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!screen){screen=new Material(Shader.Find("Universal Render Pipeline/Unlit"));screen.name="Stadium display";AssetDatabase.CreateAsset(screen,path);}
            var existing=Object.FindFirstObjectByType<DodgeStadium>();if(existing)Object.DestroyImmediate(existing.gameObject);
            var stadium=new GameObject("10 - Indoor party stadium").AddComponent<DodgeStadium>();
            stadium.BuildGeometry(game,screen);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
    }
}
