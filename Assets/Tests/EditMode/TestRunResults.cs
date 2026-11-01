using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// TestRunner API callbacks: one PASS/FAIL line per tested fixture, the JSON summary and the log written
    /// before the editor quits with the result code (0 = every fixture ran and no test failed). A run without a
    /// single tested fixture is a failure, never a pass. A watchdog ends a stalled run instead of hanging.
    /// </summary>
    public sealed class TestRunResults : ICallbacks
    {
        private readonly string jsonPath;
        private readonly string logPath;
        private readonly StringBuilder lines = new StringBuilder();
        private readonly StringBuilder current = new StringBuilder();
        private Action<int> quit;
        private double started;
        private int passed;
        private int failed;
        private int fixtures;
        private bool finished;

        public TestRunResults(string jsonPath, string logPath)
        {
            this.jsonPath = jsonPath;
            this.logPath = logPath;
        }

        /// <summary>Starts the watchdog; <paramref name="quitAction"/> receives the result code.</summary>
        public void Start(Action<int> quitAction)
        {
            quit = quitAction;
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Watchdog;
        }

        private void Watchdog()
        {
            if (started > 0 && EditorApplication.timeSinceStartup - started > 900)
            {
                EditorApplication.update -= Watchdog;
                Finish(1, "timeout: the run did not finish in 15 minutes");
            }
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log("[FlappyCopy.Tests] run started");
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.HasChildren)
            {
                // A leaf test: remember it until its fixture reports, so a fixture is one line in the log.
                current.Append(result.TestStatus == TestStatus.Passed ? "PASS " : "FAIL ").Append(result.Name);
                if (result.TestStatus != TestStatus.Passed)
                {
                    failed++;
                    current.Append(" :: ").Append(result.Message);
                    Debug.LogError("[FlappyCopy.Tests] FAIL " + result.Name + " :: " + result.Message);
                }
                else
                {
                    passed++;
                }

                current.AppendLine();
                return;
            }

            fixtures++;
            lines.Append(result.Name).Append("  (").Append(result.PassCount).Append(" passed, ")
                .Append(result.FailCount).Append(" failed, ").Append(result.SkipCount).Append(" skipped)")
                .AppendLine();
            lines.Append(current);
            current.Clear();
            Debug.Log("[FlappyCopy.Tests] " + result.Name + ": " + result.PassCount + " passed, " + result.FailCount + " failed");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (fixtures == 0)
            {
                Finish(1, "no test fixture ran (the test runner did not start the suite)");
                return;
            }

            Finish(failed == 0 ? 0 : 1, null);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        private void Finish(int exitCode, string reason)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            EditorApplication.update -= Watchdog;
            string json = reason != null
                ? "{\"result\":\"failed\",\"total\":0,\"passed\":0,\"failed\":1,\"reason\":\"" + reason + "\"}"
                : "{\"result\":\"" + (exitCode == 0 ? "passed" : "failed") + "\",\"total\":" + (passed + failed)
                  + ",\"passed\":" + passed + ",\"failed\":" + failed + ",\"fixtures\":" + fixtures + "}";
            File.WriteAllText(jsonPath, json);
            File.WriteAllText(logPath, lines.ToString());
            Debug.Log("[FlappyCopy.Tests] run finished: " + json);
            quit?.Invoke(exitCode);
        }
    }
}
