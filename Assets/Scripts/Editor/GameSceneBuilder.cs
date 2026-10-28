using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    public static class ProjectPaths
    {
        public const string ScenesFolder = "Assets/Scenes";
        public const string PrefabsFolder = "Assets/Prefabs";
        public const string DataFolder = "Assets/Data";
        public const string TexturesFolder = "Assets/Textures";
        public const string SettingsFolder = "Assets/Settings";
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string BirdPrefabPath = "Assets/Prefabs/Bird.prefab";
        public const string PipePairPrefabPath = "Assets/Prefabs/PipePair.prefab";
        public const string TestResultsPath = "Temp/harness/editmode_results.json";
        public const string TestStatePath = "Temp/harness/test_state.txt";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string normalized = path.Replace('\\', '/');
            string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            string name = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        public static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    /// <summary>
    /// One-click (and command line) regeneration of the scene, prefabs and data assets. Idempotent:
    /// running it again rebuilds the same game, which is why the repository needs no manual setup.
    /// </summary>
    public static class GameSceneBuilder
    {
        [MenuItem("Flappy Copy/Rebuild Scene, Prefabs and Data")]
        public static void RebuildAll()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.ScenesFolder);
            ProjectPaths.EnsureFolder(ProjectPaths.PrefabsFolder);
            ProjectPaths.EnsureFolder(ProjectPaths.DataFolder);
            ProjectPaths.EnsureFolder(ProjectPaths.TexturesFolder);

            // Art and data first: the prefabs reference them, and each SaveAndReimport must settle
            // before the next step reads the sprite (otherwise a prefab gets an empty sprite).
            SpriteTextures.CreateAll();
            FlappyData.CreateAll();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            FlappyPrefabs.CreateAll();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            GameScene.Assemble();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ProjectPaths.ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ProjectPaths.ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FlappyCopy] Scene, prefabs and data rebuilt.");
        }
    }
}
