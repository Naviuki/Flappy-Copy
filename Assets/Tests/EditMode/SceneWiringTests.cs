using System.Reflection;
using FlappyCopy.Core;
using NUnit.Framework;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Scene wiring: every reference the game needs at runtime is connected in the saved scene, and the prefabs
    /// have the components the game looks for. EditMode tests through Unity's Test Runner (which runs on the
    /// main thread with a fresh inspector context), so prefab and scene access behave like at runtime.
    /// </summary>
    public sealed class SceneWiringTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static T Get<T>(object target, string field) where T : class
        {
            FieldInfo info = target?.GetType().GetField(field, Private);
            Check.True(info != null, "field " + field + " exists on " + target?.GetType().Name);
            return info?.GetValue(target) as T;
        }

        [Test]
        public static void GameControllerHasEveryReference()
        {
            var game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            Check.True(game != null, "GameController present in the loaded scene");
            if (game == null)
            {
                return;
            }

            Check.True(Get<Data.PlayerSettings>(game, "playerSettings") != null, "playerSettings wired");
            Check.True(Get<Data.WorldSettings>(game, "worldSettings") != null, "worldSettings wired");
            Check.True(Get<Data.PipeSettings>(game, "pipeSettings") != null, "pipeSettings wired");
            Check.True(Get<Cameras.PortraitFraming>(game, "cameraFraming") != null, "camera framing wired");
            Check.True(Get<Player.BirdController>(game, "bird") != null, "bird wired");
            Check.True(Get<Pipes.PipeSpawner>(game, "pipeSpawner") != null, "pipe spawner wired");
            Check.True(Get<World.InfinityGround>(game, "ground") != null, "ground wired");
        }

        [Test]
        public static void HudIsWiredToTheGame()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<UI.HudController>();
            Check.True(hud != null, "HudController present");
            if (hud == null)
            {
                return;
            }

            Check.True(Get<GameController>(hud, "game") != null, "game wired");
            var view = Get<UI.HudView>(hud, "view");
            Check.True(view != null, "HudView wired");
            Check.True(view != null && Get<TMPro.TMP_Text>(view, "scoreText") != null, "score text");
            Check.True(view != null && Get<GameObject>(view, "menuPanel") != null, "menu panel");
            Check.True(view != null && Get<UnityEngine.UI.Button>(view, "restartButton") != null, "restart button");
        }

        [Test]
        public static void BirdPrefabIsComplete()
        {
            var bird = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EditorTools.ProjectPaths.BirdPrefabPath);
            Check.True(bird != null, "bird prefab exists");
            if (bird == null)
            {
                return;
            }

            Check.Equal(ProjectLayers.Player, bird.layer, "bird layer");
            Check.True(bird.GetComponent<CollisionRelay>() != null, "collision relay");
            Check.True(bird.GetComponent<Player.BirdController>() != null, "bird controller");
            Check.True(bird.GetComponent<UnityEngine.Rigidbody2D>() != null, "rigidbody");
        }

        [Test]
        public static void PipePairPrefabIsComplete()
        {
            var pipe = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EditorTools.ProjectPaths.PipePairPrefabPath);
            Check.True(pipe != null, "pipe prefab exists");
            if (pipe == null)
            {
                return;
            }

            Check.Equal(ProjectLayers.Pipe, pipe.layer, "pipe layer");
            var pair = pipe.GetComponent<Pipes.PipePair>();
            Check.True(pair != null, "pipe pair component");
            Check.True(pair != null && Get<Pipes.PipePassGate>(pair, "gate") != null, "score gate");

            Transform top = pipe.transform.Find("TopHitbox");
            Check.True(top != null, "top hitbox");
            Check.True(top != null && top.GetComponent<BoxCollider2D>() != null, "top hitbox collider");
        }

        [Test]
        public static void LayersAndSortingLayersExist()
        {
            foreach (string layer in ProjectLayers.LayerNames)
            {
                Check.True(LayerMask.NameToLayer(layer) >= 0, "layer " + layer);
            }

            foreach (string sortingLayer in SortingLayers.All)
            {
                Check.True(ProjectLayers.HasSortingLayer(sortingLayer), "sorting layer " + sortingLayer);
            }
        }
    }
}
