using FlappyCopy.Cameras;
using FlappyCopy.Core;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// Structural checks of the game scene that do not need Unity components to be created: the scene has a
    /// camera and HUD, the gameplay values in the data assets are sane, the design box is portrait 9:16.
    /// (Component wiring is checked by the runtime self-test player, see Tools/run_editmode_tests.ps1.)
    /// </summary>
    public sealed class SceneTests
    {
        [NUnit.Framework.Test]
        public static void DesignBoxIsPortraitNineSixteen()
        {
            Check.Equal(9, DesignSpace.PortraitWidth, "portrait width");
            Check.Equal(16, DesignSpace.PortraitHeight, "portrait height");
            Check.Near(8f, DesignSpace.HalfHeight, 0.0001f, "half height");
            Check.Near(4.5f, DesignSpace.HalfWidth, 0.0001f, "half width");
        }

        [NUnit.Framework.Test]
        public static void CameraKeepsTheDesignBoxOnStandardScreens()
        {
            Check.Near(DesignSpace.HalfHeight, PortraitFraming.ComputeOrthographicSize(1080, 1920), 0.001f, "9:16");
            Check.Near(DesignSpace.HalfHeight, PortraitFraming.ComputeOrthographicSize(720, 1280), 0.001f, "720p");
            Check.True(PortraitFraming.ComputeOrthographicSize(1080, 2340) > DesignSpace.HalfHeight, "tall phone");
            Check.Near(DesignSpace.HalfHeight, PortraitFraming.ComputeOrthographicSize(1920, 1080), 0.001f, "windowed landscape");
        }

        [NUnit.Framework.Test]
        public static void PipeSettingsArePlayable()
        {
            var pipes = ScriptableObject.CreateInstance<Data.PipeSettings>();
            Check.True(pipes.PipeWidth > 0.2f && pipes.PipeWidth < 3f, "pipe width");
            Check.True(pipes.BaseGap >= pipes.MinimumGap, "base gap above the floor");
            Check.True(pipes.MinimumGap >= Data.PipeSettings.DefaultMinimumGap, "minimum gap");
            Check.True(pipes.GapCenterRange.x < pipes.GapCenterRange.y, "gap range ordered");
            Check.True(pipes.CapHeight > 0f && pipes.CapHeight < pipes.BaseGap, "cap height");
        }

        [NUnit.Framework.Test]
        public static void PlayerSettingsAreTunedForFlapping()
        {
            var player = ScriptableObject.CreateInstance<Data.PlayerSettings>();
            Check.True(player.GravityScale > 0f, "gravity");
            Check.True(player.FlapVelocity > 0f, "flap velocity");
            Check.True(player.MaximumFallSpeed > player.FlapVelocity * 0.5f, "fall speed");
            Check.True(player.StartPosition.x < 0f, "start left of centre");
            Check.True(Mathf.Abs(player.StartPosition.x) < DesignSpace.HalfWidth, "start inside the frame");
        }

        [NUnit.Framework.Test]
        public static void SceneAssetIsTheBuildScene()
        {
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            Check.Equal(1, scenes.Length, "one scene in the build list");
            Check.True(scenes.Length > 0 && scenes[0].enabled, "scene enabled");
            Check.Equal("Assets/Scenes/Game.unity", scenes.Length > 0 ? scenes[0].path : string.Empty, "game scene path");
        }
    }
}
