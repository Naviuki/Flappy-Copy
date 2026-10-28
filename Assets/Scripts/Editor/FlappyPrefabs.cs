using FlappyCopy.Core;
using FlappyCopy.Data;
using FlappyCopy.Pipes;
using FlappyCopy.Player;
using FlappyCopy.Pooling;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.EditorTools
{
    using PlayerSettings = FlappyCopy.Data.PlayerSettings;

    /// <summary>Creates or refreshes the ScriptableObject tuning assets used by the scene.</summary>
    public static class FlappyData
    {
        public const string PlayerSettingsPath = "Assets/Data/PlayerSettings.asset";
        public const string PipeSettingsPath = "Assets/Data/PipeSettings.asset";
        public const string WorldSettingsPath = "Assets/Data/WorldSettings.asset";

        public static PlayerSettings Player => AssetDatabase.LoadAssetAtPath<PlayerSettings>(PlayerSettingsPath);
        public static PipeSettings Pipes => AssetDatabase.LoadAssetAtPath<PipeSettings>(PipeSettingsPath);
        public static WorldSettings World => AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldSettingsPath);

        public static void CreateAll()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.DataFolder);
            PlayerSettings player = ProjectPaths.LoadOrCreateAsset<PlayerSettings>(PlayerSettingsPath);
            PipeSettings pipes = ProjectPaths.LoadOrCreateAsset<PipeSettings>(PipeSettingsPath);
            WorldSettings world = ProjectPaths.LoadOrCreateAsset<WorldSettings>(WorldSettingsPath);

            // Tuned so the bird falls ~1 unit in 0.3 s and a flap lifts ~1.8 units.
            player.Configure(2.4f, 6.2f, 14f);
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(pipes);
            EditorUtility.SetDirty(world);
        }
    }

    /// <summary>
    /// Builds the Bird and PipePair prefabs from procedural sprites (no external art). Sizes are explicit
    /// in world units; see <see cref="Part"/> for the pivot/scale convention.
    /// </summary>
    public static class FlappyPrefabs
    {
        private const float BodyWidth = 0.5f;
        private const float BodyHeight = 0.42f;

        public static void CreateAll()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.PrefabsFolder);
            CreatePipePair();
            CreateBird();
        }

        private static void CreatePipePair()
        {
            PipeSettings settings = FlappyData.Pipes;
            var root = new GameObject("PipePair");

            Color pipe = settings.PipeColor;
            Color cap = settings.CapColor;
            float capWidth = settings.PipeWidth + (settings.CapOverhang * 2f);
            Transform top = Part("TopPipe", root.transform, SortingLayers.Order(SortingLayers.Pipes), -settings.PipeWidth * 0.5f,
                0f, settings.PipeWidth, DesignSpace.PlayHalfHeight, pipe);
            Transform bottom = Part("BottomPipe", root.transform, SortingLayers.Order(SortingLayers.Pipes), -settings.PipeWidth * 0.5f,
                0f, settings.PipeWidth, 1f, pipe);
            Transform topCap = Part("TopCap", root.transform, SortingLayers.Order(SortingLayers.Pipes), -capWidth * 0.5f,
                DesignSpace.PlayHalfHeight - settings.CapHeight, capWidth, settings.CapHeight, cap);
            Transform bottomCap = Part("BottomCap", root.transform, SortingLayers.Order(SortingLayers.Pipes), -capWidth * 0.5f,
                0f, capWidth, settings.CapHeight, cap);

            // The hitboxes are sized/placed every time the pair is activated (see PipePair.Layout).
            new GameObject("TopHitbox").transform.SetParent(root.transform, false);
            new GameObject("BottomHitbox").transform.SetParent(root.transform, false);
            root.AddComponent<CollisionRelay>();
            GameScene.SetLayerRecursively(root, ProjectLayers.Pipe);

            PipePassGate gate = GameScene.AddGate(root.transform);
            var pipePair = root.AddComponent<PipePair>();
            GameScene.SetPrivate(pipePair, "topPipe", top);
            GameScene.SetPrivate(pipePair, "bottomPipe", bottom);
            GameScene.SetPrivate(pipePair, "topCap", topCap);
            GameScene.SetPrivate(pipePair, "bottomCap", bottomCap);
            GameScene.SetPrivate(pipePair, "gate", gate);
            GameScene.SetPrivate(pipePair, "settings", settings);

            PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.PipePairPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateBird()
        {
            PlayerSettings settings = FlappyData.Player;
            WorldSettings world = FlappyData.World;

            var root = new GameObject("Bird");
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            Transform bodyTransform = Part("Body", visual, SortingLayers.Order(SortingLayers.Player), -BodyWidth * 0.5f,
                -BodyHeight * 0.5f, BodyWidth, BodyHeight,
                world != null ? world.BirdColor : new Color(0.99f, 0.83f, 0.15f, 1f));
            var collider = bodyTransform.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            Part("Belly", visual, SortingLayers.Order(SortingLayers.Player) + 1, -0.19f, -0.14f, 0.26f, 0.2f,
                new Color(1f, 0.95f, 0.78f, 1f));
            Part("Wing", visual, SortingLayers.Order(SortingLayers.Player) + 2, -0.24f, -0.06f, 0.2f, 0.14f,
                world != null ? world.WingColor : new Color(0.98f, 0.71f, 0.10f, 1f));
            Part("Beak", visual, SortingLayers.Order(SortingLayers.Player) + 3, 0.16f, -0.08f, 0.16f, 0.09f,
                new Color(0.98f, 0.55f, 0.15f, 1f));
            Part("Eye", visual, SortingLayers.Order(SortingLayers.Player) + 4, 0.06f, 0.04f, 0.11f, 0.11f, Color.white);
            Part("Pupil", visual, SortingLayers.Order(SortingLayers.Player) + 5, 0.09f, 0.06f, 0.05f, 0.05f,
                new Color(0.1f, 0.1f, 0.12f, 1f));

            root.AddComponent<CollisionRelay>();
            var bird = root.AddComponent<BirdController>();
            GameScene.SetPrivate(bird, "settings", settings);
            GameScene.SetPrivate(bird, "visual", visual);

            GameScene.SetLayerRecursively(root, ProjectLayers.Player);
            PrefabUtility.SaveAsPrefabAsset(root, ProjectPaths.BirdPrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Bottom-left pivot sprite with an explicit size in world units.</summary>
        private static Transform Part(string name, Transform parent, int order, float x, float y, float width, float height, Color color)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(x, y, 0f);
            part.transform.localScale = new Vector3(width, height, 1f);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteTextures.Load(SpriteTextures.WhitePath);
            renderer.color = color;
            renderer.sortingOrder = order;
            return part.transform;
        }
    }
}
