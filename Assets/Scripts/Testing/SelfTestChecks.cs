using System;
using System.Reflection;
using UnityEngine;

namespace FlappyCopy.Testing
{
    /// <summary>
    /// Runtime checks of the built scene for the self-test player (compiled from the same assertions the
    /// EditMode scene tests use, with the small "check" helpers kept local).
    /// </summary>
    public static class SelfTestChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private sealed class Expect : Exception
        {
            public Expect(string message) : base(message)
            {
            }
        }

        private static void Check(bool condition, string what)
        {
            if (!condition)
            {
                throw new Expect("FAILED: " + what);
            }
        }

        private static T Field<T>(object target, string field) where T : class
        {
            FieldInfo info = target?.GetType().GetField(field, Private);
            Check(info != null, "field " + field + " on " + target?.GetType().Name);
            return info?.GetValue(target) as T;
        }

        private static GameObject Prefab(string name)
        {
            return Resources.Load<GameObject>("CheckPrefabs/" + name);
        }

        public static void GameControllerHasEveryReference()
        {
            var game = UnityEngine.Object.FindAnyObjectByType<Core.GameController>();
            Check(game != null, "GameController present");
            if (game == null)
            {
                return;
            }

            Check(Field<Data.PlayerSettings>(game, "playerSettings") != null, "playerSettings wired");
            Check(Field<Data.PipeSettings>(game, "pipeSettings") != null, "pipeSettings wired");
            Check(Field<Data.WorldSettings>(game, "worldSettings") != null, "worldSettings wired");
            Check(Field<Cameras.PortraitFraming>(game, "cameraFraming") != null, "camera framing wired");
            Check(Field<Player.BirdController>(game, "bird") != null, "bird wired");
            Check(Field<Pipes.PipeSpawner>(game, "pipeSpawner") != null, "pipe spawner wired");
            Check(Field<World.InfinityGround>(game, "ground") != null, "ground wired");
            Check(Field<Audio.AudioService>(game, "audioService") != null, "audio service wired");
        }

        public static void HudIsWiredToTheGame()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<UI.HudController>();
            Check(hud != null, "HudController present");
            if (hud == null)
            {
                return;
            }

            Check(Field<Core.GameController>(hud, "game") != null, "game wired");
            var view = Field<UI.HudView>(hud, "view");
            Check(view != null, "HudView wired");
            if (view == null)
            {
                return;
            }

            Check(Field<TMPro.TMP_Text>(view, "scoreText") != null, "score text");
            Check(Field<GameObject>(view, "menuPanel") != null, "menu panel");
            Check(Field<GameObject>(view, "gameOverPanel") != null, "game over panel");
            Check(Field<UnityEngine.UI.Button>(view, "restartButton") != null, "restart button");
        }

        public static void BirdPrefabIsTriggerOnThePlayerLayer()
        {
            var bird = Prefab("Bird");
            Check(bird != null, "bird prefab loaded");
            if (bird == null)
            {
                return;
            }

            Check(bird.layer == Core.ProjectLayers.Player, "bird layer");
            Check(bird.GetComponent<Core.CollisionRelay>() != null, "collision relay");
            Check(bird.GetComponent<Player.BirdController>() != null, "bird controller");
            Collider2D collider = bird.GetComponent<Collider2D>();
            Check(collider != null && collider.isTrigger, "bird collider is a trigger on the root (the relay listens there)");
        }

        public static void PipePairPrefabIsTriggerOnThePipeLayer()
        {
            var pipe = Prefab("PipePair");
            Check(pipe != null, "pipe prefab loaded");
            if (pipe == null)
            {
                return;
            }

            Check(pipe.layer == Core.ProjectLayers.Pipe, "pipe layer");
            Transform top = pipe.transform.Find("TopHitbox");
            Transform bottom = pipe.transform.Find("BottomHitbox");
            Check(top != null && top.GetComponent<BoxCollider2D>() != null && top.GetComponent<BoxCollider2D>().isTrigger,
                "top hitbox trigger");
            Check(bottom != null && bottom.GetComponent<BoxCollider2D>() != null
                  && bottom.GetComponent<BoxCollider2D>().isTrigger, "bottom hitbox trigger");
        }

        public static void LayersSortingLayersAndCollisions()
        {
            foreach (string layer in Core.ProjectLayers.LayerNames)
            {
                Check(LayerMask.NameToLayer(layer) >= 0, "layer " + layer);
            }

            foreach (string sortingLayer in Core.SortingLayers.All)
            {
                Check(Core.ProjectLayers.HasSortingLayer(sortingLayer), "sorting layer " + sortingLayer);
            }

            Check(Core.ProjectLayers.IsCollisionEnabled(Core.ProjectLayers.Player, Core.ProjectLayers.Pipe,
                Core.ProjectLayers.Ground), "player collides with pipes and ground");
        }
    }
}
