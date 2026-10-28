using System;
using System.Collections.Generic;

namespace FlappyCopy.Pooling
{
    /// <summary>Notifications a pooled behaviour receives. All calls happen on the spawning thread.</summary>
    public interface IPoolable
    {
        void OnSpawnedFromPool();
        void OnDespawnedToPool();
    }

    /// <summary>Pool contract used by gameplay code; keeps Instantiate/Destroy out of the hot path.</summary>
    public interface IObjectPool<T> where T : class
    {
        int TotalCount { get; }
        int ActiveCount { get; }
        int AvailableCount { get; }

        T Get();
        void Release(T instance);
        void ReleaseAll();
    }

    /// <summary>Factory indirection so tests can substitute tiny fakes for prefabs.</summary>
    public interface IPoolFactory<out T> where T : class
    {
        T Create();
    }

    /// <summary>
    /// Stack based object pool. Pre-creates <paramref name="prewarm"/> instances, grows only when the
    /// pool is exhausted and never destroys instances while it is alive.
    /// </summary>
    public sealed class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly Stack<T> available;
        private readonly List<T> all;
        private readonly HashSet<T> active;
        private readonly IPoolFactory<T> factory;
        private readonly Action<T, bool> setActive;

        public ObjectPool(IPoolFactory<T> factory, int prewarm = 0, Action<T, bool> setActive = null)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.setActive = setActive;
            available = new Stack<T>(Math.Max(1, prewarm));
            all = new List<T>(Math.Max(1, prewarm));
            active = new HashSet<T>();

            for (int i = 0; i < prewarm; i++)
            {
                Despawn(CreateInstance());
            }
        }

        public int TotalCount => all.Count;
        public int ActiveCount => active.Count;
        public int AvailableCount => available.Count;

        public T Get()
        {
            T instance = available.Count > 0 ? available.Pop() : CreateInstance();
            active.Add(instance);
            SetActive(instance, true);
            if (instance is IPoolable poolable)
            {
                poolable.OnSpawnedFromPool();
            }

            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null || !active.Remove(instance))
            {
                return;
            }

            Despawn(instance);
        }

        public void ReleaseAll()
        {
            if (active.Count == 0)
            {
                return;
            }

            var snapshot = new List<T>(active);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Release(snapshot[i]);
            }
        }

        private T CreateInstance()
        {
            T instance = factory.Create();
            if (instance == null)
            {
                throw new InvalidOperationException("Pool factory returned null for " + typeof(T).Name);
            }

            all.Add(instance);
            Despawn(instance);
            return instance;
        }

        private void Despawn(T instance)
        {
            if (instance is IPoolable poolable)
            {
                poolable.OnDespawnedToPool();
            }

            SetActive(instance, false);
            available.Push(instance);
        }

        private void SetActive(T instance, bool isActive)
        {
            setActive?.Invoke(instance, isActive);
        }
    }
}
