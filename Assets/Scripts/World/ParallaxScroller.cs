using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.World
{
    /// <summary>
    /// Scrolls a container horizontally and wraps it, used for the cloud band. The offset is pure data,
    /// so the motion is identical on every frame rate.
    /// </summary>
    public sealed class ParallaxScroller : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)] private float scrollSpeed = 0.25f;
        [SerializeField, Min(0.1f)] private float repeatWidth = DesignSpace.PortraitWidth * 2f;
        [SerializeField] private bool loop = true;

        private float offset;

        public float RepeatWidth => repeatWidth;
        public float Offset => offset;

        /// <summary>Scrolls a child band: <paramref name="deltaTime"/> seconds at <paramref name="baseSpeed"/>.</summary>
        public void Tick(float deltaTime, float baseSpeed)
        {
            if (deltaTime <= 0f || baseSpeed <= 0f)
            {
                return;
            }

            offset += ScrollMath.ParallaxOffset(baseSpeed * deltaTime, scrollSpeed);
            if (loop)
            {
                offset = ScrollMath.Wrap(offset, repeatWidth);
            }

            Vector3 position = transform.localPosition;
            position.x = -offset;
            transform.localPosition = position;
        }
    }
}
