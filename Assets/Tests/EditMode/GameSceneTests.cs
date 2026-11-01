using FlappyCopy.Audio;
using FlappyCopy.Core;
using FlappyCopy.Data;
using FlappyCopy.EditorTools;
using FlappyCopy.Input;
using FlappyCopy.Pipes;
using FlappyCopy.Player;
using FlappyCopy.Pooling;
using FlappyCopy.Score;
using FlappyCopy.Testing;
using FlappyCopy.UI;
using NUnit.Framework;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Runtime scene checks (the same five assertions the self-test player runs through <see cref="SelfTestChecks"/>)
    /// and the assets the game loads: settings, prefabs, audio, input and the object pool. All read-only, so they
    /// can run in any order on the game scene.
    /// </summary>
    public static class GameSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string BirdPrefab = "Assets/Prefabs/Bird.prefab";
        private const string PipePrefab = "Assets/Prefabs/PipePair.prefab";

        [Test]
        public static void SceneChecksAreRegistered()
        {
            string[] checks =
            {
                nameof(SelfTestChecks.GameControllerHasEveryReference),
                nameof(SelfTestChecks.HudIsWiredToTheGame),
                nameof(SelfTestChecks.BirdPrefabIsTriggerOnThePlayerLayer),
                nameof(SelfTestChecks.PipePairPrefabIsTriggerOnThePipeLayer),
                nameof(SelfTestChecks.LayersSortingLayersAndCollisions)
            };

            foreach (string check in checks)
            {
                Check.True(typeof(SelfTestChecks).GetMethod(check,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null,
                    "self-test player check " + check);
            }
        }

        [Test]
        public static void SettingsAssetsExist()
        {
            Check.True(UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerSettings>(FlappyData.PlayerSettingsPath) != null,
                "player settings asset");
            Check.True(UnityEditor.AssetDatabase.LoadAssetAtPath<PipeSettings>(FlappyData.PipeSettingsPath) != null,
                "pipe settings asset");
            Check.True(UnityEditor.AssetDatabase.LoadAssetAtPath<WorldSettings>(FlappyData.WorldSettingsPath) != null,
                "world settings asset");
        }

        [Test]
        public static void SceneAndPrefabsExist()
        {
            Check.True(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(ScenePath) != null,
                "game scene asset");
            Check.True(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BirdPrefab) != null, "bird prefab asset");
            Check.True(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PipePrefab) != null, "pipe prefab asset");
        }

        [Test]
        public static void PrefabsCarryTheGameScripts()
        {
            string bird = System.IO.File.ReadAllText(BirdPrefab);
            Check.True(bird.Contains(nameof(BirdController)), "bird prefab references the bird controller");
            string pipe = System.IO.File.ReadAllText(PipePrefab);
            Check.True(pipe.Contains(nameof(PipePair)), "pipe prefab references the pipe pair");
            Check.True(pipe.Contains(nameof(PipePassGate)), "pipe prefab references the score gate");
        }

        [Test]
        public static void AudioAssetsExist()
        {
            foreach (SfxId id in new[] { SfxId.Flap, SfxId.Point, SfxId.Hit, SfxId.Ground })
            {
                string path = "Assets/Resources/Sfx" + id + ".wav";
                Check.True(System.IO.File.Exists(path), "audio clip " + path);
            }
        }

        [Test]
        public static void InputIsTheLegacyManager()
        {
            IInputSource source = InputSampleSource.CreateDefault();
            Check.True(source is UnityInputSource, "the game reads the legacy Input Manager");
        }

        [Test]
        public static void PoolItemIsRecycledNotRecreated()
        {
            var factory = new TestPoolFactory();
            var pool = new ObjectPool<TestPoolItem>(factory, 4, null);
            TestPoolItem item = pool.Get();
            pool.Release(item);
            TestPoolItem again = pool.Get();
            Check.True(ReferenceEquals(item, again), "the pool hands the same instance back");
            Check.Equal(4, factory.Created, "no new instance while the pool has one available");
        }

        [Test]
        public static void SoundPreferenceDefaultsToOn()
        {
            var storage = new MemorySoundPreference();
            Check.True(storage.LoadEnabled(), "sound is on by default");
        }
    }
}
