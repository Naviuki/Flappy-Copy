using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Fresh-code gate used by the test scripts before running checks.</summary>
    public static class SelfCheckEntry
    {
        public static void RunAndExit()
        {
            Debug.Log("[FlappyCopy] self check: editor is running fresh code");
            EditorApplication.Exit(0);
        }
    }
}
