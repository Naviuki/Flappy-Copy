using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Scales the ground strips so they always cover the visible width, and tells
    /// <see cref="InfinityGround"/> about it. Works on 9:16 and on wider windows alike.
    /// </summary>
    [DefaultExecutionOrder(40)]
    public sealed class GroundFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private InfinityGround ground;

        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                Apply();
            }
        }

        public void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || ground == null)
            {
                return;
            }

            float width = (2f * cam.orthographicSize * cam.aspect) + 0.5f;
            if (width > ground.StripWidth)
            {
                ground.SetStripWidth(width);
            }
        }
    }
}
