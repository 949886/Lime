using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Lime.Diagnostics.Reference.Solve;
using Lime.Diagnostics.Reference.V2;
using Lime.Game;
using Lime.Game.Camera;

namespace Lime.Tests.Smoke;

public partial class ReferenceStartCameraBaselineSmoke : Node
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

            gameRoot.Player.GlobalPosition = gameRoot.ReferenceLevel.PlayerStart.GlobalPosition;
            gameRoot.Player.Velocity = Vector3.Zero;
            gameRoot.CameraDirector.ActivateInstant(CameraId.StartPerspective);
            await WaitFramesAsync(3);

            var dataset = ReferenceDatasetV2Catalog.CreateSeed();
            var sample = FindTransitionSample(dataset, 264); // 8.80s: stable Start hold.
            var camera = gameRoot.CameraDirector.RenderCamera;

            Require(gameRoot.ReferenceLevel.CalibrationLandmarks.TryGetValue(
                    ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft,
                    out var stairLeft),
                "Start foreground stair-left whitebox landmark is missing.");
            Require(gameRoot.ReferenceLevel.CalibrationLandmarks.TryGetValue(
                    ReferenceCalibrationLandmarkId.StartForegroundStairTopRight,
                    out var stairRight),
                "Start foreground stair-right whitebox landmark is missing.");

            var leftObservation = FindLandmark(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft);
            var rightObservation = FindLandmark(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopRight);

            var left = ReferenceProjectionEvaluator.EvaluatePoint(
                camera, dataset.SourceSize, leftObservation.Pixel, stairLeft!.GlobalPosition);
            var right = ReferenceProjectionEvaluator.EvaluatePoint(
                camera, dataset.SourceSize, rightObservation.Pixel, stairRight!.GlobalPosition);
            var player = ReferenceProjectionEvaluator.EvaluatePoint(
                camera, dataset.SourceSize, sample.PlayerFeetPixel, gameRoot.Player.GlobalPosition);

            var referenceVp = ReferenceVanishingPointEvaluator.Solve(
                ReferenceStartFloorGridMeasurements.LongitudinalLines);
            var longitudinalVp = ReferenceProjectionEvaluator.EvaluateWorldDirectionVanishingPoint(
                camera,
                dataset.SourceSize,
                referenceVp.Pixel,
                Vector3.Forward);

            var worldReport = new ReferenceProjectionErrorReport(new List<ReferenceProjectionError>
            {
                ToError(ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft, left, leftObservation.Confidence),
                ToError(ReferenceCalibrationLandmarkId.StartForegroundStairTopRight, right, rightObservation.Confidence),
            });
            var cameraFramingReport = new ReferenceProjectionErrorReport(new List<ReferenceProjectionError>
            {
                ToError(ReferenceCalibrationLandmarkId.Unknown, player, sample.PlayerConfidence),
            });

            PrintPoint("world/stair-left", left);
            PrintPoint("world/stair-right", right);
            PrintPoint("camera/player-feet", player);
            PrintPoint("camera/longitudinal-vp", longitudinalVp);
            GD.Print($"[M1.6.3 BASELINE] reference_grid_line_rms={referenceVp.WeightedRmsLineDistance:0.00}px");
            GD.Print($"[M1.6.3 BASELINE] solve_viewport={ReferenceProjectionEvaluator.SolveViewportSize}");
            GD.Print($"[M1.6.3 BASELINE] CAMERA_FRAMING start_hold_8.80 " +
                     $"rms={cameraFramingReport.RmsPixelError:0.00}px max={cameraFramingReport.MaxPixelError:0.00}px");
            GD.Print($"[M1.6.3 BASELINE] CAMERA_ORIENTATION start_hold_8.80 " +
                     $"longitudinal_vp_error={longitudinalVp.PixelError:0.00}px " +
                     $"delta={longitudinalVp.Delta}");
            GD.Print($"[M1.6.3 BASELINE] WORLD_LAYOUT start_hold_8.80 count={worldReport.Count} " +
                     $"mean={worldReport.MeanPixelError:0.00}px rms={worldReport.RmsPixelError:0.00}px " +
                     $"max={worldReport.MaxPixelError:0.00}px");

            Require(worldReport.Count == 2, "World-layout baseline must contain both stair points.");
            Require(cameraFramingReport.Count == 1, "Camera framing baseline must contain Player feet independently.");
            Require(float.IsFinite(worldReport.RmsPixelError) && worldReport.RmsPixelError > 1.0f,
                "World-layout RMS must be finite and non-trivial.");
            Require(float.IsFinite(cameraFramingReport.RmsPixelError),
                "Camera framing RMS must be finite.");
            Require(float.IsFinite(longitudinalVp.PixelError),
                "Camera orientation vanishing-point error must be finite.");

            GD.Print("[M1.6.3] PASS: Start camera framing/orientation and world-layout baselines measured separately.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: Start camera baseline: {exception}");
            GetTree().Quit(1);
        }
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

    private static ReferenceLandmarkObservation FindLandmark(
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

    private static ReferenceProjectionError ToError(
        ReferenceCalibrationLandmarkId id,
        ReferencePointProjection point,
        float weight) => new(
            id,
            point.ReferencePixel,
            point.ProjectedPixel,
            point.Delta,
            point.PixelError,
            weight);

    private static void PrintPoint(string label, ReferencePointProjection point)
    {
        GD.Print($"[M1.6.3 BASELINE] {label}: reference={point.ReferencePixel} " +
                 $"projected={point.ProjectedPixel} delta={point.Delta} error={point.PixelError:0.00}px");
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
