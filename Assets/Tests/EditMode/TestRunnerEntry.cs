using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Starts the EditMode suite through Unity's TestRunner API from a scripted, windowed editor session and
    /// writes Temp/harness/editmode_results.json (+ .log): each check is a [Test] method of this project's
    /// fixtures, so the scene checks run on the main thread with the real Test Runner.
    /// Called by Tools/run_tests.bat (Unity must run without -batchmode).
    /// </summary>
    public static class TestRunnerEntry
    {
        private const string ResultsDefault = "Temp/harness/editmode_results";

        public static void RunEditModeAndExit()
        {
            System.IO.File.AppendAllText(Path.Combine(Path.GetTempPath(), "flappycopy_runner_proof.txt"),
                "RunEditModeAndExit at " + System.DateTime.Now.ToString("HH:mm:ss") + " batch=" + Application.isBatchMode + "\n");
            if (Application.isBatchMode)
            {
                Debug.LogError("[FlappyCopy.Tests] The TestRunner API needs a scripted editor without -batchmode.");
                EditorApplication.Exit(2);
                return;
            }

            Debug.Log("[FlappyCopy.Tests] EditMode run requested...");
            EditorApplication.delayCall += Run;
            EditorApplication.update += Heartbeat;
        }

        private static void Heartbeat()
        {
            System.IO.File.AppendAllText(Path.Combine(Path.GetTempPath(), "flappycopy_runner_proof.txt"),
                "tick at " + System.DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        private static void Run()
        {
            Debug.Log("[FlappyCopy.Tests] starting the EditMode run");
            string results = Path.GetFullPath(ResultsDefault);
            Directory.CreateDirectory(Path.GetDirectoryName(results) ?? "Temp");
            var sink = new TestRunResults(results + ".json", results + ".log");
            sink.Start(exitCode =>
            {
                EditorApplication.update -= Heartbeat;
                EditorApplication.Exit(exitCode);
            });

            var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            api.RegisterCallbacks(sink);
            api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(
                new UnityEditor.TestTools.TestRunner.Api.Filter
                {
                    testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode
                }));
        }
    }
}
