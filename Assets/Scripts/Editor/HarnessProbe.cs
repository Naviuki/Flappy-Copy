using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Test-bed entry point for the CLI harness: runs the real Unity build and reports JSON.</summary>
    public static class HarnessProbe
    {
        public const string DefaultReportPath = "Temp/harness/build_probe.json";

        [UnityEditor.MenuItem("Flappy Copy/Checks/Probe Build (Harness)")]
        public static void RunFromMenu()
        {
            UnityEngine.Debug.Log(Run(DefaultReportPath));
        }

        /// <summary>
        /// -executeMethod FlappyCopy.EditorTools.HarnessProbe.RunAndExit
        /// Writes a JSON report to -harnessReport &lt;path&gt; (or <see cref="DefaultReportPath"/>).
        /// </summary>
        public static void RunAndExit()
        {
            string reportPath = ReadArg("-harnessReport", DefaultReportPath);
            string json = Run(reportPath);
            UnityEngine.Debug.Log("[FlappyCopy] " + json);
            UnityEditor.EditorApplication.Exit(json.Contains("\"result\":\"succeeded\"") ? 0 : 1);
        }

        public static string Run(string reportPath)
        {
            string[] scenes = { ProjectPaths.ScenePath };
            var options = new UnityEditor.BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Temp/harness/FlappyCopyProbe.exe",
                target = UnityEditor.BuildTarget.StandaloneWindows64,
                options = UnityEditor.BuildOptions.Development
            };

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? "Temp");
            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            var files = new List<string>();
            foreach (UnityEditor.Build.Reporting.BuildFile file in report.GetFiles())
            {
                files.Add(file.path.Replace('\\', '/') + ":" + file.size);
            }

            string json = "{ \"result\": \"" + summary.result.ToString().ToLowerInvariant() + "\", "
                          + "\"totalErrors\": " + summary.totalErrors + ", "
                          + "\"totalWarnings\": " + summary.totalWarnings + ", "
                          + "\"totalSize\": " + summary.totalSize + ", "
                          + "\"scenes\": \"" + ProjectPaths.ScenePath + "\", "
                          + "\"files\": [" + string.Join(", ", files.ToArray()) + "] }";
            File.WriteAllText(reportPath, json);
            return json;
        }

        private static string ReadArg(string name, string fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
