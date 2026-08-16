using Godot;

namespace Lime.Actors.Player;

[GlobalClass]
public partial class PlayerMovementProfile : Resource
{
    [Export] public float WalkSpeed { get; set; } = 3.0f;
    [Export] public float RunSpeed { get; set; } = 5.0f;
    [Export] public float Acceleration { get; set; } = 18.0f;
    [Export] public float Deceleration { get; set; } = 22.0f;
    [Export] public float GravityScale { get; set; } = 1.0f;
    [Export] public float FloorSnapLength { get; set; } = 0.25f;
    [Export] public float FloorMaxAngleDegrees { get; set; } = 50.0f;

    public float FloorMaxAngleRadians => Mathf.DegToRad(FloorMaxAngleDegrees);
}
