using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Adds a line to a proof file with the current time, so a scripted editor run can be inspected.</summary>
    public static class RunnerProof
    {
        private static string PathName => Path.Combine(Path.GetTempPath(), "flappycopy_runner_proof.txt");

        public static void Write(string message)
        {
            File.AppendAllText(PathName, System.DateTime.Now.ToString("HH:mm:ss") + " " + message + "\n");
        }
    }

    /// <summary>Proves that an -executeMethod entry point of the editor project assembly runs in a scripted session.</summary>
    public static class RunnerSelfTest
    {
        public static void RunAndExit()
        {
            RunnerProof.Write("RunnerSelfTest: hello batch=" + Application.isBatchMode);
            EditorApplication.Exit(0);
        }
    }
}
