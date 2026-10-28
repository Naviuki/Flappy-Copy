using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Reports code, scene and build-health problems with machines for CI; never fails silently.</summary>
    public static class ProjectChecks
    {
        [MenuItem("Flappy Copy/Checks/Run All Checks")]
        public static void RunAll()
        {
            Debug.Log("[FlappyCopy] " + Describe());
        }

        /// <summary>-executeMethod FlappyCopy.EditorTools.ProjectChecks.RunAllAndExit</summary>
        public static void RunAllAndExit()
        {
            string report = Describe();
            if (report.StartsWith("OK"))
            {
                Debug.Log("[FlappyCopy] ALL CHECKS PASSED. " + report);
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError("[FlappyCopy] CHECKS FAILED. " + report);
            EditorApplication.Exit(1);
        }

        public static string Describe()
        {
            var builder = new System.Text.StringBuilder();
            int failures = 0;

            Report(builder, ref failures, File.Exists(TmpEssentials.SettingsPath), "TMP settings on disk");
            Report(builder, ref failures, File.Exists(TmpEssentials.FontPath), "TMP default font on disk");
            Report(builder, ref failures, AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectPaths.ScenePath) != null,
                "Game scene present: " + ProjectPaths.ScenePath);

            bool buildScene = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                buildScene |= scene.enabled && scene.path == ProjectPaths.ScenePath;
            }

            Report(builder, ref failures, buildScene, "Game scene is the enabled build scene");

            foreach (string layer in Core.ProjectLayers.LayerNames)
            {
                Report(builder, ref failures, LayerMask.NameToLayer(layer) >= 0, "layer " + layer);
            }

            foreach (string sortingLayer in Core.SortingLayers.All)
            {
                Report(builder, ref failures, Core.ProjectLayers.HasSortingLayer(sortingLayer), "sorting layer " + sortingLayer);
            }

            return (failures == 0 ? "OK" : failures + " failed") + " | " + builder;
        }

        private static void Report(System.Text.StringBuilder builder, ref int failures, bool ok, string what)
        {
            if (!ok)
            {
                failures++;
            }

            builder.Append(ok ? "[ok] " : "[FAILED] ").Append(what).Append("; ");
        }
    }
}
