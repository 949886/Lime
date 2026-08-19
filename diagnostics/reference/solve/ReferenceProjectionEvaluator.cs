using System;
using System.Collections.Generic;
using Godot;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Diagnostics.Reference.Solve;

public static class ReferenceProjectionEvaluator
{
    public static ReferenceProjectionErrorReport EvaluateShot(
        Camera3D camera,
        Vector2I sourceSize,
        ReferenceShot shot,
        IReadOnlyDictionary<ReferenceCalibrationLandmarkId, Node3D> worldLandmarks)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(worldLandmarks);

        var viewportSize = camera.GetViewport().GetVisibleRect().Size;
        var errors = new List<ReferenceProjectionError>(shot.Landmarks.Count);

        foreach (var observation in shot.Landmarks)
        {
            if (!worldLandmarks.TryGetValue(observation.LandmarkId, out var landmark))
            {
                continue;
            }

            var projectedPixel = camera.UnprojectPosition(landmark.GlobalPosition);
            var referencePixel = ScaleSourcePixelToViewport(observation.Pixel, sourceSize, viewportSize);
            var delta = projectedPixel - referencePixel;
            var weight = observation.IsOccluded ? observation.Confidence * 0.5f : observation.Confidence;

            errors.Add(new ReferenceProjectionError(
                observation.LandmarkId,
                referencePixel,
                projectedPixel,
                delta,
                delta.Length(),
                weight));
        }

        return new ReferenceProjectionErrorReport(errors);
    }

    public static Vector2 ScaleSourcePixelToViewport(
        Vector2 sourcePixel,
        Vector2I sourceSize,
        Vector2 viewportSize)
    {
        if (sourceSize.X <= 0 || sourceSize.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSize), "Source size must be positive.");
        }

        return new Vector2(
            sourcePixel.X * viewportSize.X / sourceSize.X,
            sourcePixel.Y * viewportSize.Y / sourceSize.Y);
    }
}
