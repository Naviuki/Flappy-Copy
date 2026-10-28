using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Editor entry point for the EditMode checks (CI and the CLI harness): runs every real [Test] method
    /// of the project's test fixtures, prints one PASS/FAIL line per test and writes
    /// Temp/harness/editmode_results.json plus a .log. Optional: "-testFilter &lt;name part&gt;".
    /// </summary>
    public static class EditModeTestEntry
    {
        private const string ResultsPath = "Temp/harness/editmode_results";

        [MenuItem("Flappy Copy/Checks/Run EditMode Tests")]
        public static void RunFromMenu()
        {
            Run(exit: false);
        }

        public static void RunAndExit()
        {
            Run(exit: true);
        }

        private static void Run(bool exit)
        {
            string filter = ReadArgument("-testFilter") ?? string.Empty;
            List<string> lines = new List<string>();
            List<string> failures = new List<string>();
            foreach (string name in Discover())
            {
                if (filter.Length > 0 && !name.Contains(filter))
                {
                    continue;
                }

                string result = Invoke(name);
                if (result == null)
                {
                    lines.Add("PASS " + name);
                    Debug.Log("[FlappyCopy.Tests] PASS " + name);
                }
                else
                {
                    lines.Add("FAIL " + name + " :: " + result);
                    failures.Add(name);
                    Debug.LogError("[FlappyCopy.Tests] FAIL " + name + " :: " + result);
                }
            }

            string path = Path.GetFullPath(ResultsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Temp");
            string json = "{\"result\":\"" + (failures.Count == 0 && lines.Count > 0 ? "passed" : "failed") + "\","
                          + "\"total\":" + lines.Count + ",\"passed\":" + (lines.Count - failures.Count)
                          + ",\"failed\":" + failures.Count + "}";
            File.WriteAllText(path + ".json", json);
            File.WriteAllLines(path + ".log", lines);
            Debug.Log("[FlappyCopy.Tests] finished: " + json);
            if (exit)
            {
                EditorApplication.Exit(failures.Count == 0 && lines.Count > 0 ? 0 : 1);
            }
        }

        /// <summary>"Fixture.Method" for every parameterless [Test] method, grouped by fixture.</summary>
        public static List<string> Discover()
        {
            List<string> names = new List<string>();
            var assembly = typeof(EditModeTestEntry).Assembly;
            foreach (Type type in assembly.GetTypes())
            {
                if (!type.Name.EndsWith("Tests", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false).Length > 0
                        && method.GetParameters().Length == 0)
                    {
                        names.Add(type.Name + "." + method.Name);
                    }
                }
            }

            return names;
        }

        private static string Invoke(string name)
        {
            int dot = name.IndexOf('.');
            var assembly = typeof(EditModeTestEntry).Assembly;
            Type type = assembly.GetType(typeof(EditModeTestEntry).Namespace + "." + name.Substring(0, dot));
            MethodInfo method = type?.GetMethod(name.Substring(dot + 1), BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                return "test not found";
            }

            try
            {
                method.Invoke(null, null);
                return null;
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                return inner.GetType().Name + ": " + inner.Message;
            }
            catch (Exception exception)
            {
                return exception.GetType().Name + ": " + exception.Message;
            }
        }

        private static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
