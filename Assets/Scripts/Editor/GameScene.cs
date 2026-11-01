using FlappyCopy.Cameras;
using FlappyCopy.Core;
using FlappyCopy.Data;
using FlappyCopy.Pipes;
using FlappyCopy.Player;
using FlappyCopy.UI;
using FlappyCopy.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FlappyCopy.EditorTools
{
    using PlayerSettings = FlappyCopy.Data.PlayerSettings;

    /// <summary>
    /// Builds Assets/Scenes/Game.unity from code: camera, parallax background, ground, pipes, bird,
    /// HUD canvas and the composition root. Prefabs and data assets must exist already.
    /// Sprites are scaled to world units explicitly (see <see cref="Block"/>); UI uses the 9:16
    /// <see cref="DeviceRect"/> container so the layout is identical on every window size.
    /// </summary>
    public static class GameScene
    {
        private const float GroundTop = -DesignSpace.PlayHalfHeight;
        private const float HalfRefWidth = DesignSpace.ReferenceWinWidth * 0.5f;
        private const float HalfRefHeight = DesignSpace.ReferenceWinHeight * 0.5f;

        [MenuItem("Flappy Copy/Open Game Scene")]
        public static void OpenGameScene()
        {
            EditorSceneManager.OpenScene(ProjectPaths.ScenePath);
        }

        public static void Assemble()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.BirdPrefabPath);
            GameObject pipePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectPaths.PipePairPrefabPath);
            if (birdPrefab == null || pipePrefab == null)
            {
                Debug.LogError("[FlappyCopy] Prefabs are missing; rebuild the project first.");
                return;
            }

            WorldSettings world = FlappyData.World;
            PipeSettings pipes = FlappyData.Pipes;
            PlayerSettings player = FlappyData.Player;

            // ------------------------------------------------------------ camera
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, DesignSpace.CameraDistance);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = DesignSpace.HalfHeight;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = world != null ? world.SkyColor : new Color(0.42f, 0.77f, 0.94f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.depth = -1;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraObject.AddComponent<AudioListener>();
            var framing = cameraObject.AddComponent<PortraitFraming>();

            new GameObject("Boot").AddComponent<LayersBootstrapper>();

            // ------------------------------------------------------------ game root
            GameObject gameObject = new GameObject("Game");
            GameController game = gameObject.AddComponent<GameController>();

            Transform background = new GameObject("Background").transform;
            background.SetParent(gameObject.transform, false);
            background.localPosition = new Vector3(0f, 0f, 40f);
            Block("Sky", background, -40f, -20f, 80f, 40f, 0f,
                world != null ? world.SkyColor : Color.cyan, SortingLayers.Order(SortingLayers.Background));
            BuildClouds(background, world);
            BuildScenery(background, world);

            Transform ground = new GameObject("Ground").transform;
            ground.SetParent(gameObject.transform, false);
            ground.localPosition = new Vector3(0f, GroundTop, 0f);
            var strips = new Transform[2];
            for (int i = 0; i < strips.Length; i++)
            {
                Transform strip = new GameObject("Strip " + i).transform;
                strip.SetParent(ground, false);
                strip.localPosition = new Vector3(i * DesignSpace.PortraitWidth, 0f, 0f);
                Block("Grass " + i, strip, 0f, DesignSpace.GroundHeight - 0.2f, DesignSpace.PortraitWidth, 0.4f, -0.01f,
                    world != null ? world.GrassColor : Color.green, SortingLayers.Order(SortingLayers.Ground));
                Block("Soil " + i, strip, 0f, -0.2f, DesignSpace.PortraitWidth, DesignSpace.GroundHeight - 0.2f, 0f,
                    world != null ? world.SoilColor : new Color(0.8f, 0.7f, 0.5f), SortingLayers.Order(SortingLayers.Ground));
                Block("Stripe " + i, strip, 0f, DesignSpace.GroundHeight - 0.7f, DesignSpace.PortraitWidth, 0.14f, -0.02f,
                    world != null ? world.StripeColor : new Color(0.7f, 0.6f, 0.4f), SortingLayers.Order(SortingLayers.Ground) + 1);
                strips[i] = strip;
            }

            var groundScroller = ground.gameObject.AddComponent<InfinityGround>();
            SetPrivate(groundScroller, "worldSettings", world);
            SetPrivateArray(groundScroller, "strips", strips);

            BoxCollider2D groundCollider = ground.gameObject.AddComponent<BoxCollider2D>();
            groundCollider.isTrigger = true;
            groundCollider.size = new Vector2(200f, DesignSpace.GroundHeight - 0.2f);
            groundCollider.offset = new Vector2(0f, (DesignSpace.GroundHeight - 0.2f) * 0.5f);
            ground.gameObject.AddComponent<CollisionRelay>();
            SetLayerRecursively(ground.gameObject, ProjectLayers.Ground);
            ground.gameObject.tag = ProjectLayers.GroundName;

            // ------------------------------------------------------------ pipes and bird
            Transform pipesRoot = new GameObject("Pipes").transform;
            pipesRoot.SetParent(gameObject.transform, false);
            var spawner = pipesRoot.gameObject.AddComponent<PipeSpawner>();
            SetPrivate(spawner, "pipePairPrefab", pipePrefab.GetComponent<PipePair>());
            SetPrivate(spawner, "pipeSettings", pipes);
            SetPrivate(spawner, "worldSettings", world);
            SetPrivate(spawner, "game", game);
            SetPrivate(spawner, "targetCamera", camera);

            var birdInstance = (GameObject)PrefabUtility.InstantiatePrefab(birdPrefab);
            birdInstance.name = "Bird";
            birdInstance.transform.SetParent(gameObject.transform, false);
            Vector2 start = player != null ? player.StartPosition : new Vector2(-1.6f, 1.2f);
            birdInstance.transform.position = new Vector3(start.x, start.y, 0f);
            var bird = birdInstance.GetComponent<BirdController>();
            SetPrivate(bird, "game", game);

            SetPrivate(game, "playerSettings", player);
            SetPrivate(game, "pipeSettings", pipes);
            SetPrivate(game, "worldSettings", world);
            SetPrivate(game, "cameraFraming", framing);
            SetPrivate(game, "bird", bird);
            SetPrivate(game, "pipeSpawner", spawner);
            SetPrivate(game, "ground", groundScroller);

            var audioService = gameObject.AddComponent<Audio.AudioService>();
            SetPrivate(game, "audioService", audioService);

            BuildHud(game);
        }

        // ------------------------------------------------------------------ world helpers

        public static void SetLayerRecursively(GameObject root, int layer)
        {
            if (layer < 0)
            {
                return;
            }

            root.layer = layer;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
            }
        }

        public static PipePassGate AddGate(Transform parent)
        {
            var gateObject = new GameObject("ScoreGate");
            gateObject.transform.SetParent(parent, false);
            var gate = gateObject.AddComponent<PipePassGate>();
            if (ProjectLayers.PipePass >= 0)
            {
                gateObject.layer = ProjectLayers.PipePass;
            }

            return gate;
        }

        /// <summary>Sprite with a bottom-left pivot, positioned by its corner and sized in world units.</summary>
        public static SpriteRenderer Block(string name, Transform parent, float x, float y, float width, float height,
            float z, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = new Vector3(x, y, z);
            spriteObject.transform.localScale = new Vector3(width, height, 1f);
            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteTextures.Load(SpriteTextures.WhitePath);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void BuildClouds(Transform background, WorldSettings world)
        {
            Color color = world != null ? world.CloudColor : Color.white;
            color.a = world != null ? Mathf.Clamp01(world.CloudOpacity) : 0.45f;
            float[][] blobs =
            {
                new[] { 0.4f, 4.3f, 1.2f }, new[] { 2.1f, 4.7f, 1.05f }, new[] { 3.9f, 4.2f, 1.1f },
                new[] { 5.6f, 4.9f, 1f }, new[] { 7.3f, 4.35f, 1.25f }, new[] { 9.0f, 4.6f, 0.95f }
            };
            for (int i = 0; i < blobs.Length; i++)
            {
                float width = 1.6f * blobs[i][2];
                float height = 0.62f;
                Block("Cloud " + i, background, blobs[i][0] - (width * 0.5f), blobs[i][1] - (height * 0.5f),
                    width, height, 0f, color, SortingLayers.Order(SortingLayers.Background) + 1);
            }
        }

        private static void BuildScenery(Transform background, WorldSettings world)
        {
            Color skyline = world != null ? world.SkylineColor : new Color(0.52f, 0.82f, 0.72f);
            Color house = world != null ? world.HouseColor : new Color(0.8f, 0.92f, 0.76f);
            Color bush = world != null ? world.BushColor : new Color(0.4f, 0.72f, 0.33f);

            Transform row = new GameObject("Scenery").transform;
            row.SetParent(background, false);
            row.localPosition = new Vector3(0f, -4.4f, 2f);
            float[] heights = { 1.4f, 2.2f, 1f, 1.8f, 1.2f, 2.5f, 1.6f, 1.1f, 2f, 1.3f, 1.7f, 1f };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = -6.5f + (i * 1.15f);
                Block("House " + i, row, x, 0f, 0.95f, heights[i], 0f, skyline, SortingLayers.Order(SortingLayers.Scenery));
                Block("Roof " + i, row, x - 0.1f, heights[i], 1.15f, 0.24f, 0f, house,
                    SortingLayers.Order(SortingLayers.Scenery) + 1);
            }

            for (int i = 0; i < 6; i++)
            {
                Block("Bush " + i, row, -6.7f + (i * 2.4f), 0f, 1.6f, 0.7f, -1f, bush,
                    SortingLayers.Order(SortingLayers.Scenery));
            }
        }

        // ------------------------------------------------------------------ HUD

        private static void BuildHud(GameController game)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(game.transform, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();

            var canvasObject = new GameObject("HUD Canvas");
            canvasObject.transform.SetParent(game.transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignSpace.ReferenceWinWidth, DesignSpace.ReferenceWinHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasObject.AddComponent<GraphicRaycaster>();

            // Full-screen press area (behind everything else).
            GameObject playArea = UiObject("PlayAreaButton", canvasObject.transform);
            Stretch(playArea);
            var image = playArea.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            var button = playArea.AddComponent<Button>();
            button.targetGraphic = image;
            var playAreaButton = playArea.AddComponent<PlayAreaButton>();
            SetPrivate(playAreaButton, "game", game);

            Transform device = UiObject("Device", canvasObject.transform).transform;
            device.gameObject.AddComponent<DeviceRect>().Configure(Vector2.zero);

            GameObject menuPanel = UiObject("MenuPanel", device);
            Stretch(menuPanel);
            TMP_Text title = Label("Title", menuPanel, new Vector2(0f, 380f), 150f, Color.white);
            title.text = "FLAPPY COPY";
            TMP_Text hint = Label("Hint", menuPanel, new Vector2(0f, 190f), 64f, Color.white);
            hint.text = "SPACE / CLICK / TAP";
            TMP_Text menuBest = Label("MenuBest", menuPanel, new Vector2(0f, 80f), 52f, new Color(1f, 1f, 1f, 0.9f));
            menuBest.SetText("BEST  {0}", 0);

            GameObject hudPanel = UiObject("HudPanel", device);
            Stretch(hudPanel);
            TMP_Text scoreText = Label("Score", hudPanel, new Vector2(0f, 600f), 170f, Color.white);
            scoreText.SetText("{0}", 0);

            GameObject gameOverPanel = UiObject("GameOverPanel", device);
            Stretch(gameOverPanel);
            TMP_Text gameOverTitle = Label("GameOver", gameOverPanel, new Vector2(0f, 380f), 130f, new Color(1f, 0.42f, 0.35f, 1f));
            gameOverTitle.text = "GAME OVER";
            TMP_Text resultScore = Label("ResultScore", gameOverPanel, new Vector2(0f, 160f), 190f, Color.white);
            resultScore.SetText("{0}", 0);
            TMP_Text resultBest = Label("ResultBest", gameOverPanel, new Vector2(0f, 20f), 52f, new Color(1f, 1f, 1f, 0.9f));
            resultBest.SetText("BEST  {0}", 0);
            TMP_Text newRecord = Label("NewRecord", gameOverPanel, new Vector2(0f, -60f), 56f, new Color(1f, 0.85f, 0.2f, 1f));
            newRecord.text = "NEW RECORD!";

            GameObject restartObject = UiObject("RestartButton", gameOverPanel.transform);
            var restartRect = restartObject.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.5f);
            restartRect.anchorMax = new Vector2(0.5f, 0.5f);
            restartRect.pivot = new Vector2(0.5f, 0.5f);
            restartRect.anchoredPosition = new Vector2(0f, -300f);
            restartRect.sizeDelta = new Vector2(560f, 170f);
            var restartImage = restartObject.AddComponent<Image>();
            restartImage.color = new Color(1f, 0.64f, 0.22f, 1f);
            var restartButton = restartObject.AddComponent<Button>();
            restartButton.targetGraphic = restartImage;
            TMP_Text restartLabel = Label("Label", restartObject, Vector2.zero, 64f, Color.white, fill: true);
            restartLabel.text = "RESTART";

            // Sound toggle: top right corner of the 9:16 frame, visible in the menu and on the result panel.
            GameObject soundObject = UiObject("SoundButton", device);
            var soundRect = soundObject.GetComponent<RectTransform>();
            soundRect.anchorMin = new Vector2(1f, 1f);
            soundRect.anchorMax = new Vector2(1f, 1f);
            soundRect.pivot = new Vector2(1f, 1f);
            soundRect.anchoredPosition = new Vector2(-40f, -40f);
            soundRect.sizeDelta = new Vector2(300f, 90f);
            var soundImage = soundObject.AddComponent<Image>();
            soundImage.color = new Color(1f, 1f, 1f, 0.18f);
            var soundButton = soundObject.AddComponent<Button>();
            soundButton.targetGraphic = soundImage;
            TMP_Text soundLabel = Label("Label", soundObject, Vector2.zero, 44f, Color.white, fill: true);
            soundLabel.text = "SOUND  ON";

            var view = canvasObject.AddComponent<HudView>();
            SetPrivate(view, "scoreText", scoreText);
            SetPrivate(view, "bestMenuText", menuBest);
            SetPrivate(view, "resultScoreText", resultScore);
            SetPrivate(view, "resultBestText", resultBest);
            SetPrivate(view, "newRecordText", newRecord);
            SetPrivate(view, "menuPanel", menuPanel);
            SetPrivate(view, "hudPanel", hudPanel);
            SetPrivate(view, "gameOverPanel", gameOverPanel);
            SetPrivate(view, "restartButton", restartButton);
            SetPrivate(view, "soundButton", soundButton);
            SetPrivate(view, "soundLabel", soundLabel);

            var controller = canvasObject.AddComponent<HudController>();
            SetPrivate(controller, "view", view);
            SetPrivate(controller, "game", game);
        }

        private static GameObject UiObject(string name, Transform parent)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                uiObject.layer = uiLayer;
            }

            return uiObject;
        }

        /// <summary>Centered label in device pixels; <paramref name="position"/> is measured from the device center.</summary>
        private static TMP_Text Label(string name, GameObject parent, Vector2 position, float size, Color color, bool fill = false)
        {
            GameObject labelObject = UiObject(name, parent.transform);
            var rect = labelObject.GetComponent<RectTransform>();
            if (fill)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(HalfRefWidth * 2f, size * 1.4f);
            }

            var text = labelObject.AddComponent<TextMeshProUGUI>();
            text.font = TmpFont.Get();
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.55f;
            text.fontSizeMax = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color32(20, 55, 85, 255);
            return text;
        }

        private static void Stretch(GameObject target)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ serialized wiring

        public static void SetPrivate(Object target, string fieldName, Object value)
        {
            SerializedProperty property = FindProperty(target, fieldName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetPrivate(Object target, string fieldName, float value)
        {
            SerializedProperty property = FindProperty(target, fieldName);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetPrivateArray<T>(Object target, string fieldName, T[] values) where T : Object
        {
            SerializedProperty property = FindProperty(target, fieldName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning("[FlappyCopy] Cannot set '" + fieldName + "': target is null.");
                return null;
            }

            SerializedProperty property = new SerializedObject(target).FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarningFormat("[FlappyCopy] Field '{0}' not found on {1}.", fieldName, target.GetType().Name);
            }

            return property;
        }
    }

    /// <summary>Loads the TextMeshPro default font that ships with the TMP package.</summary>
    public static class TmpFont
    {
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public static TMP_FontAsset Get()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            if (font == null)
            {
                Debug.LogWarning("[FlappyCopy] TMP font asset missing; import TMP Essential Resources.");
            }

            return font;
        }
    }
}
