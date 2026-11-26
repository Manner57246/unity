using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_EDITOR_OSX
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
#endif

namespace SuperDodgeball.Editor
{
    public static class DodgeMvpBuild
    {
        [MenuItem("Super Dodgeball/Build Windows MVP")]
        public static void Build()
        {
            BuildMvp(BuildTarget.StandaloneWindows64, "SuperDodgeball-MVP", "SuperDodgeball.exe", "mvp-build.json");
        }

#if UNITY_EDITOR_OSX
        [MenuItem("Super Dodgeball/Build macOS MVP (Apple Silicon)")]
        public static void BuildMacOS()
        {
            var previous = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
            try
            {
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
                BuildMvp(BuildTarget.StandaloneOSX, "SuperDodgeball-macOS", "SuperDodgeball.app", "macos-mvp-build.json");
            }
            finally
            {
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = previous;
            }
        }

        static void SignMacDevelopmentPlayer(string playerPath)
        {
            // Unity can thin universal dylibs to ARM64, invalidating the original signatures.
            var plugins = Path.Combine(playerPath, "Contents", "PlugIns");
            if (Directory.Exists(plugins))
            {
                foreach (var library in Directory.EnumerateFiles(plugins, "*.dylib"))
                    RunCodeSign("--force", "--sign", "-", library);
            }

            RunCodeSign("--force", "--sign", "-", playerPath);
            RunCodeSign("--verify", "--deep", "--strict", playerPath);
        }

        static void RunCodeSign(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("/usr/bin/codesign")
            {
                UseShellExecute = false,
                RedirectStandardError = true
            };
            foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start codesign.");
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException("macOS code signing failed:\n" + error);
        }
#endif

        static void BuildMvp(BuildTarget target, string folderName, string playerName, string reportName)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
            var folder = Path.GetFullPath(Path.Combine("Builds", folderName));
            Directory.CreateDirectory(folder);
            PlayerSettings.productName = "Super Dodgeball";
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { DodgeballSceneBuilder.ScenePath },
                locationPathName = Path.Combine(folder, playerName),
                target = target,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
#if UNITY_EDITOR_OSX
            if (target == BuildTarget.StandaloneOSX && report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                SignMacDevelopmentPlayer(options.locationPathName);
#endif
            Directory.CreateDirectory("work");
            File.WriteAllText(Path.Combine("work", reportName), JsonUtility.ToJson(new BuildResult {
                succeeded = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded,
                path = options.locationPathName, errors = report.summary.totalErrors, size = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds },true));
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("MVP build failed: " + report.summary.result);
            Debug.Log("[Dodgeball] " + target + " MVP built: " + options.locationPathName);
        }
        [Serializable] sealed class BuildResult
        {
            public bool succeeded; public string path; public int errors; public ulong size; public double seconds;
        }
    }
}
