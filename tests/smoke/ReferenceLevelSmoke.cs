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
            Require(player.IsOnFloor(), "Player must settle on StartPlatform collision.");
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
                "Reference traversal must begin in StartPerspective.");

            await DriveToMarker(player, playerInput, camera, level.CameraCheck01, 280);
            await DriveToMarker(player, playerInput, camera, level.CameraCheck02, 320);
            var miningStairTop = level.GetNode<MeshInstance3D>(
                "ReferenceLevelVisuals/MiningApproachTerrace/Stair/Steps/Step01");
            await DriveToHorizontalPosition(player, playerInput, camera,
                miningStairTop.GlobalPosition + new Vector3(0, 0, 0.45f), "MiningApproachStair", 200);
            await DriveToMarker(player, playerInput, camera, level.CameraCheck03, 260);
            await DriveToMarker(player, playerInput, camera, level.RouteEnd, 520);
            playerInput.ClearVirtualMove();
            await WaitPhysicsFrames(12);

            Require(player.IsOnFloor(), "Player must remain grounded at RouteEnd.");
            Require(HorizontalDistance(player.GlobalPosition, level.RouteEnd.GlobalPosition) < 0.60f,
                "Player must traverse composed level to RouteEnd.");
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
                "Starting movement must transition to ExplorePerspective.");

            GD.Print("[M1.4] PASS: composed Start-to-pool-route traversal smoke completed successfully.");
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
            "Geometry/UpperPlatform", "Geometry/Stairs01", "Geometry/IntermediatePlatform",
            "Geometry/Stairs02", "Geometry/Check03BackPlatform", "Geometry/Check03Projection",
            "Geometry/LowerCorridor", "Geometry/PlazaExtension", "Geometry/WaterPool",
        })
            Require(level.GetNodeOrNull<Node3D>(legacyPath) is null,
                $"Legacy replaced node {legacyPath} must not return.");

        foreach (var legacyPath in new[]
        {
            "Collision/WorldCollision/IntermediatePlatform", "Collision/WorldCollision/StairRamp02",
            "Collision/WorldCollision/Check03BackPlatform", "Collision/WorldCollision/Check03Projection",
            "Collision/WorldCollision/LowerCorridor", "Collision/WorldCollision/PlazaExtension",
        })
            Require(level.GetNodeOrNull<CollisionShape3D>(legacyPath) is null,
                $"Legacy replaced collision {legacyPath} must not return.");

        var start = level.GetNode<Node3D>("ReferenceLevelVisuals/StartPlatform");
        Require(start.GetNodeOrNull<CollisionShape3D>("Collision/RearDeck") is not null,
            "StartPlatform must own rear deck collision.");
        Require(start.GetNodeOrNull<Node3D>("RightStair") is not null &&
                start.GetNodeOrNull<Node3D>("LeftStair") is not null,
            "StartPlatform must preserve dual stair topology.");

        var lower = level.GetNode<Node3D>("ReferenceLevelVisuals/StartLowerTerrace");
        ValidateBoxPair(lower, "Deck/MainDeck", "Collision/MainDeck", "StartLowerTerrace MainDeck");

        var mining = level.GetNode<Node3D>("ReferenceLevelVisuals/MiningApproachTerrace");
        ValidateBoxPair(mining, "Landing/BackDeck", "Collision/LandingBack", "MiningApproach BackDeck");
        ValidateBoxPair(mining, "Landing/ForwardProjection", "Collision/LandingProjection", "MiningApproach Projection");
        Require(mining.GetNodeOrNull<CollisionShape3D>("Collision/StairRamp") is not null,
            "MiningApproach must own stair ramp collision.");

        var corridor = level.GetNode<Node3D>("ReferenceLevelVisuals/LowerCorridorSection");
        foreach (var name in new[] { "ApproachDeck", "MidDeck", "FarDeck" })
            ValidateBoxPair(corridor, $"Deck/{name}", $"Collision/{name}", $"LowerCorridor {name}");

        ValidatePoolTerrace(level);
        ValidateBoxSync(level, "LowerFrontPlatform");
        ValidateStairCount(level.GetNode<Node3D>("Geometry/LowerFrontStairs"), "LowerFrontStairs");
    }

    private static void ValidatePoolTerrace(ReferenceLevel level)
    {
        var pool = level.GetNode<Node3D>("ReferenceLevelVisuals/PoolTerrace");
        foreach (var name in new[] { "RearDeck", "FrontDeck", "ScreenRightDeck", "ScreenLeftDeck" })
            ValidateBoxPair(pool, $"Deck/{name}", $"Collision/{name}", $"PoolTerrace {name}");

        Require(pool.GetNodeOrNull<MeshInstance3D>("Pool/Water") is not null,
            "PoolTerrace must contain an explicit water surface.");
        Require(pool.GetNodeOrNull<CollisionShape3D>("Collision/Water") is null,
            "Pool water opening must not be covered by a walkable collision shape.");
        Require(pool.GetNodeOrNull<Node3D>("Rails") is not null && pool.GetNodeOrNull<Node3D>("Grid") is not null,
            "PoolTerrace must own rails and tile-grid structure.");
    }

    private static void ValidateBoxPair(Node3D root, string meshPath, string collisionPath, string label)
    {
        var meshNode = root.GetNode<MeshInstance3D>(meshPath);
        var mesh = meshNode.Mesh as BoxMesh ?? throw new InvalidOperationException($"{label} must use BoxMesh.");
        var collision = root.GetNode<CollisionShape3D>(collisionPath);
        var shape = collision.Shape as BoxShape3D ?? throw new InvalidOperationException($"{label} collision must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f, $"{label} collision must match visual.");
    }

    private static void ValidateBoxSync(ReferenceLevel level, string name)
    {
        var meshNode = level.GetNode<MeshInstance3D>($"Geometry/{name}");
        var mesh = meshNode.Mesh as BoxMesh ?? throw new InvalidOperationException($"Geometry/{name} must use BoxMesh.");
        var collision = level.GetNode<CollisionShape3D>($"Collision/WorldCollision/{name}");
        var shape = collision.Shape as BoxShape3D ?? throw new InvalidOperationException($"Collision {name} must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f, $"{name} collision must match visual.");
    }

    private static void ValidateStairCount(Node3D stairs, string label)
    {
        var count = 0;
        foreach (var child in stairs.GetChildren())
            if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal)) count++;
        Require(count == ReferenceLevel.ReferenceStairStepCount,
            $"{label} must contain {ReferenceLevel.ReferenceStairStepCount} steps. Actual={count}.");
    }

    private async Task DriveToMarker(PlayerController player, PlayerInput input, Camera3D camera, Marker3D marker, int maxFrames)
    {
        await DriveToHorizontalPosition(player, input, camera, marker.GlobalPosition, marker.Name.ToString(), maxFrames);
        await WaitPhysicsFrames(8);
        Require(Mathf.Abs(player.GlobalPosition.Y - marker.GlobalPosition.Y) < 0.42f,
            $"Player reached {marker.Name} horizontally but not expected height.");
    }

    private async Task DriveToHorizontalPosition(PlayerController player, PlayerInput input, Camera3D camera, Vector3 target, string name, int maxFrames)
    {
        for (var frame = 0; frame < maxFrames; frame++)
        {
            var horizontal = target - player.GlobalPosition;
            horizontal.Y = 0;
            if (horizontal.Length() < 0.38f)
            {
                input.ClearVirtualMove();
                await WaitPhysicsFrames(4);
                return;
            }
            var direction = horizontal.Normalized();
            var forward = -camera.GlobalBasis.Z; forward.Y = 0; forward = forward.Normalized();
            var right = camera.GlobalBasis.X; right.Y = 0; right = right.Normalized();
            input.SetVirtualMove(new Vector2(direction.Dot(right), -direction.Dot(forward)));
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
        input.ClearVirtualMove();
        throw new InvalidOperationException($"Player could not reach {name}. Player={player.GlobalPosition}, Target={target}.");
    }

    private async Task WaitPhysicsFrames(int count)
    {
        for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
