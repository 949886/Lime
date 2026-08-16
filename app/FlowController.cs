using System;
using Godot;

namespace Lime.App;

public enum GameState
{
    Hub,
    Transition,
    Explore,
    Cutscene,
}

public enum UiState
{
    None,
    Dialogue,
    Modal,
}

public partial class FlowController : Node
{
    public event Action? StateChanged;

    public bool IsStarted { get; private set; }
    public GameState CurrentGameState { get; private set; } = GameState.Hub;
    public UiState CurrentUiState { get; private set; } = UiState.None;

    public bool GameplayInputEnabled =>
        IsStarted && CurrentGameState == GameState.Explore && CurrentUiState == UiState.None;

    public void Start()
    {
        if (IsStarted)
        {
            return;
        }

        IsStarted = true;
        CurrentGameState = GameState.Explore;
        CurrentUiState = UiState.None;
        StateChanged?.Invoke();
    }

    public void SetGameState(GameState state)
    {
        EnsureStarted();

        if (CurrentGameState == state)
        {
            return;
        }

        CurrentGameState = state;
        StateChanged?.Invoke();
    }

    public void SetUiState(UiState state)
    {
        EnsureStarted();

        if (CurrentUiState == state)
        {
            return;
        }

        CurrentUiState = state;
        StateChanged?.Invoke();
    }

    private void EnsureStarted()
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("FlowController must be started before changing flow state.");
        }
    }
}
