using System;
using Godot;
using Lime.Diagnostics.Reference.Solve;

namespace Lime.Tests.Smoke;

public partial class ReferenceVanishingPointSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            ValidateSyntheticSolve();
            ValidateMeasuredStartGrid();
            GD.Print("[M1.6.3] PASS: vanishing-point solver and Start floor-grid observations are stable.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: vanishing-point smoke: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateSyntheticSolve()
    {
        var expected = new Vector2(100.0f, -50.0f);
        var lines = new[]
        {
            Through(expected, new Vector2(10.0f, 200.0f)),
            Through(expected, new Vector2(200.0f, 250.0f)),
            Through(expected, new Vector2(-100.0f, 180.0f)),
        };

        var result = ReferenceVanishingPointEvaluator.Solve(lines);
        Require(result.Pixel.DistanceTo(expected) < 0.05f,
            $"Synthetic vanishing point should solve exactly; got {result.Pixel}.");
        Require(result.WeightedRmsLineDistance < 0.05f,
            "Synthetic line residual should be near zero.");
    }

    private static void ValidateMeasuredStartGrid()
    {
        var result = ReferenceVanishingPointEvaluator.Solve(
            ReferenceStartFloorGridMeasurements.LongitudinalLines);

        GD.Print($"[M1.6.3 VANISHING] frame={ReferenceStartFloorGridMeasurements.SourceFrame} " +
                 $"longitudinal={result.Pixel} line_rms={result.WeightedRmsLineDistance:0.00}px " +
                 $"lines={result.LineCount}");

        Require(result.LineCount == 4,
            "Start floor-grid contract must retain four independently reviewed line segments.");
        Require(result.Pixel.X >= 1200.0f && result.Pixel.X <= 1320.0f,
            $"Start longitudinal vanishing-point X drifted unexpectedly: {result.Pixel.X:0.00}.");
        Require(result.Pixel.Y >= -500.0f && result.Pixel.Y <= -250.0f,
            $"Start longitudinal vanishing-point Y drifted unexpectedly: {result.Pixel.Y:0.00}.");
        Require(result.WeightedRmsLineDistance <= 30.0f,
            $"Start floor-grid lines no longer form a coherent parallel family; RMS={result.WeightedRmsLineDistance:0.00}px.");
    }

    private static ReferenceLineObservation Through(Vector2 vanishingPoint, Vector2 point)
    {
        var direction = point - vanishingPoint;
        return new ReferenceLineObservation(point, point + direction * 0.5f, 1.0f);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
