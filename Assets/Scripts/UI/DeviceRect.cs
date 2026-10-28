using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.UI
{
    /// <summary>
    /// Portrait UI helper: places a UGUI object inside a "device" rect that is always 9:16 and centered
    /// in the canvas, so the HUD looks identical on 9:16, 9:19.5 phones and wide desktop windows.
    /// Coordinates are in 1080 x 1920 reference pixels: x/y from the device's center, y up.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DeviceRect : MonoBehaviour
    {
        [SerializeField, Tooltip("Square size in reference pixels; -1 fills the whole 9:16 device.")]
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

        public void Apply()
        {
            var rect = transform as RectTransform;
            if (rect == null || !(rect.parent is RectTransform parent))
            {
                return;
            }

            float canvasWidth = parent.rect.width;
            float canvasHeight = parent.rect.height;
            if (canvasWidth <= 0f || canvasHeight <= 0f)
            {
                return;
            }

            float scale = Mathf.Min(canvasWidth / DesignSpace.ReferenceWinWidth, canvasHeight / DesignSpace.ReferenceWinHeight);
            float guardedScale = scale > 0f && !float.IsNaN(scale) ? scale : 0.001f;
            Vector2 deviceSize = new Vector2(DesignSpace.ReferenceWinWidth, DesignSpace.ReferenceWinHeight) * guardedScale;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position * guardedScale;
            rect.sizeDelta = size > 0f ? new Vector2(size, size) * guardedScale : deviceSize;
        }
    }
}
