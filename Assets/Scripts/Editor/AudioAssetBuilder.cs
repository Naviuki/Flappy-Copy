using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    /// <summary>
    /// Builds the authored audio into AudioClips: short synthesized sound effects written as 16-bit mono WAV
    /// files (no downloaded files), imported with the right settings for one-shot playback.
    /// </summary>
    public static class AudioAssetBuilder
    {
        public const string AudioFolder = "Assets/Resources";
        public const int SampleRate = 22050;

        [MenuItem("Flappy Copy/Audio/Rebuild Sound Effects")]
        public static void CreateAll()
        {
            ProjectPaths.EnsureFolder(AudioFolder);
            WriteWav("SfxFlap", 0.08f, 660f, 1f, 0.25f);
            WriteWav("SfxPoint", 0.1f, 1200f, 1f, 0.2f);
            WriteWav("SfxHit", 0.14f, 180f, 0.85f, 0.9f);
            WriteWav("SfxGround", 0.18f, 140f, 0.9f, 0.9f);
        }

        public static void CreateAllAndExit()
        {
            CreateAll();
            EditorApplication.Exit(0);
        }

        /// <summary>One-shots: no loop, no compression artefacts and no long decompression at playback time.</summary>
        private static void WriteWav(string name, float seconds, float frequency, float toneMix, float noiseMix)
        {
            string path = Path.Combine(AudioFolder, name + ".wav");
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * seconds));
            var data = new float[samples];
            var random = new System.Random(name.GetHashCode());
            float previousNoise = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float white = (float)((random.NextDouble() * 2.0) - 1.0);
                previousNoise = (previousNoise * 0.7f) + (white * 0.3f);
                float envelope = Mathf.Clamp01(t / 0.02f) * Mathf.Pow(1f - t, 2f);
                data[i] = ((sine * toneMix) + (previousNoise * noiseMix)) * envelope * 0.8f;
            }

            File.WriteAllBytes(path, ToWav(data));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        private static byte[] ToWav(float[] samples)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int dataSize = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);
                foreach (float sample in samples)
                {
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
