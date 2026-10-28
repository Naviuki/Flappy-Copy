using System;
using UnityEngine;

namespace FlappyCopy.Core
{
    /// <summary>Reports a body overlap, with the layer name and a point on the surface.</summary>
    public interface ICollisionListener
    {
        void OnCollisionEntered(Collider2D other, string layerName, Vector2 contactPoint);
    }

    /// <summary>
    /// Forwards OnTriggerEnter2D to <see cref="ICollisionListener"/> on the same GameObject.
    /// Layers are resolved by name so the physics matrix can stay untouched.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CollisionRelay : MonoBehaviour
    {
        private ICollisionListener listener;

        private void Awake()
        {
            listener = GetComponent<ICollisionListener>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (listener == null)
            {
                return;
            }

            Vector2 point = other != null ? other.ClosestPoint(transform.position) : (Vector2)transform.position;
            listener.OnCollisionEntered(other, LayerName(other), point);
        }

        private static string LayerName(Collider2D other)
        {
            if (other == null)
            {
                return string.Empty;
            }

            int layer = other.gameObject.layer;
            string name = LayerMask.LayerToName(layer);
            return string.IsNullOrEmpty(name) ? layer.ToString() : name;
        }
    }
}
