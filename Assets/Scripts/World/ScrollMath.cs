using FlappyCopy.Core;

namespace FlappyCopy.World
{
    /// <summary>Scroll positions as pure data: the ground strip wrap and the parallax offset.</summary>
    public static class ScrollMath
    {
        /// <summary>Wraps <paramref name="offset"/> into [0, span) for the repeating strip.</summary>
        public static float Wrap(float offset, float span)
        {
            if (span <= 0f || float.IsNaN(offset) || float.IsInfinity(offset))
            {
                return 0f;
            }

            float wrapped = offset % span;
            return wrapped < 0f ? wrapped + span : wrapped;
        }

        /// <summary>
        /// X of strip <paramref name="index"/>. Two strips of half the frame width tile the design width
        /// exactly, start at the left edge (or one strip before it) and share one scroll offset.
        /// </summary>
        public static float StripX(int index, float stripWidth, float offset)
        {
            return (index * stripWidth) - Wrap(offset, stripWidth);
        }

        /// <summary>Width of one ground strip: two strips tile the 9:16 design width.</summary>
        public static float StripWidthFor(float frameWidth)
        {
            return frameWidth * 0.5f;
        }

        /// <summary>Small multiplier for parallax layers (clouds at a quarter of the world speed).</summary>
        public static float ParallaxOffset(float distance, float speedFactor)
        {
            return distance * speedFactor;
        }
    }
}
