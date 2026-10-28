using FlappyCopy.Core;
using FlappyCopy.Data;
using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Endless ground built from two strips that share one scroll offset. Strip widths come from the
    /// laid-out sprite size (a flat quad is one unit wide before scaling), so the strips tile exactly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InfinityGround : MonoBehaviour
    {
        [SerializeField, Tooltip("Strip containers, left to right, bottom-left pivot.")]
        private Transform[] strips;

        [SerializeField] private WorldSettings worldSettings;

        public float StripWidth => strips != null && strips.Length > 0 && strips[0] != null ? strips[0].localScale.x : DesignSpace.PortraitWidth;
        public float Offset { get; private set; }
        public int StripCount => strips != null ? strips.Length : 0;

        private void Awake()
        {
            ApplyLayout();
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
                if (strips[i] == null)
                {
                    continue;
                }

                Vector3 position = strips[i].localPosition;
                position.x = ScrollMath.StripX(i, width, Offset);
                strips[i].localPosition = position;
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

        /// <summary>Called by the fitter when the window is wider than one strip.</summary>
        public void SetStripWidth(float width)
        {
            if (strips == null)
            {
                return;
            }

            float safeWidth = Mathf.Max(1f, width);
            for (int i = 0; i < strips.Length; i++)
            {
                if (strips[i] == null)
                {
                    continue;
                }

                Vector3 scale = strips[i].localScale;
                scale.x = safeWidth;
                strips[i].localScale = scale;
            }

            ApplyLayout();
        }
    }
}
