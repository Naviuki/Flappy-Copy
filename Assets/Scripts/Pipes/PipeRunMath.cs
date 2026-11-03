using UnityEngine;

namespace FlappyCopy.Pipes
{
    /// <summary>
    /// Convenience for tests/tools: the slot maths the spawner and the pipe run share.
    /// Production code uses the same formulas inside <see cref="PipeRun"/>.
    /// </summary>
    public static class PipeRunMath
    {
        /// <summary>
        /// Slots needed so the lane is covered from just right of the play area (half width
        /// <paramref name="screenRightEdge"/>) to one spacing past its left edge.
        /// </summary>
        public static int SlotsFor(float screenRightEdge, float spacing, float spawnOffset)
        {
            if (spacing <= 0f || screenRightEdge <= 0f)
            {
                return 0;
            }

            // One lead slot right of the play area, then one slot per spacing across it, plus one that is
            // already off the left edge (it is the next pair to be recycled).
            float lane = (screenRightEdge * 2f) + spawnOffset + spacing;
            return Mathf.Max(1, Mathf.CeilToInt(lane / spacing));
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
