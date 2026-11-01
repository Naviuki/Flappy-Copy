using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyCopy.UI
{
    public enum HudPanel
    {
        Menu = 0,
        Hud = 1,
        GameOver = 2
    }

    public interface IHudView
    {
        event Action RestartRequested;
        event Action SoundToggleRequested;

        void SetScore(int score);
        void SetBestScore(int bestScore);
        void ShowPanel(HudPanel panel);
        void ShowRunResult(int score, int bestScore, bool isNewRecord);
        void CloseResult();
    }

    /// <summary>
    /// Thin view over the TMP texts, the panels and the two buttons. Holds no rules: it renders what
    /// <see cref="HudController"/> tells it and raises one event per button.
    /// </summary>
    public sealed class HudView : MonoBehaviour, IHudView
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestMenuText;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private TMP_Text resultBestText;
        [SerializeField] private TMP_Text newRecordText;

        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundLabel;

        public event Action RestartRequested;
        public event Action SoundToggleRequested;

        private void OnEnable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }

            if (soundButton != null)
            {
                soundButton.onClick.AddListener(HandleSoundClicked);
            }
        }

        private void OnDisable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (soundButton != null)
            {
                soundButton.onClick.RemoveListener(HandleSoundClicked);
            }
        }

        public void SetScore(int score)
        {
            scoreText?.SetText("{0}", score);
        }

        public void SetBestScore(int bestScore)
        {
            bestMenuText?.SetText("BEST  {0}", bestScore);
            resultBestText?.SetText("BEST  {0}", bestScore);
        }

        public void SetSoundEnabled(bool enabled)
        {
            soundLabel?.SetText(enabled ? "SOUND  ON" : "SOUND  OFF");
        }

        public void ShowPanel(HudPanel panel)
        {
            SetActive(menuPanel, panel == HudPanel.Menu);
            SetActive(hudPanel, panel == HudPanel.Hud);
            SetActive(gameOverPanel, panel == HudPanel.GameOver);
        }

        public void ShowRunResult(int score, int bestScore, bool isNewRecord)
        {
            resultScoreText?.SetText("{0}", score);
            SetBestScore(bestScore);
            SetActive(newRecordText != null ? newRecordText.gameObject : null, isNewRecord);
            ShowPanel(HudPanel.GameOver);
        }

        public void CloseResult()
        {
            SetActive(newRecordText != null ? newRecordText.gameObject : null, false);
        }

        private void HandleRestartClicked() => RestartRequested?.Invoke();

        private void HandleSoundClicked() => SoundToggleRequested?.Invoke();

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
