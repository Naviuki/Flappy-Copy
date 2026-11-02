using System.Collections.Generic;
using FlappyCopy.Core;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Sanity pass over the generated scene. Feature checks (reflection over the test fixtures) live in
    /// BehaveProjectTests; this one checks what a renderer sees: every sprite that should be on screen is
    /// actually drawn, and repeated layout calls (frames, windows) never move or resize anything.
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
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllLines(ResultsPath, issues);
            EditorApplication.Exit(issues.Count == 0 ? 0 : 1);
        }

        /// <summary>Returns the list of problems found in the generated scene (empty means healthy).</summary>
        public static List<string> Validate()
        {
            var issues = new List<string>();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectPaths.ScenePath);

            var game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            if (game == null)
            {
                issues.Add("the scene has no GameController");
                return issues;
            }

            // Layout stability: the ground must be laid out the same way every time (regression guard for a
            // layout that doubled the strip width on every call).
            var ground = UnityEngine.Object.FindAnyObjectByType<World.InfinityGround>();
            if (ground != null)
            {
                ground.ApplyLayout();
                Vector3[] before = new Vector3[ground.transform.childCount];
                for (int i = 0; i < before.Length; i++)
                {
                    before[i] = ground.transform.GetChild(i).localPosition;
                }

                ground.ApplyLayout();
                ground.ApplyLayout();
                for (int i = 0; i < before.Length; i++)
                {
                    Vector3 after = ground.transform.GetChild(i).localPosition;
                    if ((after - before[i]).sqrMagnitude > 0.0001f)
                    {
                        issues.Add("the ground strips move every time they are laid out (" + ground.transform.GetChild(i).name + ")");
                    }
                }
            }

            foreach (string issue in issues)
            {
                Debug.Log("[FlappyCopy] " + issue);
            }

            // Every generated sprite must use the game's material: a missing material reference makes a sprite
            // fall back to a default that ignores its transform scale in an edit-mode render.
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
            {
                if (renderer.sharedMaterial == null)
                {
                    issues.Add("sprite " + renderer.name + " has no material (rebuild the textures and the scene)");
                }
            }

            Debug.Log("[FlappyCopy] Validation finished, issues: " + issues.Count);
            return issues;
        }
    }
}
