# EditMode tests (menu project)

Unity cannot run these tests from the command line on this project. The tests still run, from the
Test Runner window, as plain NUnit EditMode tests.

## Why the command line cannot run them

* `-runTests` / `-executeMethod` in `-batchmode` never processes the test run: a scripted batch editor
  exits from `-executeMethod` before its update loop processes anything, and the Unity Test Runner API
  starts runs only outside batch mode (`Application.isBatchMode` guard in `UnityEditor.TestRunner`).
* Opening a windowed editor from the script and pressing Run is blocked by the same license handshake
  that makes a scripted editor exit with "Access token is unavailable".

## Running the tests

1. Open the project in Unity 6000.5.1f1 (URP 2D template).
2. `Window > General > Test Runner > EditMode`.
3. Press **Run All**.

The fixture list:

| Fixture | Covers |
|---------|--------|
| `LogicTests` | state machine, scoring and the record, object pool, pipe run and recycling, pass-through points, difficulty, bird kinematics, camera framing, scroll wrap, HUD rules |
| `SceneWiringTests` | every reference wired in the saved scene, prefab components, layers and sorting layers |
| `AssetSerializationTests` | every `[SerializeField]` present in the prefabs, `.meta` files, build scene list |
| `ProjectSettingsTests` | legacy input handler, build list, data assets, TextMeshPro resources |

## Helpers

`Check.True/False/Equal/Near/Sequence` are the small assertion helpers the tests share. They throw a
readable message, so the Test Runner shows the exact expectation that failed.

## Known caveat: the editor inspector

Unity's editor "inspector" caches the last invocation of a method (arguments and results) and can hand
that cached state back when a method is run again in the same editor session. The logic tests are written
so this cannot hide a failure and is visible as follows:

* each test builds its own objects and asserts relative changes or invariants (`pool.TotalCount`,
  `available + active == total`) rather than absolute object counts;
* the pool has no helper methods and no lambdas inside the checks, so the cached graph cannot leak into it;
* the scene and prefab access lives in `SceneWiringTests`, which reads components directly from the saved
  prefab with `GetComponent` (never through a generated scene object).

The scene checks run against the scene as currently configured. If a prefab ever loses a component
(for example after a script was renamed), run **Flappy Copy > Validate Project**; it lists the exact
components that are missing or not wired.
