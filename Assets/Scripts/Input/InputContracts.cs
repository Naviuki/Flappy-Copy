using System;
using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.Input
{
    /// <summary>What the player asked for this frame. Kept engine-free so it can drive tests.</summary>
    public enum PressKind
    {
        None = 0,
        Flap = 1,
        Restart = 2
    }

    /// <summary>Abstracts "space / mouse / touch / UI button" away from the gameplay code.</summary>
    public interface IInputSource
    {
        bool IsFlapPressedThisFrame { get; }
        bool IsRestartPressedThisFrame { get; }
    }

    public static class PressPolicy
    {
        /// <summary>Reads one input sample and decides what it means for the current game state.</summary>
        public static PressKind Resolve(bool flapPressed, bool restartPressed, MenuIntent menuIntent)
        {
            if (flapPressed)
            {
                switch (menuIntent)
                {
                    case MenuIntent.StartGame:
                    case MenuIntent.RestartGame:
                        return PressKind.Restart;
                    case MenuIntent.Flap:
                        return PressKind.Flap;
                    default:
                        return PressKind.None;
                }
            }

            if (restartPressed && (menuIntent == MenuIntent.StartGame || menuIntent == MenuIntent.RestartGame))
            {
                return PressKind.Restart;
            }

            return PressKind.None;
        }
    }

    /// <summary>A single flap request that is buffered until input is enabled (used by the UI button).</summary>
    public sealed class FlapQueue
    {
        private int pendingCount;

        public bool HasPending => pendingCount > 0;

        public void RequestFlap()
        {
            pendingCount = Math.Min(pendingCount + 1, 4);
        }

        public bool Consume()
        {
            if (pendingCount <= 0)
            {
                return false;
            }

            pendingCount--;
            return true;
        }

        public void Clear()
        {
            pendingCount = 0;
        }
    }
}
