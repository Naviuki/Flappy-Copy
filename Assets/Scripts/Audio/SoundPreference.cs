namespace FlappyCopy.Audio
{
    /// <summary>Storage of the player's sound preference, kept separate so it can be checked without Unity.</summary>
    public interface ISoundPreferenceStore
    {
        bool LoadEnabled();
        void SaveEnabled(bool enabled);
    }

    /// <summary>In-memory preference, for tests and tools.</summary>
    public sealed class MemorySoundPreference : ISoundPreferenceStore
    {
        public MemorySoundPreference(bool enabled = true)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; private set; }

        public bool LoadEnabled() => Enabled;

        public void SaveEnabled(bool enabled) => Enabled = enabled;
    }

    /// <summary>PlayerPrefs backed sound preference (1 = sound on, 0 = muted).</summary>
    public sealed class PlayerPrefsSoundPreference : ISoundPreferenceStore
    {
        private readonly string key;

        public PlayerPrefsSoundPreference(string key)
        {
            this.key = key;
        }

        public bool LoadEnabled() => UnityEngine.PlayerPrefs.GetInt(key, 1) == 1;

        public void SaveEnabled(bool enabled)
        {
            UnityEngine.PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
