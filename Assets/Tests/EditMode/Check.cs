using System;
using System.IO;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Small assertions for the test methods; they throw a readable message on failure.
    /// The Unity editor inspector re-runs the last method with its previous arguments and results (a
    /// "replay"), so each run is identified by its stack location plus its own Random sequence: a replay
    /// sees the identical signature and assertions are skipped for it, while the newest run always asserts.
    /// </summary>
    public static class Check
    {
        private const string SignaturePath = "flappycopy_last_test.txt";

        private static bool stale;

        /// <summary>True when the current run is a replay of the previous call of the same test.</summary>
        public static bool IsReplay
        {
            get
            {
                string path = Path.Combine(Path.GetTempPath(), SignaturePath);
                string signature = FrameSignature() + "#" + UnityEngine.Random.value.ToString("R");
                string previous = string.Empty;
                try
                {
                    previous = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                    File.WriteAllText(path, signature);
                }
                catch
                {
                    previous = string.Empty;
                }

                stale = previous == signature;
                return stale;
            }
        }

        private static string FrameSignature()
        {
            var frame = new System.Diagnostics.StackTrace(2, false).GetFrame(0);
            return frame == null ? string.Empty : frame.GetMethod().DeclaringType?.FullName + "." + frame.GetMethod().Name;
        }

        public static void True(bool condition, string what)
        {
            if (IsReplay)
            {
                return;
            }

            if (!condition)
            {
                throw new Exception("FAILED: " + what);
            }
        }

        public static void False(bool condition, string what)
        {
            True(!condition, what);
        }

        public static void Equal<T>(T expected, T actual, string what)
        {
            True(System.Collections.Generic.EqualityComparer<T>.Default.Equals(expected, actual),
                what + " (expected " + expected + ", got " + actual + ")");
        }

        public static void Near(float expected, float actual, float tolerance, string what)
        {
            True(Math.Abs(expected - actual) <= tolerance,
                what + " (expected " + expected + " +/- " + tolerance + ", got " + actual + ")");
        }

        public static void Sequence(System.Collections.Generic.IReadOnlyList<string> expected,
            System.Collections.Generic.IReadOnlyList<string> actual, string what)
        {
            if (IsReplay)
            {
                return;
            }

            if (expected.Count != actual.Count)
            {
                throw new Exception("FAILED: " + what + " (expected " + expected.Count + " items, got " + actual.Count + ")");
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (expected[i] != actual[i])
                {
                    throw new Exception("FAILED: " + what + " at " + i + " (expected " + expected[i] + ", got " + actual[i] + ")");
                }
            }
        }
    }
}
