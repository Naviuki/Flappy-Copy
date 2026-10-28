using UnityEngine;

namespace FlappyCopy.Pipes
{
    /// <summary>
    /// Per-pair scoring flag. A pair scores once: when the bird was inside the gap and has passed the
    /// pipe column (its left edge is behind the pair). Pooled with the pair, so the flag resets.
    /// The rule itself lives in <see cref="PipePassRule"/> so it can be checked without a scene.
    /// </summary>
    public sealed class PipePassGate : MonoBehaviour
    {
        private const float DefaultReach = 6f;

        private bool inside;
        private bool awarded;

        public bool Awarded => awarded;
        public bool BirdInside => inside;

        /// <summary>How far from the pair the bird can be and still be tracked (avoids culling cost).</summary>
        public float Reach { get; private set; } = DefaultReach;

        public void ResetForPair()
        {
            inside = false;
            awarded = false;
        }

        /// <summary>Returns true on the frame the pair is awarded.</summary>
        public bool Sample(float birdLocalX, float birdY, float birdHalfWidth, float gapTop, float gapBottom)
        {
            return PipePassRule.Sample(ref inside, ref awarded, birdLocalX, birdY, birdHalfWidth, gapTop, gapBottom);
        }
    }
}
