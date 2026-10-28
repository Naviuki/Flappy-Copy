using FlappyCopy.Core;
using FlappyCopy.Data;
using FlappyCopy.Pooling;
using UnityEngine;

namespace FlappyCopy.Pipes
{
    /// <summary>
    /// Owns the pipe object pool and the run: pairs are created once (no Instantiate while playing),
    /// scrolled left, recycled when they leave the screen and re-rolled with a fresh gap.
    /// </summary>
    public sealed class PipeSpawner : MonoBehaviour, IPipePairFactory
    {
        [SerializeField] private PipePair pipePairPrefab;
        [SerializeField] private PipeSettings pipeSettings;
        [SerializeField] private WorldSettings worldSettings;
        [SerializeField] private GameController game;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private int prewarmCount = 6;

        private ObjectPool<PipePair> pool;
        private PipeRun run;
        private int poolSize;
        private float lastCenter;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        public int TotalPairs => pool?.TotalCount ?? 0;
        public int ActivePairs => run?.Count ?? 0;
        public PipeRun Run => run;
        public float ScreenRightEdge => CalculateRightEdge();

        private void Awake()
        {
            if (pipePairPrefab == null || pipeSettings == null || worldSettings == null || game == null)
            {
                Debug.LogError("PipeSpawner is missing a reference (prefab, settings or game).", this);
                enabled = false;
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            pool = new ObjectPool<PipePair>(new PrefabPoolFactory<PipePair>(pipePairPrefab, transform),
                Mathf.Max(1, prewarmCount), (pair, active) => pair.gameObject.SetActive(active));
            poolSize = Mathf.Max(1, prewarmCount);
            run = new PipeRun(this, BuildConfig(poolSize), seed: 20240612);
        }

        private void Start()
        {
            if (enabled && game != null)
            {
                EnsureScreenCoverage();
                ResetRun();
            }
            else if (enabled)
            {
                EnsureScreenCoverage();
            }
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                EnsureScreenCoverage();
            }
        }

        /// <summary>Puts every live pair back on its starting slot and clears the scroll distance.</summary>
        public void ResetRun()
        {
            if (run == null)
            {
                return;
            }

            run.Reset();
            lastCenter = 0f;
            for (int i = 0; i < run.Count; i++)
            {
                IPipePair pair = run.GetPair(i);
                float x = run.XOf(i);
                pair.MoveTo(x);
                pair.Activate(x, game.GetGap(0), 0f, run.Random);
            }
        }

        /// <summary>Moves the pipes left; recycled pairs get a gap from the current score.</summary>
        public void Tick(float deltaTime, float baseSpeed, int score)
        {
            if (run == null)
            {
                return;
            }

            float speed = PipeRunMath.Speed(baseSpeed, game.GetSpeedIncrease(score));
            run.Tick(deltaTime, speed, () => game.GetGap(score), PlaceNextCenter);
        }

        public IPipePair Spawn(float x, float gap, float centerY, System.Random random)
        {
            PipePair pair = pool.Get();
            pair.Activate(x, gap, centerY, random);
            return pair;
        }

        private float PlaceNextCenter(float gap)
        {
            lastCenter = pipeSettings.NextGapCenter(lastCenter, gap, run.Random);
            return lastCenter;
        }

        private PipeRunConfig BuildConfig(int size)
        {
            float width = pipeSettings != null ? pipeSettings.PipeWidth : 1.5f;
            float spacing = worldSettings != null ? worldSettings.PipeSpacing : 5.2f;
            float offset = worldSettings != null ? worldSettings.SpawnOffsetFromRightEdge : 1.4f;
            return new PipeRunConfig(spacing, width, ScreenRightEdge, offset, size,
                pipeSettings != null ? pipeSettings.BaseGap : 4f);
        }

        /// <summary>Grows the pool only (never shrinks), so the widest window shown stays covered.</summary>
        private void EnsureScreenCoverage()
        {
            if (run == null || worldSettings == null)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            int needed = Mathf.Max(prewarmCount,
                PipeRunMath.SlotsFor(ScreenRightEdge, worldSettings.PipeSpacing, worldSettings.SpawnOffsetFromRightEdge));
            if (needed <= poolSize)
            {
                return;
            }

            poolSize = needed;
            while (pool.TotalCount < needed)
            {
                PipePair spare = pool.Get();
                pool.Release(spare);
            }

            pool.ReleaseAll();
            run = new PipeRun(this, BuildConfig(needed), seed: 20240612);
            if (game != null)
            {
                ResetRun();
            }
        }

        private float CalculateRightEdge()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return DesignSpace.HalfWidth;
            }

            return Mathf.Max(DesignSpace.HalfWidth, cam.orthographicSize * cam.aspect);
        }
    }
}
