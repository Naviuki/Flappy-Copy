using System;
using FlappyCopy.Audio;
using FlappyCopy.Cameras;
using FlappyCopy.Data;
using FlappyCopy.Input;
using FlappyCopy.Pipes;
using FlappyCopy.Player;
using FlappyCopy.Score;
using FlappyCopy.World;
using UnityEngine;

namespace FlappyCopy.Core
{
    /// <summary>Difficulty read model shared by the pipe spawner and the gap gate.</summary>
    public interface IDifficultyProvider
    {
        float GetGap(int score);
        float GetSpeedIncrease(int score);
    }

    /// <summary>What the UI is allowed to observe. Views never reach into gameplay code.</summary>
    public interface IGameEvents
    {
        event Action<GameState, GameState> StateChanged;
        event Action<int> PointScored;
        event Action<int, bool> RunEnded;
        IScoreService Score { get; }
    }

    /// <summary>
    /// Composition root: builds the services, the state machine and the input, then owns the per-frame
    /// loop. Bird, pipes and UI only react to events; there is no view code in here.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameController : MonoBehaviour, IGameContext, IDifficultyProvider, IGameEvents
    {
        [Header("Data")]
        [SerializeField] private PlayerSettings playerSettings;
        [SerializeField] private PipeSettings pipeSettings;
        [SerializeField] private WorldSettings worldSettings;

        [Header("Camera")]
        [SerializeField] private PortraitFraming cameraFraming;

        [Header("World")]
        [SerializeField] private BirdController bird;
        [SerializeField] private PipeSpawner pipeSpawner;
        [SerializeField] private InfinityGround ground;

        [Header("Audio")]
        [SerializeField] private AudioService audioService;

        [Header("Debug")]
        [SerializeField] private DebugSettings debugSettings;

        private GameStateMachine stateMachine;
        private ScoreService scoreService;
        private UnityInputSource inputSource;
        private PlayerInputReader inputReader;
        private bool isReady;

        // Test seams: a scripted input source and a fixed time step can replace the real ones in checks.
        private PlayerInputReader inputReaderForTests;
        private float? deltaTimeForTests;

        public event Action<GameState, GameState> StateChanged;
        public event Action<int> PointScored;
        public event Action<int, bool> RunEnded;

        public IScoreService Score => scoreService;
        public GameState State => stateMachine?.Current.Id ?? GameState.Menu;
        public bool IsPlaying => State == GameState.Playing;
        public PlayerSettings Player => playerSettings;
        public PipeSettings Pipes => pipeSettings;
        public WorldSettings World => worldSettings;
        public BirdController Bird => bird;

        private void Awake()
        {
            if (playerSettings == null || pipeSettings == null || worldSettings == null)
            {
                Debug.LogError("GameController is missing its ScriptableObject settings.", this);
                enabled = false;
                return;
            }

            LayersBootstrapper.EnsureAll();

            scoreService = new ScoreService(new PlayerPrefsScoreStorage(GameConstants.BestScorePrefKey));
            stateMachine = new GameStateMachine(this);
            inputSource = new UnityInputSource();
            inputReader = new PlayerInputReader(inputSource);

            stateMachine.StateChanged += RaiseStateChanged;
            scoreService.ScoreChanged += RaisePointScored;
            scoreService.RunEnded += RaiseRunEnded;
            isReady = true;
        }

        private void Start()
        {
            if (!isReady)
            {
                return;
            }

            cameraFraming.Apply();
            bird.ResetToStart();
            stateMachine.EnterInitial();

            if (debugSettings != null && debugSettings.LogBoot)
            {
                Debug.LogFormat("[FlappyCopy] boot: state={0}, gravity={1}g, pipesSpeed={2}, maxGravity={3}",
                    State, playerSettings.GravityScale, worldSettings.PipeSpeed, Physics2D.gravity.y);
            }
        }

        private void Update()
        {
            StepFrame();
        }

        /// <summary>Test hook: one frame of the game loop (same code the player runs).</summary>
        public void StepFrameForTests()
        {
            StepFrame();
        }

        private void StepFrame()
        {
            if (!isReady)
            {
                return;
            }

            PlayerInputReader reader = inputReaderForTests ?? inputReader;
            PressKind press = reader.Poll(stateMachine.CurrentPressIntent);
            float deltaTime = deltaTimeForTests ?? Time.deltaTime;

            if (press == PressKind.Restart)
            {
                StartRun();
                return;
            }

            switch (stateMachine.Current.Id)
            {
                case GameState.Menu:
                    bird.TickIdle(deltaTime);
                    break;
                case GameState.Playing:
                    TickPlaying(deltaTime, press == PressKind.Flap);
                    break;
                case GameState.GameOver:
                    bird.TickFallen(deltaTime);
                    scoreService.EndRun();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (isReady)
            {
                stateMachine.StateChanged -= RaiseStateChanged;
                scoreService.ScoreChanged -= RaisePointScored;
                scoreService.RunEnded -= RaiseRunEnded;
            }

            inputSource?.Dispose();
        }

        /// <summary>Requested by the full-screen UI button; the request is treated like a space press.</summary>
        public void RequestPress()
        {
            inputSource?.RequestFlap();
        }

        /// <summary>Raised by the bird's trigger hitboxes (pipes, ground, ceiling).</summary>
        public void NotifyBirdCrashed()
        {
            if (!isReady || stateMachine.Current.Id != GameState.Playing)
            {
                return;
            }

            Play(SfxId.Hit);
            inputSource?.Dispose();
            stateMachine.TryChange(GameState.GameOver);
        }

        public float GetGap(int score)
        {
            return pipeSettings.GetGap(playerSettings.GetGapMultiplier(score));
        }

        public float GetSpeedIncrease(int score)
        {
            return playerSettings.GetVelocityIncrease(score);
        }

        // ----------------------------------------------------------------- IGameContext

        bool IGameContext.TryChangeState(GameState next) => stateMachine.TryChange(next);
        void IGameContext.OnMenuEntered() { }

        void IGameContext.OnPlayStarted()
        {
            bird.Flap();
            Play(SfxId.Flap);
        }

        void IGameContext.OnGameOverEntered() => bird.OnDied();

        /// <summary>Mute switch of the HUD: sound effects only, remembered between runs.</summary>
        public bool IsMuted => audioService != null && audioService.IsMuted;

        public void ToggleMute()
        {
            if (audioService != null)
            {
                audioService.ToggleMute();
            }
        }

        // ----------------------------------------------------------------- run flow

        private void StartRun()
        {
            scoreService.ResetRun();
            bird.ResetToStart();
            pipeSpawner.ResetRun();
            ground.ResetRun();
            stateMachine.TryChange(GameState.Playing);
            bird.Flap();
        }

        private void TickPlaying(float deltaTime, bool flap)
        {
            if (flap)
            {
                Play(SfxId.Flap);
            }

            bird.Tick(deltaTime, flap);

            float speed = PipeRunMath.Speed(worldSettings.PipeSpeed, GetSpeedIncrease(scoreService.Score));
            pipeSpawner.Tick(deltaTime, speed, scoreService.Score);
            ground.Tick(deltaTime, worldSettings.GroundScrollSpeed);
        }

        private void RaiseStateChanged(GameState from, GameState to)
        {
            StateChanged?.Invoke(from, to);
        }

        private void RaisePointScored(int value)
        {
            Play(SfxId.Point);
            PointScored?.Invoke(value);
        }

        private void Play(SfxId id)
        {
            if (audioService != null)
            {
                audioService.Play(id);
            }
        }

        private void RaiseRunEnded(int value, bool isNewRecord)
        {
            RunEnded?.Invoke(value, isNewRecord);
        }
    }
}
