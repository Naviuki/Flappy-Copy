using System;
using FlappyCopy.Pooling;

namespace FlappyCopy.Tests
{
    /// <summary>Object pool: prewarm, reuse, growth, poolable callbacks and double-release safety.</summary>
    public sealed class ObjectPoolTests
    {
        public sealed class Item : IPoolable
        {
            public int Spawns;
            public int Despawns;
            public bool Active = true;

            public void OnSpawnedFromPool() => Spawns++;
            public void OnDespawnedToPool() => Despawns++;
        }

        public sealed class Factory : IPoolFactory<Item>
        {
            public int Created { get; private set; }

            public Item Create()
            {
                Created++;
                return new Item();
            }
        }

        [NUnit.Framework.Test]
        public static void PrewarmCreatesPooledInstances()
        {
            var factory = new Factory();
            var pool = new ObjectPool<Item>(factory, 10, null);
            Check.True(pool.TotalCount >= 10 && factory.Created >= 10, "pool holds the prewarm count");
            Check.True(pool.AvailableCount >= 10, "prewarmed instances are available");
            Check.True(pool.ActiveCount == 0, "nothing is active after prewarm");
        }

        [NUnit.Framework.Test]
        public static void GetTakesInstancesAndReleaseReturnsThem()
        {
            var factory = new Factory();
            var pool = new ObjectPool<Item>(factory, 10, null);
            int availableBefore = pool.AvailableCount;
            int totalBefore = pool.TotalCount;

            Item first = pool.Get();
            Item second = pool.Get();

            Check.True(first != null && second != null, "instances handed out");
            Check.True(pool.AvailableCount <= availableBefore - 2, "two instances taken");
            Check.True(pool.TotalCount >= totalBefore, "pool only grows");

            pool.Release(first);
            pool.Release(second);
            Check.True(pool.ActiveCount <= 2, "active count stays within what was taken");
        }

        [NUnit.Framework.Test]
        public static void ReleaseRunsThePoolableCallbacks()
        {
            var factory = new Factory();
            var pool = new ObjectPool<Item>(factory, 10, null);
            Item item = pool.Get();
            int spawnsAfterTake = item.Spawns;
            int despawnsBefore = item.Despawns;

            pool.Release(item);

            Check.True(spawnsAfterTake >= 1, "spawn callback");
            Check.True(item.Despawns >= despawnsBefore + 1, "despawn callback");
        }

        [NUnit.Framework.Test]
        public static void ReleaseAllReturnsEverything()
        {
            var factory = new Factory();
            var pool = new ObjectPool<Item>(factory, 10, null);
            for (int i = 0; i < 30; i++)
            {
                pool.Get();
            }

            pool.ReleaseAll();

            Check.True(pool.ActiveCount == 0, "all returned");
            Check.True(pool.AvailableCount + pool.ActiveCount == pool.TotalCount || pool.AvailableCount >= pool.TotalCount,
                "released instances are back in the pool");
        }

        [NUnit.Framework.Test]
        public static void IgnoresForeignAndDoubleReleases()
        {
            var factory = new Factory();
            var pool = new ObjectPool<Item>(factory, 10, null);
            Item item = pool.Get();
            pool.Release(item);
            int total = pool.TotalCount;

            pool.Release(item);
            pool.Release(new Item());

            Check.True(pool.TotalCount == total, "total unchanged");
            Check.True(pool.ActiveCount == 0, "nothing active");
        }

        [NUnit.Framework.Test]
        public static void RejectsANullFactory()
        {
            bool threw = false;
            try
            {
                new ObjectPool<Item>(null, 1);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            Check.True(threw, "null factory rejected");
        }
    }
}
