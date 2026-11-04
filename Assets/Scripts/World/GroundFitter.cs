using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Keeps the ground strips as wide as the visible frame. The camera already frames the world
    /// (<see cref="Cameras.PortraitFraming"/>); this only asks <see cref="InfinityGround"/> to cover that
    /// frame when the window, the page (WebGL) or the device (mobile) changes size.
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
            if (Cameras.ViewMetrics.Width != lastWidth || Cameras.ViewMetrics.Height != lastHeight)
            {
                Apply();
            }
        }

        public void Apply()
        {
            lastWidth = Cameras.ViewMetrics.Width;
            lastHeight = Cameras.ViewMetrics.Height;
            ground?.SetStripWidth(InfinityGround.VisibleWidth());
        }
    }
}
