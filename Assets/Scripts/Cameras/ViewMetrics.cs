using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.Cameras
{
    /// <summary>
    /// Real screen metrics for the framing tools. The window, the page (WebGL) and the device (mobile) can
    /// all decide the actual pixels, so everything that frames the world reads the size from here.
    /// </summary>
    public static class ViewMetrics
    {
        /// <summary>Width of the visible frame in pixels (at least 1).</summary>
        public static int Width => Mathf.Max(1, Screen.width);

        /// <summary>Height of the visible frame in pixels (at least 1).</summary>
        public static int Height => Mathf.Max(1, Screen.height);

        /// <summary>Aspect ratio of the visible frame (width / height).</summary>
        public static float Aspect => Width / (float)Height;

        /// <summary>Falls back to the design frame when there is no screen at all (a headless tool).</summary>
        public static float SafeAspect => Width <= 1 || Height <= 1
            ? DesignSpace.PortraitWidth / (float)DesignSpace.PortraitHeight
            : Aspect;
    }
}
