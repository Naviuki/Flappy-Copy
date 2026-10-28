using System;
using UnityEngine;

namespace FlappyCopy.Pipes
{
    /// <summary>
    /// Per-pair scoring bookkeeping, evaluated from the bird's position every frame (the couple of
    /// colliders involved is exactly the one Flappy-style scoring needs, with no trigger-timing races).
    /// </summary>
    public sealed class PipePassSensor
    {
        private readonly PipeSpawner spawner;
        private readonly Transform bird;
        private float birdHalfWidth = 0.25f;

        public PipePassSensor(PipeSpawner spawner, Transform bird)
        {
            this.spawner = spawner;
            this.bird = bird;
        }

        /// <summary>Raised once for every pipe pair that the bird has fully cleared.</summary>
        public event Action<PipePair> PairPassed;

        public void SetBirdHalfWidth(float halfWidth)
        {
            birdHalfWidth = Mathf.Max(0f, halfWidth);
        }

        /// <summary>Samples the bird against all live pairs; cheap (a handful of pairs per frame).</summary>
        public void Sample()
        {
            PipeRun run = spawner != null ? spawner.Run : null;
            if (run == null || bird == null)
            {
                return;
            }

            Vector3 birdPosition = bird.position;
            for (int i = 0; i < run.Count; i++)
            {
                if (!(run.GetPair(i) is PipePair pair) || pair.Gate == null)
                {
                    continue;
                }

                float localX = birdPosition.x - pair.transform.position.x;
                if (Mathf.Abs(localX) > pair.Gate.Reach)
                {
                    continue;
                }

                if (pair.Gate.Sample(localX, birdPosition.y, birdHalfWidth, pair.GapTop, pair.GapBottom))
                {
                    PairPassed?.Invoke(pair);
                }
            }
        }
    }
}
