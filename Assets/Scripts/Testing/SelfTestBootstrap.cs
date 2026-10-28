#if FLAPPYCOPY_SELF_TEST
using UnityEngine;

namespace FlappyCopy.Testing
{
    /// <summary>
    /// Self-test hook of the dedicated test build (FLAPPYCOPY_SELF_TEST): starts a runner that checks the
    /// loaded game scene and quits with the result. A normal build compiles none of this code.
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

    /// <summary>Waits a few frames, runs the checks, writes the report and quits with 0 when they all pass.</summary>
    public sealed class SelfTestRunner : MonoBehaviour
    {
        private int frame;

        private void Awake()
        {
            Log("runner awake scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            frame++;
            if (frame < 5)
            {
                return;
            }

            enabled = false;
            Log("running checks");
            string report = "failures=0\n";
            int failures = 0;
            failures += Run(ref report, "GameControllerHasEveryReference", SelfTestChecks.GameControllerHasEveryReference);
            failures += Run(ref report, "HudIsWiredToTheGame", SelfTestChecks.HudIsWiredToTheGame);
            failures += Run(ref report, "BirdPrefabIsTriggerOnThePlayerLayer", SelfTestChecks.BirdPrefabIsTriggerOnThePlayerLayer);
            failures += Run(ref report, "PipePairPrefabIsTriggerOnThePipeLayer", SelfTestChecks.PipePairPrefabIsTriggerOnThePipeLayer);
            failures += Run(ref report, "LayersSortingLayersAndCollisions", SelfTestChecks.LayersSortingLayersAndCollisions);
            report = report.Replace("failures=0", "failures=" + failures);
            SelfTestReport.Write(report);
            Log("done failures=" + failures);
            Application.Quit(failures == 0 ? 0 : 1);
        }

        private static void Log(string message)
        {
            Debug.Log("[FlappyCopy.SelfTest] " + message);
            SelfTestReport.Write("progress: " + message + "\n");
        }

        private static int Run(ref string report, string name, System.Action check)
        {
            try
            {
                check();
                report += "PASS " + name + "\n";
                return 0;
            }
            catch (System.Exception exception)
            {
                report += "FAIL " + name + " :: " + exception.Message + "\n";
                return 1;
            }
        }
    }

    /// <summary>Writes the self-test report into the project's Temp folder (the working directory of the player).</summary>
    public static class SelfTestReport
    {
        public static void Write(string text)
        {
            try
            {
                string path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Temp", "scenechecks.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? "Temp");
                System.IO.File.WriteAllText(path, text);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("self-test report: " + exception.Message);
            }
        }
    }
}
#endif
