using FlappyCopy.Core;
using FlappyCopy.Data;
using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Endless ground built from two strips that share one scroll offset. The strips are as wide as the 9:16
    /// frame and start one strip to the left of it, so the covered range is always [-strip, +strip] and both
    /// edges of the frame stay under the ground; wider windows (the camera reveals more world sideways) widen
    /// the strips to match.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InfinityGround : MonoBehaviour
    {
        [SerializeField, Tooltip("Strip containers, left to right, bottom-left pivot.")]
        private Transform[] strips;

        [SerializeField] private WorldSettings worldSettings;

        /// <summary>Width the strips are scaled to (the scene stores half the 9:16 frame).</summary>
        private float lastWidth;

        /// <summary>One strip in world units (the strips are as wide as this).</summary>
        public float StripWidth => lastWidth > 0f ? lastWidth : DesignSpace.PortraitWidth;

        public float Offset { get; private set; }
        public int StripCount => strips != null ? strips.Length : 0;

        /// <summary>
        /// Width of the visible frame in world units. A frame wider than 9:16 (a landscape window) shows more
        /// world sideways, exactly like <see cref="Cameras.PortraitFraming"/> frames it.
        /// </summary>
        public static float VisibleWidth()
        {
            float height = Screen.height > 0 ? Screen.height : DesignSpace.ReferenceWinHeight;
            float width = Screen.width > 0 ? Screen.width : DesignSpace.ReferenceWinWidth;
            float design = DesignSpace.PortraitWidth / (float)DesignSpace.PortraitHeight;
            float aspect = width / height;
            float frameWidth = aspect > design ? DesignSpace.HalfHeight * 2f * aspect : DesignSpace.PortraitWidth;
            return Mathf.Max(frameWidth, DesignSpace.PortraitWidth);
        }

        private void Awake()
        {
            lastWidth = 0f;
            ApplyLayout();
        }

        private void Update()
        {
            // The window can be resized (or start at another size): keep the strips as wide as the frame.
            float wanted = VisibleWidth();
            if (Mathf.Abs(wanted - lastWidth) > 0.01f)
            {
                SetStripWidth(wanted);
            }
        }

        public void ApplyLayout()
        {
            if (strips == null)
            {
                return;
            }

            float width = StripWidth;
            for (int i = 0; i < strips.Length; i++)
            {
                Transform strip = strips[i];
                if (strip == null)
                {
                    continue;
                }

                if (lastWidth > 0f && Mathf.Abs(strip.localScale.x - lastWidth) > 0.001f)
                {
                    Vector3 scale = strip.localScale;
                    scale.x = lastWidth;
                    strip.localScale = scale;
                }

                Vector3 position = strip.localPosition;
                position.x = ScrollMath.StripX(i, width, Offset);
                strip.localPosition = position;
            }
        }

        public void ResetRun()
        {
            Offset = 0f;
            ApplyLayout();
        }

        /// <summary>Scrolls left by the ground speed (scaled from the pipe speed) and wraps the offset.</summary>
        public void Tick(float deltaTime, float baseSpeed)
        {
            if (strips == null || strips.Length == 0 || deltaTime <= 0f || baseSpeed <= 0f)
            {
                return;
            }

            float multiplier = worldSettings != null && worldSettings.PipeSpeed > 0f
                ? worldSettings.GroundScrollSpeed / worldSettings.PipeSpeed
                : 1f;
            Offset = ScrollMath.Wrap(Offset + (baseSpeed * Mathf.Max(0.1f, multiplier) * deltaTime), StripWidth);
            ApplyLayout();
        }

        /// <summary>Resizes the strips (and the blocks on them) to the given width and lays them out again.</summary>
        public void SetStripWidth(float width)
        {
            if (strips == null)
            {
                return;
            }

            lastWidth = Mathf.Max(0.1f, width);

            // Blocks are one sprite unit wide inside a strip of the same width; the strip is scaled to the frame.
            for (int i = 0; i < strips.Length; i++)
            {
                Transform strip = strips[i];
                if (strip != null)
                {
                    for (int child = 0; child < strip.childCount; child++)
                    {
                        Transform block = strip.GetChild(child);
                        Vector3 blockScale = block.localScale;
                        blockScale.x = 1f;
                        block.localScale = blockScale;
                    }
                }
            }

            ApplyLayout();
        }
    }
}
