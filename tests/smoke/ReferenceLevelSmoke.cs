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

            ValidateLevelContract(referenceLevel, renderCamera);
            ValidateSpawnContract(gameRoot);

            var cameraStart = renderCamera.GlobalPosition;

            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.CameraCheck01, 240);
            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.CameraCheck02, 280);
            await DriveToMarker(player, playerInput, renderCamera,
                referenceLevel.CameraCheck03, 220);
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
                "Reference traversal must preserve the bootstrap ExplorePerspective camera.");

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

    private static void ValidateLevelContract(ReferenceLevel level, Camera3D renderCamera)
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
        var plazaExtension = level.GetNode<MeshInstance3D>("Geometry/PlazaExtension");
        var lowerFrontPlatform = level.GetNode<MeshInstance3D>("Geometry/LowerFrontPlatform");

        Require(upperPlatform.Mesh is BoxMesh upperMesh,
            "UpperPlatform must use a BoxMesh graybox mass.");
        Require(intermediatePlatform.Mesh is BoxMesh intermediateMesh,
            "IntermediatePlatform must use a BoxMesh graybox mass.");
        Require(lowerCorridor.Mesh is BoxMesh corridorMesh,
            "LowerCorridor must use a BoxMesh graybox mass.");

        Require(Near(upperMesh.Size.X, 18.0f) && Near(upperMesh.Size.Z, 12.0f),
            "UpperPlatform bootstrap footprint must remain 18 x 12.");
        Require(Near(intermediateMesh.Size.X, 8.0f) && Near(intermediateMesh.Size.Z, 5.0f),
            "IntermediatePlatform bootstrap footprint must remain 8 x 5.");
        Require(Near(corridorMesh.Size.X, 22.0f) && Near(corridorMesh.Size.Z, 3.5f),
            "LowerCorridor bootstrap footprint must remain 22 x 3.5.");
        Require(corridorMesh.Size.X > corridorMesh.Size.Z * 5.0f,
            "LowerCorridor must stay a narrow cross-corridor instead of becoming a broad platform.");

        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is not null,
            "Reference graybox must include first stair visuals.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs02") is not null,
            "Reference graybox must include second stair visuals.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/LowerFrontStairs") is not null,
            "Reference graybox must include the separate lower-front stair run.");
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

        var stairRamp01 = worldCollision.GetNode<CollisionShape3D>("StairRamp01");
        var stairRamp02 = worldCollision.GetNode<CollisionShape3D>("StairRamp02");
        Require(worldCollision.GetNodeOrNull<CollisionShape3D>("LowerFrontRamp") is not null,
            "Lower-front stair visuals must use their own hidden ramp collision.");
        Require(Mathf.Abs(stairRamp01.Rotation.X - stairRamp02.Rotation.X) < 0.01f,
            "The two main stair runs must remain parallel descents.");

        Require(level.PlayerStart.GlobalPosition.Y > level.CameraCheck01.GlobalPosition.Y + 1.0f,
            "The first stair run must descend from the starting upper platform.");
        Require(Mathf.Abs(level.CameraCheck01.GlobalPosition.Y - level.CameraCheck02.GlobalPosition.Y) < 0.15f,
            "CameraCheck01 and CameraCheck02 must remain on the same intermediate platform.");
        Require(level.CameraCheck02.GlobalPosition.Y > level.CameraCheck03.GlobalPosition.Y + 1.0f,
            "The second main stair run must continue descending to the lower corridor.");
        Require(Mathf.Abs(level.CameraCheck03.GlobalPosition.Y - level.RouteEnd.GlobalPosition.Y) < 0.15f,
            "CameraCheck03 and RouteEnd must remain on the same lower corridor level.");

        var cameraRight = renderCamera.GlobalBasis.X;
        cameraRight.Y = 0.0f;
        cameraRight = cameraRight.Normalized();

        var cameraForward = -renderCamera.GlobalBasis.Z;
        cameraForward.Y = 0.0f;
        cameraForward = cameraForward.Normalized();

        var routeDelta = level.RouteEnd.GlobalPosition - level.CameraCheck03.GlobalPosition;
        routeDelta.Y = 0.0f;

        Require(routeDelta.Dot(cameraRight) > 8.0f,
            "After Stairs02 the lower corridor must travel toward screen-right in the production Explore camera.");
        Require(Mathf.Abs(routeDelta.Dot(cameraForward)) < 3.0f,
            "After Stairs02 the route must turn roughly 90 degrees along the cross-corridor.");

        var plazaDelta = plazaExtension.GlobalPosition - level.RouteEnd.GlobalPosition;
        plazaDelta.Y = 0.0f;
        Require(plazaDelta.Dot(cameraRight) > 8.0f,
            "PlazaExtension must continue farther toward screen-right beyond the pre-black RouteEnd.");

        var lowerFrontDelta = lowerFrontPlatform.GlobalPosition - lowerCorridor.GlobalPosition;
        lowerFrontDelta.Y = 0.0f;
        Require(lowerFrontDelta.Dot(cameraRight) < -8.0f,
            "The separate lower-front platform must stay on the opposite/left side of the main corridor composition.");
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
        for (var frame = 0; frame < maxFrames; frame++)
        {
            var delta = marker.GlobalPosition - player.GlobalPosition;
            var horizontalDelta = new Vector3(delta.X, 0.0f, delta.Z);

            if (horizontalDelta.Length() < 0.38f)
            {
                playerInput.ClearVirtualMove();
                await WaitPhysicsFrames(8);

                Require(Mathf.Abs(player.GlobalPosition.Y - marker.GlobalPosition.Y) < 0.40f,
                    $"Player reached {marker.Name} horizontally but not at the expected traversable height. " +
                    $"Player={player.GlobalPosition}, Target={marker.GlobalPosition}.");
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
            $"Player could not reach traversal marker {marker.Name} within {maxFrames} physics frames. " +
            $"Player={player.GlobalPosition}, Target={marker.GlobalPosition}, " +
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
