using System;
using FlappyCopy.Core;

namespace FlappyCopy.Score
{
    /// <summary>Persistence for the best score. Implementations may use PlayerPrefs, disk or memory.</summary>
    public interface IScoreStorage
    {
        int LoadBestScore();
        void SaveBestScore(int bestScore);
    }

    /// <summary>Scoring contract used by the gameplay and by the UI.</summary>
    public interface IScoreService
    {
        int Score { get; }
        int BestScore { get; }
        bool IsNewRecord { get; }

        event Action<int> ScoreChanged;
        event Action<int, bool> RunEnded;

        void AddPoint();
        void ResetRun();
        void EndRun();
    }

    /// <summary>
    /// Pure scoring model: current run score, persisted best score and the new-record flag.
    /// Raises <see cref="ScoreChanged"/> per point and <see cref="RunEnded"/> once per run.
    /// </summary>
    public sealed class ScoreService : IScoreService
    {
        private readonly IScoreStorage storage;
        private int bestScore;

        public ScoreService(IScoreStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            bestScore = Math.Max(0, this.storage.LoadBestScore());
        }

        public event Action<int> ScoreChanged;
        public event Action<int, bool> RunEnded;

        public int Score { get; private set; }
        public int BestScore => bestScore;
        public bool IsNewRecord { get; private set; }

        public void AddPoint()
        {
            Score++;
            ScoreChanged?.Invoke(Score);
        }

        public void ResetRun()
        {
            Score = 0;
            IsNewRecord = false;
            ScoreChanged?.Invoke(Score);
        }

        public void EndRun()
        {
            IsNewRecord = Score > bestScore;
            if (IsNewRecord)
            {
                bestScore = Score;
                storage.SaveBestScore(bestScore);
            }

            RunEnded?.Invoke(Score, IsNewRecord);
        }
    }

    /// <summary>Stores the best score in PlayerPrefs under a single documented key.</summary>
    public sealed class PlayerPrefsScoreStorage : IScoreStorage
    {
        private readonly string key;

        public PlayerPrefsScoreStorage(string key)
        {
            this.key = string.IsNullOrEmpty(key) ? GameConstants.BestScorePrefKey : key;
        }

        public int LoadBestScore()
        {
            return UnityEngine.PlayerPrefs.GetInt(key, 0);
        }

        public void SaveBestScore(int bestScore)
        {
            UnityEngine.PlayerPrefs.SetInt(key, bestScore);
            UnityEngine.PlayerPrefs.Save();
        }
    }

    /// <summary>In-memory storage for tests and for the editor build script.</summary>
    public sealed class MemoryScoreStorage : IScoreStorage
    {
        public int Value;
        public int SaveCount;

        public MemoryScoreStorage(int initialValue = 0)
        {
            Value = initialValue;
        }

        public int LoadBestScore()
        {
            return Value;
        }

        public void SaveBestScore(int bestScore)
        {
            Value = bestScore;
            SaveCount++;
        }
    }
}
