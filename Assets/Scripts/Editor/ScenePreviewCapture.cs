using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>Captures the game scene to a PNG for reviews (edit-mode render through the scene camera).</summary>
    public static class ScenePreviewCapture
    {
        public const int Width = 540;
        public const int Height = 960;

        [MenuItem("Flappy Copy/Checks/Capture Scene Preview")]
        public static void CaptureFromMenu()
        {
            Capture("Docs/preview_scene.png");
        }

        /// <summary>-executeMethod FlappyCopy.EditorTools.ScenePreviewCapture.CaptureAndExit</summary>
        public static void CaptureAndExit()
        {
            string path = Capture("Docs/preview_scene.png");
            EditorApplication.Exit(path != null ? 0 : 1);
        }

        public static string Capture(string path)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[FlappyCopy] Exit play mode before capturing the edit-mode preview.");
                return null;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ProjectPaths.ScenePath);
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[FlappyCopy] No MainCamera in the game scene.");
                return null;
            }

            RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = camera.targetTexture;
            camera.targetTexture = target;
            camera.aspect = Width / (float)Height;
            camera.Render();

            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = previous;
            camera.ResetAspect();

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            Debug.Log("[FlappyCopy] Scene preview written: " + path);
            return path;
        }
    }
}
