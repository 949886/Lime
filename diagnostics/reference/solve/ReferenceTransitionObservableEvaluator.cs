using System;
using System.Collections.Generic;
using Godot;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Diagnostics.Reference.Solve;

/// <summary>
/// Converts dense Start -> Explore reference samples into relative screen-space
/// observables. Ratios are normalized to the first dense sample so they describe
/// camera/subject evolution without requiring the current whitebox to already
/// match the reference world geometry.
/// </summary>
public static class ReferenceTransitionObservableEvaluator
{
    public static IReadOnlyList<ReferenceTransitionObservable> Evaluate(ReferenceDatasetV2 dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        if (dataset.CameraTransitionSamples.Count == 0)
        {
            throw new InvalidOperationException("Reference dataset has no dense camera-transition samples.");
        }

        var first = Measure(dataset.CameraTransitionSamples[0]);
        RequirePositive(first.GateWidthPixels, "Initial gate width");
        RequirePositive(first.ForegroundStairWidthPixels, "Initial foreground stair width");
        RequirePositive(first.PlayerHeightPixels, "Initial Player height");

        var result = new List<ReferenceTransitionObservable>(dataset.CameraTransitionSamples.Count);
        foreach (var sample in dataset.CameraTransitionSamples)
        {
            var measured = Measure(sample);
            result.Add(new ReferenceTransitionObservable(
                sample.SourceFrame,
                sample.TimestampSeconds,
                measured.GateWidthPixels,
                measured.ForegroundStairWidthPixels,
                measured.PlayerHeightPixels,
                measured.GateCenterPixel,
                measured.ForegroundStairCenterPixel,
                sample.PlayerFeetPixel,
                measured.GateWidthPixels / first.GateWidthPixels,
                measured.ForegroundStairWidthPixels / first.ForegroundStairWidthPixels,
                measured.PlayerHeightPixels / first.PlayerHeightPixels));
        }

        return result;
    }

    private static RawMeasurement Measure(ReferenceCameraTransitionSample sample)
    {
        var gateLeft = Find(sample, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft);
        var gateRight = Find(sample, ReferenceCalibrationLandmarkId.StartGateGridUpperRight);
        var stairLeft = Find(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft);
        var stairRight = Find(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopRight);

        var gateWidth = gateLeft.Pixel.DistanceTo(gateRight.Pixel);
        var stairWidth = stairLeft.Pixel.DistanceTo(stairRight.Pixel);
        RequirePositive(gateWidth, $"Gate width at frame {sample.SourceFrame}");
        RequirePositive(stairWidth, $"Foreground stair width at frame {sample.SourceFrame}");
        RequirePositive(sample.PlayerPixelHeight, $"Player height at frame {sample.SourceFrame}");

        return new RawMeasurement(
            gateWidth,
            stairWidth,
            sample.PlayerPixelHeight,
            (gateLeft.Pixel + gateRight.Pixel) * 0.5f,
            (stairLeft.Pixel + stairRight.Pixel) * 0.5f);
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

        throw new InvalidOperationException(
            $"Dense transition sample frame {sample.SourceFrame} is missing landmark {id}.");
    }

    private static void RequirePositive(float value, string label)
    {
        if (!float.IsFinite(value) || value <= 0.0f)
        {
            throw new InvalidOperationException($"{label} must be finite and positive. Actual={value}.");
        }
    }

    private readonly record struct RawMeasurement(
        float GateWidthPixels,
        float ForegroundStairWidthPixels,
        float PlayerHeightPixels,
        Vector2 GateCenterPixel,
        Vector2 ForegroundStairCenterPixel);
}

public readonly record struct ReferenceTransitionObservable(
    int SourceFrame,
    double TimestampSeconds,
    float GateWidthPixels,
    float ForegroundStairWidthPixels,
    float PlayerHeightPixels,
    Vector2 GateCenterPixel,
    Vector2 ForegroundStairCenterPixel,
    Vector2 PlayerFeetPixel,
    float GateWidthRatio,
    float ForegroundStairWidthRatio,
    float PlayerHeightRatio)
{
    public float DepthScaleSeparation => GateWidthRatio - ForegroundStairWidthRatio;
}
