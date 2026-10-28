using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Integrity checks of the serialized game assets: every script GUID used by the scene and the prefabs must
    /// exist on disk, and the components the game needs must be declared in the assets. This catches assets
    /// written against a stale script import (which is what a player reports as "the referenced script is
    /// missing") and components silently dropped by an editor session with old scripts.
    /// </summary>
    public sealed class StaleGuidTests
    {
        private static readonly Regex ScriptLine = new Regex(@"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})", RegexOptions.Compiled);

        [NUnit.Framework.Test]
        public static void SceneUsesOnlyExistingScripts()
        {
            var guids = new HashSet<string>();
            foreach (Match match in ScriptLine.Matches(File.ReadAllText("Assets/Scenes/Game.unity")))
            {
                guids.Add(match.Groups[1].Value);
            }

            Check.True(guids.Count >= 3, "scene has the game, HUD and layer components");
            foreach (string guid in guids)
            {
                Check.True(AssetDatabase.GUIDToAssetPath(guid).Length > 0, "scene script guid " + guid + " exists");
            }
        }

        [NUnit.Framework.Test]
        public static void PrefabsUseOnlyExistingScripts()
        {
            string[] prefabs = Directory.GetFiles("Assets/Prefabs", "*.prefab");
            Check.True(prefabs.Length >= 2, "bird and pipe prefabs present");
            foreach (string prefab in prefabs)
            {
                foreach (Match match in ScriptLine.Matches(File.ReadAllText(prefab)))
                {
                    string guid = match.Groups[1].Value;
                    Check.True(AssetDatabase.GUIDToAssetPath(guid).Length > 0, prefab + ": script " + guid + " exists");
                }
            }
        }

        [NUnit.Framework.Test]
        public static void SceneDeclaresTheGameComponents()
        {
            string scene = File.ReadAllText("Assets/Scenes/Game.unity");
            Check.True(scene.Contains("FlappyCopy.Core.GameController"), "scene has the GameController component");
            Check.True(scene.Contains("FlappyCopy.UI.HudController"), "scene has the HUD controller");
            Check.True(scene.Contains("FlappyCopy.UI.HudView"), "scene has the HUD view");
        }

        [NUnit.Framework.Test]
        public static void PrefabsDeclareTheirComponents()
        {
            string bird = File.ReadAllText("Assets/Prefabs/Bird.prefab");
            Check.True(bird.Contains("FlappyCopy.Player.BirdController"), "bird has the BirdController");
            Check.True(bird.Contains("FlappyCopy.Core.CollisionRelay"), "bird has a collision relay");
            Check.True(bird.Contains("settings:"), "bird has its settings reference");

            string pipe = File.ReadAllText("Assets/Prefabs/PipePair.prefab");
            Check.True(pipe.Contains("FlappyCopy.Pipes.PipePair"), "pipe pair has the PipePair component");
            Check.True(pipe.Contains("FlappyCopy.Pipes.PipePassGate"), "pipe pair has the score gate");
            Check.True(pipe.Contains("FlappyCopy.Core.CollisionRelay"), "pipe pair has a collision relay");
        }
    }
}
