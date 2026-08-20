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

            var miningStairTop = level.GetNode<MeshInstance3D>(
                "ReferenceLevelVisuals/MiningApproachTerrace/Stair/Steps/Step01");
            await DriveToHorizontalPosition(
                player,
                playerInput,
                camera,
                miningStairTop.GlobalPosition + new Vector3(0, 0, 0.45f),
                "MiningApproachStair",
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

            GD.Print("[M1.4] PASS: composed Start-to-mining-approach traversal smoke completed successfully.");
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
        foreach (var legacyPath in new[]
                 {
                     "Geometry/UpperPlatform",
                     "Geometry/Stairs01",
                     "Geometry/IntermediatePlatform",
                     "Geometry/Stairs02",
                     "Geometry/Check03BackPlatform",
                     "Geometry/Check03Projection",
                 })
        {
            Require(level.GetNodeOrNull<Node3D>(legacyPath) is null,
                $"Legacy replaced node {legacyPath} must not return.");
        }
        foreach (var legacyPath in new[]
                 {
                     "Collision/WorldCollision/IntermediatePlatform",
                     "Collision/WorldCollision/StairRamp02",
                     "Collision/WorldCollision/Check03BackPlatform",
                     "Collision/WorldCollision/Check03Projection",
                 })
        {
            Require(level.GetNodeOrNull<CollisionShape3D>(legacyPath) is null,
                $"Legacy replaced collision {legacyPath} must not return.");
        }

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

        ValidateLowerTerrace(level);
        ValidateMiningApproach(level);
        ValidateStairCount(level.GetNode<Node3D>("Geometry/LowerFrontStairs"), "Geometry/LowerFrontStairs");

        foreach (var name in new[]
                 {
                     "LowerCorridor",
                     "PlazaExtension",
                     "LowerFrontPlatform",
                 })
        {
            ValidateBoxSync(level, name);
        }
    }

    private static void ValidateLowerTerrace(ReferenceLevel level)
    {
        var terrace = level.GetNode<Node3D>("ReferenceLevelVisuals/StartLowerTerrace");
        var mainDeck = terrace.GetNode<MeshInstance3D>("Deck/MainDeck");
        var mainMesh = mainDeck.Mesh as BoxMesh
            ?? throw new InvalidOperationException("StartLowerTerrace Deck/MainDeck must use BoxMesh.");
        var mainCollision = terrace.GetNode<CollisionShape3D>("Collision/MainDeck");
        var mainShape = mainCollision.Shape as BoxShape3D
            ?? throw new InvalidOperationException("StartLowerTerrace Collision/MainDeck must use BoxShape3D.");
        Require(mainShape.Size.DistanceTo(mainMesh.Size) < 0.01f,
            "StartLowerTerrace main deck collision must match its visual.");
        var mainDeckInLevel = level.ToLocal(mainDeck.GlobalPosition);
        Require(Mathf.Abs(mainDeckInLevel.Y + mainMesh.Size.Y * 0.5f - 0.8f) < 0.01f,
            $"StartLowerTerrace top surface must stay at local Y=0.8. Actual={mainDeckInLevel.Y + mainMesh.Size.Y * 0.5f:0.000}.");
        Require(terrace.GetNodeOrNull<MeshInstance3D>("RetainingWalls/FrontScreenRight") is not null &&
                terrace.GetNodeOrNull<MeshInstance3D>("RetainingWalls/FrontScreenLeft") is not null,
            "StartLowerTerrace front wall must stay split around the mining stair opening.");
    }

    private static void ValidateMiningApproach(ReferenceLevel level)
    {
        var approach = level.GetNode<Node3D>("ReferenceLevelVisuals/MiningApproachTerrace");
        var steps = approach.GetNode<Node3D>("Stair/Steps");
        var stepCount = 0;
        foreach (var child in steps.GetChildren())
        {
            if (child is not MeshInstance3D step ||
                !step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
                continue;

            stepCount++;
            var stepMesh = step.Mesh as BoxMesh
                ?? throw new InvalidOperationException($"MiningApproach {step.Name} must use BoxMesh.");
            Require(Mathf.Abs(stepMesh.Size.X - 3.6f) < 0.01f,
                $"MiningApproach stair must use the reviewed 3.6m width. Actual={stepMesh.Size.X:0.00}.");
        }
        Require(stepCount == ReferenceLevel.ReferenceStairStepCount,
            $"MiningApproach stair must contain 12 steps. Actual={stepCount}.");
        Require(approach.GetNodeOrNull<CollisionShape3D>("Collision/StairRamp") is not null,
            "MiningApproach stair must own its ramp collision.");

        ValidateMiningLandingPiece(level, approach, "Landing/BackDeck", "Collision/LandingBack");
        ValidateMiningLandingPiece(level, approach, "Landing/ForwardProjection", "Collision/LandingProjection");

        var backDeck = approach.GetNode<MeshInstance3D>("Landing/BackDeck");
        var backMesh = (BoxMesh)backDeck.Mesh;
        var localBack = level.ToLocal(backDeck.GlobalPosition);
        Require(Mathf.Abs(localBack.Y + backMesh.Size.Y * 0.5f + 0.4f) < 0.01f,
            $"MiningApproach landing must keep local top Y=-0.4. Actual={localBack.Y + backMesh.Size.Y * 0.5f:0.000}.");
        Require(approach.GetNodeOrNull<MeshInstance3D>("Rails/StairMouthScreenRightRail") is not null &&
                approach.GetNodeOrNull<MeshInstance3D>("Rails/StairMouthScreenLeftRail") is not null,
            "MiningApproach landing rail must stay split around the stair mouth.");
        Require(approach.GetNodeOrNull<MeshInstance3D>("Landing/BoundaryScreenRightWall") is not null &&
                approach.GetNodeOrNull<MeshInstance3D>("Landing/BoundaryScreenLeftWall") is not null,
            "MiningApproach landing boundary must preserve the central projection opening.");
    }

    private static void ValidateMiningLandingPiece(
        ReferenceLevel level,
        Node3D approach,
        string meshPath,
        string collisionPath)
    {
        var meshNode = approach.GetNode<MeshInstance3D>(meshPath);
        var mesh = meshNode.Mesh as BoxMesh
            ?? throw new InvalidOperationException($"{meshPath} must use BoxMesh.");
        var collision = approach.GetNode<CollisionShape3D>(collisionPath);
        var shape = collision.Shape as BoxShape3D
            ?? throw new InvalidOperationException($"{collisionPath} must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
            $"{meshPath} collision must match its visual.");
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
