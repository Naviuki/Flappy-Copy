using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Makes ground strips and full-width background pieces as wide as the visible screen. A sprite is
    /// assumed to be one unit wide and anchored where the layout builder placed it, so width = screen
    /// width keeps it covering the whole window on both 9:16 and wider screens.
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
