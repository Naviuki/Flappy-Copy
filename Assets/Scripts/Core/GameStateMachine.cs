namespace FlappyCopy.Core
{
    /// <summary>What a single input press means while the given menu state is showing.</summary>
    public enum MenuIntent
    {
        Flap = 0,
        StartGame = 1,
        RestartGame = 2,
        Ignore = 3
    }

    /// <summary>Top level game states. Transitions are defined by <see cref="GameStateRules"/>.</summary>
    public enum GameState
    {
        Menu = 0,
        Playing = 1,
        GameOver = 2
    }

    /// <summary>State pattern: one object per state, with guards/callbacks expressed as observers.</summary>
    public interface IGameState
    {
        GameState Id { get; }
        MenuIntent GetPressIntent();
        void Enter();
        void Exit();
    }

    public interface IGameContext
    {
        bool TryChangeState(GameState next);
        void OnMenuEntered();
        void OnPlayStarted();
        void OnGameOverEntered();
    }

    /// <summary>Pure, engine-free rules for the state machine so the allowed flow is unit testable.</summary>
    public static class GameStateRules
    {
        public static bool CanTransition(GameState from, GameState to)
        {
            return from switch
            {
                GameState.Menu => to == GameState.Playing,
                GameState.Playing => to == GameState.GameOver || to == GameState.Menu,
                GameState.GameOver => to == GameState.Playing || to == GameState.Menu,
                _ => false
            };
        }

        public static MenuIntent GetIntent(GameState state)
        {
            return state switch
            {
                GameState.Menu => MenuIntent.StartGame,
                GameState.GameOver => MenuIntent.RestartGame,
                _ => MenuIntent.Flap
            };
        }
    }

    public sealed class MenuState : IGameState
    {
        private readonly IGameContext context;

        public MenuState(IGameContext context)
        {
            this.context = context;
        }

        public GameState Id => GameState.Menu;
        public MenuIntent GetPressIntent() => MenuIntent.StartGame;

        public void Enter() => context.OnMenuEntered();
        public void Exit() { }
    }

    public sealed class PlayingState : IGameState
    {
        private readonly IGameContext context;

        public PlayingState(IGameContext context)
        {
            this.context = context;
        }

        public GameState Id => GameState.Playing;
        public MenuIntent GetPressIntent() => MenuIntent.Flap;

        public void Enter() => context.OnPlayStarted();
        public void Exit() { }
    }

    public sealed class GameOverState : IGameState
    {
        private readonly IGameContext context;

        public GameOverState(IGameContext context)
        {
            this.context = context;
        }

        public GameState Id => GameState.GameOver;
        public MenuIntent GetPressIntent() => MenuIntent.RestartGame;

        public void Enter() => context.OnGameOverEntered();
        public void Exit() { }
    }

    /// <summary>
    /// Holds the current <see cref="IGameState"/> and enforces <see cref="GameStateRules"/>.
    /// Every transition fires <see cref="StateChanged"/> so views can react without polling.
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly IGameState[] states;

        public GameStateMachine(IGameContext context)
        {
            states = new IGameState[]
            {
                new MenuState(context),
                new PlayingState(context),
                new GameOverState(context)
            };
            Current = states[0];
        }

        public event System.Action<GameState, GameState> StateChanged;

        public IGameState Current { get; private set; }

        public MenuIntent CurrentPressIntent => Current.GetPressIntent();

        public bool TryChange(GameState next)
        {
            if (!GameStateRules.CanTransition(Current.Id, next))
            {
                return false;
            }

            GameState from = Current.Id;
            Current.Exit();
            Current = GetState(next);
            Current.Enter();
            StateChanged?.Invoke(from, next);
            return true;
        }

        /// <summary>Enters the first state without a transition (boot), still notifying views.</summary>
        public void EnterInitial()
        {
            Current.Enter();
        }

        private IGameState GetState(GameState id)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].Id == id)
                {
                    return states[i];
                }
            }

            return states[0];
        }
    }
}
