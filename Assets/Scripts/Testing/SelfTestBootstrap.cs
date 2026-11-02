#if FLAPPYCOPY_SELF_TEST
using System.Collections.Generic;
using FlappyCopy.Core;
using FlappyCopy.Pipes;
using UnityEngine;

namespace FlappyCopy.Testing
{
    /// <summary>
    /// Self-test hook of the dedicated test build (FLAPPYCOPY_SELF_TEST): starts a runner that checks the
    /// loaded game scene, plays one short run without flapping (the bird must fall into the ground and the run
    /// must end), writes the report next to the player and quits with 0 when everything passed.
    /// A normal build compiles none of this code.
    /// </summary>
    public static class SelfTestBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            try
            {
                var runner = new GameObject("FlappyCopy.SelfTest");
                Object.DontDestroyOnLoad(runner);
                runner.AddComponent<SelfTestRunner>();
            }
            catch (System.Exception exception)
            {
                SelfTestReport.Write("failures=1\nFAIL bootstrap :: " + exception.Message);
            }
        }
    }

    /// <summary>Waits a few frames, runs the checks, plays one run, writes the report and quits.</summary>
    public sealed class SelfTestRunner : MonoBehaviour
    {
        private const float RunLimitSeconds = 10f;
        private const float ScreenshotAtSeconds = 0.25f;

        private readonly List<string> lines = new List<string>();
        private int frame;
        private int failures;
        private bool checksDone;
        private bool pipesSeen;
        private bool shotTaken;
        private string PanoramaPath => SelfTestReport.PathOf("result.png");
        private bool finished;
        private float runStartedAt = -1f;

        private void Awake()
        {
            Log("runner awake scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            frame++;
            if (frame < 5 || finished)
            {
                return;
            }

            if (!checksDone)
            {
                checksDone = true;
                RunChecks();
                GameController game = FindGame();
                if (game == null)
                {
                    FinishRun(false, "no GameController");
                    return;
                }

                game.RequestPress();
                runStartedAt = Time.realtimeSinceStartup;
                return;
            }

            GameController controller = FindGame();
            if (controller == null)
            {
                FinishRun(false, "the GameController disappeared");
                return;
            }

            if (!pipesSeen && FindObjectsByType<PipePair>().Length > 0)
            {
                pipesSeen = true;
            }

            float elapsed = Time.realtimeSinceStartup - runStartedAt;

            // One shot of the run in progress, for the docs: the HUD, the bird and the first pipe pair.
            if (!shotTaken && elapsed >= ScreenshotAtSeconds)
            {
                shotTaken = true;
                ScreenCapture.CaptureScreenshot(SelfTestReport.PathOf("gameplay.png"));
            }

            if (controller.State == GameState.GameOver)
            {
                FinishRun(true, "the run ended after " + elapsed.ToString("F2") + "s");
                return;
            }

            if (elapsed > RunLimitSeconds)
            {
                FinishRun(false, "the bird was still flying after " + RunLimitSeconds + "s without flaps");
            }
        }

        private static GameController FindGame()
        {
            return FindAnyObjectByType<GameController>();
        }

        private void RunChecks()
        {
            failures += Run("GameControllerHasEveryReference", SelfTestChecks.GameControllerHasEveryReference);
            failures += Run("HudIsWiredToTheGame", SelfTestChecks.HudIsWiredToTheGame);
            failures += Run("BirdPrefabIsTriggerOnThePlayerLayer", SelfTestChecks.BirdPrefabIsTriggerOnThePlayerLayer);
            failures += Run("PipePairPrefabIsTriggerOnThePipeLayer", SelfTestChecks.PipePairPrefabIsTriggerOnThePipeLayer);
            failures += Run("LayersSortingLayersAndCollisions", SelfTestChecks.LayersSortingLayersAndCollisions);
        }

        private void FinishRun(bool crashed, string detail)
        {
            failures += crashed ? Pass("RunStartsAndFallsIntoTheGround") : Fail("RunStartsAndFallsIntoTheGround", detail);
            failures += pipesSeen ? Pass("PipesAreSpawned") : Fail("PipesAreSpawned", "no pipe pair appeared");
            Log("done failures=" + failures);
            finished = true;

            // The result panel is up now: one last capture that shows the whole menu/result UI as well.
            if (crashed && !System.IO.File.Exists(PanoramaPath))
            {
                ScreenCapture.CaptureScreenshot(PanoramaPath);
            }

            var report = new System.Text.StringBuilder();
            report.Append("failures=").Append(failures).AppendLine();
            foreach (string line in lines)
            {
                report.AppendLine(line);
            }

            SelfTestReport.Write(report.ToString());
            Application.Quit(failures == 0 ? 0 : 1);
        }

        private int Pass(string name)
        {
            lines.Add("PASS " + name);
            return 0;
        }

        private int Fail(string name, string detail)
        {
            lines.Add("FAIL " + name + " :: " + detail);
            Debug.LogError("[FlappyCopy.SelfTest] FAIL " + name + " :: " + detail);
            return 1;
        }

        private int Run(string name, System.Action check)
        {
            try
            {
                check();
                return Pass(name);
            }
            catch (System.Exception exception)
            {
                return Fail(name, exception.Message);
            }
        }

        private static void Log(string message)
        {
            Debug.Log("[FlappyCopy.SelfTest] " + message);
            SelfTestReport.Append("progress: " + message);
        }
    }

    /// <summary>Writes the self-test output next to the test player, so a headless run can be inspected.</summary>
    public static class SelfTestReport
    {
        /// <summary>Folder of the player executable (Application.dataPath is "&lt;player&gt;_Data").</summary>
        private static string Folder
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Application.dataPath)
                       ?? System.IO.Directory.GetCurrentDirectory();
            }
        }

        /// <summary>Path of a file written next to the player executable.</summary>
        public static string PathOf(string fileName)
        {
            return System.IO.Path.Combine(Folder, fileName);
        }

        private static string ReportPath => PathOf("selftest_output.txt");

        public static void Write(string text)
        {
            try
            {
                System.IO.File.WriteAllText(ReportPath, text);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("self-test report: " + exception.Message);
            }
        }

        public static void Append(string line)
        {
            try
            {
                System.IO.File.AppendAllText(ReportPath, line + System.Environment.NewLine);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("self-test report: " + exception.Message);
            }
        }
    }
}
#endif
