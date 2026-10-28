namespace FlappyCopy.Pipes
{
    /// <summary>
    /// The scoring rule of a pipe pair as pure logic, shared by the gate behaviour and by the checks:
    /// a pair is awarded once, when the bird was inside the gap and then passed the pipe column.
    /// </summary>
    public static class PipePassRule
    {
        /// <summary>
        /// Samples the bird relative to the pair (pipes at x = 0). <paramref name="inside"/> and
        /// <paramref name="awarded"/> are the pair's per-run flags; returns true on the frame it is awarded.
        /// </summary>
        public static bool Sample(ref bool inside, ref bool awarded, float birdLocalX, float birdY,
            float birdHalfWidth, float gapTop, float gapBottom)
        {
            if (awarded)
            {
                return false;
            }

            if (birdY <= gapTop && birdY >= gapBottom)
            {
                inside = true;
            }

            if (inside && birdLocalX - birdHalfWidth > 0f)
            {
                awarded = true;
                inside = false;
                return true;
            }

            return false;
        }
    }
}
