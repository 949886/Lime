using System;
using Godot;
using Lime.Diagnostics.Reference.Solve;

namespace Lime.Tests.Smoke;

/// <summary>
/// Diagnostic-only orientation search. Production Camera resources are never changed.
/// FOV stays fixed at the M1.6.1 baseline (45 deg); the floor-grid vanishing point
/// provides two image constraints for yaw/pitch. FOV/distance are solved separately
/// once a semantic Player apparent-height gauge is available.
/// </summary>
public partial class ReferenceStartOrientationSweepSmoke : Node
{
    private const float FixedFov = 45.0f;

    public override void _Ready()
    {
        try
        {
            var camera = new Camera3D
            {
                Current = true,
                Fov = FixedFov,
            };
            AddChild(camera);

            var referenceVp = ReferenceVanishingPointEvaluator.Solve(
                ReferenceStartFloorGridMeasurements.LongitudinalLines);

            var baseline = Evaluate(camera, referenceVp.Pixel, 180.0f, -16.0f);
            var best = Search(camera, referenceVp.Pixel);

            GD.Print($"[M1.6.3 ORIENTATION] reference_vp={referenceVp.Pixel} " +
                     $"line_fit_rms={referenceVp.WeightedRmsLineDistance:0.00}px");
            GD.Print($"[M1.6.3 ORIENTATION] baseline yaw=180.00 pitch=-16.00 fov={FixedFov:0.00} " +
                     $"projected={baseline.ProjectedPixel} delta={baseline.Delta} error={baseline.PixelError:0.00}px");
            GD.Print($"[M1.6.3 ORIENTATION] best_fixed_fov yaw={best.Yaw:0.00} pitch={best.Pitch:0.00} " +
                     $"fov={FixedFov:0.00} projected={best.Projection.ProjectedPixel} " +
                     $"delta={best.Projection.Delta} error={best.Projection.PixelError:0.00}px");

            Require(float.IsFinite(best.Projection.PixelError), "Orientation sweep must produce a finite best error.");
            Require(best.Projection.PixelError <= baseline.PixelError + 0.01f,
                "Orientation sweep must not be worse than the M1.6.1 baseline.");

            GD.Print("[M1.6.3] PASS: Start orientation sweep measured without modifying production Camera resources.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: Start orientation sweep: {exception}");
            GetTree().Quit(1);
        }
    }

    private static SweepResult Search(Camera3D camera, Vector2 referenceVp)
    {
        // Coarse search around the current StartPerspective orientation.
        var best = new SweepResult(180.0f, -16.0f, Evaluate(camera, referenceVp, 180.0f, -16.0f));
        for (var yaw = 176.0f; yaw <= 184.0001f; yaw += 0.25f)
        {
            for (var pitch = -28.0f; pitch <= -8.0001f; pitch += 0.25f)
            {
                var projection = Evaluate(camera, referenceVp, yaw, pitch);
                if (projection.PixelError < best.Projection.PixelError)
                {
                    best = new SweepResult(yaw, pitch, projection);
                }
            }
        }

        // Refine to 0.02 degree around the coarse optimum.
        var coarse = best;
        for (var yaw = coarse.Yaw - 0.30f; yaw <= coarse.Yaw + 0.3001f; yaw += 0.02f)
        {
            for (var pitch = coarse.Pitch - 0.30f; pitch <= coarse.Pitch + 0.3001f; pitch += 0.02f)
            {
                var projection = Evaluate(camera, referenceVp, yaw, pitch);
                if (projection.PixelError < best.Projection.PixelError)
                {
                    best = new SweepResult(yaw, pitch, projection);
                }
            }
        }

        return best;
    }

    private static ReferencePointProjection Evaluate(
        Camera3D camera,
        Vector2 referenceVp,
        float yawDegrees,
        float pitchDegrees)
    {
        camera.RotationDegrees = new Vector3(pitchDegrees, yawDegrees, 0.0f);
        camera.Fov = FixedFov;
        return ReferenceProjectionEvaluator.EvaluateWorldDirectionVanishingPoint(
            camera,
            new Vector2I(2548, 1426),
            referenceVp,
            Vector3.Forward);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private readonly record struct SweepResult(
        float Yaw,
        float Pitch,
        ReferencePointProjection Projection);
}
