using UnityEngine;

namespace FlappyCopy.Pooling
{
    /// <summary>Creates pool members by cloning a prefab under a container.</summary>
    public sealed class PrefabPoolFactory<T> : IPoolFactory<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform container;

        public PrefabPoolFactory(T prefab, Transform container = null)
        {
            this.prefab = prefab;
            this.container = container;
        }

        public T Create()
        {
            T instance = Object.Instantiate(prefab, container, false);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
