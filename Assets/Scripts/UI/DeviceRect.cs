using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.UI
{
    /// <summary>
    /// Portrait UI helper: places a UGUI object inside a "device" rect that is always 9:16 and centered in
    /// the canvas, so the HUD looks identical on 9:16, 9:19.5 phones and wide desktop windows.
    /// The device is 1080 x 1920 reference units on every window (<see cref="PortraitHudScaler"/> keeps the
    /// canvas in the camera's units), and children are positioned from its center in the same units.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DeviceRect : MonoBehaviour
    {
        [SerializeField, Tooltip("Square size in reference units; -1 fills the whole 9:16 device.")]
        private float size = -1f;

        [SerializeField] private Vector2 position;

        public float Size => size;

        public void Configure(Vector2 referencePosition, float squareSize = -1f)
        {
            position = referencePosition;
            size = squareSize;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Start()
        {
            Apply();
        }

        /// <summary>Centers the box on the 9:16 device. Every child of the device inherits this rect.</summary>
        public void Apply()
        {
            var rect = transform as RectTransform;
            if (rect == null || !(rect.parent is RectTransform))
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size > 0f
                ? new Vector2(size, size)
                : new Vector2(DesignSpace.ReferenceWinWidth, DesignSpace.ReferenceWinHeight);
        }
    }
}
