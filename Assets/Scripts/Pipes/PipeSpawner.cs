using FlappyCopy.Cameras;
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

        /// <summary>How far left of the frame pipes keep being laid out (a little more than one spacing).</summary>
        private const float LeftReach = 7f;

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

        /// <summary>Every pipe pair the bird has fully cleared, as seen by the scoring sensor (debug view).</summary>
        private event System.Action<PipePair> PairPassed;

        /// <summary>Number of passes the sensor delivered; a tool can show it (the score itself is the real result).</summary>
        public int PassCount { get; private set; }

        private PipePassSensor sensor;

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

            // The sensor is created on first use (it needs the bird, a sibling object): scoring always runs.
            if (Sensor() == null)
            {
                Debug.LogWarning("PipeSpawner has no bird to score against; pipe passes will not count.");
            }
        }

        private void Update()
        {
            if (ViewMetrics.Width != lastScreenWidth || ViewMetrics.Height != lastScreenHeight)
            {
                EnsureScreenCoverage();
            }

            // Scoring is position based: the run is only live while the game is playing.
            if (game != null && game.IsPlaying)
            {
                Sensor()?.Sample();
            }
        }

        private void HandlePairPassed(PipePair pair)
        {
            PassCount++;
            game?.AddPoint();
            PairPassed?.Invoke(pair);
        }

        /// <summary>Back to the start layout: every pair on its slot, the first one with the bird's start height.</summary>
        public void ResetRun()
        {
            if (run == null)
            {
                return;
            }

            run.Reset();
            int score = game != null && game.Score != null ? game.Score.Score : 0;
            float firstCenter = pipeSettings != null ? pipeSettings.GapCenterRange.x : 0f;
            lastCenter = firstCenter;
            for (int i = 0; i < run.Count; i++)
            {
                IPipePair pair = run.GetPair(i);
                float x = run.XOf(i);

                // The first pair the bird meets is in the way of its start height, so a run always begins with
                // a gap the bird can reach without moving; the pairs behind it are spread over the play area.
                float center = i == 0 ? firstCenter : PlaceNextCenter(game != null ? game.GetGap(score) : run.Config.Gap);
                pair.MoveTo(x);
                pair.Activate(x, game != null ? game.GetGap(score) : run.Config.Gap, center, run.Random);
            }
        }

        /// <summary>Moves the pipes left; recycled pairs get a gap from the current score.</summary>
        public void Tick(float deltaTime, float baseSpeed, int score)
        {
            if (run == null)
            {
                return;
            }

            // The run is laid out in a fixed box; scrolling without limits would walk it out of the play
            // area, so the scroll distance is wrapped and pairs that left the play area are recycled.
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

        /// <summary>
        /// Scoring sensor of the run, created on first use: it needs the bird, which is a sibling object, so
        /// the Awake order may not have provided it yet.
        /// </summary>
        private PipePassSensor Sensor()
        {
            if (sensor != null || game == null || game.Bird == null)
            {
                return sensor;
            }

            sensor = new PipePassSensor(this, game.Bird.transform);
            sensor.PairPassed += HandlePairPassed;
            return sensor;
        }

        private PipeRunConfig BuildConfig(int size)
        {
            float width = pipeSettings != null ? pipeSettings.PipeWidth : 1.5f;
            float spacing = worldSettings != null ? worldSettings.PipeSpacing : 5.2f;
            float offset = worldSettings != null ? worldSettings.SpawnOffsetFromRightEdge : 1.4f;
            return new PipeRunConfig(spacing, width, PlayHalfWidth(), offset, size,
                pipeSettings != null ? pipeSettings.BaseGap : 4f);
        }

        /// <summary>
        /// Half-width of the play area: the 9:16 design width, or more when a wide window reveals more world.
        /// Pipes are laid out and recycled across exactly this width, so a pair never pops inside the view.
        /// </summary>
        public float PlayHalfWidth()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return DesignSpace.HalfWidth;
            }

            return Cameras.PortraitFraming.PlayHalfWidth(cam.orthographicSize, cam.aspect);
        }

        /// <summary>Grows the pool only (never shrinks), so the widest window shown stays covered.</summary>
        private void EnsureScreenCoverage()
        {
            if (run == null || worldSettings == null)
            {
                return;
            }

            lastScreenWidth = ViewMetrics.Width;
            lastScreenHeight = ViewMetrics.Height;
            int needed = Mathf.Max(prewarmCount,
                PipeRunMath.SlotsFor(PlayHalfWidth(), worldSettings.PipeSpacing, worldSettings.SpawnOffsetFromRightEdge));
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
            return PlayHalfWidth();
        }
    }
}
