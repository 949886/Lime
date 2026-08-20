using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Actors.Player;
using Lime.Game.Camera;
using Lime.Game.World.Levels.Reference;

namespace Lime.Tests.Smoke;

public partial class ReferenceLevelSmoke : Node
{
    public override async void _Ready()
    {
        PlayerInput? playerInput = null;
        try
        {
            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");
            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await WaitPhysicsFrames(8);

            var level = gameRoot.ReferenceLevel;
            var player = gameRoot.Player;
            var camera = gameRoot.CameraDirector.RenderCamera;
            playerInput = player.GetNode<PlayerInput>("%PlayerInput");

            ValidateLevelContract(level);
            Require(player.GlobalPosition.DistanceTo(level.PlayerStart.GlobalPosition) < 0.12f,
                "GameRoot must place Player at PlayerStart.");
            Require(player.IsOnFloor(), "Player must settle on the StartPlatform rear-deck collision.");
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
                "Reference traversal must begin in StartPerspective.");

            await DriveToMarker(player, playerInput, camera, level.CameraCheck01, 280);
            await DriveToMarker(player, playerInput, camera, level.CameraCheck02, 320);

            var stairs02Top = level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01");
            await DriveToHorizontalPosition(
                player,
                playerInput,
                camera,
                stairs02Top.GlobalPosition + new Vector3(0, 0, 0.45f),
                "Stairs02Approach",
                200);

            await DriveToMarker(player, playerInput, camera, level.CameraCheck03, 260);
            await DriveToMarker(player, playerInput, camera, level.RouteEnd, 520);
            playerInput.ClearVirtualMove();
            await WaitPhysicsFrames(12);

            Require(player.IsOnFloor(), "Player must remain grounded at RouteEnd.");
            Require(HorizontalDistance(player.GlobalPosition, level.RouteEnd.GlobalPosition) < 0.60f,
                "Player must traverse through the composed level to RouteEnd.");
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
                "Starting movement must transition to ExplorePerspective.");

            GD.Print("[M1.4] PASS: segmented StartPlatform traversal smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.4] FAIL: {exception}");
            GetTree().Quit(1);
        }
        finally
        {
            playerInput?.ClearVirtualMove();
        }
    }

    private static void ValidateLevelContract(ReferenceLevel level)
    {
        Require(level.GetNodeOrNull<Node3D>("Geometry/UpperPlatform") is null,
            "Legacy UpperPlatform must not return after StartPlatform replacement.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is null,
            "Legacy Stairs01 must not return after StartPlatform replacement.");

        var startPlatform = level.GetNode<Node3D>("ReferenceLevelVisuals/StartPlatform");
        var deck = startPlatform.GetNode<Node3D>("Deck");
        ValidateDeckPiece(startPlatform, deck, "RearDeck");
        ValidateDeckPiece(startPlatform, deck, "FrontRightDeck");
        ValidateDeckPiece(startPlatform, deck, "FrontCenterDeck");
        ValidateDeckPiece(startPlatform, deck, "FrontLeftDeck");

        var rearDeck = deck.GetNode<MeshInstance3D>("RearDeck");
        var rearMesh = rearDeck.Mesh as BoxMesh
            ?? throw new InvalidOperationException("StartPlatform Deck/RearDeck must use BoxMesh.");
        Require(rearMesh.Size.X >= 19.5f && rearMesh.Size.Z > 5.5f,
            $"Start rear plaza must be broad and deep enough for PlayerStart. Size={rearMesh.Size}.");

        var rightStair = startPlatform.GetNode<Node3D>("RightStair");
        var leftStair = startPlatform.GetNode<Node3D>("LeftStair");
        ValidateStairCount(rightStair.GetNode<Node3D>("StairVisual"), "StartPlatform/RightStair");
        ValidateStairCount(leftStair.GetNode<Node3D>("StairVisual"), "StartPlatform/LeftStair");
        Require(leftStair.Position.X - rightStair.Position.X > 8.0f,
            "Detailed StartPlatform must keep the two stair openings widely separated.");
        Require(rightStair.GetNodeOrNull<CollisionShape3D>("Collision/Ramp") is not null,
            "RightStair must own the live traversal collision.");

        Require(level.GetNodeOrNull<Node3D>("ReferenceLevelVisuals/StartPlatformProps") is not null,
            "Detailed StartPlatform props must be part of production visuals.");

        ValidateStairCount(level.GetNode<Node3D>("Geometry/Stairs02"), "Geometry/Stairs02");
        ValidateStairCount(level.GetNode<Node3D>("Geometry/LowerFrontStairs"), "Geometry/LowerFrontStairs");

        foreach (var name in new[]
                 {
                     "IntermediatePlatform",
                     "LowerCorridor",
                     "PlazaExtension",
                     "LowerFrontPlatform",
                     "Check03BackPlatform",
                     "Check03Projection",
                 })
        {
            ValidateBoxSync(level, name);
        }
    }

    private static void ValidateDeckPiece(Node3D startPlatform, Node3D deck, string name)
    {
        var meshNode = deck.GetNode<MeshInstance3D>(name);
        var mesh = meshNode.Mesh as BoxMesh
            ?? throw new InvalidOperationException($"StartPlatform Deck/{name} must use BoxMesh.");
        var collision = startPlatform.GetNode<CollisionShape3D>($"Collision/{name}");
        var shape = collision.Shape as BoxShape3D
            ?? throw new InvalidOperationException($"StartPlatform Collision/{name} must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
            $"StartPlatform {name} collision must match its visual mesh.");
    }

    private static void ValidateBoxSync(ReferenceLevel level, string name)
    {
        var meshNode = level.GetNode<MeshInstance3D>($"Geometry/{name}");
        var mesh = meshNode.Mesh as BoxMesh
            ?? throw new InvalidOperationException($"Geometry/{name} must use BoxMesh.");
        var collision = level.GetNode<CollisionShape3D>($"Collision/WorldCollision/{name}");
        var shape = collision.Shape as BoxShape3D
            ?? throw new InvalidOperationException($"Collision/WorldCollision/{name} must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
            $"{name} collision size must match its current graybox visual.");
    }

    private static void ValidateStairCount(Node3D stairs, string label)
    {
        var count = 0;
        foreach (var child in stairs.GetChildren())
        {
            if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
                count++;
        }
        Require(count == ReferenceLevel.ReferenceStairStepCount,
            $"{label} must contain {ReferenceLevel.ReferenceStairStepCount} steps. Actual={count}.");
    }

    private async Task DriveToMarker(
        PlayerController player,
        PlayerInput playerInput,
        Camera3D movementReference,
        Marker3D marker,
        int maxFrames)
    {
        await DriveToHorizontalPosition(
            player, playerInput, movementReference, marker.GlobalPosition, marker.Name.ToString(), maxFrames);
        await WaitPhysicsFrames(8);
        Require(Mathf.Abs(player.GlobalPosition.Y - marker.GlobalPosition.Y) < 0.42f,
            $"Player reached {marker.Name} horizontally but not at expected height.");
    }

    private async Task DriveToHorizontalPosition(
        PlayerController player,
        PlayerInput playerInput,
        Camera3D movementReference,
        Vector3 target,
        string targetName,
        int maxFrames)
    {
        for (var frame = 0; frame < maxFrames; frame++)
        {
            var delta = target - player.GlobalPosition;
            var horizontalDelta = new Vector3(delta.X, 0, delta.Z);
            if (horizontalDelta.Length() < 0.38f)
            {
                playerInput.ClearVirtualMove();
                await WaitPhysicsFrames(4);
                return;
            }

            var direction = horizontalDelta.Normalized();
            var forward = -movementReference.GlobalBasis.Z;
            forward.Y = 0;
            forward = forward.Normalized();
            var right = movementReference.GlobalBasis.X;
            right.Y = 0;
            right = right.Normalized();

            playerInput.SetVirtualMove(new Vector2(direction.Dot(right), -direction.Dot(forward)));
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }

        playerInput.ClearVirtualMove();
        throw new InvalidOperationException(
            $"Player could not reach {targetName}. Player={player.GlobalPosition}, Target={target}.");
    }

    private async Task WaitPhysicsFrames(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static float HorizontalDistance(Vector3 left, Vector3 right)
    {
        return new Vector2(left.X - right.X, left.Z - right.Z).Length();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
