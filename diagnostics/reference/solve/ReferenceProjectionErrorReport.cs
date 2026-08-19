using System;
using System.Collections.Generic;

namespace Lime.Diagnostics.Reference.Solve;

public sealed class ReferenceProjectionErrorReport
{
    public IReadOnlyList<ReferenceProjectionError> Errors { get; }
    public int Count => Errors.Count;
    public float MeanPixelError { get; }
    public float RmsPixelError { get; }
    public float MaxPixelError { get; }

    public ReferenceProjectionErrorReport(IReadOnlyList<ReferenceProjectionError> errors)
    {
        Errors = errors;

        if (errors.Count == 0)
        {
            return;
        }

        double weightedError = 0.0;
        double weightedSquaredError = 0.0;
        double totalWeight = 0.0;
        var maxError = 0.0f;

        foreach (var error in errors)
        {
            var weight = Math.Max(0.0001f, error.Weight);
            weightedError += error.PixelError * weight;
            weightedSquaredError += error.PixelError * error.PixelError * weight;
            totalWeight += weight;
            maxError = Math.Max(maxError, error.PixelError);
        }

        MeanPixelError = (float)(weightedError / totalWeight);
        RmsPixelError = (float)Math.Sqrt(weightedSquaredError / totalWeight);
        MaxPixelError = maxError;
    }
}
