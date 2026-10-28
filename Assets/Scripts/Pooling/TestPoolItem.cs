using System;

namespace FlappyCopy.Pooling
{
    /// <summary>
    /// Plain pooled item used by the checks (no editor-only objects). It numbers itself so a run can detect
    /// a stale instance from an earlier pool or session.
    /// </summary>
    public sealed class TestPoolItem : IPoolable
    {
        public readonly int Serial;
        public int Spawns;
        public int Despawns;
        public bool Active = true;

        public TestPoolItem(int serial)
        {
            Serial = serial;
        }

        public void OnSpawnedFromPool() => Spawns++;
        public void OnDespawnedToPool() => Despawns++;
    }

    /// <summary>Factory for <see cref="TestPoolItem"/> that counts and numbers created instances.</summary>
    public sealed class TestPoolFactory : IPoolFactory<TestPoolItem>
    {
        private int created;

        public int Created => created;

        public TestPoolItem Create()
        {
            created++;
            return new TestPoolItem(created);
        }
    }
}
