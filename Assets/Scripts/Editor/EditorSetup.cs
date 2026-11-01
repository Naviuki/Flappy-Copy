using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Batch-mode entry points for the CLI harness (TMP resources, scene build, checks).</summary>
    public static class EditorSetup
    {
        /// <summary>Deletes the leftovers of the Unity 2D template (sample scene and input actions).</summary>
        public static void DeleteTemplateAssets()
        {
            DeleteIfExists("Assets/Scenes/SampleScene.unity");
            DeleteIfExists("Assets/InputSystem_Actions.inputactions");
            AssetDatabase.Refresh();
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>-executeMethod FlappyCopy.EditorTools.EditorSetup.ImportTmpAndExit</summary>
        public static void ImportTmpAndExit()
        {
            bool ok = TmpEssentials.EnsureInstalled();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (ok)
            {
                AssetDatabase.ImportAsset(TmpEssentials.SettingsPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(TmpEssentials.FontPath, ImportAssetOptions.ForceSynchronousImport);
            }

            Debug.Log("[FlappyCopy] TMP resources installed: " + ok);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>
        /// One-shot project setup: TMP resources, player settings, fresh data/prefabs/scene, validation.
        /// -executeMethod FlappyCopy.EditorTools.EditorSetup.PrepareAndExit
        /// </summary>
        public static void PrepareAndExit()
        {
            bool tmp = TmpEssentials.EnsureInstalled();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            DeleteTemplateAssets();
            PlayerSetup.Apply();
            GameSceneBuilder.RebuildAll();

            var problems = ProjectSelfCheck.Run();
            foreach (string problem in problems)
            {
                Debug.LogError("[FlappyCopy] Prepare: " + problem);
            }

            var issues = ProjectValidator.Validate();
            foreach (string issue in issues)
            {
                Debug.LogError("[FlappyCopy] Prepare: " + issue);
            }

            ProjectChecks.Describe();
            Debug.Log("[FlappyCopy] Prepare finished. TMP=" + tmp + ", problems=" + (problems.Count + issues.Count));
            EditorApplication.Exit(tmp && problems.Count == 0 && issues.Count == 0 ? 0 : 1);
        }

        public static void ExtractTmpPackageAndExit()
        {
            int written = TmpEssentials.Extract(GameScenePaths.TmpEssentialsPackage);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[FlappyCopy] TMP files written: " + written);
            EditorApplication.Exit(written > 0 ? 0 : 1);
        }
    }
}
