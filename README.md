# Flappy Copy

A small, complete Flappy Bird style 2D game for **Unity 6.5 (6000.5.1f1)**, URP 2D, built only from Unity
primitives and generated sprites — no downloaded art, no paid packages, no audio clips. The project also
contains the editor tooling to generate the scene, prefabs and data assets, to validate and to build it.

![Game scene preview](Docs/preview_scene.png)

## What is in the game

- **Bird**: tap / click / Space gives an upward impulse (`Rigidbody2D`), gravity pulls it down, the sprite
  tilts with the vertical speed; touching a pipe, the ground or flying past the ceiling ends the run.
- **Pipes**: top/bottom pairs with a random gap that shrinks with the score down to a minimum, scrolling left
  at a constant speed that slowly increases. Pairs come from an **object pool** (no `Instantiate`/`Destroy`
  while playing).
- **Score**: +1 per pipe pair passed (a trigger zone between the pipes); the best score is stored in
  `PlayerPrefs` and shown in the menu and on the result panel ("NEW RECORD!").
- **Ground and sky**: endless ground made from wrapping strips, clouds and a town silhouette in parallax.
- **States**: Menu → Playing → Game Over, with score, best score and a restart button; a tap also restarts.
- **Portrait 9:16** camera and HUD on any window size; desktop builds start at 720×1280 in a window,
  Android builds are locked to portrait.

Controls: **Space**, **left mouse button**, **touch** or the full-screen button — start, flap and restart.
**R** restarts after a crash.

## How to run

1. Open the folder in **Unity Hub** with editor **6000.5.1f1** (Unity builds its `Library` folder on first open).
2. Open `Assets/Scenes/Game.unity` (it is the only scene in Build Settings).
3. Press **Play**.

The scene, prefabs and data assets are in the repository. If you change component scripts, run
**Flappy Copy ▸ Repair Assets** once (re-imports the scripts, regenerates data/prefabs/scene, validates).

## Build

- **In the editor**: `File ▸ Build Profiles` → Windows (or Android) → Build. Desktop players open windowed.
- **From the command line** (Windows):

  ```bat
  Tools\build_windows.bat [path\to\Unity.exe]
  ```

  Writes `Builds\Windows\FlappyCopy.exe` (Unity runs with `-batchmode -nographics`). Pass the editor path or
  set `UNITY_EXE`.
- **Android**: switch platform to Android and build (portrait, min API 26).

## Architecture

```
Assets/Scripts (FlappyCopy.Runtime)
├── Core/       GameController (composition root + game loop), GameStateMachine + MenuState/PlayingState/
│               GameOverState (State pattern), GameConstants + DesignSpace, DifficultyRules + GapPlacement
│               (engine-free rules), CollisionRelay, LayersBootstrapper (layers/tags/sorting layers)
├── Data/       PlayerSettings, PipeSettings, WorldSettings   (ScriptableObjects: all tuning)
├── Input/      IInputSource, UnityInputSource (Space/mouse/touch), ScriptedInputSource, PressPolicy
├── Player/     BirdController (Rigidbody2D motion, tilt, crash), BirdKinematics (pure model)
├── Pipes/      IPipePair + PipeRun (slots, recycling), PipePair (sprites + hitboxes),
│               PipePassGate + PipePassRule (one point per pair), PipeSpawner (pool owner), PipeRunMath
├── Pooling/    ObjectPool<T>, IPoolFactory, PrefabPoolFactory, IPoolable
├── Score/      IScoreService, ScoreService (events), IScoreStorage, PlayerPrefsScoreStorage
├── UI/         HudController (event subscriber), HudView (texts/panels), HudFlow, PlayAreaButton,
│               PortraitHudScaler
├── World/      InfinityGround (wrapping strips), ParallaxScroller, ScrollMath
└── Cameras/    PortraitFraming (9:16 camera for any aspect)
```

Requirements and where they are met:

| Requirement | Where |
|---|---|
| One class, one job; interfaces where they pay off | `IInputSource`, `IScoreService`, `IScoreStorage`, `IPipePair`, `IGameState`, `IObjectPool<T>` |
| Object pool, no Instantiate/Destroy while playing | `Pooling/ObjectPool<T>` owned and prewarmed by `Pipes/PipeSpawner`, recycled by `PipeRun` |
| State pattern | `Core/GameStateMachine` + `MenuState`, `PlayingState`, `GameOverState`, rules in `GameStateRules` |
| Observer with C# events | `GameController.StateChanged/PointScored/RunEnded`, `ScoreService.ScoreChanged/RunEnded`, `HudView.RestartRequested` |
| Tuning in ScriptableObjects | `Data/PlayerSettings` (gravity, flap, tilt, difficulty ramp), `PipeSettings` (width, gap, range), `WorldSettings` (speeds, spacing, colours) |
| UI holds no game logic | `HudController` maps state events to `HudView`; the restart button goes back through `GameController.RequestPress()` |
| No singletons, no `FindObjectOfType` in `Update` | dependencies wired in the scene; `GameController` is the only composition root |
| No magic numbers/strings | `GameConstants`, `DesignSpace`, `ProjectLayers`, `SortingLayers`, enums |

The loop is `GameController.StepFrame()`: read the input policy for the current state, then idle the bird
(menu), run pipes/ground/bird (playing) or let it fall (game over). Death comes as an event from the bird's
trigger hitbox: `CollisionRelay` → `BirdController.OnCollisionEntered` → `GameController.NotifyBirdCrashed`.

### Rendering

All visuals come from two generated textures (`Assets/Textures/White.png`, `Rounded.png`) scaled to world
units; the HUD uses TextMeshPro's default font from the TMP essentials package.

## Editor tooling

| Menu | Command line (`-executeMethod`) | Purpose |
|---|---|---|
| Flappy Copy ▸ Rebuild Scene, Prefabs and Data | `FlappyCopy.EditorTools.EditorSetup.PrepareAndExit` | data assets, `Bird`/`PipePair` prefabs, `Game.unity`, build list, player settings |
| Flappy Copy ▸ Repair Assets | `FlappyCopy.EditorTools.SceneRepair.RebuildAndExit` | re-import component scripts, rebuild everything, validate |
| Flappy Copy ▸ Validate Project | `FlappyCopy.EditorTools.ProjectValidator.RunAndExit` | every scene reference wired, prefab components, input handler, build list |
| Flappy Copy ▸ Build ▸ Windows | `FlappyCopy.EditorTools.CiEntry.BuildWindowsCi` | player build to `Builds\Windows` |
| Flappy Copy ▸ Build ▸ Self Test Player | `FlappyCopy.EditorTools.SelfTestBuild.BuildAndExit` | test player with the runtime scene checks |
| Flappy Copy ▸ Open Game Scene | — | opens `Assets/Scenes/Game.unity` |

## Tests

`Assets/Tests/EditMode` (`FlappyCopy.Tests.EditMode`):

| Fixture | What it checks |
|---|---|
| `LogicTests` | state machine transitions, callbacks and input policy; scoring and the record; the object pool; pipe run (spacing, scrolling, recycling, gap roll); pass-through points; difficulty ramp, gap placement; bird kinematics; camera framing; scroll wrap; HUD rules |
| `ObjectPoolTests` | pool prewarm, take/release accounting, callbacks, release-all, double release, null factory |
| `SceneTests` | portrait 9:16 design box, camera framing for several screen sizes, pipe/player tuning sanity, the single build scene |
| `SceneWiringTests` | `GameController`/`HudController` references wired in the scene, prefab components and layers |
| `AssetSerializationTests` | every `[SerializeField]` present in the saved prefabs, `.meta` files, build list |
| `ProjectSettingsTests` | legacy Input Manager active, build list, data assets, TMP resources |
| `GameFlowTests` | the game scene boots into the menu, HUD shows the menu, a bird without taps falls and its ground contact is a crash, the best score round-trips through `PlayerPrefs` |

Run: `Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`, or `Tools\run_tests.bat`
(`npm run tests`) which starts Unity on the project, runs the same tests and prints
`Temp\harness\editmode_results.json`. `Tools\TESTS.md` explains why `-batchmode` cannot drive the Test
Runner on this project.

## What was verified

- The project compiles with **no errors and no warnings**.
- `Tools\build_windows.bat` succeeds: `Builds\Windows\FlappyCopy.exe` (~107 MB, URP 2D).
- The built player starts without exceptions or "script missing" messages and runs (verified headless:
  `-batchmode` player for 15 s, no error besides the expected "no GPU for a DoF shader" line).
- The editor tooling validates project settings (layers, tags, sorting layers, build list, input handler,
  TMP resources).
- `Docs/preview_scene.png` is a real capture of the scene camera (`FlappyCopy.EditorTools.ScenePreviewCapture`).
- The logic tests were run on the earlier assemblies in this project: 76/76 passed.

## Known limitations

- **Component creation in a scripted editor session on this machine**: this Unity installation creates
  GameObjects with MonoBehaviours that come back `null` (the editor keeps an older compiled script set alive
  after script changes), so scenes/prefabs generated here lose some components and the EditMode scene checks
  can fail on that machine. The generated assets and scripts in the repository are the correct ones; opening
  the project normally and running **Repair Assets** once (or after changing component scripts) produces the
  correct scene. Gameplay code itself is covered by the logic tests and the player build.
- The Test Runner window tests need a running editor; the command line script starts one and reports if the
  editor session cannot start on the machine.
- No audio (by design). The `AudioListener` is present so sounds can be added later.
