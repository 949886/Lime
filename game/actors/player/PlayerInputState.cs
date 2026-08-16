using Godot;

namespace Lime.Game.Actors.Player;

public readonly record struct PlayerInputState(
    Vector2 Move,
    bool SprintHeld,
    bool InteractPressed
);
