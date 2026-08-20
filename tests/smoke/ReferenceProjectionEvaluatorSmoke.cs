using System;
using System.Collections.Generic;
using Godot;
using Lime.Diagnostics.Reference.Solve;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Tests.Smoke;

public partial class ReferenceProjectionEvaluatorSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            var viewport = new SubViewport
            {
                Size = new Vector2I(2560, 1440),
            };
            AddChild(viewport);

            var camera = new Camera3D
            {
                Current = true,
                Position = Vector3.Zero,
                Fov = 45.0f,
            };
            viewport.AddChild(camera);

            var exactMarker = new Marker3D { Position = new Vector3(0.0f, 0.0f, -5.0f) };
            var offsetMarker = new Marker3D { Position = new Vector3(0.0f, 0.0f, -5.0f) };
            viewport.AddChild(exactMarker);
            viewport.AddChild(offsetMarker);

            var shot = new ReferenceShot { Id = "synthetic_projection" };
            shot.Landmarks.Add(new ReferenceLandmarkObservation
            {
                LandmarkId = ReferenceCalibrationLandmarkId.StartGateGridUpperLeft,
                Pixel = new Vector2(1274.0f, 713.0f),
                Confidence = 1.0f,
            });
            shot.Landmarks.Add(new ReferenceLandmarkObservation
            {
                LandmarkId = ReferenceCalibrationLandmarkId.StartGateGridUpperRight,
                Pixel = new Vector2(1374.0f, 713.0f),
                Confidence = 1.0f,
            });

            var landmarks = new Dictionary<ReferenceCalibrationLandmarkId, Node3D>
            {
                [ReferenceCalibrationLandmarkId.StartGateGridUpperLeft] = exactMarker,
                [ReferenceCalibrationLandmarkId.StartGateGridUpperRight] = offsetMarker,
            };

            var report = ReferenceProjectionEvaluator.EvaluateShot(
                camera,
                new Vector2I(2548, 1426),
                shot,
                landmarks);

            Require(report.Count == 2, "Projection evaluator must report both mapped landmarks.");

            var exact = report.Errors[0];
            var offset = report.Errors[1];

            Require(exact.PixelError < 1.0f,
                $"Center landmark should project to the reference center; error={exact.PixelError:F3}px.");
            Require(offset.PixelError > 99.0f && offset.PixelError < 102.0f,
                $"Synthetic 100 source-pixel offset should remain about 100 viewport pixels; error={offset.PixelError:F3}px.");
            Require(offset.DeltaPixel.X < -99.0f && Math.Abs(offset.DeltaPixel.Y) < 1.0f,
                $"Projection delta direction is wrong: {offset.DeltaPixel}.");
            Require(report.RmsPixelError > 70.0f && report.RmsPixelError < 72.0f,
                $"Unexpected RMS error: {report.RmsPixelError:F3}px.");
            Require(report.MaxPixelError == offset.PixelError,
                "Max error must identify the deliberately offset landmark.");

            GD.Print($"[M1.6.3] PASS: projection evaluator RMS={report.RmsPixelError:F2}px max={report.MaxPixelError:F2}px.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: {exception}");
            GetTree().Quit(1);
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
