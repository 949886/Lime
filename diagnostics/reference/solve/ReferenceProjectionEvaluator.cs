using System;
using System.Collections.Generic;
using Godot;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Diagnostics.Reference.Solve;

public static class ReferenceProjectionEvaluator
{
    public static readonly Vector2 SolveViewportSize = new(2560.0f, 1440.0f);

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

    public static ReferencePointProjection EvaluateWorldDirectionVanishingPoint(
        Camera3D camera,
        Vector2I sourceSize,
        Vector2 sourceReferenceVanishingPoint,
        Vector3 worldDirection) =>
        EvaluateWorldDirectionVanishingPoint(
            camera,
            sourceSize,
            sourceReferenceVanishingPoint,
            worldDirection,
            SolveViewportSize);

    public static ReferencePointProjection EvaluateWorldDirectionVanishingPoint(
        Camera3D camera,
        Vector2I sourceSize,
        Vector2 sourceReferenceVanishingPoint,
        Vector3 worldDirection,
        Vector2 solveViewportSize)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (worldDirection.LengthSquared() < 0.000001f)
        {
            throw new ArgumentException("World direction must be non-zero.", nameof(worldDirection));
        }

        var direction = worldDirection.Normalized();
        var cameraForward = -camera.GlobalBasis.Z.Normalized();
        if (cameraForward.Dot(direction) < 0.0f)
        {
            direction = -direction;
        }

        // A point arbitrarily far along a world direction approaches that
        // direction's projective vanishing point. Starting at Camera position
        // removes translation from the objective entirely.
        var pointAtInfinity = camera.GlobalPosition + direction * 100000.0f;
        return EvaluatePoint(
            camera,
            sourceSize,
            sourceReferenceVanishingPoint,
            pointAtInfinity,
            solveViewportSize);
    }

    public static ReferencePointProjection EvaluatePoint(
        Camera3D camera,
        Vector2I sourceSize,
        Vector2 sourceReferencePixel,
        Vector3 worldPosition) =>
        EvaluatePoint(camera, sourceSize, sourceReferencePixel, worldPosition, SolveViewportSize);

    public static ReferencePointProjection EvaluatePoint(
        Camera3D camera,
        Vector2I sourceSize,
        Vector2 sourceReferencePixel,
        Vector3 worldPosition,
        Vector2 solveViewportSize)
    {
        ArgumentNullException.ThrowIfNull(camera);

        if (solveViewportSize.X <= 0.0f || solveViewportSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(solveViewportSize), "Solve viewport must be positive.");
        }

        var runtimeViewportSize = camera.GetViewport().GetVisibleRect().Size;
        if (runtimeViewportSize.X <= 0.0f || runtimeViewportSize.Y <= 0.0f)
        {
            throw new InvalidOperationException("Camera viewport must be positive before projection evaluation.");
        }

        var referencePixel = ScalePixel(sourceReferencePixel, new Vector2(sourceSize.X, sourceSize.Y), solveViewportSize);
        var runtimeProjectedPixel = camera.UnprojectPosition(worldPosition);
        var projectedPixel = ScalePixel(runtimeProjectedPixel, runtimeViewportSize, solveViewportSize);
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

        return ScalePixel(sourcePixel, new Vector2(sourceSize.X, sourceSize.Y), viewportSize);
    }

    private static Vector2 ScalePixel(Vector2 pixel, Vector2 fromSize, Vector2 toSize)
    {
        if (fromSize.X <= 0.0f || fromSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(fromSize), "Source viewport must be positive.");
        }

        return new Vector2(
            pixel.X * toSize.X / fromSize.X,
            pixel.Y * toSize.Y / fromSize.Y);
    }
}
