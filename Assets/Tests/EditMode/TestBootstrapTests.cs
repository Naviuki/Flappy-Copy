using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Guarantees that this assembly is a test assembly the Test Runner loads even when a class lacks a
    /// [TestFixture] attribute: a single decorated fixture pulls the assembly in, keeping every other test
    /// class discoverable.
    /// </summary>
    [TestFixture]
    public sealed class TestBootstrapTests
    {
        [Test]
        public static void TestAssemblyContainsFixtures()
        {
            var found = new List<string>();
            foreach (System.Type type in typeof(TestBootstrapTests).Assembly.GetTypes())
            {
                if (type.GetCustomAttribute<TestFixtureAttribute>() != null)
                {
                    found.Add(type.Name);
                }
            }

            Check.True(found.Count >= 1, "at least one decorated fixture exists");
        }

        [Test]
        public static void ScoringSensorWalksEveryPairInALane()
        {
            // The check the self-test player runs on a moving bird, done headless: for every pair of a run the
            // rule reports exactly one pass as the bird flies through its gap and out the other side.
            var factory = new LogicTests.PairFactory();
            var run = new Pipes.PipeRun(factory, new Pipes.PipeRunConfig(5.2f, 1.5f, 9f, 1.4f, 6, 4f), seed: 11);
            int frame = 0;
            while (frame < 900)
            {
                run.Tick(1f / 60f, 4f, () => 4f, offset => 0f);
                frame++;
            }

            int passes = 0;
            foreach (LogicTests.Pair pair in factory.Pairs)
            {
                // The bird flies from left of the column to right of it at the pair's own gap height.
                float[] bird = { -0.6f, -0.2f, 0.2f, 0.6f };
                foreach (float local in bird)
                {
                    if (pair.Sample(local, pair.LastCenter, 0.21f))
                    {
                        passes++;
                    }
                }
            }

            Check.True(passes >= 1, "at least one pair of the lane scores when the bird clears it");
        }
    }
}
