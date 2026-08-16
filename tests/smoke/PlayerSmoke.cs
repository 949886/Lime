using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Actors.Player;
using Lime.Game.World;

namespace Lime.Tests.Smoke;

public partial class PlayerSmoke : Node3D
{
    private static readonly string[] CharacterScenePaths =
    [
        "res://assets/spine/1015_aglna2/Angelina.tscn",
        "res://assets/spine/4235_thumpy/Thumpy.tscn",
        "res://assets/spine/4236_tmslot/Timeslot.tscn",
        "res://assets/spine/4237_jcinta/Jacinta.tscn",
    ];

    public override async void _Ready()
    {
        try
        {
            var player = GetNode<PlayerController>("%Player");
            var playerInput = player.GetNode<PlayerInput>("%PlayerInput");
            var movementReference = GetNode<Camera3D>("%MovementReference");

            player.SetMovementReference(movementReference);
            player.SetInputEnabled(true);

            await WaitPhysicsFrames(6);

            ValidateSceneContract(player);
            ValidateCharacterPresentationScenes();
            ValidateInputContract(player, playerInput);
            await ValidateCameraRelativeWalk(player, playerInput, movementReference);
            await ValidateSprint(player, playerInput);
            await ValidateInputDisableDeceleration(player, playerInput);

            GD.Print("[M1.2] PASS: Player runtime smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.2] FAIL: {exception}");
            GetTree().Quit(1);
        }
        finally
        {
            Input.ActionRelease(InputActions.Sprint);
        }
    }

    private static void ValidateSceneContract(PlayerController player)
    {
        Require(player.IsOnFloor(), "Player must settle on the World collision floor.");
        Require(player.CollisionLayer == CollisionLayers.PlayerMask,
            "Player collision layer must be Player (2).");
        Require(player.CollisionMask == CollisionLayers.WorldMask,
            "Player collision mask must contain World (1) only in M1.2.");
        Require(player.MotionMode == CharacterBody3D.MotionModeEnum.Grounded,
            "Player must use grounded CharacterBody3D motion mode.");
        Require(player.UpDirection == Vector3.Up,
            "Player up direction must remain Vector3.Up.");
        Require(player.FloorConstantSpeed,
            "Player must keep constant ground speed across slopes.");
        Require(player.FloorStopOnSlope,
            "Player must stop on slopes when there is no movement input.");
        Require(Mathf.Abs(player.FloorSnapLength - player.MovementProfile.FloorSnapLength) < 0.001f,
            "Player floor snap must come from PlayerMovementProfile.");
        Require(Mathf.Abs(player.FloorMaxAngle - player.MovementProfile.FloorMaxAngleRadians) < 0.001f,
            "Player floor max angle must come from PlayerMovementProfile.");

        var collisionShape = player.GetNode<CollisionShape3D>("CollisionShape3D");
        var capsule = collisionShape.Shape as CapsuleShape3D
            ?? throw new InvalidOperationException("Player collision shape must be CapsuleShape3D.");

        Require((collisionShape.Position - new Vector3(0.0f, 0.5f, 0.0f)).Length() < 0.001f,
            "Player root origin must remain at the feet/ground contact point.");
        Require(Mathf.Abs(capsule.Radius - 0.30f) < 0.001f,
            "Bootstrap player capsule radius must be 0.30 m.");
        Require(Mathf.Abs(capsule.Height - 1.00f) < 0.001f,
            "Bootstrap player capsule total height must be 1.00 m.");

        var visualRoot = player.GetNode<Node3D>("VisualRoot");
        var characterVisual = visualRoot.GetNodeOrNull<Node3D>("CharacterVisual");
        Require(characterVisual is not null,
            "Player VisualRoot must contain the 3D Angelina CharacterVisual scene.");
        Require(visualRoot.GetNodeOrNull<MeshInstance3D>("PlaceholderVisual") is null,
            "PlaceholderVisual must be removed after the 3D character scene is integrated.");
    }

    private static void ValidateCharacterPresentationScenes()
    {
        foreach (var scenePath in CharacterScenePaths)
        {
            var scene = GD.Load<PackedScene>(scenePath)
                ?? throw new InvalidOperationException($"Character scene must load: {scenePath}");
            var instance = scene.Instantiate();

            try
            {
                var root = instance as Node3D
                    ?? throw new InvalidOperationException($"Character scene root must be Node3D: {scenePath}");
                var sprite = root.GetNodeOrNull<Sprite3D>("Sprite3D")
                    ?? throw new InvalidOperationException($"Character scene must contain Sprite3D: {scenePath}");
                Require(sprite.Texture is ViewportTexture,
                    $"Character Sprite3D must render a ViewportTexture: {scenePath}");

                var viewport = root.GetNodeOrNull<SubViewport>("Sprite3D/SubViewport")
                    ?? throw new InvalidOperationException(
                        $"Character scene must contain SubViewport: {scenePath}");
                Require(viewport.TransparentBg,
                    $"Character SubViewport must use transparent background: {scenePath}");
                Require(root.GetNodeOrNull<Node>("Sprite3D/SubViewport/SpineSprite") is not null,
                    $"Character SubViewport must contain SpineSprite: {scenePath}");
            }
            finally
            {
                instance.Free();
            }
        }
    }

    private static void ValidateInputContract(PlayerController player, PlayerInput playerInput)
    {
        playerInput.SetVirtualMove(new Vector2(3.0f, -4.0f));
        var captured = playerInput.Capture();
        Require(captured.Move.Length() <= 1.001f,
            "Virtual movement input must be clamped to unit length.");
        playerInput.ClearVirtualMove();

        player.SetInputEnabled(false);
        var disabled = playerInput.Capture();
        Require(disabled == default,
            "Disabled PlayerInput must capture a neutral PlayerInputState.");
        player.SetInputEnabled(true);
    }

    private async Task ValidateCameraRelativeWalk(
        PlayerController player,
        PlayerInput playerInput,
        Camera3D movementReference)
    {
        var expectedForward = -movementReference.GlobalBasis.Z;
        expectedForward.Y = 0.0f;
        expectedForward = expectedForward.Normalized();

        var startPosition = player.GlobalPosition;
        playerInput.SetVirtualMove(Vector2.Up);
        await WaitPhysicsFrames(30);

        var displacement = player.GlobalPosition - startPosition;
        var horizontalDisplacement = new Vector3(displacement.X, 0.0f, displacement.Z);
        var horizontalSpeed = GetHorizontalSpeed(player.Velocity);

        Require(horizontalDisplacement.Length() > 0.5f,
            "Player must move when virtual forward input is supplied.");
        Require(horizontalDisplacement.Normalized().Dot(expectedForward) > 0.98f,
            "Player forward movement must use the injected Camera3D basis.");
        Require(horizontalSpeed > player.MovementProfile.WalkSpeed - 0.25f,
            "Player must accelerate to approximately WalkSpeed.");
        Require(horizontalSpeed <= player.MovementProfile.WalkSpeed + 0.10f,
            "Player walk velocity must not exceed WalkSpeed.");

        playerInput.ClearVirtualMove();
        await WaitPhysicsFrames(20);
        Require(GetHorizontalSpeed(player.Velocity) < 0.05f,
            "Player must decelerate naturally after movement input is released.");
    }

    private async Task ValidateSprint(PlayerController player, PlayerInput playerInput)
    {
        Input.ActionPress(InputActions.Sprint);
        playerInput.SetVirtualMove(Vector2.Up);
        await WaitPhysicsFrames(30);

        var horizontalSpeed = GetHorizontalSpeed(player.Velocity);
        Require(horizontalSpeed > player.MovementProfile.WalkSpeed + 1.0f,
            "Sprint input must produce a speed above WalkSpeed.");
        Require(horizontalSpeed <= player.MovementProfile.RunSpeed + 0.10f,
            "Sprint velocity must not exceed RunSpeed.");
    }

    private async Task ValidateInputDisableDeceleration(
        PlayerController player,
        PlayerInput playerInput)
    {
        var speedBeforeDisable = GetHorizontalSpeed(player.Velocity);

        player.SetInputEnabled(false);
        playerInput.ClearVirtualMove();
        Input.ActionRelease(InputActions.Sprint);
        await WaitPhysicsFrames(8);

        var speedAfterDisable = GetHorizontalSpeed(player.Velocity);
        Require(speedAfterDisable < speedBeforeDisable - 0.5f,
            "Disabling PlayerInput must preserve physics and allow natural deceleration.");
    }

    private async Task WaitPhysicsFrames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static float GetHorizontalSpeed(Vector3 velocity)
    {
        return new Vector2(velocity.X, velocity.Z).Length();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
