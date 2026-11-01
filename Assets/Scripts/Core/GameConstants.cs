using UnityEngine;

namespace FlappyCopy.Core
{
    /// <summary>Scene names, player preference keys and other string literals used by the game.</summary>
    public static class GameConstants
    {
        public const string GameSceneName = "Game";
        public const string GameScenePath = "Assets/Scenes/Game.unity";
        public const string BestScorePrefKey = "FlappyCopy.BestScore";
        public const string SoundEnabledPrefKey = "FlappyCopy.SoundEnabled";
        public const string ProductName = "Flappy Copy";
        public const string DisplayTitle = "FLAPPY COPY";
    }

    /// <summary>
    /// Portrait 9:16 design box. All gameplay coordinates are expressed in design units inside this box,
    /// regardless of the real screen aspect ratio (which only widens the visible field).
    /// </summary>
    public static class DesignSpace
    {
        public const int PortraitWidth = 9;
        public const int PortraitHeight = 16;
        public const int ReferenceWinWidth = 1080;
        public const int ReferenceWinHeight = 1920;
        public const int ReferenceWinWidthMin = 720;
        public const int ReferenceWinHeightMin = 1280;

        public const float HalfHeight = PortraitHeight * 0.5f;
        public const float HalfWidth = PortraitWidth * 0.5f;
        public const float GroundHeight = 2f;
        public const float PlayHalfHeight = HalfHeight - (GroundHeight * 0.5f);
        public const float DeviceWidthUnits = PortraitWidth;
        public const float CameraDistance = -10f;
    }

    /// <summary>Layer names (see <see cref="ProjectLayers"/>) and the single UI button used by the game.</summary>
    public static class UiNames
    {
        public const string InputButton = "PlayArea";
    }
}
