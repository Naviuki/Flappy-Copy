using UnityEngine;

namespace FlappyCopy.Audio
{
    /// <summary>
    /// Generates the game's sound effects as AudioClips in memory (mono, <see cref="SampleRate"/> Hz): a
    /// frequency sweep for the flap and the point, a low-passed noise burst for the hit and the ground.
    /// Used when no authored clip is available (Assets/Resources/Sfx*.wav), so the game can never be silent
    /// because a file is missing.
    /// </summary>
    public static class SfxLibrary
    {
        public const int SampleRate = 22050;

        /// <summary>Frequency sweep with a click-free envelope.</summary>
        public static AudioClip CreateBlip(string name, float startHz, float endHz, float durationSeconds, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float frequency = Mathf.Lerp(startHz, endHz, t);
                phase += (2f * Mathf.PI * frequency) / SampleRate;
                data[i] = Mathf.Sin(phase) * Envelope(t) * Mathf.Clamp01(volume);
            }

            return Create(name, data);
        }

        /// <summary>Low-passed noise burst: a soft thud rather than white hiss.</summary>
        public static AudioClip CreateNoise(string name, float durationSeconds, float volume)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[samples];
            var random = new System.Random(1234);
            float previous = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float white = (float)((random.NextDouble() * 2.0) - 1.0);
                previous = (previous * 0.7f) + (white * 0.3f);
                data[i] = previous * Envelope(t) * Mathf.Clamp01(volume);
            }

            return Create(name, data);
        }

        /// <summary>Fast attack, exponential decay: no clicks at the start or the end.</summary>
        public static float Envelope(float t)
        {
            float attack = Mathf.Clamp01(t / 0.02f);
            float release = Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            return attack * release;
        }

        private static AudioClip Create(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
