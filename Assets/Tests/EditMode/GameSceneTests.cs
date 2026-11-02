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
using FlappyCopy.World;
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
        public static void SpritesAreOneUnitWideWithABottomLeftPivot()
        {
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/White.png");
            Check.True(sprite != null, "white sprite");
            Check.Near(1f, sprite.bounds.size.x, 0.001f, "a sprite is exactly one world unit wide (PPU 32, 32 px)");
            Check.Near(0f, sprite.pivot.x, 0.001f, "bottom-left pivot x");
            Check.Near(0f, sprite.pivot.y, 0.001f, "bottom-left pivot y");

            // Every sprite in the prefabs and the scene is scaled by its size in world units, which only works
            // with the convention above: a 1-unit sprite and a bottom-left pivot.
            GameObject bird = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BirdPrefab);
            Transform body = bird.transform.Find("Visual/Body");
            Check.True(body != null, "bird body");
            var renderer = body.GetComponent<SpriteRenderer>();
            Check.True(renderer != null && renderer.sprite == sprite, "the bird is drawn with the white sprite");
            Check.Near(0.5f, body.localScale.x, 0.001f, "bird body is 0.5 world units wide");
            Check.Near(1f, renderer.sprite.bounds.size.x, 0.001f, "one sprite unit before scaling");
        }

        [Test]
        public static void GroundStripsTileTheFrame()
        {
            // The scene stores the ground strips already scaled to the frame: the first strip starts one strip
            // left of the frame and the second at the left edge, so both edges are covered without a bare patch.
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            var ground = UnityEngine.Object.FindAnyObjectByType<InfinityGround>();
            Check.True(ground != null, "the scene has the endless ground");
            Check.Equal(2, ground.StripCount, "two ground strips");
            Check.Near(DesignSpace.PortraitWidth, ground.StripWidth, 0.001f, "a strip is a frame wide");
            Check.True(ground.transform.Find("Strip 0").localPosition.x <= -DesignSpace.HalfWidth,
                "the first strip starts left of the frame");
            Check.Near(0f, ground.transform.Find("Strip 1").localPosition.x, 0.001f,
                "the second strip starts at the left edge");
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
