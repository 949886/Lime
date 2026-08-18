using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Actors.Player;
using Lime.Game.Camera;
using Lime.Game.World;
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
            ValidateSpawnContract(gameRoot);
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
                "Reference traversal must begin in StartPerspective.");

            var cameraStart = camera.GlobalPosition;

            await DriveToMarker(player, playerInput, camera, level.CameraCheck01, 280);
            await DriveToMarker(player, playerInput, camera, level.CameraCheck02, 320);

            var stairs02Top = level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01");
            var stairs02Approach = stairs02Top.GlobalPosition + new Vector3(0.0f, 0.0f, 0.45f);
            await DriveToHorizontalPosition(
                player, playerInput, camera, stairs02Approach, "Stairs02Approach", 200);

            await DriveToMarker(player, playerInput, camera, level.CameraCheck03, 260);

            gameRoot.CameraDirector.SnapActiveToTarget();
            await WaitPhysicsFrames(3);
            ValidateCheck03ScreenTopology(level, camera);

            await DriveToMarker(player, playerInput, camera, level.RouteEnd, 520);
            playerInput.ClearVirtualMove();
            await WaitPhysicsFrames(12);

            Require(player.IsOnFloor(), "Player must remain grounded at RouteEnd.");
            Require(HorizontalDistance(player.GlobalPosition, level.RouteEnd.GlobalPosition) < 0.60f,
                "Player must traverse from PlayerStart through Check03 to RouteEnd.");
            Require(Mathf.Abs(player.GlobalPosition.Y - level.RouteEnd.GlobalPosition.Y) < 0.35f,
                "RouteEnd height must match the traversable lower corridor.");
            Require(camera.GlobalPosition.DistanceTo(cameraStart) > 8.0f,
                "Production camera must follow the player across the route.");
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
                "Starting movement must transition to ExplorePerspective.");

            GD.Print("[M1.4] PASS: Reference graybox traversal smoke completed successfully.");
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
        Require(level.GetNodeOrNull<Node3D>("Geometry") is not null,
            "ReferenceLevel must contain Geometry.");
        Require(level.GetNodeOrNull<StaticBody3D>("Collision/WorldCollision") is not null,
            "ReferenceLevel must contain WorldCollision.");
        Require(level.GetNodeOrNull<Node3D>("ReferenceMarkers") is not null,
            "ReferenceLevel must contain ReferenceMarkers.");

        var worldCollision = level.GetNode<StaticBody3D>("Collision/WorldCollision");
        Require(worldCollision.CollisionLayer == CollisionLayers.WorldMask,
            "ReferenceLevel static collision must use World layer 1.");
        Require(worldCollision.CollisionMask == 0,
            "Static reference collision does not need a collision mask.");

        ValidateBoxSync(level, "UpperPlatform");
        ValidateBoxSync(level, "IntermediatePlatform");
        ValidateBoxSync(level, "LowerCorridor");
        ValidateBoxSync(level, "PlazaExtension");
        ValidateBoxSync(level, "LowerFrontPlatform");
        ValidateBoxSync(level, "Check03BackPlatform");
        ValidateBoxSync(level, "Check03Projection");

        ValidateStairCount(level, "Geometry/Stairs01");
        ValidateStairCount(level, "Geometry/Stairs02");
        ValidateStairCount(level, "Geometry/LowerFrontStairs");

        ValidateRampCoversStairs(
            level,
            "Collision/WorldCollision/StairRamp01",
            "Geometry/Stairs01");
        ValidateRampCoversStairs(
            level,
            "Collision/WorldCollision/StairRamp02",
            "Geometry/Stairs02");
        ValidateRampCoversStairs(
            level,
            "Collision/WorldCollision/LowerFrontRamp",
            "Geometry/LowerFrontStairs");

        var sideSteps = GetSteps(level.GetNode<Node3D>("Geometry/LowerFrontStairs"));
        var sideTop = sideSteps[0].GlobalPosition;
        var sideBottom = sideSteps[^1].GlobalPosition;
        Require(sideBottom.X > sideTop.X + 0.9f,
            "Check03 lateral stair must descend toward world +X / screen-left with a meaningful span.");
        Require(sideBottom.Y < sideTop.Y - 0.8f,
            "Check03 lateral stair must visibly descend to the lower-left landing.");
        Require(Mathf.Abs(sideBottom.Z - sideTop.Z) < 0.25f,
            "Check03 lateral stair must face left instead of continuing toward the camera.");

        var backPlatform = level.GetNode<MeshInstance3D>("Geometry/Check03BackPlatform");
        var projection = level.GetNode<MeshInstance3D>("Geometry/Check03Projection");
        var backBox = (BoxMesh)backPlatform.Mesh;
        var projectionBox = (BoxMesh)projection.Mesh;

        Require(projectionBox.Size.X < backBox.Size.X,
            "Check03 must have a narrower forward projection below the rear platform.");
        Require(projection.GlobalPosition.Z < backPlatform.GlobalPosition.Z,
            "Check03 projection must extend toward the camera/screen-bottom.");

        var check03SurfacePoint = backPlatform.ToGlobal(
            new Vector3(0.0f, backBox.Size.Y * 0.5f, backBox.Size.Z * 0.5f));
        Require(level.CameraCheck03.GlobalPosition.DistanceTo(check03SurfacePoint) < 0.08f,
            $"CameraCheck03 must stay on the rear top edge of Check03BackPlatform. " +
            $"Marker={level.CameraCheck03.GlobalPosition}, Surface={check03SurfacePoint}.");
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
            $"{name} collision size must follow the current visual mesh. Mesh={mesh.Size}, Shape={shape.Size}.");
        Require(collision.GlobalTransform.Origin.DistanceTo(meshNode.GlobalTransform.Origin) < 0.01f,
            $"{name} collision position must follow the current visual mesh.");
        Require(BasisNear(collision.GlobalBasis, meshNode.GlobalBasis),
            $"{name} collision basis/scale must follow the current visual mesh.");
    }

    private static void ValidateStairCount(ReferenceLevel level, string path)
    {
        var steps = GetSteps(level.GetNode<Node3D>(path));
        Require(steps.Count >= 10,
            $"{path} must use 10+ visible treads. Actual={steps.Count}.");
        Require(steps.Count == ReferenceLevel.ReferenceStairStepCount,
            $"{path} must use the frozen M1.6 {ReferenceLevel.ReferenceStairStepCount}-tread calibration.");
    }

    private static void ValidateRampCoversStairs(
        ReferenceLevel level,
        string rampPath,
        string stairsPath)
    {
        var ramp = level.GetNode<CollisionShape3D>(rampPath);
        var shape = ramp.Shape as BoxShape3D
            ?? throw new InvalidOperationException($"{rampPath} must use a hidden BoxShape3D ramp.");
        var steps = GetSteps(level.GetNode<Node3D>(stairsPath));

        Require(steps.Count >= 2,
            $"{stairsPath} must contain enough steps to validate ramp coverage.");

        var first = steps[0];
        var last = steps[^1];
        var firstMesh = first.Mesh as BoxMesh
            ?? throw new InvalidOperationException($"{stairsPath}/{first.Name} must use BoxMesh.");
        var lastMesh = last.Mesh as BoxMesh
            ?? throw new InvalidOperationException($"{stairsPath}/{last.Name} must use BoxMesh.");

        var runVector = last.GlobalPosition - first.GlobalPosition;
        var centerRunLength = runVector.Length();
        Require(centerRunLength > 0.5f,
            $"{stairsPath} must have a meaningful multi-step run.");

        var runDirection = runVector / centerRunLength;
        var requiredRunLength = centerRunLength +
                                ProjectedBoxLength(first.GlobalBasis, firstMesh.Size, runDirection) * 0.5f +
                                ProjectedBoxLength(last.GlobalBasis, lastMesh.Size, runDirection) * 0.5f;
        var rampRunLength = ProjectedBoxLength(ramp.GlobalBasis, shape.Size, runDirection);

        Require(rampRunLength + 0.20f >= requiredRunLength,
            $"{rampPath} must cover the full oriented stair run. " +
            $"Ramp={rampRunLength:0.000}, Required={requiredRunLength:0.000}, Stairs={stairsPath}.");

        var expectedCenter = (first.GlobalPosition + last.GlobalPosition) * 0.5f;
        var centerOffsetAlongRun = Mathf.Abs((ramp.GlobalPosition - expectedCenter).Dot(runDirection));
        Require(centerOffsetAlongRun < 0.25f,
            $"{rampPath} must remain centered under its stair run. " +
            $"OffsetAlongRun={centerOffsetAlongRun:0.000}, Stairs={stairsPath}.");

        var horizontalRun = new Vector3(runVector.X, 0.0f, runVector.Z);
        Require(horizontalRun.Length() > 0.1f,
            $"{stairsPath} must have a horizontal direction for width validation.");

        horizontalRun = horizontalRun.Normalized();
        var crossDirection = new Vector3(-horizontalRun.Z, 0.0f, horizontalRun.X);
        var requiredWidth = Mathf.Max(
            ProjectedBoxLength(first.GlobalBasis, firstMesh.Size, crossDirection),
            ProjectedBoxLength(last.GlobalBasis, lastMesh.Size, crossDirection));
        var rampWidth = ProjectedBoxLength(ramp.GlobalBasis, shape.Size, crossDirection);

        Require(rampWidth + 0.10f >= requiredWidth,
            $"{rampPath} must cover the visible tread width. " +
            $"Ramp={rampWidth:0.000}, Required={requiredWidth:0.000}, Stairs={stairsPath}.");
    }

    private static float ProjectedBoxLength(Basis basis, Vector3 size, Vector3 unitDirection)
    {
        return Mathf.Abs(unitDirection.Dot(basis.X)) * size.X +
               Mathf.Abs(unitDirection.Dot(basis.Y)) * size.Y +
               Mathf.Abs(unitDirection.Dot(basis.Z)) * size.Z;
    }

    private static void ValidateCheck03ScreenTopology(ReferenceLevel level, Camera3D camera)
    {
        var player = camera.UnprojectPosition(level.CameraCheck03.GlobalPosition);
        var rearStair = camera.UnprojectPosition(
            level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01").GlobalPosition);
        var projection = camera.UnprojectPosition(
            level.GetNode<MeshInstance3D>("Geometry/Check03Projection").GlobalPosition);
        var sideSteps = GetSteps(level.GetNode<Node3D>("Geometry/LowerFrontStairs"));
        var sideTop = camera.UnprojectPosition(sideSteps[0].GlobalPosition);
        var sideBottom = camera.UnprojectPosition(sideSteps[^1].GlobalPosition);

        GD.Print(
            $"[M1.4] Check03 topology: rear={rearStair}, player={player}, projection={projection}, " +
            $"sideTop={sideTop}, sideBottom={sideBottom}.");

        Require(rearStair.Y < player.Y,
            "Check03 rear stair must appear above the player.");
        Require(projection.Y > player.Y,
            "Check03 forward projection must appear below the player.");
        Require(sideBottom.X < sideTop.X,
            "Check03 lateral stair must travel toward screen-left.");
        Require(sideBottom.Y > sideTop.Y,
            "Check03 lateral stair must travel downward on screen.");
    }

    private static void ValidateSpawnContract(GameRoot gameRoot)
    {
        Require(gameRoot.Player.GlobalPosition.DistanceTo(gameRoot.ReferenceLevel.PlayerStart.GlobalPosition) < 0.12f,
            "GameRoot must place Player at PlayerStart.");
        Require(gameRoot.Player.IsOnFloor(),
            "Player must settle on the synchronized PlayerStart collision.");
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
            $"Player reached {marker.Name} horizontally but not at its expected height. " +
            $"Player={player.GlobalPosition}, Target={marker.GlobalPosition}.");
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
            var horizontalDelta = new Vector3(delta.X, 0.0f, delta.Z);
            if (horizontalDelta.Length() < 0.38f)
            {
                playerInput.ClearVirtualMove();
                await WaitPhysicsFrames(4);
                return;
            }

            var direction = horizontalDelta.Normalized();
            var forward = -movementReference.GlobalBasis.Z;
            forward.Y = 0.0f;
            forward = forward.Normalized();
            var right = movementReference.GlobalBasis.X;
            right.Y = 0.0f;
            right = right.Normalized();

            playerInput.SetVirtualMove(new Vector2(
                direction.Dot(right),
                -direction.Dot(forward)));
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }

        playerInput.ClearVirtualMove();
        throw new InvalidOperationException(
            $"Player could not reach {targetName} within {maxFrames} physics frames. " +
            $"Player={player.GlobalPosition}, Target={target}, Velocity={player.Velocity}, IsOnFloor={player.IsOnFloor()}.");
    }

    private async Task WaitPhysicsFrames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static List<MeshInstance3D> GetSteps(Node3D stairs)
    {
        var result = new List<MeshInstance3D>();
        foreach (var child in stairs.GetChildren())
        {
            if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
            {
                result.Add(step);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Name.ToString(), right.Name.ToString()));
        return result;
    }

    private static bool BasisNear(Basis left, Basis right)
    {
        return left.X.DistanceTo(right.X) < 0.01f &&
               left.Y.DistanceTo(right.Y) < 0.01f &&
               left.Z.DistanceTo(right.Z) < 0.01f;
    }

    private static float HorizontalDistance(Vector3 left, Vector3 right)
    {
        return new Vector2(left.X - right.X, left.Z - right.Z).Length();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
