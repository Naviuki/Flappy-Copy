using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// One-click validation of everything the game needs at runtime: the scene graph (references connected),
    /// the prefabs, the project settings and the input handler. Also usable from the command line:
    /// -executeMethod FlappyCopy.EditorTools.ProjectValidator.RunAndExit
    /// </summary>
    public static class ProjectValidator
    {
        private const string ResultsPath = "Logs/validation.txt";

        [MenuItem("Flappy Copy/Validate Project")]
        public static void RunFromMenu()
        {
            List<string> issues = Validate();
            Debug.Log("[FlappyCopy] Validation: " + (issues.Count == 0 ? "OK" : issues.Count + " issue(s): " + string.Join("; ", issues)));
        }

        public static void RunAndExit()
        {
            List<string> issues = Validate();
            foreach (string issue in issues)
            {
                Debug.LogError("[FlappyCopy] " + issue);
            }

            Debug.Log("[FlappyCopy] Validation finished, issues: " + issues.Count);
            EditorApplication.Exit(issues.Count == 0 ? 0 : 1);
        }

        /// <summary>Returns every problem found; an empty list means the project is healthy.</summary>
        public static List<string> Validate()
        {
            var issues = new List<string>();
            Core.LayersBootstrapper.EnsureAll();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectPaths.ScenePath) == null)
            {
                issues.Add("missing scene " + ProjectPaths.ScenePath);
                return issues;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectPaths.ScenePath);

            var game = UnityEngine.Object.FindAnyObjectByType<Core.GameController>();
            if (game == null)
            {
                issues.Add("scene has no GameController");
                return issues;
            }

            RequireRef(issues, game, "playerSettings", "GameController.playerSettings");
            RequireRef(issues, game, "pipeSettings", "GameController.pipeSettings");
            RequireRef(issues, game, "worldSettings", "GameController.worldSettings");
            RequireRef(issues, game, "cameraFraming", "GameController.cameraFraming");
            RequireRef(issues, game, "bird", "GameController.bird");
            RequireRef(issues, game, "pipeSpawner", "GameController.pipeSpawner");
            RequireRef(issues, game, "ground", "GameController.ground");

            var hud = UnityEngine.Object.FindAnyObjectByType<UI.HudController>();
            if (hud == null)
            {
                issues.Add("scene has no HudController");
            }
            else
            {
                RequireRef(issues, hud, "game", "HudController.game");
                RequireRef(issues, hud, "view", "HudController.view");
            }

            var spawner = UnityEngine.Object.FindAnyObjectByType<Core.GameController>()?.GetComponentInChildren<Pipes.PipeSpawner>();
            if (spawner != null)
            {
                RequireRef(issues, spawner, "pipePairPrefab", "PipeSpawner.pipePairPrefab");
                RequireRef(issues, spawner, "game", "PipeSpawner.game");
            }

            issues.AddRange(ValidatePrefabs());
            issues.AddRange(ValidateProjectSettings());
            return issues;
        }

        private static IEnumerable<string> ValidatePrefabs()
        {
            var issues = new List<string>();
            var bird = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.BirdPrefabPath);
            if (bird == null)
            {
                issues.Add("missing bird prefab");
            }
            else
            {
                if (bird.GetComponent<Core.CollisionRelay>() == null)
                {
                    issues.Add("Bird prefab lost its CollisionRelay (rebuild the prefabs)");
                }

                if (bird.GetComponent<Player.BirdController>() == null)
                {
                    issues.Add("Bird prefab lost its BirdController");
                }
            }

            var pipe = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PipePairPrefabPath);
            if (pipe == null)
            {
                issues.Add("missing pipe prefab");
            }
            else
            {
                Transform top = pipe.transform.Find("TopHitbox");
                if (top == null || top.GetComponent<BoxCollider2D>() == null)
                {
                    issues.Add("Pipe prefab lost its top hitbox (rebuild the prefabs)");
                }
            }

            return issues;
        }

        private static IEnumerable<string> ValidateProjectSettings()
        {
            var issues = new List<string>();
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets != null && assets.Length > 0)
            {
                var serialized = new SerializedObject(assets[0]);
                SerializedProperty handler = serialized.FindProperty("activeInputHandler");
                if (handler != null && handler.intValue != 0)
                {
                    issues.Add("active Input handling must be the legacy Input Manager (Player Setup > Apply)");
                }
            }

            int enabled = 0;
            bool gameScene = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabled++;
                    gameScene |= scene.path == ProjectPaths.ScenePath;
                }
            }

            if (!gameScene || enabled != 1)
            {
                issues.Add("build settings must contain the game scene only");
            }

            PlayerPrefs.Save();
            return issues;
        }

        private static void RequireRef(List<string> issues, UnityEngine.Object target, string field, string label)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue == null)
            {
                issues.Add(label + " is not wired");
            }
        }
    }
}
