using System;
using System.Collections.Generic;
using Godot;

namespace Lime.Diagnostics.Reference.Solve;

public static class ReferenceVanishingPointEvaluator
{
    public static ReferenceVanishingPointResult Solve(IReadOnlyList<ReferenceLineObservation> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count < 2)
        {
            throw new ArgumentException("At least two reference lines are required.", nameof(lines));
        }

        double m00 = 0.0;
        double m01 = 0.0;
        double m11 = 0.0;
        double r0 = 0.0;
        double r1 = 0.0;
        double weightSum = 0.0;

        foreach (var line in lines)
        {
            var direction = line.Direction;
            if (direction.LengthSquared() < 1.0f)
            {
                throw new ArgumentException("Reference line endpoints must be distinct.", nameof(lines));
            }

            // ax + by + c = 0, normalized so residual is a pixel distance.
            var a = (double)line.StartPixel.Y - line.EndPixel.Y;
            var b = (double)line.EndPixel.X - line.StartPixel.X;
            var c = (double)line.StartPixel.X * line.EndPixel.Y -
                    (double)line.EndPixel.X * line.StartPixel.Y;
            var norm = Math.Sqrt(a * a + b * b);
            a /= norm;
            b /= norm;
            c /= norm;

            var weight = Math.Clamp(line.Confidence, 0.01f, 1.0f);
            m00 += weight * a * a;
            m01 += weight * a * b;
            m11 += weight * b * b;
            r0 += weight * a * -c;
            r1 += weight * b * -c;
            weightSum += weight;
        }

        var determinant = m00 * m11 - m01 * m01;
        if (Math.Abs(determinant) < 1e-9)
        {
            throw new InvalidOperationException("Reference lines do not provide a stable vanishing-point intersection.");
        }

        var x = (r0 * m11 - m01 * r1) / determinant;
        var y = (m00 * r1 - m01 * r0) / determinant;
        var pixel = new Vector2((float)x, (float)y);

        double weightedSquaredDistance = 0.0;
        foreach (var line in lines)
        {
            var a = (double)line.StartPixel.Y - line.EndPixel.Y;
            var b = (double)line.EndPixel.X - line.StartPixel.X;
            var c = (double)line.StartPixel.X * line.EndPixel.Y -
                    (double)line.EndPixel.X * line.StartPixel.Y;
            var norm = Math.Sqrt(a * a + b * b);
            var distance = (a * x + b * y + c) / norm;
            var weight = Math.Clamp(line.Confidence, 0.01f, 1.0f);
            weightedSquaredDistance += weight * distance * distance;
        }

        return new ReferenceVanishingPointResult(
            pixel,
            (float)Math.Sqrt(weightedSquaredDistance / weightSum),
            lines.Count);
    }
}
