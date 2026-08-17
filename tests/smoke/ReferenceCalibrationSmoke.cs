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
    private const float GeometryAnchorTolerance = 0.015f;
    private const float StartAnchorTolerance = 0.025f;
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
            ValidateCalibratedGeometry(gameRoot.ReferenceLevel);
            await ValidateStartFraming(gameRoot);
            await ValidateExploreCamera(gameRoot);
            await ValidatePerspectiveAnchors(gameRoot);
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
            $"CharacterVisual is the M1.6 world-scale ruler and must stay at 1:1. Actual={characterVisual.Scale}.");
        Require(Mathf.Abs(sprite.PixelSize - 0.002f) < 0.00001f,
            "Character Sprite3D pixel size must remain 0.002; apparent size is calibrated with camera/world scale.");
        Require(sprite.Offset.DistanceTo(new Vector2(0.0f, 256.0f)) < 0.01f,
            "Character Sprite3D offset must preserve feet-at-root alignment.");
    }

    private static void ValidateCalibratedGeometry(ReferenceLevel level)
    {
        var upper = level.GetNode<MeshInstance3D>("Geometry/UpperPlatform");
        var upperCollision = level.GetNode<CollisionShape3D>("Collision/WorldCollision/UpperPlatform");
        var intermediate = level.GetNode<MeshInstance3D>("Geometry/IntermediatePlatform");
        var intermediateCollision = level.GetNode<CollisionShape3D>("Collision/WorldCollision/IntermediatePlatform");
        var stair01Top = level.GetNode<MeshInstance3D>("Geometry/Stairs01/Step01");
        var stair02Top = level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01");
        var stairRamp01 = level.GetNode<CollisionShape3D>("Collision/WorldCollision/StairRamp01");
        var cylinder01 = level.GetNode<MeshInstance3D>("Geometry/CylinderBuilding01");
        var cylinder02 = level.GetNode<MeshInstance3D>("Geometry/CylinderBuilding02");
        var foregroundBlocker = level.GetNode<MeshInstance3D>("Geometry/ForegroundBlocker");

        Require(upper.GlobalPosition.DistanceTo(new Vector3(1.4f, 1.85f, 3.0f)) < 0.01f,
            "UpperPlatform must use the Start-reference center on the first stair axis.");
        Require(upper.Mesh is BoxMesh upperBox &&
                upperBox.Size.DistanceTo(new Vector3(8.0f, 0.3f, 4.0f)) < 0.01f,
            "UpperPlatform must use the measured 8x4m Start footprint.");
        Require(upperCollision.Shape is BoxShape3D upperShape &&
                upperShape.Size.DistanceTo(new Vector3(8.0f, 0.3f, 4.0f)) < 0.01f,
            "UpperPlatform collision must match the 8x4m visual footprint.");
        Require(upperCollision.GlobalPosition.DistanceTo(upper.GlobalPosition) < 0.01f,
            "UpperPlatform collision must share the calibrated visual center.");
        Require(level.PlayerStart.GlobalPosition.DistanceTo(new Vector3(1.4f, 2.02f, 1.65f)) < 0.01f,
            "PlayerStart must sit 0.65m behind the first stair on its centerline.");

        Require(intermediate.GlobalPosition.DistanceTo(new Vector3(0.0f, 0.65f, -4.5f)) < 0.01f,
            "IntermediatePlatform must keep the cross-checkpoint calibrated centre.");
        Require(intermediate.Mesh is BoxMesh intermediateBox &&
                intermediateBox.Size.DistanceTo(new Vector3(8.0f, 0.3f, 5.0f)) < 0.01f,
            "IntermediatePlatform must keep the 8x5m footprint that matches Check02.");
        Require(intermediateCollision.Shape is BoxShape3D intermediateShape &&
                intermediateShape.Size.DistanceTo(new Vector3(8.0f, 0.3f, 5.0f)) < 0.01f,
            "IntermediatePlatform collision must match the visual footprint.");

        Require(stair01Top.Mesh is BoxMesh stepBox && Mathf.Abs(stepBox.Size.X - 3.0f) < 0.01f,
            "Reference stair width must stay at the player-relative 3.0m baseline.");
        Require(stairRamp01.Shape is BoxShape3D rampShape && Mathf.Abs(rampShape.Size.X - 3.0f) < 0.01f,
            "Stair ramp collision width must match the 3.0m visual stair width.");
        Require(stair01Top.GlobalPosition.X > 0.0f && stair02Top.GlobalPosition.X < 0.0f,
            "Reference route must place Stairs01 screen-left/world +X and Stairs02 screen-right/world -X.");

        Require(Mathf.Abs(level.CameraCheck01.GlobalPosition.X - 1.4f) < 0.01f,
            "CameraCheck01 must remain at the calibrated bottom of Stairs01.");
        Require(Mathf.Abs(level.CameraCheck02.GlobalPosition.X) < 0.01f,
            "CameraCheck02 must remain centered on the intermediate platform.");
        Require(Mathf.Abs(level.CameraCheck03.GlobalPosition.X - (-1.4f)) < 0.01f,
            "CameraCheck03 must remain at the calibrated bottom of Stairs02.");

        Require(cylinder01.GlobalPosition.X > 0.0f && cylinder02.GlobalPosition.X < 0.0f,
            "The two cylindrical masses must straddle the reference route instead of occupying the same screen side.");
        Require(Mathf.Abs(cylinder01.GlobalPosition.Z - (-7.5f)) < 0.01f &&
                Mathf.Abs(cylinder02.GlobalPosition.Z - (-7.5f)) < 0.01f,
            "The cylindrical masses must keep the calibrated shared depth near the intermediate/lower route.");
        Require(foregroundBlocker.GlobalPosition.DistanceTo(new Vector3(-1.68f, 0.0f, -15.0f)) < 0.01f,
            "Foreground blocker must preserve the Check03 lower-centre occlusion calibration.");
    }

    private async Task ValidateStartFraming(GameRoot gameRoot)
    {
        var level = gameRoot.ReferenceLevel;
        gameRoot.Player.GlobalPosition = level.PlayerStart.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;
        gameRoot.CameraDirector.ActivateInstant(CameraId.StartPerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.StartPerspective,
            "The 8.50s Start checkpoint must use the close StartPerspective camera.");
        Require((int)camera.Projection == 0,
            "StartPerspective must remain a perspective camera.");
        Require(Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "StartPerspective FOV must stay at 45 degrees.");

        var offset = camera.GlobalPosition - gameRoot.Player.GlobalPosition;
        Require(offset.DistanceTo(new Vector3(0.0f, 1.5f, -3.0f)) < 0.05f,
            $"Start camera offset must match the 8.5s player-size solve (0, 1.5, -3). Actual={offset}.");
        Require(Mathf.Abs(camera.GlobalRotationDegrees.X - (-16.0f)) < 0.1f,
            $"Start camera pitch must place the player's feet at the 8.5s reference height. Actual={camera.GlobalRotationDegrees.X}.");

        MeasureNormalizedAnchor(camera, level.PlayerStart.GlobalPosition,
            new Vector2(0.500f, 0.725f), "Start Player feet", StartAnchorTolerance);

        // Freeze the visible 8.5s platform silhouette rather than accepting a
        // single centre-point match. UpperPlatform top is Y=2.0, front Z=1 and
        // back Z=5 after the 8x4m calibration.
        MeasureNormalizedAnchor(camera, new Vector3(1.4f, 2.0f, 1.0f),
            new Vector2(0.500f, 0.867f), "Start platform front edge", StartAnchorTolerance);
        MeasureNormalizedAnchor(camera, new Vector3(5.4f, 2.0f, 5.0f),
            new Vector2(0.086f, 0.446f), "Start platform back screen-left", StartAnchorTolerance);
        MeasureNormalizedAnchor(camera, new Vector3(-2.6f, 2.0f, 5.0f),
            new Vector2(0.914f, 0.446f), "Start platform back screen-right", StartAnchorTolerance);
    }

    private async Task ValidateExploreCamera(GameRoot gameRoot)
    {
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
            "ExplorePerspective must be the post-pullback production camera.");
        Require((int)camera.Projection == 0,
            "ExplorePerspective must remain a perspective camera.");
        Require(Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "ExplorePerspective FOV must stay at the calibrated 45 degree baseline.");

        var offset = camera.GlobalPosition - gameRoot.Player.GlobalPosition;
        Require(offset.DistanceTo(new Vector3(0.0f, 6.0f, -12.0f)) < 0.05f,
            $"Explore camera follow offset must remain (0, 6, -12). Actual={offset}.");
        Require(Mathf.Abs(camera.GlobalRotationDegrees.X - (-22.0f)) < 0.1f,
            $"Explore camera pitch must remain -22 degrees. Actual={camera.GlobalRotationDegrees.X}.");

        var explorePcam = gameRoot.CameraDirector
            .GetNode<Node3D>("PCams/ExplorePerspective")
            .AsPhantomCamera3D();
        Require(Mathf.Abs(explorePcam.TweenDuration - 1.5f) < 0.01f,
            "Start-to-Explore pullback must use the measured 1.5 second camera transition.");
    }

    private async Task ValidatePerspectiveAnchors(GameRoot gameRoot)
    {
        var level = gameRoot.ReferenceLevel;

        await ValidateCheckpointAsync(
            gameRoot,
            level.CameraCheck01,
            (level.GetNode<MeshInstance3D>("Geometry/Stairs01/Step01").GlobalPosition,
                new Vector2(0.500f, 0.408f), "Check01 Stairs01 top"));

        await ValidateCheckpointAsync(
            gameRoot,
            level.CameraCheck02,
            (level.GetNode<MeshInstance3D>("Geometry/Stairs01/Step07").GlobalPosition,
                new Vector2(0.403f, 0.471f), "Check02 Stairs01 bottom"),
            (level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01").GlobalPosition,
                new Vector2(0.633f, 0.639f), "Check02 Stairs02 top"));

        await ValidateCheckpointAsync(
            gameRoot,
            level.CameraCheck03,
            (level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01").GlobalPosition,
                new Vector2(0.500f, 0.406f), "Check03 Stairs02 top"),
            (level.GetNode<MeshInstance3D>("Geometry/ForegroundBlocker").GlobalPosition,
                new Vector2(0.536f, 0.796f), "Check03 foreground blocker"));
    }

    private async Task ValidateCheckpointAsync(
        GameRoot gameRoot,
        Marker3D checkpoint,
        params (Vector3 WorldPosition, Vector2 Expected, string Label)[] anchors)
    {
        gameRoot.Player.GlobalPosition = checkpoint.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        foreach (var anchor in anchors)
        {
            MeasureNormalizedAnchor(camera, anchor.WorldPosition, anchor.Expected, anchor.Label,
                GeometryAnchorTolerance);
        }
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

        GD.Print(
            $"[M1.6] Orthographic A/B: projection={camera.Projection}, size={camera.Size:0.000}, " +
            $"perspectivePoint={perspectivePoint}, orthographicPoint={orthographicPoint}, " +
            $"delta={perspectivePoint.DistanceTo(orthographicPoint):0.0000}.");

        Require((int)camera.Projection == 1,
            "Orthographic A/B camera must apply orthographic projection.");
        Require(Mathf.Abs(camera.Size - 11.1f) < 0.01f,
            $"Orthographic size must match the calibrated 11.1m target-plane view height. Actual={camera.Size}.");
        Require(perspectivePoint.DistanceTo(orthographicPoint) < 0.01f,
            "Perspective and Orthographic A/B must preserve target-plane framing while exposing projection distortion differences.");

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
            $"{_anchorFailures.Count} reference silhouette/geometry anchor(s) exceeded their normalized " +
            "screen-space tolerance:\n- " + string.Join("\n- ", _anchorFailures));
    }

    private static Vector2 ProjectNormalized(Camera3D camera, Vector3 worldPosition)
    {
        var viewportSize = camera.GetViewport().GetVisibleRect().Size;
        Require(viewportSize.X > 0.0f && viewportSize.Y > 0.0f,
            "Calibration smoke requires a non-zero viewport size.");

        var pixel = camera.UnprojectPosition(worldPosition);
        return new Vector2(pixel.X / viewportSize.X, pixel.Y / viewportSize.Y);
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
