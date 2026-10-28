using System.IO;
using FlappyCopy.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Player builds for CI and for the release folder: Windows and Linux, portrait window.</summary>
    public static class BuildScript
    {
        public const string WindowsOutput = "Builds/Windows/FlappyCopy.exe";
        public const string LinuxOutput = "Builds/Linux/FlappyCopy.x86_64";

        [MenuItem("Flappy Copy/Build/Windows")]
        public static void BuildWindowsMenu()
        {
            Build(BuildTarget.StandaloneWindows64, WindowsOutput, false);
        }

        /// <summary>CI entry point: -executeMethod FlappyCopy.EditorTools.BuildScript.BuildWindowsBatch.</summary>
        public static void BuildWindowsBatch()
        {
            bool ok = Build(BuildTarget.StandaloneWindows64, WindowsOutput, false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static void BuildLinuxBatch()
        {
            bool ok = Build(BuildTarget.StandaloneLinux64, LinuxOutput, false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build(BuildTarget target, string outputPath, bool development)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectPaths.ScenePath) == null)
            {
                Debug.LogError("[FlappyCopy] Build failed: missing scene " + ProjectPaths.ScenePath
                               + ". Run 'Flappy Copy > Rebuild Scene, Prefabs and Data' first.");
                return false;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ProjectPaths.ScenePath, true)
            };

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            PlayerSetup.Apply();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectPaths.ScenePath },
                locationPathName = outputPath,
                target = target,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.LogFormat("[FlappyCopy] Build succeeded: {0} ({1:N0} bytes)", outputPath, summary.totalSize);
                return true;
            }

            Debug.LogErrorFormat("[FlappyCopy] Build failed: {0}, errors={1}", summary.result, summary.totalErrors);
            return false;
        }
    }
}
