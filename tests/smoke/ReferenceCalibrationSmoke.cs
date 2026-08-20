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
            ValidateSceneReplacements(gameRoot.ReferenceLevel);
            ValidatePoolTerrace(gameRoot.ReferenceLevel);
            ValidateRemainingGraybox(gameRoot.ReferenceLevel);
            await ValidateStartCamera(gameRoot);
            await ValidateExploreCamera(gameRoot);

            GD.Print("[M1.6] PASS: calibration uses composed Start-to-pool replacements.");
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

    private static void ValidateSceneReplacements(ReferenceLevel level)
    {
        foreach (var path in new[]
        {
            "Geometry/UpperPlatform", "Geometry/Stairs01", "Geometry/IntermediatePlatform",
            "Geometry/Stairs02", "Geometry/Check03BackPlatform", "Geometry/Check03Projection",
            "Geometry/LowerCorridor", "Geometry/PlazaExtension", "Geometry/WaterPool",
        })
            Require(level.GetNodeOrNull<Node3D>(path) is null,
                $"Legacy replaced node {path} must remain removed.");

        foreach (var path in new[]
        {
            "Collision/WorldCollision/IntermediatePlatform", "Collision/WorldCollision/StairRamp02",
            "Collision/WorldCollision/Check03BackPlatform", "Collision/WorldCollision/Check03Projection",
            "Collision/WorldCollision/LowerCorridor", "Collision/WorldCollision/PlazaExtension",
        })
            Require(level.GetNodeOrNull<CollisionShape3D>(path) is null,
                $"Legacy replaced collision {path} must remain removed.");

        var start = level.GetNode<Node3D>("ReferenceLevelVisuals/StartPlatform");
        Require(start.GetNodeOrNull<CollisionShape3D>("Collision/RearDeck") is not null,
            "StartPlatform must keep its own collision.");

        var lower = level.GetNode<Node3D>("ReferenceLevelVisuals/StartLowerTerrace");
        ValidateBoxPair(lower, "Deck/MainDeck", "Collision/MainDeck", "StartLowerTerrace MainDeck");

        var mining = level.GetNode<Node3D>("ReferenceLevelVisuals/MiningApproachTerrace");
        Require(mining.GetNodeOrNull<CollisionShape3D>("Collision/StairRamp") is not null,
            "MiningApproach stair must own its ramp collision.");
        ValidateBoxPair(mining, "Landing/BackDeck", "Collision/LandingBack", "MiningApproach BackDeck");
        ValidateBoxPair(mining, "Landing/ForwardProjection", "Collision/LandingProjection", "MiningApproach Projection");

        var corridor = level.GetNode<Node3D>("ReferenceLevelVisuals/LowerCorridorSection");
        foreach (var piece in new[] { "ApproachDeck", "MidDeck", "FarDeck" })
            ValidateBoxPair(corridor, $"Deck/{piece}", $"Collision/{piece}", $"LowerCorridor {piece}");

        var routeLocal = level.ToLocal(level.RouteEnd.GlobalPosition);
        var mid = corridor.GetNode<MeshInstance3D>("Deck/MidDeck");
        var midMesh = (BoxMesh)mid.Mesh;
        var midLocal = level.ToLocal(mid.GlobalPosition);
        Require(routeLocal.X >= midLocal.X - midMesh.Size.X * 0.5f &&
                routeLocal.X <= midLocal.X + midMesh.Size.X * 0.5f,
            "RouteEnd must stay on composed LowerCorridor MidDeck.");
    }

    private static void ValidatePoolTerrace(ReferenceLevel level)
    {
        var pool = level.GetNode<Node3D>("ReferenceLevelVisuals/PoolTerrace");
        foreach (var piece in new[] { "RearDeck", "FrontDeck", "ScreenRightDeck", "ScreenLeftDeck" })
            ValidateBoxPair(pool, $"Deck/{piece}", $"Collision/{piece}", $"PoolTerrace {piece}");

        var water = pool.GetNode<MeshInstance3D>("Pool/Water");
        Require(water.Mesh is BoxMesh, "PoolTerrace water must be an explicit mesh resource.");
        Require(pool.GetNodeOrNull<CollisionShape3D>("Collision/Water") is null,
            "Pool opening must stay non-walkable instead of restoring a full plaza collision.");
        Require(pool.GetNodeOrNull<MeshInstance3D>("Pool/RearRim") is not null &&
                pool.GetNodeOrNull<MeshInstance3D>("Pool/FrontRim") is not null &&
                pool.GetNodeOrNull<Node3D>("Rails") is not null,
            "PoolTerrace must own pool rim and rail structure.");
    }

    private static void ValidateRemainingGraybox(ReferenceLevel level)
    {
        var platform = level.GetNode<MeshInstance3D>("Geometry/LowerFrontPlatform");
        var mesh = platform.Mesh as BoxMesh
            ?? throw new InvalidOperationException("LowerFrontPlatform must use BoxMesh until replacement.");
        var shape = level.GetNode<CollisionShape3D>("Collision/WorldCollision/LowerFrontPlatform").Shape as BoxShape3D
            ?? throw new InvalidOperationException("LowerFrontPlatform collision must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
            "Remaining LowerFrontPlatform graybox collision must still match visual.");

        var stairs = level.GetNode<Node3D>("Geometry/LowerFrontStairs");
        var count = 0;
        foreach (var child in stairs.GetChildren())
            if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal)) count++;
        Require(count == ReferenceLevel.ReferenceStairStepCount,
            $"LowerFrontStairs must keep 12 steps until replacement. Actual={count}.");
    }

    private static void ValidateBoxPair(Node3D root, string meshPath, string collisionPath, string label)
    {
        var meshNode = root.GetNode<MeshInstance3D>(meshPath);
        var mesh = meshNode.Mesh as BoxMesh
            ?? throw new InvalidOperationException($"{label} must use BoxMesh.");
        var shape = root.GetNode<CollisionShape3D>(collisionPath).Shape as BoxShape3D
            ?? throw new InvalidOperationException($"{label} collision must use BoxShape3D.");
        Require(shape.Size.DistanceTo(mesh.Size) < 0.01f,
            $"{label} collision must match visual.");
    }

    private async Task ValidateStartCamera(GameRoot gameRoot)
    {
        gameRoot.Player.GlobalPosition = gameRoot.ReferenceLevel.PlayerStart.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;
        gameRoot.CameraDirector.ActivateInstant(CameraId.StartPerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        var startPcam = gameRoot.CameraDirector.GetNode<Node3D>("PCams/StartPerspective").AsPhantomCamera3D();
        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
            "Start must use StartPerspective.");
        Require((int)camera.Projection == 0 && Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "StartPerspective must remain a 45 degree perspective camera.");
        Require(startPcam.FollowOffset.DistanceTo(new Vector3(-0.016825f, 2.296143f, -2.502186f)) < 0.001f,
            $"StartPerspective must keep solved framing offset. Actual={startPcam.FollowOffset}.");
        Require(startPcam.Node3D.RotationDegrees.DistanceTo(new Vector3(-32.02f, 179.79f, 0)) < 0.02f,
            $"StartPerspective must keep solved orientation. Actual={startPcam.Node3D.RotationDegrees}.");
    }

    private async Task ValidateExploreCamera(GameRoot gameRoot)
    {
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);
        var camera = gameRoot.CameraDirector.RenderCamera;
        Require((int)camera.Projection == 0 && Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "ExplorePerspective must remain a 45 degree perspective camera.");
        var explorePcam = gameRoot.CameraDirector.GetNode<Node3D>("PCams/ExplorePerspective").AsPhantomCamera3D();
        Require(Mathf.Abs(explorePcam.TweenDuration - 1.5f) < 0.01f,
            "Start-to-Explore pullback must keep 1.5 second transition.");
    }

    private async Task WaitFramesAsync(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
