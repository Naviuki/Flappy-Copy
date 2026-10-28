using UnityEngine;

namespace FlappyCopy.Pipes
{
    /// <summary>
    /// Convenience for tests/tools: report whether a pair reaches off the left edge.
    /// Production code uses the same condition inside <see cref="PipeRun"/>.
    /// </summary>
    public static class PipeRunMath
    {
        /// <summary>Slots needed to keep a screen (half width <paramref name="screenRightEdge"/>) covered.</summary>
        public static int SlotsFor(float screenRightEdge, float spacing, float spawnOffset)
        {
            if (spacing <= 0f || screenRightEdge <= 0f)
            {
                return 0;
            }

            float coverage = (screenRightEdge + spawnOffset) * 2f;
            return Mathf.Max(1, Mathf.CeilToInt(coverage / spacing) + 1);
        }

        /// <summary>Base speed plus the score ramp, clamped to a positive floor.</summary>
        public static float Speed(float baseSpeed, float increase, float minimum = 0.1f)
        {
            return Mathf.Max(minimum, baseSpeed + increase);
        }

        /// <summary>True when the pair centre has scrolled past the left edge plus its own half width.</summary>
        public static bool IsOffScreen(float x, float halfWidth, float screenRightEdge)
        {
            return x + halfWidth < -screenRightEdge;
        }
    }
}
