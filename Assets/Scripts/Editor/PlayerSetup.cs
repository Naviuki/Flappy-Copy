using System.IO;
using FlappyCopy.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Player settings that make the project launch the same way from the editor and from a build.</summary>
    public static class PlayerSetup
    {
        public const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Flappy Copy/Apply Player Settings")]
        public static void ApplyMenu()
        {
            Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("[FlappyCopy] Player settings applied.");
        }

        public static void Apply()
        {
            // The game reads the legacy Input Manager (Space / mouse / touch); the 2D template enabled
            // "Input System package (New)" which throws at runtime for UnityEngine.Input.
            SetProjectSetting("activeInputHandler", 0);

            PlayerSettings.companyName = "Flappy Copy";
            PlayerSettings.productName = GameConstants.ProductName;
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = DesignSpace.ReferenceWinWidthMin;
            PlayerSettings.defaultScreenHeight = DesignSpace.ReferenceWinHeightMin;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.flappycopy.game");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }

        /// <summary>Writes a ProjectSettings.asset value by name (some player settings have no public API).</summary>
        private static void SetProjectSetting(string propertyName, int value)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                return;
            }

            var serialized = new SerializedObject(assets[0]);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.intValue == value)
            {
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Imports the bundled TextMeshPro essential resources (LiberationSans SDF) when missing, so the
        /// HUD works without the manual "Window > TextMeshPro > Import TMP Essential Resources" step.
        /// </summary>
        public static bool EnsureTextMeshProResources()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TmpSettingsPath) != null)
            {
                return true;
            }

            TmpEssentials.EnsureInstalled();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return File.Exists(TmpSettingsPath);
        }
    }
}
