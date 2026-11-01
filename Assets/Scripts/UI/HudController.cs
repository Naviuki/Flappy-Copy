using System;
using FlappyCopy.Core;
using FlappyCopy.Score;
using UnityEngine;

namespace FlappyCopy.UI
{
    public static class HudFlow
    {
        public static HudPanel PanelFor(GameState state)
        {
            return state switch
            {
                GameState.Menu => HudPanel.Menu,
                GameState.Playing => HudPanel.Hud,
                _ => HudPanel.GameOver
            };
        }

        public static bool IsScoreVisible(GameState state) => state == GameState.Playing || state == GameState.GameOver;

        public static bool IsRestartVisible(GameState state) => state == GameState.GameOver;
    }

    /// <summary>
    /// Subscribes the HUD to game events and decides which panel is visible. Presentation only: the
    /// score lives in <see cref="IScoreService"/>, the restart goes back through the game as a press.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private HudView view;
        [SerializeField] private GameController game;

        private bool isSubscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Subscribes once, as soon as both references are there (the component is enabled before Unity calls
        /// Awake, so the subscription is set up here, not in Awake, and survives being enabled again).
        /// </summary>
        private void Subscribe()
        {
            if (isSubscribed || view == null || game == null)
            {
                return;
            }

            game.StateChanged += HandleStateChanged;
            game.PointScored += HandlePointScored;
            game.RunEnded += HandleRunEnded;
            view.RestartRequested += HandleRestartRequested;
            view.SoundToggleRequested += HandleSoundToggleRequested;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            game.StateChanged -= HandleStateChanged;
            game.PointScored -= HandlePointScored;
            game.RunEnded -= HandleRunEnded;
            view.RestartRequested -= HandleRestartRequested;
            view.SoundToggleRequested -= HandleSoundToggleRequested;
            isSubscribed = false;
        }

        private void Start()
        {
            if (view == null || game == null)
            {
                Debug.LogError("HudController needs both a HudView and the GameController.", this);
                enabled = false;
                return;
            }

            Subscribe();
            view.SetBestScore(game.Score.BestScore);
            view.SetScore(0);
            view.SetSoundEnabled(!game.IsMuted);
            ShowFor(game.State);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            CloseResult();
            ShowFor(to);
            if (to == GameState.Playing)
            {
                view.SetScore(0);
            }
        }

        private void HandlePointScored(int score)
        {
            view.SetScore(score);
        }

        private void HandleRunEnded(int score, bool isNewRecord)
        {
            view.ShowRunResult(score, game.Score.BestScore, isNewRecord);
        }

        private void HandleRestartRequested()
        {
            game.RequestPress();
        }

        private void HandleSoundToggleRequested()
        {
            game.ToggleMute();
            view.SetSoundEnabled(!game.IsMuted);
        }

        private void ShowFor(GameState state)
        {
            view.ShowPanel(HudFlow.PanelFor(state));
        }

        private void CloseResult()
        {
            view.CloseResult();
        }
    }
}
