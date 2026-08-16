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
            playerInput = player.GetNode<PlayerInput>("%PlayerInput");

            ValidateLevelContract(referenceLevel);
            ValidateSpawnContract(gameRoot);

            var cameraStart = gameRoot.CameraDirector.RenderCamera.GlobalPosition;

            await DriveToMarker(player, playerInput, gameRoot.CameraDirector.RenderCamera,
                referenceLevel.CameraCheck01, 240);
            await DriveToMarker(player, playerInput, gameRoot.CameraDirector.RenderCamera,
                referenceLevel.CameraCheck02, 280);
            await DriveToMarker(player, playerInput, gameRoot.CameraDirector.RenderCamera,
                referenceLevel.CameraCheck03, 220);
            await DriveToMarker(player, playerInput, gameRoot.CameraDirector.RenderCamera,
                referenceLevel.RouteEnd, 380);

            playerInput.ClearVirtualMove();
            await WaitPhysicsFrames(12);

            Require(player.IsOnFloor(),
                "Player must remain grounded at RouteEnd.");
            Require(HorizontalDistance(player.GlobalPosition, referenceLevel.RouteEnd.GlobalPosition) < 0.55f,
                "Player must be able to traverse the reference graybox from PlayerStart to RouteEnd.");
            Require(Mathf.Abs(player.GlobalPosition.Y - referenceLevel.RouteEnd.GlobalPosition.Y) < 0.35f,
                "RouteEnd marker height must match the traversable pool-plaza level.");
            Require(gameRoot.CameraDirector.RenderCamera.GlobalPosition.DistanceTo(cameraStart) > 8.0f,
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
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/UpperPlatform") is not null,
            "Reference graybox must include the starting upper platform.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/IntermediatePlatform") is not null,
            "Reference graybox must include the intermediate lateral terrace.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/LowerLanding") is not null,
            "Reference graybox must include the lower landing after the second descent.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/LowerConcourse") is not null,
            "Reference graybox must include the lower concourse.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/PoolPlaza") is not null,
            "Reference graybox must include the pool/NPC plaza extension.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/BackWalkway") is not null,
            "Reference graybox must include the raised walkway behind the pool.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is not null,
            "Reference graybox must include first stair visuals.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs02") is not null,
            "Reference graybox must include second stair visuals.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/PoolStairs") is not null,
            "Reference graybox must keep the pool-area staircase separate from the two main descents.");
        Require(level.GetNodeOrNull<MeshInstance3D>("Geometry/ForegroundBlocker") is not null,
            "Reference graybox must include a foreground blocker volume.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Rails") is not null,
            "Reference graybox must include rail silhouettes.");

        var worldCollision = level.GetNode<StaticBody3D>("Collision/WorldCollision");
        Require(worldCollision.CollisionLayer == CollisionLayers.WorldMask,
            "ReferenceLevel static collision must use World layer 1.");
        Require(worldCollision.CollisionMask == 0,
            "Static reference World collision does not need a collision mask.");

        var stairRamp01 = worldCollision.GetNode<CollisionShape3D>("StairRamp01");
        var stairRamp02 = worldCollision.GetNode<CollisionShape3D>("StairRamp02");
        Require(worldCollision.GetNodeOrNull<CollisionShape3D>("PoolStairRamp") is not null,
            "Pool-area stair visuals must use a separate hidden ramp collision.");

        Require(Mathf.Abs(stairRamp02.GlobalPosition.X - stairRamp01.GlobalPosition.X) > 3.0f,
            "The two main stair runs must be laterally staggered instead of sharing one camera-facing centerline.");
        Require(level.PlayerStart.GlobalPosition.Y > level.CameraCheck01.GlobalPosition.Y + 1.0f,
            "The first stair run must descend from the starting upper platform.");
        Require(Mathf.Abs(level.CameraCheck01.GlobalPosition.Y - level.CameraCheck02.GlobalPosition.Y) < 0.15f,
            "CameraCheck01 and CameraCheck02 must remain on the same intermediate terrace.");
        Require(Mathf.Abs(level.CameraCheck02.GlobalPosition.X - level.CameraCheck01.GlobalPosition.X) > 3.0f,
            "The intermediate terrace must carry the route laterally between the two stair runs.");
        Require(level.CameraCheck02.GlobalPosition.Y > level.CameraCheck03.GlobalPosition.Y + 1.0f,
            "The second main stair run must continue descending instead of restoring elevation.");
        Require(Mathf.Abs(level.CameraCheck03.GlobalPosition.Y - level.RouteEnd.GlobalPosition.Y) < 0.15f,
            "The lower route and pool-plaza endpoint must remain on the same traversal level.");
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
