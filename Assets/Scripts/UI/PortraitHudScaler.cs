using FlappyCopy.Cameras;
using FlappyCopy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyCopy.UI
{
    /// <summary>
    /// Keeps the HUD canvas in units that follow the world camera: the camera always shows 16 design units of
    /// height, and the HUD's 36-wide reference box maps onto those 9 units (4 reference units per design unit).
    /// The 9:16 device box is therefore 1080 x 1920 reference units on every window, and elements sized in
    /// reference units match the world's scale on phones, on 9:16 and on wide desktop windows.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [DisallowMultipleComponent]
    public sealed class PortraitHudScaler : MonoBehaviour
    {
        private const float ReferenceUnitsPerDesignUnit =
            DesignSpace.ReferenceWinWidth / DesignSpace.PortraitWidth / 4f;

        private CanvasScaler scaler;
        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            Apply(ViewMetrics.Width, ViewMetrics.Height);
        }

        private void Update()
        {
            if (ViewMetrics.Width != lastWidth || ViewMetrics.Height != lastHeight)
            {
                Apply(ViewMetrics.Width, ViewMetrics.Height);
            }
        }

        /// <summary>Reference size of the canvas for a screen size (see the class summary for the reasoning).</summary>
        public static Vector2 ReferenceFor(int screenWidth, int screenHeight)
        {
            float heightPixels = Mathf.Max(1, screenHeight);
            float widthPixels = Mathf.Max(1, screenWidth);
            float height = heightPixels / DesignSpace.PortraitHeight * ReferenceUnitsPerDesignUnit;
            float width = widthPixels / DesignSpace.PortraitHeight * ReferenceUnitsPerDesignUnit;
            return new Vector2(Mathf.Max(1f, Mathf.Round(width)), Mathf.Max(1f, Mathf.Round(height)));
        }

        private void Apply(int screenWidth, int screenHeight)
        {
            lastWidth = screenWidth;
            lastHeight = screenHeight;
            if (scaler != null)
            {
                scaler.referenceResolution = ReferenceFor(screenWidth, screenHeight);
            }
        }
    }
}
