using System;
using Godot;

namespace Lime.App;

public enum GameMode
{
    Hub,
    Transition,
    Explore,
    Cutscene,
}

public enum UiMode
{
    None,
    Dialogue,
    Modal,
}

public partial class FlowController : Node
{
    public bool IsStarted { get; private set; }
    public GameMode CurrentGameMode { get; private set; } = GameMode.Hub;
    public UiMode CurrentUiMode { get; private set; } = UiMode.None;

    public bool GameplayInputEnabled =>
        IsStarted && CurrentGameMode == GameMode.Explore && CurrentUiMode == UiMode.None;

    public void Start()
    {
        if (IsStarted)
        {
            return;
        }

        IsStarted = true;
        CurrentGameMode = GameMode.Explore;
        CurrentUiMode = UiMode.None;
    }

    public void SetGameMode(GameMode mode)
    {
        EnsureStarted();
        CurrentGameMode = mode;
    }

    public void SetUiMode(UiMode mode)
    {
        EnsureStarted();
        CurrentUiMode = mode;
    }

    private void EnsureStarted()
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("FlowController must be started before changing flow state.");
        }
    }
}
