using System.IO;
using System.Reflection;
using FlappyCopy.Core;
using FlappyCopy.EditorTools;
using FlappyCopy.Player;
using FlappyCopy.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Flow checks on the real game scene: the scene boots into the menu, the HUD shows the menu panel, a bird
    /// without taps falls and its ground contact counts as a crash, and the best score persists through
    /// PlayerPrefs. The fixture opens the scene itself, so its order in the suite does not matter.
    /// </summary>
    public sealed class GameFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static object Get(object target, string field) => target?.GetType().GetField(field, Private)?.GetValue(target);

        private static void OpenGameScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ProjectPaths.ScenePath)
            {
                EditorSceneManager.OpenScene(ProjectPaths.ScenePath);
            }
        }

        [Test]
        public static void SceneBootsIntoTheMenu()
        {
            OpenGameScene();
            var game = UnityEngine.Object.FindAnyObjectByType<GameController>();
            var hud = UnityEngine.Object.FindAnyObjectByType<HudController>();
            Check.True(game != null, "GameController in the game scene");
            Check.True(hud != null, "HudController in the game scene");
            if (game == null || hud == null)
            {
                return;
            }

            game.SendMessage("Awake");
            game.SendMessage("Start");
            hud.SendMessage("Awake");
            hud.SendMessage("Start");

            Check.Equal(GameState.Menu, game.State, "boots in the menu");
            Check.True(game.Score != null, "score service ready");
        }

        [Test]
        public static void HudShowsTheMenuPanel()
        {
            OpenGameScene();
            var hud = UnityEngine.Object.FindAnyObjectByType<HudController>();
            Check.True(hud != null, "HudController present");
            if (hud == null)
            {
                return;
            }

            var view = Get(hud, "view") as HudView;
            Check.True(view != null, "view wired to the HUD controller");
            if (view == null)
            {
                return;
            }

            hud.SendMessage("Awake");
            hud.SendMessage("Start");
            var menu = Get(view, "menuPanel") as GameObject;
            Check.True(menu != null, "menu panel exists");
            Check.True(menu != null && menu.activeInHierarchy, "menu panel visible in the menu state");
        }

        [Test]
        public static void BirdFallsAndGroundCountsAsCrash()
        {
            OpenGameScene();
            var bird = UnityEngine.Object.FindAnyObjectByType<BirdController>();
            Check.True(bird != null, "bird in the game scene");
            if (bird == null)
            {
                return;
            }

            Check.True(Get(bird, "settings") != null, "player settings wired to the bird");
            bird.SendMessage("Awake");
            bird.ResetToStart();
            float startY = bird.transform.position.y;
            for (int frame = 0; frame < 240; frame++)
            {
                bird.Tick(1f / 60f, false);
            }

            Check.True(bird.transform.position.y < startY - 1f, "the bird falls without a flap");
            bird.OnCollisionEntered(null, ProjectLayers.GroundName, Vector2.zero);
            Check.True(bird.HasCrashed, "the ground contact is a crash");
        }

        [Test]
        public static void BestScoreUsesPlayerPrefs()
        {
            string key = GameConstants.BestScorePrefKey + ".TestProbe";
            var storage = new Score.PlayerPrefsScoreStorage(key);
            storage.SaveBestScore(7);
            Check.Equal(7, new Score.PlayerPrefsScoreStorage(key).LoadBestScore(), "best score round trips");
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
