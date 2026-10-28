using System;
using System.Collections.Generic;

namespace FlappyCopy.Core
{
    /// <summary>Logical rendering lanes, listed bottom to top and installed as sorting layers.</summary>
    public static class SortingLayers
    {
        public const string Background = "Background";
        public const string Scenery = "Scenery";
        public const string Ground = "Ground";
        public const string Pipes = "Pipes";
        public const string Player = "Player";
        public const string Ui = "UI";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Background, Scenery, Ground, Pipes, Player, Ui
        };

        /// <summary>Index of <paramref name="layerName"/> in <see cref="All"/>; -1 when unknown.</summary>
        public static int Index(string layerName)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == layerName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Base sorting order for a lane. Sprites inside a lane add small offsets.</summary>
        public static int Order(string layerName)
        {
            int index = Index(layerName);
            return index < 0 ? 0 : index * 100;
        }

        [Obsolete("Use SortingLayers.Order(layerName).")]
        public static int SortingOrder(string layerName) => Order(layerName);
    }

    /// <summary>
    /// Adds the project's layers, tags and sorting layers when they are missing, so the game runs straight
    /// from a fresh check-out without touching Project Settings by hand.
    /// Text is matched by name only (<see cref="UnityEngine.LayerMask.NameToLayer"/>), never by numeric index.
    /// </summary>
    public static class ProjectLayers
    {
        public const string PlayerName = "Player";
        public const string PipeName = "Pipe";
        public const string GroundName = "Ground";
        public const string PipePassName = "PipePass";

        public static readonly IReadOnlyList<string> LayerNames = new[]
        {
            PlayerName, PipeName, GroundName, PipePassName
        };

        public static readonly IReadOnlyList<string> TagNames = new[]
        {
            PlayerName, PipeName, GroundName
        };

        public static int Player => UnityEngine.LayerMask.NameToLayer(PlayerName);
        public static int Pipe => UnityEngine.LayerMask.NameToLayer(PipeName);
        public static int Ground => UnityEngine.LayerMask.NameToLayer(GroundName);
        public static int PipePass => UnityEngine.LayerMask.NameToLayer(PipePassName);

        /// <summary>
        /// Physics2D spawns contacts between two layers only if both sides share the same "collides with"
        /// decision; player sees pipes and ground, pipes/ground see the player. Everything else is a
        /// trigger-only layer (PipePass) and never collides.
        /// </summary>
        public static bool IsCollisionEnabled(int playerLayer, int pipeLayer, int groundLayer)
        {
            return playerLayer >= 0
                   && playerLayer != pipeLayer
                   && playerLayer != groundLayer
                   && pipeLayer != groundLayer;
        }

        public static bool IsLayerMissing(string layerName)
        {
            return layerName != null && UnityEngine.LayerMask.NameToLayer(layerName) < 0;
        }

        public static void EnsureLayer(string layerName)
        {
            if (!IsLayerMissing(layerName))
            {
                return;
            }

            int index = FindFreeUserLayer();
            if (index < 0)
            {
                return;
            }

#if UNITY_EDITOR
            var settings = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (settings == null || settings.Length == 0)
            {
                return;
            }

            var tagManager = new UnityEditor.SerializedObject(settings[0]);
            UnityEditor.SerializedProperty layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(index).stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            UnityEditor.EditorUtility.SetDirty(settings[0]);
#endif
        }

        public static void EnsureTag(string tagName)
        {
#if UNITY_EDITOR
            var settings = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (settings == null || settings.Length == 0)
            {
                return;
            }

            var tagManager = new UnityEditor.SerializedObject(settings[0]);
            UnityEditor.SerializedProperty tags = tagManager.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            UnityEditor.EditorUtility.SetDirty(settings[0]);
#endif
        }

        /// <summary>Sorting layers are installed in <see cref="SortingLayers.All"/> order; the default layer stays first.</summary>
        public static void EnsureSortingLayer(string layerName)
        {
            if (layerName == null || HasSortingLayer(layerName))
            {
                return;
            }

#if UNITY_EDITOR
            var settings = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (settings == null || settings.Length == 0)
            {
                return;
            }

            var tagManager = new UnityEditor.SerializedObject(settings[0]);
            UnityEditor.SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayers == null)
            {
                return;
            }

            // Position = 1 + number of our layers that come before this one, so ordering is stable.
            int position = 1;
            for (int i = 0; i < SortingLayers.All.Count; i++)
            {
                if (SortingLayers.All[i] == layerName)
                {
                    break;
                }

                if (HasSortingLayer(SortingLayers.All[i]))
                {
                    position++;
                }
            }

            position = Math.Min(position, sortingLayers.arraySize);
            sortingLayers.InsertArrayElementAtIndex(position);
            UnityEditor.SerializedProperty entry = sortingLayers.GetArrayElementAtIndex(position);
            entry.FindPropertyRelative("name").stringValue = layerName;
            entry.FindPropertyRelative("uniqueID").intValue = StableSortingLayerId(layerName);
            entry.FindPropertyRelative("locked").boolValue = false;
            tagManager.ApplyModifiedProperties();
            UnityEditor.EditorUtility.SetDirty(settings[0]);
#endif
        }

        public static bool HasSortingLayer(string layerName)
        {
            UnityEngine.SortingLayer[] layers = UnityEngine.SortingLayer.layers;
            if (layers == null)
            {
                return false;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (string.Equals(layers[i].name, layerName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindFreeUserLayer()
        {
            // User layers live in the 8..31 range; the text names are the single source of truth.
            for (int i = 8; i < 32; i++)
            {
                if (string.IsNullOrEmpty(UnityEngine.LayerMask.LayerToName(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int StableSortingLayerId(string layerName)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < layerName.Length; i++)
                {
                    hash = (hash * 31) + layerName[i];
                }

                int id = Math.Abs(hash % 1000000);
                return id == 0 ? 1 : id;
            }
        }
    }
}
