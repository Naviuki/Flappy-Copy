using FlappyCopy.EditorTools;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Command line entry points for CI: build Windows, validate and one-shot project setup.</summary>
    public static class CiEntry
    {
        /// <summary>-executeMethod FlappyCopy.EditorTools.CiEntry.BuildWindowsCi</summary>
        public static void BuildWindowsCi()
        {
            bool ok = BuildScript.Build(BuildTarget.StandaloneWindows64, BuildScript.WindowsOutput, false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>-executeMethod FlappyCopy.EditorTools.CiEntry.ValidateCi</summary>
        public static void ValidateCi()
        {
            ProjectValidator.RunAndExit();
        }

        /// <summary>Fresh check-out setup: TMP resources, player settings, scene, prefabs, data.</summary>
        public static void SetupCi()
        {
            bool tmp = TmpEssentials.EnsureInstalled();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PlayerSetup.Apply();
            EditorSetup.DeleteTemplateAssets();
            GameSceneBuilder.RebuildAll();
            string report = ProjectChecks.Describe();
            Debug.Log("[FlappyCopy] Setup: TMP=" + tmp + " checks=" + report);
            EditorApplication.Exit(tmp ? 0 : 1);
        }

        /// <summary>Deletes the leftovers of the 2D template so the project contains only the game.</summary>
        public static void CleanTemplateAssetsCi()
        {
            EditorSetup.DeleteTemplateAssets();
            EditorApplication.Exit(0);
        }
    }
}
