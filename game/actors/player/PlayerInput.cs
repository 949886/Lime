using Godot;
using Lime.Game;

namespace Lime.Game.Actors.Player;

public partial class PlayerInput : Node
{
    private Vector2 _virtualMove;

    public bool Enabled { get; set; }

    public PlayerInputState Capture()
    {
        if (!Enabled)
        {
            return default;
        }

        var mappedMove = Input.GetVector(
            InputActions.MoveLeft,
            InputActions.MoveRight,
            InputActions.MoveForward,
            InputActions.MoveBackward);

        var move = _virtualMove.LengthSquared() > mappedMove.LengthSquared()
            ? _virtualMove
            : mappedMove;

        return new PlayerInputState(
            move.LimitLength(),
            Input.IsActionPressed(InputActions.Sprint),
            Input.IsActionJustPressed(InputActions.Interact));
    }

    public void SetVirtualMove(Vector2 value)
    {
        _virtualMove = value.LimitLength();
    }

    public void ClearVirtualMove()
    {
        _virtualMove = Vector2.Zero;
    }
}
