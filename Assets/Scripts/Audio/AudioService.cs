using UnityEngine;

namespace FlappyCopy.Audio
{
    /// <summary>
    /// Plays the game's short sounds on one AudioSource and remembers the mute choice. Authored clips
    /// (Assets/Resources/Sfx*.wav) are used when present; otherwise the clips are generated at startup, so
    /// the game never depends on an imported asset to make a sound.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [SerializeField] private bool muted;

        private AudioSource source;
        private AudioClip[] clips;

        public bool IsEnabled => !muted;
        public bool IsMuted => muted;
        public AudioSource Source => source;
        public int LoadedClips => clips != null ? clips.Length : 0;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.priority = 96;

            AudioClip flap = Resources.Load<AudioClip>("SfxFlap");
            AudioClip point = Resources.Load<AudioClip>("SfxPoint");
            AudioClip hit = Resources.Load<AudioClip>("SfxHit");
            AudioClip ground = Resources.Load<AudioClip>("SfxGround");
            clips = new[]
            {
                flap != null ? flap : SfxLibrary.CreateBlip("GenFlap", 660f, 990f, 0.08f, 1f),
                point != null ? point : SfxLibrary.CreateBlip("GenPoint", 1100f, 1600f, 0.09f, 1f),
                hit != null ? hit : SfxLibrary.CreateNoise("GenHit", 0.14f, 1f),
                ground != null ? ground : SfxLibrary.CreateNoise("GenGround", 0.2f, 0.8f)
            };

            bool storedMute = PlayerPrefs.GetInt(Core.GameConstants.SoundEnabledPrefKey, 1) == 0;
            muted = muted || storedMute;
        }

        public void Play(SfxId id)
        {
            if (muted || source == null || clips == null)
            {
                return;
            }

            int index = (int)id;
            if (index >= 0 && index < clips.Length && clips[index] != null)
            {
                source.PlayOneShot(clips[index], volume);
            }
        }

        public void StopAll()
        {
            if (source != null)
            {
                source.Stop();
            }
        }

        public void ToggleMute()
        {
            SetMuted(!muted);
        }

        public void SetMuted(bool isMuted)
        {
            muted = isMuted;
            PlayerPrefs.SetInt(Core.GameConstants.SoundEnabledPrefKey, muted ? 0 : 1);
            PlayerPrefs.Save();
            if (muted)
            {
                StopAll();
            }
        }

        private void OnDestroy()
        {
            if (clips == null)
            {
                return;
            }

            foreach (AudioClip clip in clips)
            {
                if (clip != null && clip.name.StartsWith("Gen", System.StringComparison.Ordinal))
                {
                    Destroy(clip);
                }
            }
        }
    }
}
