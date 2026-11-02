using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FlappyCopy.Core;
using FlappyCopy.Data;
using FlappyCopy.EditorTools;
using FlappyCopy.Pipes;
using FlappyCopy.Player;
using FlappyCopy.Pooling;
using FlappyCopy.Score;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlappyCopy.Tests
{
    /// <summary>
    /// EditMode tests of the game logic without a scene: game states, scoring and the record, the object pool,
    /// the pipe run/recycling, the pass-through rule, difficulty and gap placement, bird kinematics, camera
    /// framing, scroll wrap and the HUD rules.
    /// </summary>
    public static class LogicTests
    {
        // ------------------------------------------------------------------ state machine

        public sealed class Context : IGameContext
        {
            public readonly List<string> Calls = new List<string>();

            public bool TryChangeState(GameState next) => true;
            public void OnMenuEntered() => Calls.Add("menu");
            public void OnPlayStarted() => Calls.Add("playing");
            public void OnGameOverEntered() => Calls.Add("gameover");
        }

        [Test]
        public static void StartsInMenu()
        {
            var machine = new GameStateMachine(new Context());
            Check.Equal(GameState.Menu, machine.Current.Id, "boot state");
            Check.Equal(MenuIntent.StartGame, machine.CurrentPressIntent, "menu press starts the game");
        }

        [Test]
        public static void OnlyMenuToPlayingStartsARun()
        {
            Check.True(GameStateRules.CanTransition(GameState.Menu, GameState.Playing), "menu -> playing");
            Check.False(GameStateRules.CanTransition(GameState.Menu, GameState.GameOver), "menu -> game over");
            Check.False(GameStateRules.CanTransition(GameState.Menu, GameState.Menu), "menu -> menu");
        }

        [Test]
        public static void PlayingCanCrashOrReturnToMenu()
        {
            Check.True(GameStateRules.CanTransition(GameState.Playing, GameState.GameOver), "playing -> game over");
            Check.True(GameStateRules.CanTransition(GameState.Playing, GameState.Menu), "playing -> menu");
            Check.False(GameStateRules.CanTransition(GameState.Playing, GameState.Playing), "playing -> playing");
        }

        [Test]
        public static void GameOverCanRestart()
        {
            Check.True(GameStateRules.CanTransition(GameState.GameOver, GameState.Playing), "game over -> playing");
            Check.True(GameStateRules.CanTransition(GameState.GameOver, GameState.Menu), "game over -> menu");
            Check.False(GameStateRules.CanTransition(GameState.GameOver, GameState.GameOver), "game over -> game over");
        }

        [Test]
        public static void TransitionsRunTheStateCallbacksInOrder()
        {
            var context = new Context();
            var machine = new GameStateMachine(context);

            Check.True(machine.TryChange(GameState.Playing), "start playing");
            Check.True(machine.TryChange(GameState.GameOver), "crash");
            Check.True(machine.TryChange(GameState.Menu), "back to menu");
            Check.Sequence(new[] { "playing", "gameover", "menu" }, context.Calls, "callback order");
        }

        [Test]
        public static void IllegalTransitionIsRejected()
        {
            var machine = new GameStateMachine(new Context());
            bool raised = false;
            machine.StateChanged += (from, to) => raised = true;

            Check.False(machine.TryChange(GameState.GameOver), "menu -> game over rejected");
            Check.Equal(GameState.Menu, machine.Current.Id, "state unchanged");
            Check.False(raised, "no event for a rejected transition");
        }

        [Test]
        public static void StateChangedReportsBothSides()
        {
            var machine = new GameStateMachine(new Context());
            GameState from = GameState.Menu;
            GameState to = GameState.Menu;
            machine.StateChanged += (oldState, newState) =>
            {
                from = oldState;
                to = newState;
            };

            machine.TryChange(GameState.Playing);

            Check.Equal(GameState.Menu, from, "from");
            Check.Equal(GameState.Playing, to, "to");
        }

        [Test]
        public static void FullRunFlow()
        {
            var machine = new GameStateMachine(new Context());

            machine.TryChange(GameState.Playing);
            Check.Equal(MenuIntent.Flap, machine.CurrentPressIntent, "flap while playing");
            machine.TryChange(GameState.GameOver);
            Check.Equal(MenuIntent.RestartGame, machine.CurrentPressIntent, "restart after crash");
            machine.TryChange(GameState.Playing);

            Check.Equal(GameState.Playing, machine.Current.Id, "run restarted");
        }

        // ------------------------------------------------------------------ scoring

        [Test]
        public static void ScoreStartsAtZeroWithTheStoredRecord()
        {
            var score = new ScoreService(new MemoryScoreStorage(3));
            Check.Equal(0, score.Score, "new run starts at 0");
            Check.Equal(3, score.BestScore, "best score loaded");
            Check.False(score.IsNewRecord, "no record yet");
        }

        [Test]
        public static void PointsRaiseScoreEvents()
        {
            var score = new ScoreService(new MemoryScoreStorage());
            int calls = 0;
            int last = -1;
            score.ScoreChanged += value =>
            {
                last = value;
                calls++;
            };

            score.AddPoint();
            score.AddPoint();

            Check.Equal(2, score.Score, "two points");
            Check.Equal(2, last, "last event value");
            Check.Equal(2, calls, "event count");
        }

        [Test]
        public static void NewRunClearsTheScoreButNotTheRecord()
        {
            var score = new ScoreService(new MemoryScoreStorage(3));
            score.AddPoint();
            score.AddPoint();
            score.ResetRun();
            Check.Equal(0, score.Score, "run score reset");
            Check.Equal(3, score.BestScore, "record kept");
        }

        [Test]
        public static void RecordIsSavedOnlyWhenBeaten()
        {
            var storage = new MemoryScoreStorage(3);
            var score = new ScoreService(storage);
            for (int i = 0; i < 5; i++)
            {
                score.AddPoint();
            }

            bool reported = false;
            score.RunEnded += (value, isRecord) => reported = isRecord;
            score.EndRun();

            Check.True(reported, "new record flag on the run event");
            Check.True(score.IsNewRecord, "flag raised");
            Check.True(storage.SaveCount >= 1, "saved");
            Check.Equal(5, storage.Value, "stored record");
        }

        [Test]
        public static void EndingTwiceSavesOnce()
        {
            var storage = new MemoryScoreStorage(0);
            var score = new ScoreService(storage);
            score.AddPoint();
            score.EndRun();
            int saves = storage.SaveCount;
            score.EndRun();
            Check.Equal(1, saves, "first EndRun saved");
            Check.Equal(1, storage.SaveCount, "second EndRun is a no-op");
        }

        [Test]
        public static void EqualScoreIsNotARecord()
        {
            var storage = new MemoryScoreStorage(3);
            var score = new ScoreService(storage);
            for (int i = 0; i < 3; i++)
            {
                score.AddPoint();
            }

            score.EndRun();
            Check.False(score.IsNewRecord, "equal is not a record");
            Check.Equal(0, storage.SaveCount, "nothing saved");
        }

        // ------------------------------------------------------------------ object pool

        public sealed class Item : IPoolable
        {
            public int Spawns;
            public int Despawns;
            public bool Active = true;

            public void OnSpawnedFromPool() => Spawns++;
            public void OnDespawnedToPool() => Despawns++;
        }

        public sealed class Factory : IPoolFactory<Item>
        {
            public int Created { get; private set; }

            public Item Create()
            {
                Created++;
                return new Item();
            }
        }

        private static ObjectPool<Item> NewPool(int prewarm, out Factory factory)
        {
            factory = new Factory();
            return new ObjectPool<Item>(factory, prewarm, (item, active) => item.Active = active);
        }

        [Test]
        public static void PoolPrewarmsInstances()
        {
            var pool = NewPool(10, out Factory factory);
            Check.True(pool.TotalCount >= 10, "pool holds the prewarm count");
            Check.True(pool.AvailableCount >= 10, "prewarmed instances are available");
            Check.True(pool.ActiveCount == 0, "nothing active after prewarm");
            Check.True(factory.Created >= 10, "factory created them");
        }

        [Test]
        public static void PoolGetAndReleaseStayBalanced()
        {
            var pool = NewPool(10, out _);
            int availableBefore = pool.AvailableCount;
            int totalBefore = pool.TotalCount;

            Item first = pool.Get();
            Item second = pool.Get();
            Check.True(first != null && second != null, "instances handed out");
            Check.True(pool.AvailableCount <= availableBefore - 2, "two instances taken");
            Check.True(pool.TotalCount >= totalBefore, "pool only grows");

            pool.Release(first);
            pool.Release(second);
            Check.True(pool.ActiveCount <= 2, "release returns instances");
        }

        [Test]
        public static void PoolRunsTheCallbacks()
        {
            var pool = NewPool(10, out _);
            Item item = pool.Get();
            int spawnsAfterTake = item.Spawns;
            int despawnsBefore = item.Despawns;

            pool.Release(item);

            Check.True(spawnsAfterTake >= 1, "spawn callback");
            Check.True(item.Despawns >= despawnsBefore + 1, "despawn callback");
        }

        [Test]
        public static void PoolReleaseAllReturnsEverything()
        {
            var pool = NewPool(10, out _);
            for (int i = 0; i < 30; i++)
            {
                pool.Get();
            }

            pool.ReleaseAll();
            Check.True(pool.ActiveCount == 0, "all returned");
            Check.True(pool.AvailableCount >= pool.TotalCount, "released instances are back in the pool");
        }

        [Test]
        public static void PoolIgnoresForeignAndDoubleReleases()
        {
            var pool = NewPool(10, out _);
            Item item = pool.Get();
            pool.Release(item);
            int total = pool.TotalCount;

            pool.Release(item);
            pool.Release(new Item());

            Check.True(pool.TotalCount == total, "total unchanged");
            Check.True(pool.ActiveCount == 0, "nothing active");
        }

        [Test]
        public static void PoolRejectsANullFactory()
        {
            bool threw = false;
            try
            {
                new ObjectPool<Item>(null, 1);
            }
            catch (System.ArgumentNullException)
            {
                threw = true;
            }

            Check.True(threw, "null factory rejected");
        }

        // ------------------------------------------------------------------ pipes

        public sealed class Pair : IPipePair
        {
            private bool inside;
            private bool awarded;

            public float X { get; private set; }
            public bool IsDisposed { get; set; }
            public int ActivationCount { get; private set; }
            public float LastGap { get; private set; }
            public float LastCenter { get; private set; }
            public float GapTop => LastCenter + (LastGap * 0.5f);
            public float GapBottom => LastCenter - (LastGap * 0.5f);

            public bool Activate(float x, float gap, float centerY, System.Random random)
            {
                if (IsDisposed)
                {
                    return false;
                }

                X = x;
                LastGap = gap;
                LastCenter = centerY;
                ActivationCount++;
                return true;
            }

            public void MoveTo(float x) => X = x;

            public bool Sample(float birdX, float birdY, float birdHalfWidth)
            {
                return PipePassRule.Sample(ref inside, ref awarded, birdX, birdY, birdHalfWidth, GapTop, GapBottom);
            }
        }

        public sealed class PairFactory : IPipePairFactory
        {
            public int Created { get; private set; }
            public List<Pair> Pairs { get; } = new List<Pair>();

            public IPipePair Spawn(float x, float gap, float centerY, System.Random random)
            {
                var pair = new Pair();
                pair.MoveTo(x);
                Created++;
                Pairs.Add(pair);
                return pair;
            }
        }

        private static PipeRun NewRun(out PairFactory factory)
        {
            factory = new PairFactory();
            return new PipeRun(factory, new PipeRunConfig(5.2f, 1.5f, 4.5f, 1.4f, 4, 4f), seed: 7);
        }

        [Test]
        public static void PipeRunCreatesItsPoolOnce()
        {
            var run = NewRun(out PairFactory factory);
            Check.Equal(4, run.Count, "slot count");
            Check.Equal(4, factory.Created, "instances created up front");
        }

        [Test]
        public static void PipeRunStartsSpacedToTheRight()
        {
            var run = NewRun(out _);
            const float spawnX = 4.5f + 1.4f;
            for (int slot = 0; slot < run.Count; slot++)
            {
                Check.Near(spawnX - (slot * 5.2f), run.XOf(slot), 0.0001f, "slot " + slot);
            }
        }

        [Test]
        public static void PipeRunScrollsLeft()
        {
            var run = NewRun(out PairFactory factory);
            run.Tick(0.5f, 4f, () => 4f, offset => 0f);
            Check.Near(2f, run.Scrolled, 0.0001f, "scroll distance");
            Check.Near(5.9f - 2f, factory.Pairs[0].X, 0.0001f, "pair moved left");
            Check.Equal(4, factory.Created, "no new instances while running");
        }

        [Test]
        public static void PipeRunRecyclesInsteadOfSpawning()
        {
            var run = NewRun(out PairFactory factory);
            for (int frame = 0; frame < 200; frame++)
            {
                run.Tick(1f / 60f, 6f, () => 4f, offset => 0f);
            }

            Check.True(factory.Pairs[0].ActivationCount > 1, "first pair recycled");
            Check.Equal(4, factory.Created, "pool reused");
        }

        [Test]
        public static void RecycledPairGetsTheCurrentGap()
        {
            var run = NewRun(out PairFactory factory);
            const float center = 2.25f;
            for (int frame = 0; frame < 300 && factory.Pairs[0].ActivationCount <= 1; frame++)
            {
                run.Tick(1f / 60f, 6f, () => 4.2f, offset => center);
            }

            Check.True(factory.Pairs[0].ActivationCount >= 2, "pair recycled");
            Check.Near(4.2f, factory.Pairs[0].LastGap, 0.0001f, "new gap");
            Check.Near(center, factory.Pairs[0].LastCenter, 0.0001f, "new centre");
        }

        [Test]
        public static void PipeRunResetClearsTheScroll()
        {
            var run = NewRun(out _);
            run.Tick(1f, 3f, () => 4f, offset => 0f);
            run.Reset();
            Check.Near(0f, run.Scrolled, 0.0001f, "scroll reset");
        }

        [Test]
        public static void PipeSpeedUsesTheRampAndAFloor()
        {
            Check.Near(3.7f, PipeRunMath.Speed(3.2f, 0.5f), 0.0001f, "ramp applied");
            Check.Near(0.1f, PipeRunMath.Speed(0.05f, -10f), 0.0001f, "speed floor");
        }

        [Test]
        public static void SlotCountCoversTheScreen()
        {
            int slots = PipeRunMath.SlotsFor(4.5f, 5.2f, 1.4f);
            Check.True(slots * 5.2f >= 4.5f * 2f, "slots cover the screen");
        }

        [Test]
        public static void PassingScoresOnceFromInsideTheGap()
        {
            var pair = new Pair();
            pair.Activate(0f, 4f, 0f, new System.Random(1));
            Check.False(pair.Sample(0f, 0f, 0.25f), "inside, not yet");
            Check.True(pair.Sample(1f, 0f, 0.25f), "cleared -> point");
            Check.False(pair.Sample(2f, 0f, 0.25f), "only once");
        }

        [Test]
        public static void PassingOutsideTheGapNeverScores()
        {
            var pair = new Pair();
            pair.Activate(0f, 4f, 0f, new System.Random(1));
            Check.False(pair.Sample(0f, 3f, 0.25f), "outside, not yet");
            Check.False(pair.Sample(1f, 3f, 0.25f), "outside, passed");
        }

        // ------------------------------------------------------------------ difficulty, bird, camera, HUD

        [Test]
        public static void DifficultyRampGrowsAndCaps()
        {
            Check.Near(0f, DifficultyRules.VelocityIncrease(0), 0.0001f, "score 0");
            Check.True(DifficultyRules.VelocityIncrease(10) > DifficultyRules.VelocityIncrease(3), "ramp grows");
            Check.Near(DifficultyRules.VelocityIncrease(1000), DifficultyRules.VelocityIncrease(2000), 0.0001f, "ramp caps");
            Check.Near(1f, DifficultyRules.GapMultiplier(0), 0.0001f, "gap at score 0");
            Check.True(DifficultyRules.GapMultiplier(5) < 1f, "gap shrinks");
            Check.True(DifficultyRules.GapMultiplier(1000) >= DifficultyRules.MinimumGapMultiplier, "gap floor");
        }

        [Test]
        public static void GapCentresStayOnScreenAndMoveGradually()
        {
            var random = new System.Random(4);
            const float playHalfHeight = 8f;
            const float gap = 4.6f;
            float previous = 0f;
            for (int i = 0; i < 100; i++)
            {
                float center = GapPlacement.NextCentre(previous, gap, -2.4f, 2.7f, playHalfHeight, 0.45f, random);
                Check.True(center + (gap * 0.5f) <= playHalfHeight + 0.0001f, "top pipe on screen");
                Check.True(center - (gap * 0.5f) >= -playHalfHeight - 0.0001f, "bottom pipe on screen");
                Check.True(System.Math.Abs(center - previous) <= (playHalfHeight * 0.45f) + 0.0001f, "height step");
                previous = center;
            }
        }

        [Test]
        public static void BirdFlapsUpAndStopsAfterCrash()
        {
            var bird = new BirdKinematics(2.4f * 9.81f, 6f, 14f, 30f, -70f);
            bird.PlaceAt(1f);
            bird.Flap();
            Check.True(bird.VelocityY > 0f, "flap goes up");
            bird.Crash();
            float before = bird.VelocityY;
            bird.Flap();
            Check.Near(before, bird.VelocityY, 0.0001f, "no flap after crash");
        }

        [Test]
        public static void BirdFallsWithACappedSpeed()
        {
            var bird = new BirdKinematics(2.4f * 9.81f, 6f, 14f, 30f, -70f);
            bird.PlaceAt(0f);
            for (int i = 0; i < 120; i++)
            {
                bird.Step(1f / 60f);
            }

            Check.True(bird.Y < 0f, "falls");
            Check.True(bird.VelocityY >= -14f - 0.0001f, "fall speed capped");
        }

        [Test]
        public static void CameraKeepsTheDesignWidth()
        {
            float portrait = Cameras.PortraitFraming.ComputeOrthographicSize(1080, 1920);
            float tall = Cameras.PortraitFraming.ComputeOrthographicSize(1080, 2400);
            float wide = Cameras.PortraitFraming.ComputeOrthographicSize(1920, 1080);
            Check.Near(DesignSpace.HalfHeight, portrait, 0.001f, "9:16 framing");
            Check.True(tall > portrait, "tall screens widen the view");
            Check.Near(DesignSpace.HalfHeight, wide, 0.001f, "landscape keeps 9:16");
        }

        [Test]
        public static void ScrollWrapStaysInsideOneStrip()
        {
            Check.Near(0f, World.ScrollMath.Wrap(10f, 10f), 0.0001f, "exact span");
            Check.Near(2.5f, World.ScrollMath.Wrap(12.5f, 10f), 0.0001f, "remainder");
            Check.Near(7.5f, World.ScrollMath.Wrap(-2.5f, 10f), 0.0001f, "negative");

            // Two strips of half the frame width tile the design width exactly, whatever the offset. They start
            // at x = 0 and +width/2 and only ever slide left, so the frame stays covered from the left edge on.
            float frame = DesignSpace.PortraitWidth;
            float strip = World.ScrollMath.StripWidthFor(frame);
            for (float offset = 0f; offset < frame; offset += 0.7f)
            {
                float first = World.ScrollMath.StripX(0, strip, offset);
                Check.Near(0f, first + World.ScrollMath.Wrap(offset, strip), 0.0001f, "strip 0 starts at the edge");
                Check.Near(World.ScrollMath.StripX(1, strip, offset), first + strip, 0.0001f, "strips sit side by side");
                Check.True(first <= 0.0001f && first > -strip, "the first strip never leaves a gap on the left");
            }
        }

        [Test]
        public static void GapCentreStaysInsideThePlayArea()
        {
            float lowest = -DesignSpace.PlayHalfHeight + (4.6f * 0.5f);
            float highest = DesignSpace.PlayHalfHeight - (4.6f * 0.5f);

            var random = new System.Random(7);
            float previous = lowest;
            for (int i = 0; i < 200; i++)
            {
                float centre = GapPlacement.NextCentre(previous, 4.6f, lowest, highest,
                    DesignSpace.PlayHalfHeight, 0.45f, random);
                Check.True(centre >= lowest - 0.0001f && centre <= highest + 0.0001f, "gap centre in range");
                previous = centre;
            }

            // A gap centre that is asked for above the range is pulled down towards it (but may be limited by
            // the step), and a centre that would leave the frame is always clamped into it.
            float overshoot = highest + 10f;
            float pulled = GapPlacement.NextCentre(overshoot, 4.6f, lowest, highest,
                DesignSpace.PlayHalfHeight, 0.45f, random);
            Check.True(pulled < overshoot, "an out-of-range request moves towards the range");
        }

        [Test]
        public static void InputAndHudRules()
        {
            Check.Equal(Input.PressKind.Restart, Input.PressPolicy.Resolve(true, false, MenuIntent.StartGame), "menu press");
            Check.Equal(Input.PressKind.Flap, Input.PressPolicy.Resolve(true, false, MenuIntent.Flap), "flap");
            Check.Equal(Input.PressKind.None, Input.PressPolicy.Resolve(false, false, MenuIntent.Flap), "no press");
            Check.Equal(UI.HudPanel.Menu, UI.HudFlow.PanelFor(GameState.Menu), "menu panel");
            Check.Equal(UI.HudPanel.GameOver, UI.HudFlow.PanelFor(GameState.GameOver), "result panel");
        }
    }
}
