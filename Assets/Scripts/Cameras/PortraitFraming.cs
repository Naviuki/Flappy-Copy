using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.Cameras
{
    /// <summary>
    /// Locks the 2D camera to a portrait 9:16 frame. Unity 2D sprites made by the builder use their centre as
    /// pivot and a size in world units; the whole 9:16 box is always visible and wider screens reveal more.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class PortraitFraming : MonoBehaviour
    {
        private const float MinimumOrthographicSize = 0.1f;

        private Camera targetCamera;
        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            Apply();
        }

        private void OnEnable()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            Apply();
        }

        private void Update()
        {
            if (lastWidth != Screen.width || lastHeight != Screen.height)
            {
                Apply();
            }
        }

        /// <summary>Frames the camera for the current window size.</summary>
        public void Apply()
        {
            if (targetCamera == null)
            {
                return;
            }

            lastWidth = Mathf.Max(1, Screen.width);
            lastHeight = Mathf.Max(1, Screen.height);
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = ComputeOrthographicSize(lastWidth, lastHeight);
        }

        /// <summary>
        /// Orthographic half-height that keeps the 9:16 design box inside the view on the given screen.
        /// A taller-than-9:16 screen keeps the design height (extra width is revealed sideways); a wider screen
        /// keeps the design width (letterbox-style extra height is revealed).
        /// </summary>
        public static float ComputeOrthographicSize(int screenWidth, int screenHeight)
        {
            float width = Mathf.Max(1, screenWidth);
            float height = Mathf.Max(1, screenHeight);
            float aspect = width / height;
            float designAspect = DesignSpace.PortraitWidth / (float)DesignSpace.PortraitHeight;

            if (aspect <= designAspect)
            {
                // Taller than 9:16: fit the width.
                return Mathf.Max(MinimumOrthographicSize, (DesignSpace.PortraitWidth * 0.5f) / aspect);
            }

            // Wider than 9:16: fit the height.
            return DesignSpace.PortraitHeight * 0.5f;
        }

        /// <summary>
        /// Half-width of the play area in world units for a view of <paramref name="orthographicSize"/> and
        /// <paramref name="aspect"/>: at least the 9:16 design width, wider on a wide window. The pipes, the
        /// reveal and the ground all use it so the play area never runs past what the player sees.
        /// </summary>
        public static float PlayHalfWidth(float orthographicSize, float aspect)
        {
            float visible = orthographicSize * Mathf.Max(0.0001f, aspect);
            return Mathf.Max(DesignSpace.HalfWidth, visible);
        }

        /// <summary>Play-area half-height: the 9:16 design height, or more when a wide window shows more.</summary>
        public static float PlayHalfHeight(float orthographicSize)
        {
            return Mathf.Max(DesignSpace.HalfHeight, orthographicSize);
        }
    }
}
