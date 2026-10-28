using System;
using System.Collections.Generic;

namespace FlappyCopy.Core
{
    /// <summary>
    /// Difficulty tuning as plain data (the ScriptableObject assets mirror these values). Kept free of Unity
    /// types so the rules can be checked without an editor or a scene.
    /// </summary>
    public sealed class DifficultyRules
    {
        public const float MaximumVelocityIncrease = 3.5f;
        public const float VelocityIncreasePerPoint = 0.05f;
        public const float MinimumGapMultiplier = 0.66f;
        public const float GapShrinkPerPoint = 0.035f;

        /// <summary>Extra pipe speed earned after <paramref name="score"/> points, capped.</summary>
        public static float VelocityIncrease(int score)
        {
            return Math.Min(Math.Max(0, score) * VelocityIncreasePerPoint, MaximumVelocityIncrease);
        }

        /// <summary>Fraction (0.66..1) of the base gap left after <paramref name="score"/> points.</summary>
        public static float GapMultiplier(int score)
        {
            float multiplier = 1f - (Math.Max(0, score) * GapShrinkPerPoint);
            return Math.Min(Math.Max(multiplier, MinimumGapMultiplier), 1f);
        }
    }

    /// <summary>Gap placement rules shared by the pipe settings asset and the checks (engine-free).</summary>
    public static class GapPlacement
    {
        /// <summary>Random gap centre inside the visible range, clamped so both pipes stay on screen.</summary>
        public static float NextCentre(float previousCentre, float gap, float lowest, float highest,
            float playHalfHeight, float maximumStep, Random random)
        {
            float halfGap = gap * 0.5f;
            float min = lowest;
            float max = highest;
            if (previousCentre > min)
            {
                min = Math.Max(min, previousCentre - (playHalfHeight * maximumStep));
                max = Math.Min(max, previousCentre + (playHalfHeight * maximumStep));
            }

            float clampMin = Math.Max(min, -playHalfHeight + halfGap);
            float clampMax = Math.Min(max, playHalfHeight - halfGap);
            if (clampMax < clampMin)
            {
                clampMax = clampMin;
            }

            double roll = random != null ? random.NextDouble() : 0.5;
            return clampMin + ((float)roll * (clampMax - clampMin));
        }
    }
}
