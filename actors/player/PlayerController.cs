using System;
using Godot;

namespace Lime.Actors.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public PlayerMovementProfile MovementProfile { get; set; } = null!;

    private PlayerInput _playerInput = null!;
    private Camera3D? _movementReference;
    private Vector3 _gravity;

    public override void _Ready()
    {
        if (MovementProfile is null)
        {
            throw new InvalidOperationException(
                "PlayerController requires a PlayerMovementProfile.");
        }

        _playerInput = GetNode<PlayerInput>("%PlayerInput");

        MotionMode = MotionModeEnum.Grounded;
        UpDirection = Vector3.Up;
        FloorConstantSpeed = true;
        FloorStopOnSlope = true;
        FloorSnapLength = MovementProfile.FloorSnapLength;
        FloorMaxAngle = MovementProfile.FloorMaxAngleRadians;

        var gravityMagnitude = (float)ProjectSettings
            .GetSetting("physics/3d/default_gravity")
            .AsDouble();
        var gravityDirection = ProjectSettings
            .GetSetting("physics/3d/default_gravity_vector")
            .AsVector3();

        _gravity = gravityDirection.Normalized() * gravityMagnitude;
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = _playerInput.Capture();
        var direction = ResolveMovementDirection(input.Move);
        var targetSpeed = input.SprintHeld
            ? MovementProfile.RunSpeed
            : MovementProfile.WalkSpeed;
        var targetVelocity = direction * targetSpeed;

        var velocity = Velocity;
        var horizontalStep = (direction.IsZeroApprox()
            ? MovementProfile.Deceleration
            : MovementProfile.Acceleration) * (float)delta;

        velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, horizontalStep);
        velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, horizontalStep);

        if (IsOnFloor())
        {
            if (velocity.Y < 0.0f)
            {
                velocity.Y = 0.0f;
            }
        }
        else
        {
            velocity += _gravity * MovementProfile.GravityScale * (float)delta;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void SetMovementReference(Camera3D camera)
    {
        _movementReference = camera ?? throw new ArgumentNullException(nameof(camera));
    }

    public void SetInputEnabled(bool enabled)
    {
        _playerInput.Enabled = enabled;
    }

    private Vector3 ResolveMovementDirection(Vector2 input)
    {
        if (input.IsZeroApprox())
        {
            return Vector3.Zero;
        }

        var forward = Vector3.Forward;
        var right = Vector3.Right;

        if (_movementReference is not null)
        {
            forward = -_movementReference.GlobalBasis.Z;
            forward.Y = 0.0f;
            forward = forward.Normalized();

            right = _movementReference.GlobalBasis.X;
            right.Y = 0.0f;
            right = right.Normalized();
        }

        return (right * input.X + forward * -input.Y).LimitLength();
    }
}
