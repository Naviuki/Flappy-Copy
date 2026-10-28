using System;

namespace FlappyCopy.Pipes
{
    /// <summary>Fake pair used by the edit-mode checks; no Unity objects at all.</summary>
    public sealed class FakePipePair : IPipePair
    {
        private readonly float width;

        public FakePipePair(float width)
        {
            this.width = width;
        }

        public float X { get; private set; }
        public bool IsDisposed { get; set; }
        public int ActivationCount { get; private set; }
        public float LastGap { get; private set; }
        public float LastCenter { get; private set; }
        public float GapTop => LastCenter + (LastGap * 0.5f);
        public float GapBottom => LastCenter - (LastGap * 0.5f);
        public float RightEdge => X + (width * 0.5f);

        public bool Activate(float x, float gap, float centerY, Random random)
        {
            if (IsDisposed)
            {
                return false;
            }

            X = x;
            LastGap = gap;
            LastCenter = centerY;
            ActivationCount++;
            return true;
        }

        public void MoveTo(float x)
        {
            X = x;
        }

        /// <summary>Same scoring rule as the real gate (see <see cref="PipePassRule"/>).</summary>
        public bool Sample(float birdX, float birdY, float birdHalfWidth)
        {
            return PipePassRule.Sample(ref inside, ref awarded, birdX, birdY, birdHalfWidth, GapTop, GapBottom);
        }

        /// <summary>Gate-compatible overload, so checks can swap one for the other.</summary>
        public bool Sample(float birdX, float birdY, float birdHalfWidth, float gapTop, float gapBottom)
        {
            return PipePassRule.Sample(ref inside, ref awarded, birdX, birdY, birdHalfWidth, gapTop, gapBottom);
        }

        private bool inside;
        private bool awarded;
    }

    /// <summary>Counts created instances so checks can assert that a run recycles instead of spawning.</summary>
    public sealed class FakePipePairFactory : IPipePairFactory
    {
        private readonly float width;

        public FakePipePairFactory(float width = 1.5f)
        {
            this.width = width;
        }

        public int Created { get; private set; }

        public System.Collections.Generic.List<FakePipePair> Pairs { get; } = new System.Collections.Generic.List<FakePipePair>();

        public IPipePair Spawn(float x, float gap, float centerY, Random random)
        {
            var pair = new FakePipePair(width);
            pair.MoveTo(x);
            Created++;
            Pairs.Add(pair);
            return pair;
        }
    }
}
