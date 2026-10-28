using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// TestRunner API callbacks: one PASS/FAIL line per test, a JSON summary, a readable log, then the editor
    /// quits with the result code (0 = every test passed). A watchdog ends a stalled run instead of hanging.
    /// </summary>
    public sealed class TestRunResults : ICallbacks
    {
        private readonly string jsonPath;
        private readonly string logPath;
        private readonly StringBuilder lines = new StringBuilder();
        private Action<int> quit;
        private double started;
        private int passed;
        private int failed;

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
                File.WriteAllText(jsonPath, "{\"result\":\"timeout\",\"total\":0,\"passed\":0,\"failed\":1}");
                Debug.LogError("[FlappyCopy.Tests] timeout waiting for the test run");
                quit?.Invoke(3);
            }
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log("[FlappyCopy.Tests] run started");
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren)
            {
                return;
            }

            if (result.TestStatus == TestStatus.Passed)
            {
                passed++;
                lines.AppendLine("PASS " + result.Name);
            }
            else
            {
                failed++;
                lines.AppendLine("FAIL " + result.Name + " :: " + result.Message);
                Debug.LogError("[FlappyCopy.Tests] FAIL " + result.Name + " :: " + result.Message);
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            EditorApplication.update -= Watchdog;
            string json = "{\"result\":\"" + (failed == 0 && passed > 0 ? "passed" : "failed") + "\","
                          + "\"total\":" + (passed + failed) + ",\"passed\":" + passed + ",\"failed\":" + failed + "}";
            File.WriteAllText(jsonPath, json);
            File.WriteAllText(logPath, lines.ToString());
            Debug.Log("[FlappyCopy.Tests] run finished: " + json);
            quit?.Invoke(failed == 0 && passed > 0 ? 0 : 1);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }
    }
}
