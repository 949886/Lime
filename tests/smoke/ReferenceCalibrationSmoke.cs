using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Camera;
using Lime.Game.World.Levels.Reference;
using PhantomCamera;

namespace Lime.Tests.Smoke;

public partial class ReferenceCalibrationSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");
            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await WaitFramesAsync(4);

            ValidateCharacterVisual(gameRoot);
            ValidateStartReplacement(gameRoot.ReferenceLevel);
            ValidateRemainingCollisionSync(gameRoot.ReferenceLevel);
            ValidateRemainingStairTopology(gameRoot.ReferenceLevel);
            await ValidateStartCamera(gameRoot);
            await ValidateExploreCamera(gameRoot);

            GD.Print("[M1.6] PASS: calibration uses the segmented dual-stair StartPlatform replacement.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6] FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateCharacterVisual(GameRoot gameRoot)
    {
        var characterVisual = gameRoot.Player.GetNode<Node3D>("VisualRoot/CharacterVisual");
        var sprite = characterVisual.GetNode<Sprite3D>("Sprite3D");
        Require(characterVisual.Scale.DistanceTo(Vector3.One) < 0.001f,
            $"CharacterVisual must stay at 1:1. Actual={characterVisual.Scale}.");
        Require(Mathf.Abs(sprite.PixelSize - 0.002f) < 0.00001f,
            "Character Sprite3D pixel size must remain 0.002.");
        Require(sprite.Offset.DistanceTo(new Vector2(0, 256)) < 0.01f,
            "Character Sprite3D offset must preserve feet-at-root alignment.");
    }

    private static void ValidateStartReplacement(ReferenceLevel level)
    {
        Require(level.GetNodeOrNull<Node3D>("Geometry/UpperPlatform") is null,
            "Legacy UpperPlatform must remain removed.");
        Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is null,
            "Legacy Stairs01 must remain removed.");

        var startPlatform = level.GetNode<Node3D>("ReferenceLevelVisuals/StartPlatform");
        var deck = startPlatform.GetNode<Node3D>("Deck");
        var rear = deck.GetNode<MeshInstance3D>("RearDeck");
        var rearMesh = rear.Mesh as BoxMesh
            ?? throw new InvalidOperationException("StartPlatform Deck/RearDeck must use BoxMesh.");
        var rearCollision = startPlatform.GetNode<CollisionShape3D>("Collision/RearDeck");
        var rearShape = rearCollision.Shape as BoxShape3D
            ?? throw new InvalidOperationException("StartPlatform Collision/RearDeck must use BoxShape3D.");

        Require(rearMesh.Size.X >= 19.5f && rearMesh.Size.Z > 5.5f,
            $"Detailed Start rear plaza must retain the broad reference silhouette. Size={rearMesh.Size}.");
        Require(rearShape.Size.DistanceTo(rearMesh.Size) < 0.01f,
            "RearDeck collision must match the rear plaza mesh.");
        Require(level.PlayerStart.GlobalPosition.Z > rear.GlobalPosition.Z - rearMesh.Size.Z * 0.5f &&
                level.PlayerStart.GlobalPosition.Z < rear.GlobalPosition.Z + rearMesh.Size.Z * 0.5f,
            "PlayerStart must remain on the continuous rear plaza, not inside a stair cutout.");

        foreach (var piece in new[] { "FrontRightDeck", "FrontCenterDeck", "FrontLeftDeck" })
        {
            Require(deck.GetNodeOrNull<MeshInstance3D>(piece) is not null,
                $"StartPlatform must preserve segmented front deck piece {piece}.");
            Require(startPlatform.GetNodeOrNull<CollisionShape3D>($"Collision/{piece}") is not null,
                $"StartPlatform must preserve segmented collision for {piece}.");
        }

        var right = startPlatform.GetNode<Node3D>("RightStair");
        var left = startPlatform.GetNode<Node3D>("LeftStair");
        ValidateStartStair(right, "RightStair");
        ValidateStartStair(left, "LeftStair");
        Require(left.Position.X - right.Position.X > 8.0f,
            "Reference dual-stair spacing must remain wider than 8m in this pass.");
        Require(startPlatform.GetNodeOrNull<MeshInstance3D>("FrontArchitecture/CenterRetainingWall") is not null,
            "StartPlatform must preserve the central retaining wall between stair openings.");
        Require(level.GetNodeOrNull<Node3D>("ReferenceLevelVisuals/StartPlatformProps") is not null,
            "StartPlatformProps must remain part of the production composed scene.");
    }

    private static void ValidateStartStair(Node3D stair, string label)
    {
        Require(stair.GetNodeOrNull<CollisionShape3D>("Collision/Ramp") is not null,
            $"{label} must own its ramp collision.");
        var visual = stair.GetNode<Node3D>("StairVisual");
        var count = 0;
        foreach (var child in visual.GetChildren())
        {
            if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
                count++;
        }
        Require(count == ReferenceLevel.ReferenceStairStepCount,
            $"{label} must contain {ReferenceLevel.ReferenceStairStepCount} steps. Actual={count}.");
    }

    private static void ValidateRemainingCollisionSync(ReferenceLevel level)
    {
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
            var meshNode = level.GetNode<MeshInstance3D>($"Geometry/{name}");
            var mesh = meshNode.Mesh as BoxMesh
                ?? throw new InvalidOperationException($"Geometry/{name} must use BoxMesh.");
            var collision = level.GetNode<CollisionShape3D>($"Collision/WorldCollision/{name}");
            var shape = collision.Shape as BoxShape3D
                ?? throw new InvalidOperationException($"Collision/WorldCollision/{name} must use BoxShape3D.");
            Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
                $"{name} collision must follow its current graybox visual.");
        }
    }

    private static void ValidateRemainingStairTopology(ReferenceLevel level)
    {
        ValidateStairCount(level.GetNode<Node3D>("Geometry/Stairs02"), "Stairs02");
        ValidateStairCount(level.GetNode<Node3D>("Geometry/LowerFrontStairs"), "LowerFrontStairs");
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

    private async Task ValidateStartCamera(GameRoot gameRoot)
    {
        gameRoot.Player.GlobalPosition = gameRoot.ReferenceLevel.PlayerStart.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;
        gameRoot.CameraDirector.ActivateInstant(CameraId.StartPerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        var startPcam = gameRoot.CameraDirector
            .GetNode<Node3D>("PCams/StartPerspective")
            .AsPhantomCamera3D();

        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
            "Start must use StartPerspective.");
        Require((int)camera.Projection == 0 && Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "StartPerspective must remain a 45 degree perspective camera.");
        Require(startPcam.FollowOffset.DistanceTo(new Vector3(-0.016825f, 2.296143f, -2.502186f)) < 0.001f,
            $"StartPerspective must keep the solved framing offset. Actual={startPcam.FollowOffset}.");
        Require(startPcam.Node3D.RotationDegrees.DistanceTo(new Vector3(-32.02f, 179.79f, 0)) < 0.02f,
            $"StartPerspective must keep the solved orientation. Actual={startPcam.Node3D.RotationDegrees}.");
    }

    private async Task ValidateExploreCamera(GameRoot gameRoot)
    {
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);
        var camera = gameRoot.CameraDirector.RenderCamera;
        Require((int)camera.Projection == 0 && Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "ExplorePerspective must remain a 45 degree perspective camera.");

        var explorePcam = gameRoot.CameraDirector
            .GetNode<Node3D>("PCams/ExplorePerspective")
            .AsPhantomCamera3D();
        Require(Mathf.Abs(explorePcam.TweenDuration - 1.5f) < 0.01f,
            "Start-to-Explore pullback must keep the 1.5 second transition.");
    }

    private async Task WaitFramesAsync(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
