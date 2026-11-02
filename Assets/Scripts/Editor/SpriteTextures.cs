using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Generates the tiny colour textures the scene uses (bird, pipes, ground, UI) as PNGs and imports
    /// them as sprites. No downloaded assets: every look is authored here.
    /// Every texture is <see cref="TextureSize"/> squared and imported at <see cref="PixelsPerUnit"/>, so one
    /// sprite is exactly 1 world unit and scaling it by a size in world units gives that size.
    /// </summary>
    public static class SpriteTextures
    {
        public const int PixelsPerUnit = 32;
        public const int TextureSize = 32;
        public const string WhitePath = "Assets/Textures/White.png";

        public static void CreateAll()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.TexturesFolder);
            WriteTexture(WhitePath, TextureSize, TextureSize, (x, y) => Color.white);

            // Import settings are re-applied on every rebuild: the pivot and the size convention are part of
            // the game's layout, not a one-off import.
            AssetDatabase.ImportAsset(WhitePath, ImportAssetOptions.ForceSynchronousImport);
            ImportSprite(WhitePath);
            CreateMaterial();
        }

        /// <summary>
        /// The game's single sprite material: the URP <b>unlit</b> 2D sprite shader, so sprites are fully lit by
        /// their colour on every renderer. The default lit material renders nothing in an edit-mode render
        /// without a scene light and is shaded differently depending on the lighting setup.
        /// </summary>
        private static void CreateMaterial()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.MaterialsFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                            ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[FlappyCopy] No unlit sprite shader available for the sprite material.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(ProjectPaths.SpriteMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                material.name = ProjectPaths.SpriteMaterialName;
                AssetDatabase.CreateAsset(material, ProjectPaths.SpriteMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The material every generated sprite renderer uses (see <see cref="CreateMaterial"/>).</summary>
        public static Material Material()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(ProjectPaths.SpriteMaterialPath);
        }

        public static Sprite Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

            // Every object is positioned and sized from its bottom-left corner (see GameScene.Block), so the
            // sprite pivot has to be the bottom-left corner too.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = Vector2.zero;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
