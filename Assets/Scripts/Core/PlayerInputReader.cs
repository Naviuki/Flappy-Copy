using System;
using FlappyCopy.Input;

namespace FlappyCopy.Core
{
    /// <summary>Turns raw <see cref="IInputSource"/> samples into <see cref="PressKind"/> for the current state.</summary>
    public sealed class PlayerInputReader
    {
        private readonly IInputSource source;

        public PlayerInputReader(IInputSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public PressKind Poll(MenuIntent intent)
        {
            return PressPolicy.Resolve(source.IsFlapPressedThisFrame, source.IsRestartPressedThisFrame, intent);
        }
    }
}
