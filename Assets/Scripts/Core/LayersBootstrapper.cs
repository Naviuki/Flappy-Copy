using System.Collections.Generic;
using UnityEngine;

namespace FlappyCopy.Core
{
    /// <summary>
    /// Adds the project's layers, tags and sorting layers when they are missing, so the game runs straight
    /// from a fresh check-out without touching Project Settings by hand.
    /// Text is matched by name only (<see cref="UnityEngine.LayerMask.NameToLayer"/>), never by numeric index.
    /// Runs in edit mode too, which is what keeps the player build and the tests in sync.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    public sealed class LayersBootstrapper : MonoBehaviour
    {
        private static bool isApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetState()
        {
            isApplied = false;
        }

        private void Awake()
        {
            EnsureAll();
        }

        public static void EnsureAll()
        {
            if (isApplied)
            {
                return;
            }

            isApplied = true;

            foreach (string layerName in ProjectLayers.LayerNames)
            {
                ProjectLayers.EnsureLayer(layerName);
            }

            foreach (string tagName in ProjectLayers.TagNames)
            {
                ProjectLayers.EnsureTag(tagName);
            }

            foreach (string sortingLayer in SortingLayers.All)
            {
                ProjectLayers.EnsureSortingLayer(sortingLayer);
            }
        }
    }
}
