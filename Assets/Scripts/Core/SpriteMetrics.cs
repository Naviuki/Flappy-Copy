using UnityEngine;

namespace FlappyCopy.Core
{
    /// <summary>
    /// Sprite size convention for the whole game: sprites are made from the generated textures and scaled
    /// to world units explicitly, so the result never depends on the importer's pixels-per-unit.
    /// </summary>
    public static class SpriteMetrics
    {
        /// <summary>Must match the editor import setting in SpriteTextures.</summary>
        public const int PixelsPerUnit = 32;

        /// <summary>World units for a texture of a given pixel size at <see cref="PixelsPerUnit"/>.</summary>
        public static float UnitsFor(int pixels)
        {
            return pixels / (float)PixelsPerUnit;
        }
    }
}
