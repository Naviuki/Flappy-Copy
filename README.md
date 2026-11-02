# Flappy Copy

A small, complete Flappy Bird style 2D game for **Unity 6.5 (6000.5.1f1)**, URP 2D, built only from Unity
primitives, generated sprites and generated sound: no downloaded art, no paid packages, no audio files from
outside the project. The project also contains the editor tooling that generates the scene, prefabs and data
assets, validates them, runs the checks and cuts a release build.

![Gameplay](Docs/gameplay.png)

## What is in the game

- **Bird**: tap / click / Space gives an upward impulse (`Rigidbody2D`), gravity pulls it down, the sprite
  tilts with the vertical speed; touching a pipe, the ground or flying out of the top of the frame ends the run.
- **Pipes**: top/bottom pairs with a random gap that shrinks with the score down to a minimum, placed anywhere
  over the play area (their centre starts at the bird's height and the first gap is never blocked), scrolling
  left at a constant speed that slowly increases. Pairs come from an **object pool** (no
  `Instantiate`/`Destroy` while playing).
- **Score**: +1 per pipe pair passed (a trigger zone between the pipes); the best score is stored in
  `PlayerPrefs` and shown in the menu and on the result panel ("NEW RECORD!").
- **Sound**: a flap, a point, a hit and a ground thud, generated as short 16-bit WAV clips
  (`Assets/Resources/Sfx*.wav`, written by `FlappyCopy.EditorTools.AudioAssetBuilder`) and played through one
  `AudioSource`. If a clip is missing the game synthesises it in memory instead, so it is never silent for the
  wrong reason. The **SOUND ON/OFF** button (top right) mutes everything and remembers the choice in
  `PlayerPrefs` (`FlappyCopy.SoundEnabled`).
- **Ground and sky**: endless ground made from wrapping strips, clouds and a town silhouette in parallax.
- **States**: Menu → Playing → Game Over, with score, best score and a restart button; a tap also restarts.
- **Portrait 9:16** camera and HUD on any window size; desktop builds start at 720×1280 in a window,
  Android builds are locked to portrait.

Controls: **Space**, **left mouse button**, **touch** or the full-screen button — start, flap and restart.
**R** restarts after a crash. **SOUND** (top right) mutes/unmutes.

## How to run

1. Open the folder in **Unity Hub** with editor **6000.5.1f1** (Unity builds its `Library` folder on first open).
2. Open `Assets/Scenes/Game.unity` (it is the only scene in Build Settings).
3. Press **Play**.

The scene, prefabs and data assets are in the repository. If you change component scripts, run
**Flappy Copy ▸ Repair Assets** once (re-imports the scripts, regenerates data/prefabs/scene, validates).

## Build and release

- **In the editor**: `File ▸ Build Profiles` → Windows (or Android) → Build. Desktop players open windowed.
- **Windows player from the command line**:

  ```bat
  Tools\build_windows.bat [path\to\Unity.exe]
  ```

  Writes `Builds\Windows\FlappyCopy.exe`.
- **Windows release** (validate → build → stage → zip):

  ```bat
  Tools\build_release.bat [path\to\Unity.exe]
  ```

  Validates the project first and stops on any issue, builds the player, then writes
  `Builds\Release\FlappyCopy-Windows` (player, `README.md`, `LICENSE.md`, `VERSION.txt` with the SHA-256 of
  every file) and `Builds\FlappyCopy-Windows.zip`. Debug-only folders Unity writes next to the player are not
  shipped.
- **Android**: switch platform to Android and build (portrait, min API 26, `com.flappycopy.game`).

Player settings (applied by `FlappyCopy.EditorTools.PlayerSetup.Apply`, also from
**Flappy Copy ▸ Apply Player Settings**): product name `Flappy Copy`, version `1.0.0`, 720×1280 windowed,
portrait only on mobile, legacy Input Manager, URP 2D pipeline.

## Architecture

```
Assets/Scripts (FlappyCopy.Runtime)
├── Core/       GameController (composition root + game loop), GameStateMachine + MenuState/PlayingState/
│               GameOverState (State pattern), GameConstants + DesignSpace, DifficultyRules + GapPlacement
│               (engine-free rules), CollisionRelay, LayersBootstrapper (layers/tags/sorting layers),
│               PlayerInputReader + PressPolicy, ReleaseMarker
├── Audio/      SfxId, IAudioService + NullAudioService, AudioService (playback + mute), SfxLibrary
│               (clip synthesis), SoundPreference (memory / PlayerPrefs)
├── Data/       PlayerSettings, PipeSettings, WorldSettings   (ScriptableObjects: all tuning)
├── Input/      IInputSource, UnityInputSource (Space/mouse/touch), ScriptedInputSource
├── Player/     BirdController (Rigidbody2D motion, tilt, crash), BirdKinematics (pure model)
├── Pipes/      IPipePair + PipeRun (slots, recycling), PipePair (sprites + hitboxes),
│               PipePassGate + PipePassRule (one point per pair), PipeSpawner (pool owner), PipeRunMath
├── Pooling/    ObjectPool<T>, IPoolFactory, PrefabPoolFactory, IPoolable
├── Score/      IScoreService, ScoreService (events), IScoreStorage, PlayerPrefsScoreStorage
├── Testing/    SelfTestChecks + SelfTestBootstrap (checks of the self-test player build)
├── UI/         HudController (event subscriber), HudView (texts/panels/buttons), HudFlow, PlayAreaButton,
│               PortraitHudScaler
├── World/      InfinityGround (wrapping strips), ParallaxScroller, ScrollMath
└── Cameras/    PortraitFraming (9:16 camera for any aspect)
```

Requirements and where they are met:

| Requirement | Where |
|---|---|
| One class, one job; interfaces where they pay off | `IInputSource`, `IScoreService`, `IScoreStorage`, `IPipePair`, `IGameState`, `IObjectPool<T>`, `IAudioService` |
| Object pool, no Instantiate/Destroy while playing | `Pooling/ObjectPool<T>` owned and prewarmed by `Pipes/PipeSpawner`, recycled by `PipeRun` |
| State pattern | `Core/GameStateMachine` + `MenuState`, `PlayingState`, `GameOverState`, rules in `GameStateRules` |
| Observer with C# events | `GameController.StateChanged/PointScored/RunEnded`, `ScoreService.ScoreChanged/RunEnded`, `HudView.RestartRequested/SoundToggleRequested` |
| Tuning in ScriptableObjects | `Data/PlayerSettings` (gravity, flap, tilt, difficulty ramp), `PipeSettings` (width, gap, range), `WorldSettings` (speeds, spacing, colours) |
| UI holds no game logic | `HudController` maps state events to `HudView`; buttons call `GameController.RequestPress()` / `ToggleMute()` |
| No singletons, no `FindObjectOfType` in `Update` | dependencies wired in the scene; `GameController` is the only composition root |
| No magic numbers/strings | `GameConstants`, `DesignSpace`, `ProjectLayers`, `SortingLayers`, `SfxId`, enums |

The loop is `GameController.StepFrame()`: read the input policy for the current state, then idle the bird
(menu), run pipes/ground/bird (playing) or let it fall (game over). Death comes as an event from the bird's
trigger hitbox: `CollisionRelay` → `BirdController.OnCollisionEntered` → `GameController.NotifyBirdCrashed` →
`GameStateMachine` (and the hit/ground sounds).

### Collision setup (matters for gameplay)

- The bird prefab has **one** trigger `CircleCollider2D` on its root, next to its `Rigidbody2D`, the
  `CollisionRelay` and `BirdController` (the relay only forwards events to a listener on its own GameObject).
- The pipe prefab has two trigger `BoxCollider2D` hitboxes (`TopHitbox`, `BottomHitbox`) on the `Pipe` layer;
  `PipePair.Layout` sizes them for every gap. Pipe bodies run past the top and the ground, so a column is never
  shorter than the gap.
- The ground is a trigger box on the `Ground` layer, covering the ground block from the surface (y = -6) down.
- Flying out of the top of the frame ends the run (`BirdController.Tick`), the same crash path as a pipe.
- `LayersBootstrapper` keeps the 2D collision matrix and sorting layers in code so a fresh clone behaves the same.

### Rendering

All visuals come from one generated 32×32 white texture (`Assets/Textures/White.png`), imported at 32 pixels
per unit with a **bottom-left pivot**, so a sprite is exactly 1 world unit and each object scales it to its size
in world units (`GameScene.Block`). Every sprite uses one generated **unlit** material
(`Assets/Materials/FlappyCopySprite.mat`, `Universal Render Pipeline/2D/Sprite-Unlit-Default`), so a sprite's
colour is drawn as-is and does not depend on a scene light.

The camera shows the 9:16 design frame (16 units high, 9 wide) and the ground surface sits at y = -6. The ground
is two strips as wide as the frame: the first starts one strip left of the frame, so the covered range is always
[-strip, +strip] and both edges stay under the ground while it scrolls.

The HUD is a screen-space canvas whose reference size follows the camera (`PortraitHudScaler`): one world unit
is 360 reference units, so the 9:16 device box is 1080×1920 reference units and the HUD keeps the world's scale
on phones, on 9:16 and on wide desktop windows. The font is TextMeshPro's default from the TMP essentials
package.

## Editor tooling

| Menu | Command line (`-executeMethod`) | Purpose |
|---|---|---|
| Flappy Copy ▸ Rebuild Scene, Prefabs and Data | `FlappyCopy.EditorTools.EditorSetup.PrepareAndExit` | data assets, `Bird`/`PipePair` prefabs, `Game.unity`, build list, player settings, then validation |
| Flappy Copy ▸ Repair Assets | `FlappyCopy.EditorTools.SceneRepair.RebuildAndExit` | re-import component scripts, rebuild everything, validate |
| Flappy Copy ▸ Validate Project | `FlappyCopy.EditorTools.ProjectValidator.RunAndExit` | every scene reference wired, prefab colliders/components, audio service, input handler, build list |
| Flappy Copy ▸ Apply Player Settings | `FlappyCopy.EditorTools.PlayerSetup.ApplyMenu` | product/version/window/orientation/input handler |
| Flappy Copy ▸ Audio ▸ Rebuild Sound Effects | `FlappyCopy.EditorTools.AudioAssetBuilder.CreateAll` | writes the four WAV clips into `Assets/Resources` |
| Flappy Copy ▸ Release ▸ Build Windows Release | `FlappyCopy.EditorTools.ReleaseBuilder.BuildAndExit` | validate, build, stage `Builds/Release`, zip |
| Flappy Copy ▸ Build ▸ Windows | `FlappyCopy.EditorTools.CiEntry.BuildWindowsCi` | player build to `Builds\Windows` |
| Flappy Copy ▸ Build ▸ Self Test Player | `FlappyCopy.EditorTools.SelfTestBuild.BuildAndExit` | test player with the runtime scene + run checks |
| Flappy Copy ▸ Open Game Scene | — | opens `Assets/Scenes/Game.unity` |

## Tests

`Assets/Tests/EditMode` (`FlappyCopy.Tests.EditMode`), **60 checks**, all passing:

| Fixture | What it checks |
|---|---|
| `LogicTests` | state machine transitions, callbacks and input policy; scoring and the record; the object pool; pipe run (spacing, scrolling, recycling, gap roll); pass-through points; difficulty ramp, gap placement (range and step); bird kinematics; camera framing; ground strip tiling; HUD rules |
| `ObjectPoolTests` | pool prewarm, take/release accounting, callbacks, release-all, double release, null factory |
| `GameSceneTests` | scene/prefabs/settings assets exist, prefabs carry their scripts, the white sprite is 1 unit with a bottom-left pivot, the ground strips cover the frame, audio clips exist and are ordered, legacy input, pool reuse |
| `AudioTests` | generated clips have samples, are audible and not clipped; the envelope is click-free; the mute preference round-trips |
| `TestBootstrapTests` | the assembly always contains a decorated fixture |

```bat
Tools\run_editmode_tests.ps1
```

prints the summary, writes `Logs/harness/editmode_results.json` and exits 0 when every check passed (or use
`Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`). `Tools/TESTS.md` explains the three entry points.

**Self-test player** (runtime checks in a real build, including physics):

```bat
Unity.exe -batchmode -nographics -projectPath "." -executeMethod FlappyCopy.EditorTools.SelfTestBuild.BuildAndExit -quit
Builds\SelfTest\FlappyCopySelfTest.exe -batchmode -screen-width 540 -screen-height 960
type Builds\SelfTest\selftest_output.txt
```

It checks the scene wiring and both prefabs, then plays one run without flapping and expects `GameOver` from a
real ground collision, with pipe pairs spawned. Run it windowed (without `-batchmode`) to also get
`Builds/SelfTest/gameplay.png` and `Builds/SelfTest/result.png`, the captures used for the screenshots here.

## What was verified (2026-11-02)

- Compiles with **no errors and no warnings** (`FlappyCopy.EditorTools.SelfCheckEntry.RunAndExit`).
- `Assets/Scenes/Game.unity` rebuilds and validates with **0 issues** (all `GameController`/`HudController`
  references, `audioService`, prefab colliders and layers, legacy Input Manager, build list).
- **60/60 EditMode checks pass** headless; the log is `Logs/harness/editmode_results.log`.
- The **self-test player** reports `failures=0`: all wiring checks pass, and the scripted run starts, the bird
  falls, the ground contact ends the run (`GameOver`) and pipe pairs were spawned.
- The self-test player was also run in a wide window (1000×700): the world and the HUD scale correctly there.
- `Tools\build_release.bat` succeeds: `Builds\Release\FlappyCopy-Windows`, `Builds\FlappyCopy-Windows.zip`
  (≈40 MB), `VERSION.txt` with SHA-256 hashes.
- The release player starts headless (`-batchmode`, 540×960) for 20 s with no exceptions and no missing-script
  messages.
- `Docs/gameplay.png` (self-test player) and `Docs/preview_scene.png` (scene camera) are real captures.

### Bugs fixed while hardening the release

- A run could never end on a pipe and the bird had no collider on the object that listens to collisions.
- The world was drawn four times too small: the white sprite was imported 8 px at 32 PPU while everything was
  scaled as if a sprite were 1 unit, and it had a centred pivot instead of the bottom-left one the layout uses.
- Pipe columns stopped short of the ground and the frame, and the ground did not cover the left part of the
  frame. Pipes now span the full height behind their caps and the two frame-wide ground strips cover
  [-strip, +strip] as they scroll.
- Pipe gaps only used the top part of the screen: their centre could not reach the upper play area and the first
  gap ignored the bird's start height. The range is now the whole play area and the first gap starts at the
  bird's height.
- Flying out of the top of the frame did not end the run.
- The HUD scaled itself independently of the camera, so on a wide window it was cut at the edges and huge; it
  now follows the camera (`PortraitHudScaler` + `DeviceRect`).
- Sprites had no material of their own: the renderer fell back to a default that ignores the transform scale in
  an edit-mode render (a strip that was 9 units wide painted 4.5 units). They now use the generated unlit
  material, and `Validate Project` fails if any sprite has no material.
- The scene had no light at all; the UI sprite material is lit, so the HUD was invisible in edit-mode renders.
  The game now uses unlit sprites and the preview capture adds a temporary light if a scene has none.

## Known limitations

- **Android** is configured (portrait, min API 26, bundle id, version code) but no Android build was produced
  on this machine.
- **Music**: there is none. Only the four short effects, generated for this project.
- **Sound button placement**: it is a UI button in the top-right of the 9:16 frame; there is no key binding.
- **Unity Test Runner UI**: the API-driven run inside a scripted windowed editor can lose its scheduled start
  when the session compiles scripts on startup. The headless entry (`EditModeTestEntry`) is the reliable path
  and runs the same tests.
- If a prefab or the scene ever loses components (for example after renaming a component class), run
  **Flappy Copy ▸ Repair Assets**. **Validate Project** names the exact missing piece.
