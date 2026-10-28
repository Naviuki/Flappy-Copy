using System.IO;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Fails loudly on things that would otherwise degrade quietly: missing references in the scene,
    /// missing TMP resources, a leftover template scene, or a build list that does not point at the game.
    /// </summary>
    public static class ProjectSelfCheck
    {
        /// <summary>Returns the list of problems; empty means the project is healthy.</summary>
        public static System.Collections.Generic.List<string> Run()
        {
            var problems = new System.Collections.Generic.List<string>();

            if (!File.Exists("Assets/Scenes/Game.unity"))
            {
                problems.Add("Assets/Scenes/Game.unity is missing (run Flappy Copy > Rebuild Scene).");
            }

            if (!TmpEssentials.IsInstalled())
            {
                problems.Add("TextMeshPro essentials are missing (Assets/TextMesh Pro).");
            }

            if (File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                problems.Add("Template scene Assets/Scenes/SampleScene.unity still exists.");
            }

            if (File.Exists("Assets/InputSystem_Actions.inputactions"))
            {
                problems.Add("Unused template asset Assets/InputSystem_Actions.inputactions still exists.");
            }

            bool buildScene = false;
            foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes)
            {
                buildScene |= scene.enabled && scene.path == "Assets/Scenes/Game.unity";
            }

            if (!buildScene)
            {
                problems.Add("Assets/Scenes/Game.unity is not the enabled build scene.");
            }

            return problems;
        }

        [UnityEditor.MenuItem("Flappy Copy/Checks/Self Check")]
        public static void RunFromMenu()
        {
            System.Collections.Generic.List<string> problems = Run();
            foreach (string problem in problems)
            {
                Debug.LogWarning("[FlappyCopy] Self check: " + problem);
            }

            Debug.Log("[FlappyCopy] Self check: " + (problems.Count == 0 ? "OK" : problems.Count + " problem(s)"));
        }
    }
}
