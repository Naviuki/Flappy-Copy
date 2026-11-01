using UnityEngine;

namespace FlappyCopy.Audio
{
    /// <summary>Plays the game's short sounds.</summary>
    public interface IAudioService
    {
        void Play(SfxId id);
        void StopAll();
    }

    /// <summary>Silent implementation for tests and tools that run without audio.</summary>
    public sealed class NullAudioService : IAudioService
    {
        public int Played { get; private set; }

        public void Play(SfxId id) => Played++;

        public void StopAll()
        {
        }
    }
}
