using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using FlappyCopy.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Cuts a release build: validates the scene and assets, builds the Windows player, stages a release
    /// folder (player, README, LICENSE, VERSION with SHA-256 hashes) and zips it.
    /// Menu "Flappy Copy/Release/Build Windows Release"; CI -executeMethod FlappyCopy.EditorTools.ReleaseBuilder.BuildAndExit
    /// </summary>
    public static class ReleaseBuilder
    {
        public const string PlayerPath = "Builds/Windows/FlappyCopy.exe";
        public const string ReleaseRoot = "Builds/Release";
        public const string ArchivePath = "Builds/FlappyCopy-Windows.zip";

        [MenuItem("Flappy Copy/Release/Build Windows Release")]
        public static void BuildFromMenu()
        {
            bool ok = BuildRelease();
            EditorUtility.DisplayDialog("Flappy Copy",
                ok ? "Release built in " + ReleaseRoot + " and " + ArchivePath : "Release build failed, see the Console.",
                "OK");
        }

        public static void BuildAndExit()
        {
            bool ok = BuildRelease();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool BuildRelease()
        {
            PlayerSetup.Apply();
            AudioAssetBuilder.CreateAll();
            AssetDatabase.SaveAssets();

            var issues = ProjectValidator.Validate();
            foreach (string issue in issues)
            {
                Debug.LogError("[FlappyCopy] Release: " + issue);
            }

            if (issues.Count > 0)
            {
                Debug.LogError("[FlappyCopy] Release stopped: fix the validation issues first.");
                return false;
            }

            if (!BuildScript.Build(BuildTarget.StandaloneWindows64, PlayerPath, false))
            {
                return false;
            }

            return StageRelease();
        }

        /// <summary>Copies the player into the release folder with its documentation and a hash list.</summary>
        private static bool StageRelease()
        {
            string source = Path.GetFullPath(Path.GetDirectoryName(PlayerPath) ?? "Builds/Windows");
            string root = Path.GetFullPath(ReleaseRoot);
            string target = Path.Combine(root, "FlappyCopy-Windows");
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            CopyDirectory(source, target);

            // Unity writes folder copies of debug data next to the player; they must not ship.
            foreach (string folder in Directory.GetDirectories(target, "*", SearchOption.AllDirectories))
            {
                if ((folder.Contains("_BurstDebugInformation") || folder.Contains("_BackUpThisFolder"))
                    && Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }

            var report = new List<string>
            {
                "Flappy Copy " + PlayerSettings.bundleVersion,
                "Build target: Windows x64 (Standalone)",
                "Unity: " + Application.unityVersion,
                "Built: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                "Input: legacy Input Manager (Space / mouse / touch)",
                "Renderer: URP 2D",
                string.Empty,
                "SHA-256:"
            };

            string[] files = Directory.GetFiles(target, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                foreach (string file in files)
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", string.Empty).ToLowerInvariant();
                    report.Add(hash + "  " + Path.GetRelativePath(root, file).Replace('\\', '/'));
                }
            }

            File.WriteAllText(Path.Combine(root, "VERSION.txt"), string.Join(Environment.NewLine, report.ToArray()));
            foreach (string doc in new[] { "README.md", "LICENSE.md" })
            {
                if (File.Exists(doc))
                {
                    File.Copy(doc, Path.Combine(root, doc), true);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath) ?? "Builds");
            if (File.Exists(ArchivePath))
            {
                File.Delete(ArchivePath);
            }

            ZipFile.CreateFromDirectory(root, Path.GetFullPath(ArchivePath), System.IO.Compression.CompressionLevel.Optimal, false);
            Debug.Log("[FlappyCopy] Release staged in " + root + ", archive " + ArchivePath);
            return true;
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            foreach (string directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
            }
        }
    }
}
