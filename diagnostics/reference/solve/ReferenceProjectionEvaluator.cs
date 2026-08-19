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

        var errors = new List<ReferenceProjectionError>(shot.Landmarks.Count);

        foreach (var observation in shot.Landmarks)
        {
            if (!worldLandmarks.TryGetValue(observation.LandmarkId, out var landmark))
            {
                continue;
            }

            var point = EvaluatePoint(camera, sourceSize, observation.Pixel, landmark.GlobalPosition);
            var weight = observation.IsOccluded ? observation.Confidence * 0.5f : observation.Confidence;

            errors.Add(new ReferenceProjectionError(
                observation.LandmarkId,
                point.ReferencePixel,
                point.ProjectedPixel,
                point.Delta,
                point.PixelError,
                weight));
        }

        return new ReferenceProjectionErrorReport(errors);
    }

    public static ReferencePointProjection EvaluatePoint(
        Camera3D camera,
        Vector2I sourceSize,
        Vector2 sourceReferencePixel,
        Vector3 worldPosition)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var viewportSize = camera.GetViewport().GetVisibleRect().Size;
        var referencePixel = ScaleSourcePixelToViewport(sourceReferencePixel, sourceSize, viewportSize);
        var projectedPixel = camera.UnprojectPosition(worldPosition);
        var delta = projectedPixel - referencePixel;

        return new ReferencePointProjection(
            referencePixel,
            projectedPixel,
            delta,
            delta.Length());
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
