using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Builds a separate self-test player (Builds/SelfTest) that runs the runtime scene checks on its own main
    /// thread and writes Temp/scenechecks.txt before it quits. Define FLAPPYCOPY_SELF_TEST exists only in that
    /// test build. On machines where the editor cannot create MonoBehaviour components reliably (this one), it
    /// is also the independent confirmation that the game scene and prefabs are healthy.
    /// </summary>
    public static class SelfTestBuild
    {
        public const string Output = "Builds/SelfTest/FlappyCopySelfTest.exe";
        private const string Define = "FLAPPYCOPY_SELF_TEST";

        [MenuItem("Flappy Copy/Build/Self Test Player (Windows)")]
        public static void BuildFromMenu()
        {
            bool ok = Build();
            EditorUtility.DisplayDialog("Flappy Copy",
                ok ? "Self-test player built in Builds/SelfTest." : "Self-test build failed, see the Console.", "OK");
        }

        public static void BuildAndExit()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectPaths.ScenePath) == null)
            {
                Debug.LogError("[FlappyCopy] Self-test build: game scene missing");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Output) ?? "Builds");
            PlayerSetup.Apply();

            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            bool added = !symbols.Contains(Define);
            if (added)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone,
                    string.IsNullOrEmpty(symbols) ? Define : symbols + ";" + Define);
            }

            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ProjectPaths.ScenePath },
                    locationPathName = Output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                bool ok = report.summary.result == BuildResult.Succeeded;
                Debug.Log("[FlappyCopy] Self-test build " + (ok ? "succeeded" : "failed") + ": " + Output);
                return ok;
            }
            finally
            {
                if (added)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
                }
            }
        }
    }
}
