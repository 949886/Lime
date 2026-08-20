using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Camera;
using Lime.Game.World.Levels.Reference;
using PhantomCamera;

namespace Lime.Tests.Smoke;

public partial class ReferenceCalibrationSmoke : Node
{
    private const float Check03AnchorTolerance = 0.035f;
    private readonly List<string> _anchorFailures = [];

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
            ValidateCollisionSync(gameRoot.ReferenceLevel);
            ValidateStairDensityAndTopology(gameRoot.ReferenceLevel);
            await ValidateStartCamera(gameRoot);
            await ValidateExploreCamera(gameRoot);
            await ValidateCheck03Framing(gameRoot);
            await ValidateProjectionAb(gameRoot);
            ValidateAnchorBudget();

            GD.Print("[M1.6] PASS: Reference calibration smoke completed successfully.");
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
            $"CharacterVisual is the world-scale ruler and must stay at 1:1. Actual={characterVisual.Scale}.");
        Require(Mathf.Abs(sprite.PixelSize - 0.002f) < 0.00001f,
            "Character Sprite3D pixel size must remain 0.002.");
        Require(sprite.Offset.DistanceTo(new Vector2(0.0f, 256.0f)) < 0.01f,
            "Character Sprite3D offset must preserve feet-at-root alignment.");
    }

    private static void ValidateCollisionSync(ReferenceLevel level)
    {
        foreach (var name in new[]
                 {
                     "UpperPlatform",
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
                $"{name} collision must copy the current editor mesh size. Mesh={mesh.Size}, Shape={shape.Size}.");
            Require(collision.GlobalPosition.DistanceTo(meshNode.GlobalPosition) < 0.01f,
                $"{name} collision must copy the current editor mesh position.");
        }
    }

    private static void ValidateStairDensityAndTopology(ReferenceLevel level)
    {
        var stairs01 = GetSteps(level.GetNode<Node3D>("Geometry/Stairs01"));
        var stairs02 = GetSteps(level.GetNode<Node3D>("Geometry/Stairs02"));
        var sideStairs = GetSteps(level.GetNode<Node3D>("Geometry/LowerFrontStairs"));

        Require(stairs01.Count == ReferenceLevel.ReferenceStairStepCount,
            $"Stairs01 must use {ReferenceLevel.ReferenceStairStepCount} visible steps. Actual={stairs01.Count}.");
        Require(stairs02.Count == ReferenceLevel.ReferenceStairStepCount,
            $"Stairs02 must use {ReferenceLevel.ReferenceStairStepCount} visible steps. Actual={stairs02.Count}.");
        Require(sideStairs.Count == ReferenceLevel.ReferenceStairStepCount,
            $"Check03 side stair must use {ReferenceLevel.ReferenceStairStepCount} visible steps. Actual={sideStairs.Count}.");

        Require(stairs02[^1].GlobalPosition.Y < stairs02[0].GlobalPosition.Y - 0.9f,
            "Stairs02 must descend from the intermediate level into Check03.");
        Require(sideStairs[^1].GlobalPosition.X > sideStairs[0].GlobalPosition.X + 0.9f,
            "Check03 side stair must face world +X / screen-left with a meaningful span.");
        Require(sideStairs[^1].GlobalPosition.Y < sideStairs[0].GlobalPosition.Y - 0.9f,
            "Check03 side stair must descend to the lower-left landing.");
        Require(Mathf.Abs(sideStairs[^1].GlobalPosition.Z - sideStairs[0].GlobalPosition.Z) < 0.25f,
            "Check03 side stair must be lateral rather than another camera-facing stair.");

        var back = level.GetNode<MeshInstance3D>("Geometry/Check03BackPlatform");
        var projection = level.GetNode<MeshInstance3D>("Geometry/Check03Projection");
        var backBox = (BoxMesh)back.Mesh;
        var projectionBox = (BoxMesh)projection.Mesh;

        Require(projectionBox.Size.X < backBox.Size.X,
            "Check03 front mass must form a narrower downward projection.");
        Require(projection.GlobalPosition.Z < back.GlobalPosition.Z,
            "Check03 projection must extend toward the camera.");
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
            "8.5s Start must use StartPerspective.");
        Require((int)camera.Projection == 0 && Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "StartPerspective must remain a 45 degree perspective camera until the apparent-height solve.");

        var configuredOffset = startPcam.FollowOffset;
        Require(configuredOffset.DistanceTo(new Vector3(-0.016825f, 2.296143f, -2.502186f)) < 0.001f,
            $"StartPerspective must keep the solved framing offset. Actual={configuredOffset}.");
        Require(startPcam.Node3D.RotationDegrees.DistanceTo(new Vector3(-32.02f, 179.79f, 0.0f)) < 0.02f,
            $"StartPerspective must keep the solved orientation. Actual={startPcam.Node3D.RotationDegrees}.");

        Require(camera.GlobalRotationDegrees.DistanceTo(new Vector3(-32.02f, 179.79f, 0.0f)) < 0.1f,
            $"RenderCamera must receive the solved Start orientation. Actual={camera.GlobalRotationDegrees}.");
    }

    private async Task ValidateExploreCamera(GameRoot gameRoot)
    {
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
            "ExplorePerspective must be the post-pullback production camera.");
        Require((int)camera.Projection == 0 && Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "ExplorePerspective must remain a 45 degree perspective camera.");

        var offset = camera.GlobalPosition - gameRoot.Player.GlobalPosition;
        Require(offset.DistanceTo(new Vector3(0.0f, 6.0f, -12.0f)) < 0.05f,
            $"Explore camera offset must remain (0, 6, -12) during this Check03 geometry pass. Actual={offset}.");
        Require(Mathf.Abs(camera.GlobalRotationDegrees.X - (-22.0f)) < 0.1f,
            "Explore camera pitch must remain -22 degrees during this Check03 geometry pass.");

        var explorePcam = gameRoot.CameraDirector
            .GetNode<Node3D>("PCams/ExplorePerspective")
            .AsPhantomCamera3D();
        Require(Mathf.Abs(explorePcam.TweenDuration - 1.5f) < 0.01f,
            "Start-to-Explore pullback must keep the 1.5 second transition.");
    }

    private async Task ValidateCheck03Framing(GameRoot gameRoot)
    {
        var level = gameRoot.ReferenceLevel;
        gameRoot.Player.GlobalPosition = level.CameraCheck03.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        var rearStairs = GetSteps(level.GetNode<Node3D>("Geometry/Stairs02"));
        var sideStairs = GetSteps(level.GetNode<Node3D>("Geometry/LowerFrontStairs"));
        var projection = level.GetNode<MeshInstance3D>("Geometry/Check03Projection");

        MeasureNormalizedAnchor(camera, level.CameraCheck03.GlobalPosition,
            new Vector2(0.500f, 0.596f), "Check03 player feet", Check03AnchorTolerance);
        MeasureNormalizedAnchor(camera, rearStairs[0].GlobalPosition,
            new Vector2(0.500f, 0.406f), "Check03 rear stair top", Check03AnchorTolerance);
        MeasureNormalizedAnchor(camera, projection.GlobalPosition,
            new Vector2(0.500f, 0.815f), "Check03 forward projection", Check03AnchorTolerance);
        MeasureNormalizedAnchor(camera, sideStairs[0].GlobalPosition,
            new Vector2(0.357f, 0.714f), "Check03 side stair top", Check03AnchorTolerance);
        MeasureNormalizedAnchor(camera, sideStairs[^1].GlobalPosition,
            new Vector2(0.251f, 0.820f), "Check03 side stair bottom", Check03AnchorTolerance);

        var sideTopScreen = ProjectNormalized(camera, sideStairs[0].GlobalPosition);
        var sideBottomScreen = ProjectNormalized(camera, sideStairs[^1].GlobalPosition);
        Require(sideBottomScreen.X < sideTopScreen.X && sideBottomScreen.Y > sideTopScreen.Y,
            "Check03 side stair must visibly run down-left in screen space.");
    }

    private async Task ValidateProjectionAb(GameRoot gameRoot)
    {
        var checkpoint = gameRoot.ReferenceLevel.CameraCheck02;
        gameRoot.Player.GlobalPosition = checkpoint.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;

        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(2);
        var perspectivePoint = ProjectNormalized(gameRoot.CameraDirector.RenderCamera, checkpoint.GlobalPosition);

        gameRoot.CameraDirector.ActivateInstant(CameraId.ExploreOrthographic);
        await WaitFramesAsync(2);
        var camera = gameRoot.CameraDirector.RenderCamera;
        var orthographicPoint = ProjectNormalized(camera, checkpoint.GlobalPosition);

        Require((int)camera.Projection == 1,
            "Orthographic A/B camera must apply orthographic projection.");
        Require(Mathf.Abs(camera.Size - 11.1f) < 0.01f,
            "Orthographic A/B size must remain 11.1.");
        Require(perspectivePoint.DistanceTo(orthographicPoint) < 0.01f,
            "Perspective and Orthographic A/B must preserve target-plane framing.");

        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(2);
    }

    private void MeasureNormalizedAnchor(
        Camera3D camera,
        Vector3 worldPosition,
        Vector2 expected,
        string label,
        float tolerance)
    {
        var actual = ProjectNormalized(camera, worldPosition);
        var error = actual.DistanceTo(expected);
        GD.Print($"[M1.6] {label}: expected={expected}, actual={actual}, error={error:0.0000}");

        if (error > tolerance)
        {
            _anchorFailures.Add(
                $"{label}: expected={expected}, actual={actual}, error={error:0.0000}, tolerance={tolerance:0.000}");
        }
    }

    private void ValidateAnchorBudget()
    {
        if (_anchorFailures.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{_anchorFailures.Count} Check03 calibration anchor(s) exceeded tolerance:\n- " +
            string.Join("\n- ", _anchorFailures));
    }

    private static Vector2 ProjectNormalized(Camera3D camera, Vector3 worldPosition)
    {
        var viewportSize = camera.GetViewport().GetVisibleRect().Size;
        Require(viewportSize.X > 0.0f && viewportSize.Y > 0.0f,
            "Calibration smoke requires a non-zero viewport size.");
        var pixel = camera.UnprojectPosition(worldPosition);
        return new Vector2(pixel.X / viewportSize.X, pixel.Y / viewportSize.Y);
    }

    private static List<MeshInstance3D> GetSteps(Node3D stairs)
    {
        var result = new List<MeshInstance3D>();
        foreach (var child in stairs.GetChildren())
        {
            if (child is MeshInstance3D step && child.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
            {
                result.Add(step);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Name.ToString(), right.Name.ToString()));
        return result;
    }

    private async Task WaitFramesAsync(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
