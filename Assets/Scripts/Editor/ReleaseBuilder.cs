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
    /// Builds release players for every platform the installed editor supports and stages the archives:
    /// <list type="bullet">
    /// <item>Windows x64  → Builds/Release/FlappyCopy-Windows.zip</item>
    /// <item>Linux x64    → Builds/Release/FlappyCopy-Linux.zip (a tarball is produced on Linux itself)</item>
    /// <item>WebGL        → Builds/Release/FlappyCopy-WebGL.zip (a browser or a static host serves it)</item>
    /// <item>Android      → Builds/Release/FlappyCopy-Android.apk (only with the Android module installed)</item>
    /// </list>
    /// A platform without its build support in the editor is skipped with a message, never treated as an
    /// error: the release contains whatever this machine can build.
    /// Menu "Flappy Copy/Release/Build All Platforms";
    /// CI -executeMethod FlappyCopy.EditorTools.ReleaseBuilder.BuildAllAndExit.
    /// </summary>
    public static class ReleaseBuilder
    {
        public const string PackageRoot = "Builds/Release";

        /// <summary>Where the players are built (Unity overwrites these folders on every build).</summary>
        public const string StagingRoot = "Builds/Staging";

        public const string ReleaseRoot = PackageRoot;

        private const string ReadmePath = "Docs/Release-README.md";

        /// <summary>Platforms in the order they are built and listed.</summary>
        private static readonly ReleaseTarget[] Targets =
        {
            new ReleaseTarget("Windows", BuildTarget.StandaloneWindows64, "Windows x64 (Standalone)",
                "Builds/Staging/Windows/FlappyCopy.exe", "FlappyCopy-Windows.zip", false),
            new ReleaseTarget("Linux", BuildTarget.StandaloneLinux64, "Linux x64 (Standalone)",
                "Builds/Staging/Linux/FlappyCopy.x86_64", "FlappyCopy-Linux.zip", false),
            new ReleaseTarget("WebGL", BuildTarget.WebGL, "WebGL (browser)",
                "Builds/Staging/WebGL", "FlappyCopy-WebGL.zip", true),
            new ReleaseTarget("Android", BuildTarget.Android, "Android (APK)",
                "Builds/Staging/Android/FlappyCopy.apk", "FlappyCopy-Android.apk", false)
        };

        [MenuItem("Flappy Copy/Release/Build All Platforms")]
        public static void BuildFromMenu()
        {
            int built = BuildAll();
            EditorUtility.DisplayDialog("Flappy Copy",
                built.ToString() + " platform archive(s) written to " + ReleaseRoot + ".",
                "OK");
        }

        public static void BuildAllAndExit()
        {
            int built = BuildAll();
            EditorApplication.Exit(built > 0 ? 0 : 1);
        }

        /// <summary>Builds a single platform (command line: -executeMethod with -buildTarget already set).</summary>
        private static int BuildOnly(string name, BuildTarget target)
        {
            PlayerSetup.Apply();
            AudioAssetBuilder.CreateAll();
            AssetDatabase.SaveAssets();

            var issues = ProjectValidator.Validate();
            if (issues.Count > 0)
            {
                foreach (string issue in issues)
                {
                    Debug.LogError("[FlappyCopy] Release: " + issue);
                }

                return 0;
            }

            ReleaseTarget wanted = Array.Find(Targets, t => t.Target == target);
            if (wanted == null || !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Unknown, target))
            {
                Debug.LogWarning("[FlappyCopy] Release: " + name + " build support is not installed.");
                return 0;
            }

            Directory.CreateDirectory(ReleaseRoot);
            var report = new List<string>
            {
                "Flappy Copy " + PlayerSettings.bundleVersion,
                "Platform: " + wanted.Platform,
                "Unity: " + Application.unityVersion,
                "Built: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                string.Empty,
                "SHA-256:"
            };

            if (!BuildOne(wanted, PlayerSettings.bundleVersion, report))
            {
                return 0;
            }

            File.WriteAllText(Path.Combine(ReleaseRoot, "VERSION-" + name + ".txt"), string.Join(Environment.NewLine, report.ToArray()));
            return 1;
        }

        /// <summary>CI (one runner per platform): -executeMethod ...BuildWindowsAndExit.</summary>
        public static void BuildWindowsAndExit()
        {
            EditorApplication.Exit(BuildOnly("Windows", BuildTarget.StandaloneWindows64) > 0 ? 0 : 1);
        }

        public static void BuildLinuxAndExit()
        {
            EditorApplication.Exit(BuildOnly("Linux", BuildTarget.StandaloneLinux64) > 0 ? 0 : 1);
        }

        public static void BuildWebGLAndExit()
        {
            EditorApplication.Exit(BuildOnly("WebGL", BuildTarget.WebGL) > 0 ? 0 : 1);
        }

        public static void BuildAndroidAndExit()
        {
            EditorApplication.Exit(BuildOnly("Android", BuildTarget.Android) > 0 ? 0 : 1);
        }

        /// <summary>Validates once, then builds and archives every supported platform. Returns the count built.</summary>
        public static int BuildAll()
        {
            if (Directory.Exists(ReleaseRoot))
            {
                Directory.Delete(ReleaseRoot, true);
            }

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
                return 0;
            }

            Directory.CreateDirectory(ReleaseRoot);
            string version = PlayerSettings.bundleVersion;
            var report = new List<string>
            {
                "Flappy Copy " + version,
                "Unity: " + Application.unityVersion,
                "Built: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                "Input: legacy Input Manager (Space / mouse / touch)",
                "Renderer: URP 2D",
                string.Empty,
                "SHA-256:"
            };

            // One byte-identical README in the package: the root README is the bilingual project page and
            // embeds screenshots that the archive does not need.
            if (File.Exists(ReadmePath))
            {
                File.Copy(ReadmePath, Path.Combine(ReleaseRoot, "README.md"), true);
            }

            if (File.Exists("LICENSE.md"))
            {
                File.Copy("LICENSE.md", Path.Combine(ReleaseRoot, "LICENSE.md"), true);
            }

            int built = 0;
            foreach (ReleaseTarget target in Targets)
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Unknown, target.Target))
                {
                    string reason = target.Name + " skipped: build support not installed";
                    Debug.Log("[FlappyCopy] Release: " + reason + ".");
                    report.Add("SKIPPED: " + reason);
                    continue;
                }

                if (!BuildOne(target, version, report))
                {
                    // One failing platform must not hide the archives that were produced: it is reported and
                    // listed in VERSION.txt, the rest of the platforms still ship.
                    Debug.LogWarning("[FlappyCopy] Release: " + target.Name + " failed to build");
                    report.Add("FAILED: " + target.Name + " (see the build log)");
                    continue;
                }

                built++;
            }

            File.WriteAllText(Path.Combine(ReleaseRoot, "VERSION.txt"), string.Join(Environment.NewLine, report.ToArray()));
            Debug.Log("[FlappyCopy] Release finished: " + built + " platform(s) in " + ReleaseRoot + ".");
            return built;
        }

        private static bool BuildOne(ReleaseTarget target, string version, List<string> report)
        {
            // A player build writes the executable next to its "<name>_Data" folder; the whole folder is what
            // the archive holds, so a stale build with another name must not be packed into it.
            string outputFolder = Path.HasExtension(target.Output) ? Path.GetDirectoryName(target.Output) ?? target.Output : target.Output;
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            if (!BuildScript.Build(target.Target, target.Output, false))
            {
                return false;
            }

            string archive = Path.Combine(ReleaseRoot, target.Archive);
            if (File.Exists(archive))
            {
                File.Delete(archive);
            }

            if (target.Output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(target.Output, archive, true);
            }
            else
            {
                ZipOutput(target.Output, archive);
            }

            string hash = Sha256(archive);
            report.Add(hash + "  " + target.Archive);
            Debug.Log("[FlappyCopy] Release: " + target.Name + " -> " + archive + " (" + new FileInfo(archive).Length.ToString("N0") + " bytes)");
            return true;
        }

        /// <summary>
        /// Zips the build output: a player build writes the executable next to its _Data folder, a WebGL build
        /// writes the whole game into its folder, so the folder of the output is what goes into the archive.
        /// </summary>
        private static void ZipOutput(string output, string archive)
        {
            string folder = Path.HasExtension(output) ? (Path.GetDirectoryName(output) ?? output) : output;
            if (!Directory.Exists(folder))
            {
                throw new FileNotFoundException("Build output is missing.", folder);
            }

            ZipFile.CreateFromDirectory(folder, archive, System.IO.Compression.CompressionLevel.Optimal, true);
        }

        private static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private sealed class ReleaseTarget
        {
            public ReleaseTarget(string name, BuildTarget target, string platform, string output, string archive, bool optional)
            {
                Name = name;
                Target = target;
                Platform = platform;
                Output = output;
                Archive = archive;
                Optional = optional;
            }

            public string Name { get; }
            public BuildTarget Target { get; }
            public string Platform { get; }
            public string Output { get; }
            public string Archive { get; }

            /// <summary>A platform that may legitimately be missing from a machine (WebGL on a desktop build).</summary>
            public bool Optional { get; }
        }
    }
}
