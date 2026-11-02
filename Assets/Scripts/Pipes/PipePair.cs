using FlappyCopy.Core;
using FlappyCopy.Data;
using FlappyCopy.Pooling;
using UnityEngine;

namespace FlappyCopy.Pipes
{
    /// <summary>
    /// Pool-backed <see cref="IPipePair"/>: two pipe sprites scaled to the gap, a cap on each gap edge,
    /// two hitbox colliders and the score gate. Sprites use a bottom-left pivot with the cap baked in,
    /// so the end of every pipe is drawn at world size no matter how much it was stretched.
    /// </summary>
    public sealed class PipePair : MonoBehaviour, IPipePair, IPoolable
    {
        [SerializeField] private Transform topPipe;
        [SerializeField] private Transform bottomPipe;
        [SerializeField] private Transform topCap;
        [SerializeField] private Transform bottomCap;
        [SerializeField] private PipePassGate gate;
        [SerializeField] private PipeSettings settings;

        private float placedGap = 1f;
        private float placedCenter;
        private BoxCollider2D topHitbox;
        private BoxCollider2D bottomHitbox;

        public float X => transform.position.x;
        public bool IsDisposed => !gameObject.activeSelf;
        public PipePassGate Gate => gate;
        public float GapTop => placedCenter + (placedGap * 0.5f);
        public float GapBottom => placedCenter - (placedGap * 0.5f);
        public float PlacedGap => placedGap;
        public float PlacedCenter => placedCenter;

        private void Awake()
        {
            topHitbox = FindCollider("TopHitbox");
            bottomHitbox = FindCollider("BottomHitbox");
        }

        public bool Activate(float x, float gap, float centerY, System.Random random)
        {
            if (IsDisposed)
            {
                return false;
            }

            placedGap = Mathf.Max(0.1f, gap);
            placedCenter = centerY;
            transform.position = new Vector3(x, 0f, 0f);
            Layout(placedGap, placedCenter);
            if (gate != null)
            {
                gate.ResetForPair();
            }

            return true;
        }

        public void MoveTo(float x)
        {
            Vector3 position = transform.position;
            position.x = x;
            transform.position = position;
        }

        public void OnSpawnedFromPool()
        {
            transform.localRotation = Quaternion.identity;
        }

        public void OnDespawnedToPool()
        {
            if (gate != null)
            {
                gate.ResetForPair();
            }
        }

        /// <summary>Places the four sprites and both hitboxes for the given opening.</summary>
        public void Layout(float gap, float centerY)
        {
            if (settings == null)
            {
                return;
            }

            float halfGap = gap * 0.5f;
            float bottomTop = centerY - halfGap;
            float topBottom = centerY + halfGap;
            float cap = settings.CapHeight;
            float width = settings.PipeWidth;

            // The pair root sits at y = 0 (screen centre). Pipe bodies run past the ground and the top of the
            // frame so a column is never shorter than the gap: only the cap ends are visible.
            float floor = DesignSpace.GroundTop;
            float ceiling = DesignSpace.HalfHeight;

            // Pipe bodies start one cap inside the opening so the cap itself never overlaps the gap.
            SetSpan(bottomPipe, floor, Mathf.Max(0.01f, bottomTop - cap - floor), width);
            SetSpan(topPipe, topBottom + cap, Mathf.Max(0.01f, ceiling - topBottom - cap), width);
            SetSpan(bottomCap, bottomTop - cap, cap, width + (settings.CapOverhang * 2f));
            SetSpan(topCap, topBottom, cap, width + (settings.CapOverhang * 2f));

            if (topHitbox != null)
            {
                topHitbox.size = new Vector2(width, Mathf.Max(0.01f, ceiling - topBottom));
                topHitbox.offset = new Vector2(0f, (ceiling + topBottom) * 0.5f);
            }

            if (bottomHitbox != null)
            {
                float bottomHeight = Mathf.Max(0.01f, bottomTop - floor);
                bottomHitbox.size = new Vector2(width, bottomHeight);
                bottomHitbox.offset = new Vector2(0f, (bottomTop + floor) * 0.5f);
            }

            if (gate != null)
            {
                gate.transform.localPosition = new Vector3(0f, centerY, 0f);
            }
        }

        /// <summary>Sizes a bottom-left pivot sprite to start at <paramref name="bottom"/>.</summary>
        private static void SetSpan(Transform part, float bottom, float height, float width)
        {
            if (part == null)
            {
                return;
            }

            part.localScale = new Vector3(width, Mathf.Max(0.01f, height), 1f);
            Vector3 position = part.localPosition;
            position.x = -width * 0.5f;
            position.y = bottom;
            part.localPosition = position;
        }

        private BoxCollider2D FindCollider(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<BoxCollider2D>() : null;
        }
    }
}
