using System;

namespace FlappyCopy.Input
{
    /// <summary>Hand-written source for tests and scripted bots; never reads Unity input.</summary>
    public sealed class ScriptedInputSource : IInputSource
    {
        private bool flapPressedThisFrame;
        private bool restartPressedThisFrame;

        public bool IsFlapPressedThisFrame => flapPressedThisFrame;
        public bool IsRestartPressedThisFrame => restartPressedThisFrame;

        public void PressFlap()
        {
            flapPressedThisFrame = true;
        }

        public void PressRestart()
        {
            restartPressedThisFrame = true;
        }

        /// <summary>Call once per simulated frame, after the game logic consumed the input.</summary>
        public void EndFrame()
        {
            flapPressedThisFrame = false;
            restartPressedThisFrame = false;
        }
    }

    /// <summary>Input source that never presses anything.</summary>
    public sealed class NullInputSource : IInputSource
    {
        public bool IsFlapPressedThisFrame => false;
        public bool IsRestartPressedThisFrame => false;
    }

    public static class InputSampleSource
    {
        /// <summary>Returns the default input source for a player build.</summary>
        public static IInputSource CreateDefault()
        {
            return new UnityInputSource();
        }
    }
}
