using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FlappyCopy.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Serialization audit of the game assets: every [SerializeField] of each component script must be
    /// present in the saved prefab. A missing field means the prefab was saved before the script changed,
    /// which makes Unity load an incomplete component (and a player logs "the referenced script is missing").
    /// Also checks the .meta files of the generated assets and the build scene list.
    /// </summary>
    public sealed class AssetSerializationTests
    {
        private static readonly string[] PrefabPaths =
        {
            ProjectPaths.BirdPrefabPath,
            ProjectPaths.PipePairPrefabPath
        };

        [Test]
        public static void PrefabsHaveAllSerializedFields()
        {
            var scripts = new Dictionary<string, Type>();
            foreach (Type type in typeof(AssetSerializationTests).Assembly.GetTypes())
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    scripts[type.Name] = type;
                }
            }

            foreach (string path in PrefabPaths)
            {
                string yaml = File.ReadAllText(path);
                foreach (KeyValuePair<string, Type> script in scripts)
                {
                    if (!yaml.Contains(". " + script.Key) && !yaml.Contains(": " + script.Key))
                    {
                        continue;
                    }

                    foreach (FieldInfo field in script.Value.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        if (field.GetCustomAttributes(typeof(SerializeField), true).Length > 0)
                        {
                            Check.True(yaml.Contains(field.Name + ":"), path + ": " + script.Key + " is missing " + field.Name);
                        }
                    }
                }
            }
        }

        [Test]
        public static void GeneratedAssetsHaveMetaFiles()
        {
            string[] roots = { "Assets/Prefabs", "Assets/Data", "Assets/Scenes", "Assets/Resources", "Assets/Textures" };
            foreach (string root in roots)
            {
                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) && !File.Exists(file + ".meta"))
                    {
                        throw new Exception("missing .meta for " + file);
                    }
                }
            }
        }

        [Test]
        public static void BuildContainsTheGameSceneOnly()
        {
            int enabled = 0;
            bool found = false;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabled++;
                    found |= scene.path == ProjectPaths.ScenePath;
                }
            }

            Check.True(found, "game scene is in the build list");
            Check.Equal(1, enabled, "exactly one enabled build scene");
        }
    }
}
