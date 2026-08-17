using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Camera;
using Lime.Game.World.Levels.Reference;

namespace Lime.Tests.Smoke;

public partial class ReferenceCalibrationSmoke : Node
{
    private const float AnchorTolerance = 0.065f;

    public override async void _Ready()
    {
        try
        {
            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");

            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await WaitFramesAsync(4);

            ValidateProductionCamera(gameRoot);
            ValidateCharacterVisual(gameRoot);
            ValidateCalibratedGeometry(gameRoot.ReferenceLevel);
            await ValidatePerspectiveAnchors(gameRoot);
            await ValidateProjectionAb(gameRoot);

            GD.Print("[M1.6] PASS: Reference calibration smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6] FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateProductionCamera(GameRoot gameRoot)
    {
        var camera = gameRoot.CameraDirector.RenderCamera;

        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
            "M1.6 keeps Perspective as the production Explore projection after reference comparison.");
        Require((int)camera.Projection == 0,
            "Production Explore camera must start in Perspective projection.");
        Require(Mathf.Abs(camera.Fov - 45.0f) < 0.01f,
            "Perspective FOV must stay at the calibrated 45 degree baseline.");

        var offset = camera.GlobalPosition - gameRoot.Player.GlobalPosition;
        Require(offset.DistanceTo(new Vector3(0.0f, 6.0f, -12.0f)) < 0.05f,
            $"Production camera follow offset must be calibrated to (0, 6, -12). Actual={offset}.");
        Require(Mathf.Abs(camera.GlobalRotationDegrees.X - (-22.0f)) < 0.1f,
            $"Production camera pitch must be calibrated to -22 degrees. Actual={camera.GlobalRotationDegrees.X}.");
    }

    private static void ValidateCharacterVisual(GameRoot gameRoot)
    {
        var characterVisual = gameRoot.Player.GetNode<Node3D>("VisualRoot/CharacterVisual");
        var sprite = characterVisual.GetNode<Sprite3D>("Sprite3D");

        Require(characterVisual.Scale.DistanceTo(Vector3.One) < 0.001f,
            "M1.6 keeps CharacterVisual at 1:1 world scale after camera-distance calibration.");
        Require(Mathf.Abs(sprite.PixelSize - 0.002f) < 0.00001f,
            "Character Sprite3D pixel size must remain 0.002 for the calibrated world scale.");
        Require(sprite.Offset.DistanceTo(new Vector2(0.0f, 256.0f)) < 0.01f,
            "Character Sprite3D offset must preserve feet-at-root alignment.");
    }

    private static void ValidateCalibratedGeometry(ReferenceLevel level)
    {
        var stair01Top = level.GetNode<MeshInstance3D>("Geometry/Stairs01/Step01");
        var stair02Top = level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01");
        var cylinder01 = level.GetNode<MeshInstance3D>("Geometry/CylinderBuilding01");
        var cylinder02 = level.GetNode<MeshInstance3D>("Geometry/CylinderBuilding02");
        var foregroundBlocker = level.GetNode<MeshInstance3D>("Geometry/ForegroundBlocker");

        Require(stair01Top.GlobalPosition.X > 0.0f && stair02Top.GlobalPosition.X < 0.0f,
            "Reference route must place Stairs01 screen-left/world +X and Stairs02 screen-right/world -X.");
        Require(Mathf.Abs(level.CameraCheck01.GlobalPosition.X - 2.5f) < 0.01f,
            "CameraCheck01 must remain at the calibrated bottom of Stairs01.");
        Require(Mathf.Abs(level.CameraCheck02.GlobalPosition.X) < 0.01f,
            "CameraCheck02 must be centered on the intermediate platform for the 14.2s reference frame.");
        Require(Mathf.Abs(level.CameraCheck03.GlobalPosition.X - (-2.5f)) < 0.01f,
            "CameraCheck03 must remain at the calibrated bottom of Stairs02.");

        Require(cylinder01.GlobalPosition.X > 0.0f && cylinder02.GlobalPosition.X < 0.0f,
            "The two cylindrical masses must straddle the reference route instead of occupying the same screen side.");
        Require(Mathf.Abs(cylinder01.GlobalPosition.Z - (-7.5f)) < 0.01f &&
                Mathf.Abs(cylinder02.GlobalPosition.Z - (-7.5f)) < 0.01f,
            "The cylindrical masses must keep the calibrated shared depth near the intermediate/lower route.");
        Require(foregroundBlocker.GlobalPosition.DistanceTo(new Vector3(-3.0f, 0.0f, -15.0f)) < 0.01f,
            "Foreground blocker must preserve the Check03 lower-centre occlusion calibration.");
    }

    private async Task ValidatePerspectiveAnchors(GameRoot gameRoot)
    {
        var level = gameRoot.ReferenceLevel;

        await ValidateCheckpointAsync(
            gameRoot,
            level.CameraCheck01,
            new Vector2(0.514f, 0.628f),
            (level.GetNode<MeshInstance3D>("Geometry/Stairs01/Step01").GlobalPosition,
                new Vector2(0.500f, 0.408f), "Check01 Stairs01 top"));

        await ValidateCheckpointAsync(
            gameRoot,
            level.CameraCheck02,
            new Vector2(0.514f, 0.575f),
            (level.GetNode<MeshInstance3D>("Geometry/Stairs01/Step07").GlobalPosition,
                new Vector2(0.403f, 0.471f), "Check02 Stairs01 bottom"),
            (level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01").GlobalPosition,
                new Vector2(0.633f, 0.639f), "Check02 Stairs02 top"));

        await ValidateCheckpointAsync(
            gameRoot,
            level.CameraCheck03,
            new Vector2(0.525f, 0.590f),
            (level.GetNode<MeshInstance3D>("Geometry/Stairs02/Step01").GlobalPosition,
                new Vector2(0.500f, 0.406f), "Check03 Stairs02 top"),
            (level.GetNode<MeshInstance3D>("Geometry/ForegroundBlocker").GlobalPosition,
                new Vector2(0.536f, 0.796f), "Check03 foreground blocker"));
    }

    private async Task ValidateCheckpointAsync(
        GameRoot gameRoot,
        Marker3D checkpoint,
        Vector2 expectedPlayerFeet,
        params (Vector3 WorldPosition, Vector2 Expected, string Label)[] anchors)
    {
        gameRoot.Player.GlobalPosition = checkpoint.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;
        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);

        var camera = gameRoot.CameraDirector.RenderCamera;
        ValidateNormalizedAnchor(camera, checkpoint.GlobalPosition, expectedPlayerFeet,
            $"{checkpoint.Name} Player feet");

        foreach (var anchor in anchors)
        {
            ValidateNormalizedAnchor(camera, anchor.WorldPosition, anchor.Expected, anchor.Label);
        }
    }

    private async Task ValidateProjectionAb(GameRoot gameRoot)
    {
        var checkpoint = gameRoot.ReferenceLevel.CameraCheck02;
        gameRoot.Player.GlobalPosition = checkpoint.GlobalPosition;
        gameRoot.Player.Velocity = Vector3.Zero;

        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(3);
        var perspectivePoint = ProjectNormalized(gameRoot.CameraDirector.RenderCamera, checkpoint.GlobalPosition);

        gameRoot.CameraDirector.ActivateInstant(CameraId.ExploreOrthographic);
        await WaitFramesAsync(3);
        var camera = gameRoot.CameraDirector.RenderCamera;
        var orthographicPoint = ProjectNormalized(camera, checkpoint.GlobalPosition);

        Require((int)camera.Projection == 1,
            "Orthographic A/B camera must apply orthographic projection.");
        Require(Mathf.Abs(camera.Size - 11.1f) < 0.01f,
            "Orthographic size must match the calibrated 11.1m target-plane view height.");
        Require(perspectivePoint.DistanceTo(orthographicPoint) < 0.01f,
            "Perspective and Orthographic A/B must preserve target-plane framing while exposing projection distortion differences.");

        gameRoot.CameraDirector.ActivateInstant(CameraId.ExplorePerspective);
        await WaitFramesAsync(2);
    }

    private static void ValidateNormalizedAnchor(
        Camera3D camera,
        Vector3 worldPosition,
        Vector2 expected,
        string label)
    {
        var actual = ProjectNormalized(camera, worldPosition);
        var error = actual.DistanceTo(expected);

        GD.Print($"[M1.6] {label}: expected={expected}, actual={actual}, error={error:0.0000}");
        Require(error <= AnchorTolerance,
            $"{label} exceeds the frozen normalized screen-space error tolerance {AnchorTolerance:0.000}. " +
            $"Expected={expected}, Actual={actual}, Error={error:0.0000}.");
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
