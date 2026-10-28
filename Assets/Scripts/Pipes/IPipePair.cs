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
    /// Owns one run's worth of pipe pairs. Slot X positions are derived from an authoritative scroll
    /// distance, so the logic is deterministic: no physics, no scene, unit testable.
    /// </summary>
    public sealed class PipeRun
    {
        private readonly IPipePair[] pairs;
        private readonly PipeRunConfig config;

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

            Random = seed.HasValue ? new Random(seed.Value) : new Random();
            pairs = new IPipePair[config.PoolSize];
            for (int i = 0; i < pairs.Length; i++)
            {
                pairs[i] = factory.Spawn(XOf(i), config.Gap, 0f, Random);
            }
        }

        public Random Random { get; }

        /// <summary>Authoritative total scroll distance in world units.</summary>
        public float Scrolled { get; private set; }

        public int Count => pairs.Length;

        public PipeRunConfig Config => config;

        public IPipePair GetPair(int index) => pairs[index];

        /// <summary>X of slot <paramref name="index"/> for the current scroll distance.</summary>
        public float XOf(int index)
        {
            float wrapped = Scrolled % config.CycleLength;
            return (config.ScreenRightEdge + config.SpawnOffset) - (index * config.Spacing) - wrapped;
        }

        public void Reset()
        {
            Scrolled = 0f;
        }

        /// <summary>
        /// Advances the scroll, moves every pair and recycles pairs that fully left the screen.
        /// <paramref name="gapProvider"/> gives the current gap; <paramref name="placement"/> picks the
        /// new gap centre. Returns how many pairs were recycled this call.
        /// </summary>
        public int Tick(float deltaTime, float speed, Func<float> gapProvider, Func<float, float> placement)
        {
            if (deltaTime <= 0f)
            {
                return 0;
            }

            Scrolled += speed * deltaTime;
            float gap = gapProvider != null ? gapProvider() : config.Gap;
            int recycled = 0;
            for (int i = 0; i < pairs.Length; i++)
            {
                IPipePair pair = pairs[i];
                if (pair == null || pair.IsDisposed)
                {
                    continue;
                }

                float x = XOf(i);
                pair.MoveTo(x);
                if (x + config.HalfWidth >= -config.ScreenRightEdge)
                {
                    continue;
                }

                // Left the screen; the slot's modulo already wrapped it to the right-hand side.
                float center = placement != null ? placement(gap) : 0f;
                if (pair.Activate(x, gap, center, Random))
                {
                    recycled++;
                }
            }

            return recycled;
        }
    }
}
