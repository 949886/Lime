using System;
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

            var referenceLevel = gameRoot.ReferenceLevel;
            var player = gameRoot.Player;
            var renderCamera = gameRoot.CameraDirector.RenderCamera;
            playerInput = player.GetNode<PlayerInput>("%PlayerInput");

            ValidateLevelContract(referenceLevel);
            ValidateSpawnContract(gameRoot);
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
                "Reference traversal must begin in the close StartPerspective camera.");

            var cameraStart = renderCamera.GlobalPosition;

            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.CameraCheck01, 240);
            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.CameraCheck02, 280);

            // M1.6 calibrates the reference zig-zag so Stairs02 sits on the
            // opposite side of the intermediate platform. Align horizontally
            // with the stair entrance before crossing the platform edge instead
            // of driving diagonally through empty space toward Check03.
            var stairs02Top = referenceLevel.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01");
            var stairs02Approach = stairs02Top.GlobalPosition + new Vector3(0.0f, 0.0f, 0.65f);
            await DriveToHorizontalPosition(
                player, playerInput, renderCamera, stairs02Approach, "Stairs02Approach", 180);

            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.CameraCheck03, 220);

            gameRoot.CameraDirector.SnapActiveToTarget();
            await WaitPhysicsFrames(3);
            ValidateLowerCorridorScreenDirection(referenceLevel, renderCamera);

            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.RouteEnd, 480);

            playerInput.ClearVirtualMove();
            await WaitPhysicsFrames(12);

            Require(player.IsOnFloor(),
                "Player must remain grounded at RouteEnd.");
            Require(HorizontalDistance(player.GlobalPosition, referenceLevel.RouteEnd.GlobalPosition) < 0.55f,
                "Player must be able to traverse the reference graybox from PlayerStart to RouteEnd.");
            Require(Mathf.Abs(player.GlobalPosition.Y - referenceLevel.RouteEnd.GlobalPosition.Y) < 0.35f,
                "RouteEnd marker height must match the traversable lower corridor.");
            Require(renderCamera.GlobalPosition.DistanceTo(cameraStart) > 10.0f,
                "Production Camera must follow Player across the reference route.");
            Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
                "Starting movement must transition from StartPerspective to ExplorePerspective.");

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
        Require(level.Name == "ReferenceLevel",
            "ReferenceLevel production scene is not wired into GameRoot/LevelRoot.");
        Require(level.GetNodeOrNull<Node3D>("Environment") is not null,
            "ReferenceLevel must contain Environment.");
        Require(level.GetNodeOrNull<Node3D>("Geometry") is not null,
            "ReferenceLevel must contain Geometry.");
        Require(level.GetNodeOrNull<Node3D>("Collision") is not null,
            "ReferenceLevel must contain Collision.");
        Require(level.GetNodeOrNull<Node3D>("ReferenceMarkers") is not null,
            "ReferenceLevel must contain ReferenceMarkers.");

        Require(level.GetNodeOrNull<DirectionalLight3D>("Environment/Sun") is not null,
            "Reference graybox must include a directional light.");

        var upperPlatform = level.GetNode<MeshInstance3D>("Geometry/UpperPlatform");
        var intermediatePlatform = level.GetNode<MeshInstance3D>("Geometry/IntermediatePlatform");
        var lowerCorridor = level.GetNode<MeshInstance3D>("Geometry/LowerCorridor");
        var lowerFrontStep = level.GetNode<MeshInstance3D>("Geometry/LowerFrontStairs/Step01");

        var upperMesh = upperPlatform.Mesh as BoxMesh
            ?? throw new InvalidOperationException("UpperPlatform must use a BoxMesh graybox mass.");
        var intermediateMesh = intermediatePlatform.Mesh as BoxMesh
            ?? throw new InvalidOperationException("IntermediatePlatform must use a BoxMesh graybox mass.");
        var corridorMesh = lowerCorridor.Mesh as BoxMesh
            ?? throw new InvalidOperationException("LowerCorridor must use a BoxMesh graybox mass.");
        var lowerFrontStepMesh = lowerFrontStep.Mesh as BoxMesh
            ?? throw new InvalidOperationException("LowerFrontStairs must use BoxMesh graybox steps.");

        Require(Near(upperMesh.Size.X, 8.0f) && Near(upperMesh.Size.Z, 4.0f),
            "UpperPlatform footprint must use the Start-reference 8 x 4 calibration.");
        Require(upperPlatform.GlobalPosition.DistanceTo(new Vector3(1.4f, 1.85f, 3.0f)) < 0.01f,
            "UpperPlatform must be centered on the first stair axis and keep its front edge at Z=1.");
        Require(Near(intermediateMesh.Size.X, 8.0f) && Near(intermediateMesh.Size.Z, 5.0f),
            "IntermediatePlatform bootstrap footprint must remain 8 x 5.");

        var corridorLongSide = Mathf.Max(corridorMesh.Size.X, corridorMesh.Size.Z);
        var corridorShortSide = Mathf.Min(corridorMesh.Size.X, corridorMesh.Size.Z);
        Require(Near(corridorLongSide, 25.0f) && Near(corridorShortSide, 3.5f),
            $"LowerCorridor bootstrap footprint must remain 25 x 3.5 after lower-stair alignment. " +
            $"Actual XZ=({corridorMesh.Size.X}, {corridorMesh.Size.Z}).");
        Require(corridorLongSide > corridorShortSide * 5.0f,
            "LowerCorridor must stay a narrow cross-corridor instead of becoming a broad platform.");

        var corridorLeftEdgeX = WorldMaxX(lowerCorridor, corridorMesh);
        var lowerFrontStairLeftEdgeX = WorldMaxX(lowerFrontStep, lowerFrontStepMesh);
        Require(Near(corridorLeftEdgeX, lowerFrontStairLeftEdgeX),
            $"LowerCorridor screen-left/world-+X edge must align with the lower-front stair left edge. " +
            $"Corridor={corridorLeftEdgeX}, Stair={lowerFrontStairLeftEdgeX}.");

        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is not null,
            "Reference graybox must include first stair visuals.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs02") is not null,
            "Reference graybox must include second stair visuals.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/LowerFrontStairs") is not null,
            "Reference graybox must include the separate lower-front stair run.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/PlazaExtension") is not null,
            "Reference graybox must include the later plaza continuation.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/ForegroundBlocker") is not null,
            "Reference graybox must include a foreground blocker volume.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Rails") is not null,
            "Reference graybox must include rail silhouettes.");

        Require(level.GetNode<MeshInstance3D>("Geometry/CylinderBuilding01").Mesh is CylinderMesh,
            "Reference graybox must include the first cylindrical building mass.");
        Require(level.GetNode<MeshInstance3D>("Geometry/CylinderBuilding02").Mesh is CylinderMesh,
            "Reference graybox must include the second cylindrical building mass.");

        var worldCollision = level.GetNode<StaticBody3D>("Collision/WorldCollision");
        Require(worldCollision.CollisionLayer == CollisionLayers.WorldMask,
            "ReferenceLevel static collision must use World layer 1.");
        Require(worldCollision.CollisionMask == 0,
            "Static reference World collision does not need a collision mask.");

        var upperCollision = worldCollision.GetNode<CollisionShape3D>("UpperPlatform");
        var upperShape = upperCollision.Shape as BoxShape3D
            ?? throw new InvalidOperationException("UpperPlatform gameplay collision must use a BoxShape3D.");
        Require(upperShape.Size.DistanceTo(upperMesh.Size) < 0.01f,
            "UpperPlatform collision must match the calibrated visual footprint.");
        Require(upperCollision.GlobalPosition.DistanceTo(upperPlatform.GlobalPosition) < 0.01f,
            "UpperPlatform visual mesh and gameplay collision must share the same center.");

        var corridorCollision = worldCollision.GetNode<CollisionShape3D>("LowerCorridor");
        var corridorShape = corridorCollision.Shape as BoxShape3D
            ?? throw new InvalidOperationException("LowerCorridor gameplay collision must use a BoxShape3D.");
        Require(Near(corridorShape.Size.X, corridorMesh.Size.X) &&
                Near(corridorShape.Size.Y, corridorMesh.Size.Y) &&
                Near(corridorShape.Size.Z, corridorMesh.Size.Z),
            "LowerCorridor visual mesh and gameplay collision footprint must stay synchronized.");
        Require(corridorCollision.GlobalPosition.DistanceTo(lowerCorridor.GlobalPosition) < 0.01f,
            "LowerCorridor visual mesh and gameplay collision must share the same center.");

        var stairRamp01 = worldCollision.GetNode<CollisionShape3D>("StairRamp01");
        var stairRamp02 = worldCollision.GetNode<CollisionShape3D>("StairRamp02");
        Require(worldCollision.GetNodeOrNull<CollisionShape3D>("LowerFrontRamp") is not null,
            "Lower-front stair visuals must use their own hidden ramp collision.");
        Require(Mathf.Abs(stairRamp01.Rotation.X - stairRamp02.Rotation.X) < 0.01f,
            "The two main stair runs must remain parallel descents.");

        var upperFrontZ = upperPlatform.GlobalPosition.Z - upperMesh.Size.Z * 0.5f;
        Require(Mathf.Abs(upperFrontZ - 1.0f) < 0.01f,
            "UpperPlatform front edge must meet the first stair top at Z=1.");
        Require(Mathf.Abs(level.PlayerStart.GlobalPosition.X - 1.4f) < 0.01f,
            "PlayerStart must sit on the first stair centerline.");
        var startToStair = level.PlayerStart.GlobalPosition.Z - upperFrontZ;
        Require(startToStair > 0.55f && startToStair < 0.75f,
            "PlayerStart must sit about 0.65m behind the first stair instead of deep inside the upper platform.");
        Require(level.PlayerStart.GlobalPosition.Y > level.CameraCheck01.GlobalPosition.Y + 1.0f,
            "The first stair run must descend from the starting upper platform.");
        Require(Mathf.Abs(level.CameraCheck01.GlobalPosition.Y - level.CameraCheck02.GlobalPosition.Y) < 0.15f,
            "CameraCheck01 and CameraCheck02 must remain on the same intermediate platform.");
        Require(level.CameraCheck02.GlobalPosition.Y > level.CameraCheck03.GlobalPosition.Y + 1.0f,
            "The second main stair run must continue descending to the lower corridor.");
        Require(Mathf.Abs(level.CameraCheck03.GlobalPosition.Y - level.RouteEnd.GlobalPosition.Y) < 0.15f,
            "CameraCheck03 and RouteEnd must remain on the same lower corridor level.");
    }

    private static void ValidateLowerCorridorScreenDirection(ReferenceLevel level, Camera3D renderCamera)
    {
        var plazaExtension = level.GetNode<MeshInstance3D>("Geometry/PlazaExtension");
        var checkScreen = renderCamera.UnprojectPosition(level.CameraCheck03.GlobalPosition);
        var routeScreen = renderCamera.UnprojectPosition(level.RouteEnd.GlobalPosition);
        var plazaScreen = renderCamera.UnprojectPosition(plazaExtension.GlobalPosition);

        GD.Print(
            $"[M1.4] Screen-space corridor: Check03={checkScreen}, RouteEnd={routeScreen}, " +
            $"Plaza={plazaScreen}, CameraBasisX={renderCamera.GlobalBasis.X}, " +
            $"CameraBasisZ={renderCamera.GlobalBasis.Z}.");

        Require(routeScreen.X > checkScreen.X + 1.0f,
            "After Stairs02 the lower corridor must extend toward screen-right in the production Explore camera.");
        Require(plazaScreen.X > routeScreen.X + 1.0f,
            "PlazaExtension must continue farther toward screen-right beyond the pre-black RouteEnd.");
    }

    private static void ValidateSpawnContract(GameRoot gameRoot)
    {
        Require(gameRoot.Player.GlobalPosition.DistanceTo(gameRoot.ReferenceLevel.PlayerStart.GlobalPosition) < 0.12f,
            "GameRoot must place Player at ReferenceLevel.PlayerStart before exploration begins.");
        Require(gameRoot.Player.IsOnFloor(),
            "Player must settle on ReferenceLevel World collision at PlayerStart.");
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
        Require(Mathf.Abs(player.GlobalPosition.Y - marker.GlobalPosition.Y) < 0.40f,
            $"Player reached {marker.Name} horizontally but not at the expected traversable height. " +
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
            $"Player could not reach traversal target {targetName} within {maxFrames} physics frames. " +
            $"Player={player.GlobalPosition}, Target={target}, " +
            $"Velocity={player.Velocity}, IsOnFloor={player.IsOnFloor()}.");
    }

    private async Task WaitPhysicsFrames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static float HorizontalDistance(Vector3 left, Vector3 right)
    {
        return new Vector2(left.X - right.X, left.Z - right.Z).Length();
    }

    private static float WorldMaxX(MeshInstance3D instance, BoxMesh mesh)
    {
        var halfSize = mesh.Size * 0.5f;
        var basis = instance.GlobalBasis;
        return instance.GlobalPosition.X +
               Mathf.Abs(basis.X.X) * halfSize.X +
               Mathf.Abs(basis.Y.X) * halfSize.Y +
               Mathf.Abs(basis.Z.X) * halfSize.Z;
    }

    private static bool Near(float left, float right)
    {
        return Mathf.Abs(left - right) < 0.01f;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
