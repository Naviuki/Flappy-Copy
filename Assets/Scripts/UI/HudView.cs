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

        void SetScore(int score);
        void SetBestScore(int bestScore);
        void ShowPanel(HudPanel panel);
        void ShowRunResult(int score, int bestScore, bool isNewRecord);
        void CloseResult();
    }

    /// <summary>
    /// Thin view over the TMP texts and panels. Holds no rules: it renders what
    /// <see cref="HudController"/> tells it and raises one restart event for the button.
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

        public event Action RestartRequested;

        private void OnEnable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }
        }

        private void OnDisable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.SetText("{0}", score);
            }
        }

        public void SetBestScore(int bestScore)
        {
            if (bestMenuText != null)
            {
                bestMenuText.SetText("BEST  {0}", bestScore);
            }

            if (resultBestText != null)
            {
                resultBestText.SetText("BEST  {0}", bestScore);
            }
        }

        public void ShowPanel(HudPanel panel)
        {
            SetActive(menuPanel, panel == HudPanel.Menu);
            SetActive(hudPanel, panel == HudPanel.Hud);
            SetActive(gameOverPanel, panel == HudPanel.GameOver);
        }

        public void ShowRunResult(int score, int bestScore, bool isNewRecord)
        {
            if (resultScoreText != null)
            {
                resultScoreText.SetText("{0}", score);
            }

            SetBestScore(bestScore);
            SetActive(newRecordText != null ? newRecordText.gameObject : null, isNewRecord);
            ShowPanel(HudPanel.GameOver);
        }

        public void CloseResult()
        {
            SetActive(newRecordText != null ? newRecordText.gameObject : null, false);
        }

        private void HandleRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null && target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }
    }
}
