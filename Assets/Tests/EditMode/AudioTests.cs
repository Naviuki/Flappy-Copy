using FlappyCopy.Audio;
using NUnit.Framework;

namespace FlappyCopy.Tests
{
    /// <summary>Generated sound effects and the authored audio clips are valid; the mute preference works.</summary>
    public static class AudioTests
    {
        [Test]
        public static void GeneratedClipsHaveAudio()
        {
            var flap = SfxLibrary.CreateBlip("t_flap", 660f, 990f, 0.08f, 1f);
            var hit = SfxLibrary.CreateNoise("t_hit", 0.14f, 1f);

            Check.True(flap != null && flap.samples > 100, "flap clip has samples");
            Check.Equal(SfxLibrary.SampleRate, flap.frequency, "flap clip uses the game sample rate");
            Check.True(hit != null && hit.samples > 100, "hit clip has samples");

            var data = new float[flap.samples];
            Check.True(flap.GetData(data, 0), "flap clip data readable");
            float peak = 0f;
            foreach (float sample in data)
            {
                peak = System.Math.Max(peak, System.Math.Abs(sample));
            }

            Check.True(peak > 0.2f && peak <= 1.0001f, "flap clip is audible and not clipping");
        }

        [Test]
        public static void SilenceServiceCountsPlays()
        {
            var audio = new NullAudioService();
            audio.Play(SfxId.Flap);
            audio.Play(SfxId.Point);
            Check.Equal(2, audio.Played, "both sounds requested");
        }

        [Test]
        public static void SoundPreferenceRoundTrips()
        {
            var storage = new MemorySoundPreference(enabled: true);
            Check.True(storage.LoadEnabled(), "sound on by default");
            storage.SaveEnabled(false);
            Check.False(storage.LoadEnabled(), "mute persisted");
            storage.SaveEnabled(true);
            Check.True(storage.LoadEnabled(), "unmute persisted");
        }

        [Test]
        public static void EnvelopeIsClickFree()
        {
            Check.Near(0f, SfxLibrary.Envelope(0f), 0.0001f, "starts at silence");
            Check.Near(0f, SfxLibrary.Envelope(1f), 0.0001f, "ends at silence");
            Check.True(SfxLibrary.Envelope(0.25f) > 0.2f, "audible in the middle");
        }
    }
}
