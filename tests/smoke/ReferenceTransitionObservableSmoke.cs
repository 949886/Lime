using System;
using Godot;
using Lime.Diagnostics.Reference.Solve;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Tests.Smoke;

public partial class ReferenceTransitionObservableSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            var dataset = ReferenceDatasetV2Catalog.CreateSeed();
            var observables = ReferenceTransitionObservableEvaluator.Evaluate(dataset);

            Require(observables.Count == 18,
                $"Dense transition solve must retain 18 samples. Actual={observables.Count}.");

            var first = observables[0];
            var last = observables[^1];

            Require(first.SourceFrame == 264 && last.SourceFrame == 315,
                $"Dense transition solve must cover frames 264..315. Actual={first.SourceFrame}..{last.SourceFrame}.");
            Require(Math.Abs(first.TimestampSeconds - 8.80) < 0.001 &&
                    Math.Abs(last.TimestampSeconds - 10.50) < 0.001,
                "Dense transition solve must cover 8.80s..10.50s.");

            Require(Mathf.Abs(first.GateWidthRatio - 1.0f) < 0.0001f &&
                    Mathf.Abs(first.ForegroundStairWidthRatio - 1.0f) < 0.0001f &&
                    Mathf.Abs(first.PlayerHeightRatio - 1.0f) < 0.0001f,
                "Transition ratios must be normalized to frame 264.");

            Require(Mathf.Abs(last.GateWidthPixels - 79.0f) < 0.01f,
                $"Frame 315 gate width changed unexpectedly. Actual={last.GateWidthPixels:0.00}px.");
            Require(Mathf.Abs(last.ForegroundStairWidthPixels - 369.0f) < 0.01f,
                $"Frame 315 stair width changed unexpectedly. Actual={last.ForegroundStairWidthPixels:0.00}px.");
            Require(Mathf.Abs(last.PlayerHeightPixels - 185.0f) < 0.01f,
                $"Frame 315 Player height changed unexpectedly. Actual={last.PlayerHeightPixels:0.00}px.");

            Require(last.GateWidthRatio is > 0.44f and < 0.46f,
                $"Gate endpoint scale ratio must stay near the reviewed reference value. Actual={last.GateWidthRatio:0.000}.");
            Require(last.ForegroundStairWidthRatio is > 0.29f and < 0.31f,
                $"Foreground stair endpoint scale ratio must stay near the reviewed reference value. Actual={last.ForegroundStairWidthRatio:0.000}.");
            Require(last.PlayerHeightRatio is > 0.34f and < 0.36f,
                $"Player endpoint apparent-height ratio must stay near the reviewed reference value. Actual={last.PlayerHeightRatio:0.000}.");
            Require(last.DepthScaleSeparation > 0.14f,
                $"Different-depth landmarks must reject a uniform/FOV-only transition. Separation={last.DepthScaleSeparation:0.000}.");

            var movementStarted = false;
            var settled = true;
            foreach (var observable in observables)
            {
                if (observable.SourceFrame >= 273 && observable.ForegroundStairWidthRatio < 0.96f)
                {
                    movementStarted = true;
                }

                if (observable.SourceFrame >= 312 && observable.SourceFrame < 315)
                {
                    var next = observables[observables.IndexOf(observable) + 1];
                    if (Mathf.Abs(next.GateWidthRatio - observable.GateWidthRatio) > 0.01f ||
                        Mathf.Abs(next.ForegroundStairWidthRatio - observable.ForegroundStairWidthRatio) > 0.01f)
                    {
                        settled = false;
                    }
                }
            }

            Require(movementStarted,
                "Dense reference must show material pullback by frame 273 / 9.10s.");
            Require(settled,
                "Dense reference must be effectively settled by frames 312..315 / 10.40..10.50s.");

            GD.Print($"[M1.6.3 TRANSITION REF] samples={observables.Count} " +
                     $"gate_ratio={last.GateWidthRatio:0.000} " +
                     $"stair_ratio={last.ForegroundStairWidthRatio:0.000} " +
                     $"player_height_ratio={last.PlayerHeightRatio:0.000} " +
                     $"depth_separation={last.DepthScaleSeparation:0.000}");
            GD.Print("[M1.6.3] PASS: dense Start -> Explore reference observables are solver-ready.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: transition observable smoke: {exception}");
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
