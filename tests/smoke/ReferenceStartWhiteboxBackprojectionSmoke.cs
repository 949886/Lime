using System;
using System.Threading.Tasks;
using Godot;
using Lime.Diagnostics.Reference.Solve;
using Lime.Diagnostics.Reference.V2;
using Lime.Game;
using Lime.Game.Camera;

namespace Lime.Tests.Smoke;

/// <summary>
/// Diagnostic bridge from the solved Start camera into M1.6.4 whitebox reconstruction.
/// The reviewed frame-264 foreground-stair screen edge is backprojected through the
/// actual production Camera3D onto the known Start deck top plane (Y=2.0). This gives
/// a camera-consistent world-space target for Stairs01 without letting the existing
/// incorrect whitebox drive the solve.
/// </summary>
public partial class ReferenceStartWhiteboxBackprojectionSmoke : Node
{
    private const float StartDeckTopY = 2.0f;

    public override async void _Ready()
    {
        try
        {
            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");
            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await WaitFramesAsync(4);

            gameRoot.Player.GlobalPosition = gameRoot.ReferenceLevel.PlayerStart.GlobalPosition;
            gameRoot.Player.Velocity = Vector3.Zero;
            gameRoot.CameraDirector.ActivateInstant(CameraId.StartPerspective);
            await WaitFramesAsync(4);

            var dataset = ReferenceDatasetV2Catalog.CreateSeed();
            var sample = FindTransitionSample(dataset, 264);
            var leftSource = Find(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft).Pixel;
            var rightSource = Find(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopRight).Pixel;
            var camera = gameRoot.CameraDirector.RenderCamera;

            var leftWorld = BackprojectToHorizontalPlane(camera, dataset.SourceSize, leftSource, StartDeckTopY);
            var rightWorld = BackprojectToHorizontalPlane(camera, dataset.SourceSize, rightSource, StartDeckTopY);
            var center = (leftWorld + rightWorld) * 0.5f;
            var span = leftWorld.DistanceTo(rightWorld);

            Require(float.IsFinite(span) && span > 0.1f && span < 20.0f,
                $"Backprojected Start stair span must be finite and plausible. Actual={span}.");
            Require(Mathf.Abs(leftWorld.Y - StartDeckTopY) < 0.001f &&
                    Mathf.Abs(rightWorld.Y - StartDeckTopY) < 0.001f,
                "Backprojected Start stair points must lie on the Start deck top plane.");

            GD.Print($"[M1.6.4 START BACKPROJECT] reference_left_source={leftSource} " +
                     $"reference_right_source={rightSource}");
            GD.Print($"[M1.6.4 START BACKPROJECT] left_world={leftWorld} right_world={rightWorld}");
            GD.Print($"[M1.6.4 START BACKPROJECT] center={center} span={span:0.000} " +
                     $"x_span={Mathf.Abs(leftWorld.X - rightWorld.X):0.000} " +
                     $"z_span={Mathf.Abs(leftWorld.Z - rightWorld.Z):0.000}");
            GD.Print("[M1.6.4] PASS: Start foreground stair reference backprojected through production camera.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.4] FAIL: Start whitebox backprojection: {exception}");
            GetTree().Quit(1);
        }
    }

    private static Vector3 BackprojectToHorizontalPlane(
        Camera3D camera,
        Vector2I sourceSize,
        Vector2 sourcePixel,
        float planeY)
    {
        var solvePixel = ReferenceProjectionEvaluator.ScaleSourcePixelToViewport(
            sourcePixel,
            sourceSize,
            ReferenceProjectionEvaluator.SolveViewportSize);
        var runtimeSize = camera.GetViewport().GetVisibleRect().Size;
        if (runtimeSize.X <= 0.0f || runtimeSize.Y <= 0.0f)
        {
            throw new InvalidOperationException("Backprojection requires a non-zero runtime viewport.");
        }

        var runtimePixel = new Vector2(
            solvePixel.X * runtimeSize.X / ReferenceProjectionEvaluator.SolveViewportSize.X,
            solvePixel.Y * runtimeSize.Y / ReferenceProjectionEvaluator.SolveViewportSize.Y);
        var origin = camera.ProjectRayOrigin(runtimePixel);
        var direction = camera.ProjectRayNormal(runtimePixel);
        if (Mathf.Abs(direction.Y) < 0.00001f)
        {
            throw new InvalidOperationException("Reference ray is parallel to the Start deck plane.");
        }

        var distance = (planeY - origin.Y) / direction.Y;
        if (!float.IsFinite(distance) || distance <= 0.0f)
        {
            throw new InvalidOperationException(
                $"Reference ray does not intersect the Start deck plane in front of the camera. t={distance}.");
        }

        return origin + direction * distance;
    }

    private static ReferenceCameraTransitionSample FindTransitionSample(ReferenceDatasetV2 dataset, int frame)
    {
        foreach (var sample in dataset.CameraTransitionSamples)
        {
            if (sample.SourceFrame == frame)
            {
                return sample;
            }
        }

        throw new InvalidOperationException($"Transition sample frame {frame} was not found.");
    }

    private static ReferenceLandmarkObservation Find(
        ReferenceCameraTransitionSample sample,
        ReferenceCalibrationLandmarkId id)
    {
        foreach (var observation in sample.Landmarks)
        {
            if (observation.LandmarkId == id)
            {
                return observation;
            }
        }

        throw new InvalidOperationException($"Transition sample is missing landmark {id}.");
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
