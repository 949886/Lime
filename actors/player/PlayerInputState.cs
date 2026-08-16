using Godot;

namespace Lime.Actors.Player;

public readonly record struct PlayerInputState(
    Vector2 Move,
    bool SprintHeld,
    bool InteractPressed
);
