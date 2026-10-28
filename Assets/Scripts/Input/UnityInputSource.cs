using System;
using UnityEngine;

namespace FlappyCopy.Input
{
    /// <summary>
    /// Polls the legacy Input Manager: Space, left mouse button and touch all count as a flap.
    /// The UI play-area button feeds the same path through <see cref="RequestFlap"/>.
    /// </summary>
    public sealed class UnityInputSource : IInputSource, IDisposable
    {
        private const KeyCode FlapKey = KeyCode.Space;
        private const KeyCode RestartKey = KeyCode.R;

        private readonly FlapQueue queue = new FlapQueue();

        public bool IsFlapPressedThisFrame => queue.Consume() || UnityFlapPressed();
        public bool IsRestartPressedThisFrame => UnityEngine.Input.GetKeyDown(RestartKey);

        public void RequestFlap()
        {
            queue.RequestFlap();
        }

        public void Dispose()
        {
            queue.Clear();
        }

        private static bool UnityFlapPressed()
        {
            if (UnityEngine.Input.GetKeyDown(FlapKey))
            {
                return true;
            }

            bool mousePressed = UnityEngine.Input.GetMouseButtonDown(0);
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                return touch.phase == TouchPhase.Began || mousePressed;
            }

            return mousePressed;
        }
    }
}
