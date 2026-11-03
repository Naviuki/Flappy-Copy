using System;
using System.Collections.Generic;

namespace FlappyCopy.Pipes
{
    /// <summary>A spawned pipe pair as the pipe run logic sees it.</summary>
    public interface IPipePair
    {
        float X { get; }
        bool IsDisposed { get; }

        /// <summary>Positions the pair at <paramref name="x"/> and places the gap. False when disposed.</summary>
        bool Activate(float x, float gap, float centerY, Random random);

        /// <summary>Moves the pair horizontally (negative = left).</summary>
        void MoveTo(float x);
    }

    /// <summary>Factory that hides the pool (and allows fakes in tests).</summary>
    public interface IPipePairFactory
    {
        IPipePair Spawn(float x, float gap, float centerY, Random random);
    }

    /// <summary>Level geometry the pipe run needs, independent of ScriptableObject assets.</summary>
    public sealed class PipeRunConfig
    {
        public PipeRunConfig(float spacing, float width, float screenRightEdge, float spawnOffset, int poolSize, float gap)
        {
            Spacing = spacing;
            Width = width;
            ScreenRightEdge = screenRightEdge;
            SpawnOffset = spawnOffset;
            PoolSize = poolSize;
            Gap = gap;
        }

        public float Spacing { get; }
        public float Width { get; }
        public float ScreenRightEdge { get; }
        public float SpawnOffset { get; }
        public int PoolSize { get; }
        public float Gap { get; set; }

        public float HalfWidth => Width * 0.5f;
        public float CycleLength => Spacing * PoolSize;
    }

    /// <summary>
    /// Owns one run's worth of pipe pairs. Slot X positions are derived from an authoritative scroll distance
    /// that wraps after one spacing, so the run always covers the same lane: from just right of the play area
    /// to just left of it. Everything is deterministic, so it is unit testable without a scene.
    /// </summary>
    public sealed class PipeRun
    {
        /// <summary>Slots start one spacing right of the play area, so the first pair enters shortly after the start.</summary>
        private const float SpawnLead = 1f;

        private readonly IPipePair[] pairs;
        private readonly PipeRunConfig config;
        private float scroll;

        public PipeRun(IPipePairFactory factory, PipeRunConfig config, int? seed = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            this.config = config ?? throw new ArgumentNullException(nameof(config));
            if (config.PoolSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(config), "PoolSize must be positive.");
            }

            if (config.Spacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(config), "Spacing must be positive.");
            }

            if (config.ScreenRightEdge <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(config), "ScreenRightEdge must be positive.");
            }

            Random = seed.HasValue ? new Random(seed.Value) : new Random();
            pairs = new IPipePair[config.PoolSize];
            for (int i = 0; i < pairs.Length; i++)
            {
                // The first pair starts one spacing right of the play area and later slots follow to the left.
                pairs[i] = factory.Spawn(XOf(i), config.Gap, 0f, Random);
            }
        }

        public Random Random { get; }

        /// <summary>Scroll distance inside one spacing (always in [0, Spacing)).</summary>
        public float Scrolled => scroll;

        public float Scroll => scroll;

        public int Count => pairs.Length;

        public PipeRunConfig Config => config;

        public IPipePair GetPair(int index) => pairs[index];

        /// <summary>X of slot <paramref name="index"/> for the current scroll distance.</summary>
        public float XOf(int index)
        {
            return (config.ScreenRightEdge + (SpawnLead * config.Spacing)) - (index * config.Spacing) - scroll;
        }

        /// <summary>Sets the scroll (the spawner wraps it); the caller moves the pairs afterwards.</summary>
        public void SetScroll(float distance)
        {
            scroll = distance;
        }

        public void Reset()
        {
            scroll = 0f;
        }

        /// <summary>
        /// Advances the scroll, moves every pair and recycles pairs that fully left the play area.
        /// <paramref name="gapProvider"/> gives the current gap; <paramref name="placement"/> picks the
        /// new gap centre. Returns how many pairs were recycled this call.
        /// </summary>
        public int Tick(float deltaTime, float speed, Func<float> gapProvider, Func<float, float> placement)
        {
            if (deltaTime <= 0f)
            {
                return 0;
            }

            float scrolled = scroll + (speed * deltaTime);
            int wraps = (int)(scrolled / config.Spacing);
            scroll = scrolled - (wraps * config.Spacing);

            float gap = gapProvider != null ? gapProvider() : config.Gap;
            int recycled = 0;
            for (int i = 0; i < pairs.Length; i++)
            {
                recycled += MoveAndRecycle(i, gap, placement);
            }

            // Fast frames (or a wide window) can carry a pair past its slot: recycle from the front as well.
            for (int i = 0; i < wraps; i++)
            {
                recycled += RecycleLeftmost(gapProvider, placement);
            }

            return recycled;
        }

        /// <summary>Moves the pair at <paramref name="index"/> and recycles it once it is fully off-screen.</summary>
        private int MoveAndRecycle(int index, float gap, Func<float, float> placement)
        {
            IPipePair pair = pairs[index];
            if (pair == null || pair.IsDisposed)
            {
                return 0;
            }

            float x = XOf(index);
            pair.MoveTo(x);
            if (x + config.HalfWidth >= -config.ScreenRightEdge)
            {
                return 0;
            }

            float center = placement != null ? placement(gap) : 0f;
            return pair.Activate(x, gap, center, Random) ? 1 : 0;
        }

        /// <summary>Moves the pair furthest to the left to (almost) the right end of the lane and re-rolls its gap.</summary>
        public int RecycleLeftmost(Func<float> gapProvider, Func<float, float> placement)
        {
            if (pairs.Length == 0)
            {
                return 0;
            }

            int leftmost = 0;
            for (int i = 1; i < pairs.Length; i++)
            {
                if (XOf(i) < XOf(leftmost))
                {
                    leftmost = i;
                }
            }

            IPipePair pair = pairs[leftmost];
            if (pair == null || pair.IsDisposed)
            {
                return 0;
            }

            float gap = gapProvider != null ? gapProvider() : config.Gap;
            float center = placement != null ? placement(gap) : 0f;
            float x = XOf(leftmost) + config.CycleLength;
            pair.MoveTo(x);
            return pair.Activate(x, gap, center, Random) ? 1 : 0;
        }
    }
}
