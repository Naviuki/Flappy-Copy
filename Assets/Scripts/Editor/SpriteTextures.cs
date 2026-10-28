using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Generates the tiny colour textures the scene uses (bird, pipes, ground, UI) as PNGs and imports
    /// them as sprites. No downloaded assets: every look is authored here.
    /// </summary>
    public static class SpriteTextures
    {
        public const int PixelsPerUnit = 32;
        public const string WhitePath = "Assets/Textures/White.png";
        public const string RoundedPath = "Assets/Textures/Rounded.png";

        public static void CreateAll()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.TexturesFolder);
            WriteTexture(WhitePath, 8, 8, (x, y) => Color.white);
            const int size = 32;
            const int radius = 9;
            WriteTexture(RoundedPath, size, size, (x, y) => RoundedMask(x, y, size, size, radius));
            ImportSprite(WhitePath);
            ImportSprite(RoundedPath);
        }

        public static Sprite Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color RoundedMask(int x, int y, int width, int height, int radius)
        {
            float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (width - 1 - radius)));
            float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (height - 1 - radius)));
            bool inside = (dx * dx) + (dy * dy) <= (radius * radius);
            return inside ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        private static void WriteTexture(string path, int width, int height, System.Func<int, int, Color> pixel)
        {
            if (File.Exists(path))
            {
                return;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[(y * width) + x] = pixel(x, y);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectPaths.TexturesFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ImportSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
