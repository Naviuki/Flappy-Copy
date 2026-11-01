# EditMode tests

`Assets/Tests/EditMode` (`FlappyCopy.Tests.EditMode`) holds plain NUnit EditMode tests. There are three ways to
run them; all of them run the same `[Test]` methods.

## 1. From the command line (used by CI, works headless)

```bat
Tools\run_editmode_tests.ps1
```

or

```bat
"%UNITY_EXE%" -batchmode -nographics -projectPath "." ^
  -executeMethod FlappyCopy.Tests.EditModeTestEntry.RunAndExit -quit
```

`EditModeTestEntry` collects every public static parameterless `[Test]` method of the fixture classes and calls
them on the editor's main thread, then writes `Logs/harness/editmode_results.json` and `.log` and exits with
0 when everything passed. The file lives under `Logs/`, not `Temp/`, because Unity empties `Temp/` when the
editor quits.

This path does not use the Unity Test Runner UI, so it does not run `[SetUp]`/`[TearDown]` or parameterised
tests. Every test in this project is a self-contained static method, so nothing is skipped.

## 2. From the Test Runner window

Open the project in Unity 6000.5.1f1, then `Window > General > Test Runner > EditMode` and press **Run All**.

## 3. Through the TestRunner API in a scripted editor

`FlappyCopy.Tests.TestRunnerEntry.RunEditModeAndExit` starts the real Test Runner API (no `-batchmode`) and
writes the same result files. It needs a windowed editor session and is the least reliable of the three on a
machine where a scripted session has to compile scripts on startup: the run is scheduled with
`EditorApplication.delayCall`, and a domain reload can drop it. Use path 1 for automation.

## Fixtures

| Fixture | Covers |
|---|---|
| `LogicTests` | state machine transitions and callbacks, input policy, scoring and the record, object pool, pipe run (spacing, scrolling, recycling, gap roll), pass-through points, difficulty ramp, gap placement, bird kinematics, camera framing, scroll wrap, HUD rules |
| `ObjectPoolTests` | pool prewarm, take/release accounting, callbacks, release-all, double and foreign releases, null factory |
| `GameSceneTests` | the scene and prefabs exist, prefabs carry their scripts, audio clips exist, legacy input, pool reuse, settings asset paths |
| `AudioTests` | generated clips have data and are audible without clipping, the envelope is click-free, the mute preference round-trips, the silent service counts plays |
| `TestBootstrapTests` | at least one `[TestFixture]` exists, so the assembly is always loaded by the Test Runner |

`Check.True/False/Equal/Near/Sequence` are the small assertion helpers the tests share.

## Self-test player (runtime checks in a real build)

The EditMode tests cannot exercise physics. `FlappyCopy.EditorTools.SelfTestBuild.BuildAndExit` builds a
development player with `FLAPPYCOPY_SELF_TEST`:

```bat
Unity.exe -batchmode -nographics -projectPath "." ^
  -executeMethod FlappyCopy.EditorTools.SelfTestBuild.BuildAndExit -quit
Builds\SelfTest\FlappyCopySelfTest.exe -batchmode -screen-width 540 -screen-height 960
type Builds\SelfTest\selftest_output.txt
```

The player checks every scene reference, the HUD wiring, both prefabs (layers, trigger colliders), layers and
sorting layers, then starts a run without flapping and expects the bird to fall into the ground and the run to
end in `GameOver` (this is the real collision + relay + state machine path). It writes
`Builds/SelfTest/selftest_output.txt` and exits with 0 only if every line is `PASS`.

## If a check fails

`Flappy Copy ▸ Validate Project` (`FlappyCopy.EditorTools.ProjectValidator.RunAndExit`) lists the exact missing
reference or component. After changing component scripts, run `Flappy Copy ▸ Repair Assets`: it re-imports the
component scripts and regenerates data, prefabs and the scene.
