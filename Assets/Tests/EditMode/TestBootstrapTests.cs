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
    }
}
