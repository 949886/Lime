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
            ValidateLowerTerraceReplacement(gameRoot.ReferenceLevel);
            ValidateMiningApproachReplacement(gameRoot.ReferenceLevel);
            ValidateLowerCorridorReplacement(gameRoot.ReferenceLevel);
            ValidateRemainingCollisionSync(gameRoot.ReferenceLevel);
            ValidateRemainingStairTopology(gameRoot.ReferenceLevel);
            await ValidateStartCamera(gameRoot);
            await ValidateExploreCamera(gameRoot);

            GD.Print("[M1.6] PASS: calibration uses composed Start-to-lower-corridor replacements.");
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

        var playerStartLocal = level.ToLocal(level.PlayerStart.GlobalPosition);
        var rearLocal = level.ToLocal(rear.GlobalPosition);
        Require(playerStartLocal.Z > rearLocal.Z - rearMesh.Size.Z * 0.5f &&
                playerStartLocal.Z < rearLocal.Z + rearMesh.Size.Z * 0.5f,
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
        Require(level.GetNodeOrNull<Node3D>("ReferenceLevelVisuals/StartPlatformProps") is not null,
            "StartPlatformProps must remain part of the production composed scene.");
    }

    private static void ValidateLowerTerraceReplacement(ReferenceLevel level)
    {
        Require(level.GetNodeOrNull<Node3D>("Geometry/IntermediatePlatform") is null,
            "Legacy IntermediatePlatform visual must remain removed.");
        Require(level.GetNodeOrNull<CollisionShape3D>("Collision/WorldCollision/IntermediatePlatform") is null,
            "Legacy IntermediatePlatform collision must remain removed.");

        var terrace = level.GetNode<Node3D>("ReferenceLevelVisuals/StartLowerTerrace");
        var mainDeck = terrace.GetNode<MeshInstance3D>("Deck/MainDeck");
        var mainMesh = mainDeck.Mesh as BoxMesh
            ?? throw new InvalidOperationException("StartLowerTerrace Deck/MainDeck must use BoxMesh.");
        var mainCollision = terrace.GetNode<CollisionShape3D>("Collision/MainDeck");
        var mainShape = mainCollision.Shape as BoxShape3D
            ?? throw new InvalidOperationException("StartLowerTerrace Collision/MainDeck must use BoxShape3D.");
        Require(mainMesh.Size.Y < 0.5f,
            $"StartLowerTerrace must use a thin deck slab. Size={mainMesh.Size}.");
        Require(mainShape.Size.DistanceTo(mainMesh.Size) < 0.01f,
            "StartLowerTerrace main deck collision must match its visual.");
        var mainDeckLocal = level.ToLocal(mainDeck.GlobalPosition);
        Require(Mathf.Abs(mainDeckLocal.Y + mainMesh.Size.Y * 0.5f - 0.8f) < 0.01f,
            $"StartLowerTerrace top surface must preserve local Y=0.8. Actual={mainDeckLocal.Y + mainMesh.Size.Y * 0.5f:0.000}.");
    }

    private static void ValidateMiningApproachReplacement(ReferenceLevel level)
    {
        foreach (var path in new[]
                 {
                     "Geometry/Stairs02",
                     "Geometry/Check03BackPlatform",
                     "Geometry/Check03Projection",
                 })
        {
            Require(level.GetNodeOrNull<Node3D>(path) is null,
                $"Legacy mining approach node {path} must remain removed.");
        }
        foreach (var path in new[]
                 {
                     "Collision/WorldCollision/StairRamp02",
                     "Collision/WorldCollision/Check03BackPlatform",
                     "Collision/WorldCollision/Check03Projection",
                 })
        {
            Require(level.GetNodeOrNull<CollisionShape3D>(path) is null,
                $"Legacy mining approach collision {path} must remain removed.");
        }

        var approach = level.GetNode<Node3D>("ReferenceLevelVisuals/MiningApproachTerrace");
        var steps = approach.GetNode<Node3D>("Stair/Steps");
        ValidateStairCount(steps, "MiningApproachTerrace/Stair");
        var firstStep = steps.GetNode<MeshInstance3D>("Step01");
        var stepMesh = firstStep.Mesh as BoxMesh
            ?? throw new InvalidOperationException("MiningApproach Step01 must use BoxMesh.");
        Require(Mathf.Abs(stepMesh.Size.X - 3.6f) < 0.01f,
            $"MiningApproach stair width must remain 3.6m. Actual={stepMesh.Size.X:0.00}.");
        Require(approach.GetNodeOrNull<CollisionShape3D>("Collision/StairRamp") is not null,
            "MiningApproach stair must own its ramp collision.");

        var backDeck = approach.GetNode<MeshInstance3D>("Landing/BackDeck");
        var backMesh = backDeck.Mesh as BoxMesh
            ?? throw new InvalidOperationException("MiningApproach Landing/BackDeck must use BoxMesh.");
        var backShape = approach.GetNode<CollisionShape3D>("Collision/LandingBack").Shape as BoxShape3D
            ?? throw new InvalidOperationException("MiningApproach Collision/LandingBack must use BoxShape3D.");
        Require(backShape.Size.DistanceTo(backMesh.Size) < 0.01f,
            "MiningApproach landing-back collision must match visual.");

        var projection = approach.GetNode<MeshInstance3D>("Landing/ForwardProjection");
        var projectionMesh = projection.Mesh as BoxMesh
            ?? throw new InvalidOperationException("MiningApproach Landing/ForwardProjection must use BoxMesh.");
        var projectionShape = approach.GetNode<CollisionShape3D>("Collision/LandingProjection").Shape as BoxShape3D
            ?? throw new InvalidOperationException("MiningApproach Collision/LandingProjection must use BoxShape3D.");
        Require(projectionShape.Size.DistanceTo(projectionMesh.Size) < 0.01f,
            "MiningApproach projection collision must match visual.");
    }

    private static void ValidateLowerCorridorReplacement(ReferenceLevel level)
    {
        Require(level.GetNodeOrNull<Node3D>("Geometry/LowerCorridor") is null,
            "Legacy LowerCorridor visual must remain removed.");
        Require(level.GetNodeOrNull<CollisionShape3D>("Collision/WorldCollision/LowerCorridor") is null,
            "Legacy LowerCorridor collision must remain removed.");

        var corridor = level.GetNode<Node3D>("ReferenceLevelVisuals/LowerCorridorSection");
        foreach (var piece in new[] { "ApproachDeck", "MidDeck", "FarDeck" })
        {
            var meshNode = corridor.GetNode<MeshInstance3D>($"Deck/{piece}");
            var mesh = meshNode.Mesh as BoxMesh
                ?? throw new InvalidOperationException($"LowerCorridor Deck/{piece} must use BoxMesh.");
            var shape = corridor.GetNode<CollisionShape3D>($"Collision/{piece}").Shape as BoxShape3D
                ?? throw new InvalidOperationException($"LowerCorridor Collision/{piece} must use BoxShape3D.");
            Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
                $"LowerCorridor {piece} collision must match its visual.");
        }

        var mid = corridor.GetNode<MeshInstance3D>("Deck/MidDeck");
        var midMesh = (BoxMesh)mid.Mesh;
        var midLocal = level.ToLocal(mid.GlobalPosition);
        var routeLocal = level.ToLocal(level.RouteEnd.GlobalPosition);
        Require(Mathf.Abs(midLocal.Y + midMesh.Size.Y * 0.5f + 0.4f) < 0.01f,
            "LowerCorridor top surface must preserve local Y=-0.4.");
        Require(routeLocal.X >= midLocal.X - midMesh.Size.X * 0.5f &&
                routeLocal.X <= midLocal.X + midMesh.Size.X * 0.5f,
            "RouteEnd must stay on the new LowerCorridor MidDeck.");
    }

    private static void ValidateStartStair(Node3D stair, string label)
    {
        Require(stair.GetNodeOrNull<CollisionShape3D>("Collision/Ramp") is not null,
            $"{label} must own its ramp collision.");
        ValidateStairCount(stair.GetNode<Node3D>("StairVisual"), label);
    }

    private static void ValidateRemainingCollisionSync(ReferenceLevel level)
    {
        foreach (var name in new[] { "PlazaExtension", "LowerFrontPlatform" })
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
        ValidateStairCount(level.GetNode<Node3D>("Geometry/LowerFrontStairs"), "LowerFrontStairs");
    }

    private static void ValidateStairCount(Node3D stairs, string label)
    {
        var count = 0;
        foreach (var child in stairs.GetChildren())
        {
            if (child is MeshInstance3D step && child.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
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
