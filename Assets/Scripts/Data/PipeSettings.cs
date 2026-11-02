using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.Data
{
    /// <summary>Pipe geometry, the spacing formula and the random gap placement. Pure data, safe to unit test.</summary>
    [CreateAssetMenu(menuName = "Flappy Copy/Pipe Settings", fileName = "PipeSettings")]
    public sealed class PipeSettings : ScriptableObject
    {
        public const float DefaultMinimumGap = 2.2f;

        [Header("Geometry")]
        [SerializeField, Min(0.05f)] private float pipeWidth = 1.5f;
        [SerializeField, Min(0.05f)] private float capHeight = 0.5f;
        [SerializeField] private float capOverhang = 0.12f;

        [Header("Gaps")]
        [SerializeField, Min(0.1f)] private float baseGap = 4.6f;
        [SerializeField, Min(0.1f)] private float minimumGap = DefaultMinimumGap;
        [SerializeField] private Vector2 gapCenterRange = new(-3.2f, 3.6f);

        [Header("Placement")]
        [SerializeField, Range(0f, 1f)] private float maximumHeightStep = 0.45f;

        [Header("Presentation")]
        [SerializeField] private Color pipeColor = new(0.30f, 0.76f, 0.31f, 1f);
        [SerializeField] private Color capColor = new(0.23f, 0.63f, 0.25f, 1f);
        [SerializeField] private Color outlineColor = new(0.09f, 0.25f, 0.10f, 1f);

        public float PipeWidth => pipeWidth;
        public float CapHeight => capHeight;
        public float CapOverhang => capOverhang;
        public float BaseGap => baseGap;
        public float MinimumGap => Mathf.Min(minimumGap, baseGap);
        public Vector2 GapCenterRange => gapCenterRange;
        public float MaximumHeightStep => maximumHeightStep;
        public Color PipeColor => pipeColor;
        public Color CapColor => capColor;
        public Color OutlineColor => outlineColor;

        /// <summary>Gap for the given score: shrinks with score but never below <see cref="MinimumGap"/>.</summary>
        public float GetGap(float multiplier)
        {
            return Mathf.Max(MinimumGap, baseGap * Mathf.Clamp01(Mathf.Max(0f, multiplier)));
        }

        /// <summary>
        /// Random gap centre inside the visible range, clamped so both pipes stay on screen, and
        /// limited to <see cref="MaximumHeightStep"/> away from the previous centre.
        /// </summary>
        public float NextGapCenter(float previousCenter, float gap, System.Random random)
        {
            return GapPlacement.NextCentre(previousCenter, gap, GapCenterRange.x, GapCenterRange.y,
                DesignSpace.PlayHalfHeight, maximumHeightStep, random);
        }

        /// <summary>Sets the vertical range the gap centre may use (the editor keeps it inside the play area).</summary>
        public void ConfigureGapCenterRange(Vector2 range)
        {
            gapCenterRange = range;
        }
    }
}
