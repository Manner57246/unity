using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SuperDodgeball.Editor
{
    public static class DodgeMvpBuild
    {
        [MenuItem("Super Dodgeball/Build Windows MVP")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
            var folder = Path.GetFullPath("Builds/SuperDodgeball-MVP"); Directory.CreateDirectory(folder);
            PlayerSettings.productName = "Super Dodgeball";
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var options = new BuildPlayerOptions { scenes = new[] { DodgeballSceneBuilder.ScenePath },
                locationPathName = Path.Combine(folder,"SuperDodgeball.exe"), target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development };
            var report = BuildPipeline.BuildPlayer(options);
            Directory.CreateDirectory("work");
            File.WriteAllText("work/mvp-build.json", JsonUtility.ToJson(new BuildResult {
                succeeded = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded,
                path = options.locationPathName, errors = report.summary.totalErrors, size = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds },true));
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("MVP build failed: " + report.summary.result);
            Debug.Log("[Dodgeball] Windows MVP built: " + options.locationPathName);
        }
        [Serializable] sealed class BuildResult
        {
            public bool succeeded; public string path; public int errors; public ulong size; public double seconds;
        }
    }
}
