using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Finds and (re)creates the Unity Test Runner window in a scripted session. The window's content is the
    /// only part of Unity that runs the tests in a scripted editor (batch mode never ticks the update loop).
    /// </summary>
    public static class TestWindowOpener
    {
        public static void EnsureOpen()
        {
            var type = System.Type.GetType("UnityEditor.TestRunnerWindow, UnityEditor.TestRunner");
            if (type == null)
            {
                Debug.LogError("[FlappyCopy] TestRunnerWindow type not found");
                return;
            }

            Object existing = Object.FindAnyObjectByType(type);
            if (existing != null)
            {
                File.WriteAllText(ReportPath, "window already open");
                return;
            }

            try
            {
                EditorWindow window = EditorWindow.GetWindow(type);
                if (window != null)
                {
                    window.Focus();
                }

                File.WriteAllText(ReportPath, "window created: " + (window != null));
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ReportPath, "window failed: " + exception);
                Debug.LogError("[FlappyCopy] TestRunnerWindow: " + exception);
            }
        }

        private static string ReportPath => Path.Combine(Path.GetTempPath(), "flappycopy_testwindow.txt");
    }
}
