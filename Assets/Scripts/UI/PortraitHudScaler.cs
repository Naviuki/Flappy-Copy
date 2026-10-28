using FlappyCopy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyCopy.UI
{
    /// <summary>
    /// Locks the HUD canvas to the 9:16 portrait box: the reference width stays 1080 and the reference
    /// height follows the screen, so the HUD keeps its proportions inside the portrait frame.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [DisallowMultipleComponent]
    public sealed class PortraitHudScaler : MonoBehaviour
    {
        private CanvasScaler scaler;
        private int lastWidth = -1;
        private int lastHeight = -1;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            Apply(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                Apply(Screen.width, Screen.height);
            }
        }

        /// <summary>
        /// Reference height grows with the screen height so 1080 units always equal the visible width:
        /// exactly 1920 on 9:16, more on taller phones, and 1920 (portrait frame) on wider screens.
        /// </summary>
        public static Vector2 ReferenceFor(int screenWidth, int screenHeight)
        {
            float aspect = Mathf.Max(1, screenWidth) / (float)Mathf.Max(1, screenHeight);
            float designAspect = DesignSpace.PortraitWidth / (float)DesignSpace.PortraitHeight;
            float height = DesignSpace.ReferenceWinHeight;
            if (aspect < designAspect)
            {
                height = Mathf.Clamp(DesignSpace.ReferenceWinWidth / aspect,
                    DesignSpace.ReferenceWinHeightMin, DesignSpace.ReferenceWinHeight);
            }

            return new Vector2(DesignSpace.ReferenceWinWidth, Mathf.Round(height));
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
