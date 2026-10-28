using System;
using System.IO;
using UnityEngine;

namespace FlappyCopy.Testing
{
    /// <summary>Writes its own log next to the player, so a headless self-test can be inspected.</summary>
    public static class SelfTestLog
    {
        private static readonly string PathName = Path.Combine(Application.dataPath, "..", "selftest_output.txt");

        public static void Write(string message)
        {
            try
            {
                File.AppendAllText(PathName, "[" + Time.realtimeSinceStartup.ToString("F2") + "] " + message + Environment.NewLine);
            }
            catch (Exception)
            {
                // Logging must never break the self-test.
            }
        }
    }
}
