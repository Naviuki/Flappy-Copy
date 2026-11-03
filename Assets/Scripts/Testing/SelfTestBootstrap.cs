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
    /// must end), passes one pipe pair through the scene's own scoring sensor, writes the report next to the
    /// player and quits with 0 when everything passed. A normal build compiles none of this code.
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

        // Captures for the docs: an early one of the run in progress, and a later one that replaces it only
        // when a pipe pair is actually inside the frame.
        private const float ScreenshotAtSeconds = 0.25f;
        private const float SecondScreenshotAtSeconds = 0.9f;

        // The score check moves one pair over the bird; it starts as soon as the run is live.
        private const float ScoreCheckAtSeconds = 0.35f;

        private readonly List<string> lines = new List<string>();
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private int frame;
        private int failures;
        private bool checksDone;
        private bool pipesSeen;
        private bool shotTaken;
        private bool secondShotTaken;
        private bool scoreChecked;
        private bool scorePassed;
        private bool finished;
        private float runStartedAt = -1f;

        // Score check state.
        private PipePair scoringPair;
        private GameController scoringGame;
        private Vector3 scoringOriginal;
        private float scoringStart;
        private float scoringTarget;
        private bool scoringStarted;
        private int scoringFrames;
        private int scoreBeforeCheck;

        private static string PanoramaPath => SelfTestReport.PathOf("result.png");

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

            if (!scoreChecked && elapsed >= ScoreCheckAtSeconds)
            {
                scorePassed = StartScoringCheck(controller);
            }

            if (scoringStarted)
            {
                StepScoringCheck();
            }

            if (!shotTaken && elapsed >= ScreenshotAtSeconds)
            {
                shotTaken = true;
                ScreenCapture.CaptureScreenshot(SelfTestReport.PathOf("gameplay.png"));
            }

            if (shotTaken && !secondShotTaken && elapsed >= SecondScreenshotAtSeconds)
            {
                secondShotTaken = true;
                if (FindObjectsByType<PipePair>().Length > 0)
                {
                    ScreenCapture.CaptureScreenshot(SelfTestReport.PathOf("gameplay.png"));
                }
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

        /// <summary>
        /// Starts the score check: takes a live pair, gives it a gap around the bird and puts it just right of
        /// the bird. The following frames walk it left, through the scene's own scoring sensor.
        /// </summary>
        private bool StartScoringCheck(GameController controller)
        {
            PipePair[] pairs = FindObjectsByType<PipePair>();
            if (pairs.Length == 0)
            {
                return false;
            }

            scoringGame = controller;
            scoreBeforeCheck = controller.Score.Score;
            scoringPair = pairs[0];
            scoringOriginal = scoringPair.transform.position;
            float birdX = controller.Bird.transform.position.x;
            float birdY = controller.Bird.transform.position.y;

            // The bird first sits inside the gap, then the pair slides left until the bird is behind it.
            scoringStart = birdX + 0.6f;
            scoringTarget = birdX - 1.4f;
            scoringPair.Activate(scoringStart, 6f, birdY, null);
            scoringStarted = true;
            scoringFrames = 0;
            SetShutter(true);
            return true;
        }

        /// <summary>Moves the scoring pair across the bird one step per frame, then restores it.</summary>
        private void StepScoringCheck()
        {
            if (!scoringStarted || scoringPair == null)
            {
                return;
            }

            const int settleFrames = 3;
            const int steps = 12;
            if (scoringFrames < settleFrames)
            {
                scoringPair.MoveTo(scoringStart);
            }
            else if (scoringFrames < settleFrames + steps)
            {
                float t = (scoringFrames - settleFrames + 1) / (float)steps;
                scoringPair.MoveTo(Mathf.Lerp(scoringStart, scoringTarget, t));
            }
            else
            {
                scoringPair.transform.position = scoringOriginal;
                scorePassed = scoringGame.Score.Score >= scoreBeforeCheck + 1;
                scoringStarted = false;
                SetShutter(false);
            }

            scoringFrames++;
        }

        /// <summary>Hides every renderer for the score check, then gives back exactly the ones it hid.</summary>
        private void SetShutter(bool closed)
        {
            if (closed)
            {
                foreach (Renderer renderer in FindObjectsByType<Renderer>())
                {
                    if (renderer.enabled)
                    {
                        hiddenRenderers.Add(renderer);
                        renderer.enabled = false;
                    }
                }

                return;
            }

            foreach (Renderer renderer in hiddenRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            hiddenRenderers.Clear();
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
            failures += scorePassed
                ? Pass("ClearingAPipeAddsAPoint")
                : Fail("ClearingAPipeAddsAPoint", "passing a pair did not add a point");
            Log("done failures=" + failures);
            finished = true;

            // The result panel is up now: one more capture that shows the whole result UI as well.
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
