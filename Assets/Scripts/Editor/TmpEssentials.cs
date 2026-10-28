using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Extracts the bundled "TMP Essential Resources" package straight into Assets/. The package is a
    /// tar archive of GUID folders (pathname + asset). AssetDatabase.ImportPackage is asynchronous in
    /// batch mode and would be dropped on exit, so the files are written directly instead.
    /// </summary>
    public static class TmpEssentials
    {
        public const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public static bool IsInstalled()
        {
            return File.Exists(SettingsPath) && File.Exists(FontPath);
        }

        /// <summary>Extracts the package when needed. Returns true when TMP resources are on disk.</summary>
        public static bool EnsureInstalled()
        {
            if (!IsInstalled())
            {
                Extract(GameScenePaths.TmpEssentialsPackage);
            }

            return IsInstalled();
        }

        /// <summary>Unpacks the tar package into Assets/. Returns the number of files written.</summary>
        public static int Extract(string packagePath)
        {
            if (!File.Exists(packagePath))
            {
                Debug.LogError("[FlappyCopy] TMP Essentials package not found: " + packagePath);
                return 0;
            }

            string assetsRoot = new DirectoryInfo(Application.dataPath).Parent != null
                ? new DirectoryInfo(Application.dataPath).Parent.FullName
                : Path.GetDirectoryName(Application.dataPath);

            var targets = new Dictionary<string, string>();
            var files = new List<(string target, byte[] data)>();
            int written = 0;

            using (var gzip = new System.IO.Compression.GZipStream(
                       File.OpenRead(packagePath), System.IO.Compression.CompressionMode.Decompress))
            {
                while (true)
                {
                    byte[] header = ReadExact(gzip, 512);
                    if (header == null || IsZeroBlock(header))
                    {
                        break;
                    }

                    string name = ReadName(header);
                    long size = ReadOctal(header, 124, 12);
                    if (string.IsNullOrEmpty(name))
                    {
                        break;
                    }

                    if (!name.EndsWith("/"))
                    {
                        byte[] data = ReadExact(gzip, (int)size);
                        if (data == null)
                        {
                            break;
                        }

                        string guid = name.Substring(0, name.LastIndexOf('/'));
                        if (name.EndsWith("/pathname", System.StringComparison.Ordinal))
                        {
                            string target = Encoding.UTF8.GetString(data).Trim('\0', '\r', '\n', ' ');
                            targets[guid] = target;
                        }
                        else if (name.EndsWith("/asset", System.StringComparison.Ordinal)
                                 || name.EndsWith("/asset.meta", System.StringComparison.Ordinal))
                        {
                            bool isMeta = name.EndsWith(".meta", System.StringComparison.Ordinal);
                            files.Add((guid + (isMeta ? "#meta" : string.Empty), data));
                        }

                        SkipPadding(gzip, size);
                        continue;
                    }

                    // Directory entry: nothing to unpack.
                }
            }

            foreach ((string key, byte[] data) in files)
            {
                bool isMeta = key.EndsWith("#meta", System.StringComparison.Ordinal);
                string guid = isMeta ? key.Substring(0, key.Length - 5) : key;
                if (!targets.TryGetValue(guid, out string target))
                {
                    continue;
                }

                string relative = isMeta ? target + ".meta" : target;
                string full = Path.Combine(assetsRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(full, data);
                written++;
            }

            Debug.Log("[FlappyCopy] TMP Essentials extracted: " + written + " files into " + assetsRoot);
            return written;
        }

        private static bool IsZeroBlock(byte[] block)
        {
            for (int i = 0; i < block.Length; i++)
            {
                if (block[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReadName(byte[] header)
        {
            return Encoding.ASCII.GetString(header, 0, 100).Trim('\0', ' ');
        }

        private static long ReadOctal(byte[] header, int offset, int length)
        {
            string text = Encoding.ASCII.GetString(header, offset, length).Trim('\0', ' ');
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            long value = 0;
            foreach (char c in text)
            {
                if (c < '0' || c > '7')
                {
                    break;
                }

                value = (value * 8) + (c - '0');
            }

            return value;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            var buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int chunk = stream.Read(buffer, read, count - read);
                if (chunk <= 0)
                {
                    return null;
                }

                read += chunk;
            }

            return buffer;
        }

        private static void SkipPadding(Stream stream, long size)
        {
            long remainder = size % 512;
            if (remainder != 0)
            {
                SkipBytes(stream, 512 - remainder);
            }
        }

        private static void SkipBytes(Stream stream, long count)
        {
            var buffer = new byte[4096];
            while (count > 0)
            {
                int chunk = stream.Read(buffer, 0, (int)System.Math.Min(buffer.Length, count));
                if (chunk <= 0)
                {
                    return;
                }

                count -= chunk;
            }
        }
    }

    public static class GameScenePaths
    {
        public static string TmpEssentialsPackage
        {
            get
            {
                return Path.Combine(UnityEditor.EditorApplication.applicationContentsPath,
                    "Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            }
        }
    }
}
