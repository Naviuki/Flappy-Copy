using UnityEngine;

namespace FlappyCopy.Core
{
    /// <summary>Editor/debug toggles kept out of the gameplay code paths.</summary>
    [CreateAssetMenu(menuName = "Flappy Copy/Debug Settings", fileName = "DebugSettings")]
    public sealed class DebugSettings : ScriptableObject
    {
        [SerializeField] private bool logBoot = true;

        public bool LogBoot => logBoot;
    }
}
