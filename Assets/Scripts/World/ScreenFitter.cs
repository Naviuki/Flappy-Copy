using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Makes full-width background pieces (the sky, the horizon band) as wide as what the camera shows. A
    /// sprite is one unit wide and anchored where the layout builder placed it, so width = camera width keeps
    /// it covering the view on 9:16, on a wide window, in a browser page and on a phone.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class ScreenFitter : MonoBehaviour
    {
        [SerializeField, Tooltip("Full-width sprites that should cover the camera horizontally.")]
        private Transform[] pieces;

        [SerializeField] private Camera targetCamera;

        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (Cameras.ViewMetrics.Width != lastWidth || Cameras.ViewMetrics.Height != lastHeight)
            {
                Apply();
            }
        }

        public void Apply()
        {
            lastWidth = Cameras.ViewMetrics.Width;
            lastHeight = Cameras.ViewMetrics.Height;
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || pieces == null)
            {
                return;
            }

            float width = (2f * cam.orthographicSize * cam.aspect) + 1f;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null)
                {
                    continue;
                }

                Vector3 scale = pieces[i].localScale;
                scale.x = width;
                pieces[i].localScale = scale;
            }
        }
    }
}
