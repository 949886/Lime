using System;
using System.Threading.Tasks;
using Godot;
using Lime.Diagnostics.Reference.Solve;
using Lime.Diagnostics.Reference.V2;
using Lime.Game;
using Lime.Game.Camera;

namespace Lime.Tests.Smoke;

/// <summary>
/// Evaluates the fixed-FOV Start orientation candidate in the real GameRoot world
/// without mutating Phantom Camera or production Camera resources. The candidate
/// Camera3D reuses the baseline RenderCamera position and viewport, then changes
/// orientation only so framing regression can be measured independently.
/// </summary>
public partial class ReferenceStartOrientationCandidateSmoke : Node
{
    private static readonly Vector3 CandidateRotationDegrees = new(-32.02f, 179.79f, 0.0f);
    private const float CandidateFov = 45.0f;

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
            var sample = FindTransitionSample(dataset, 264);
            var referenceVp = ReferenceVanishingPointEvaluator.Solve(
                ReferenceStartFloorGridMeasurements.LongitudinalLines);
            var baselineCamera = gameRoot.CameraDirector.RenderCamera;

            var candidateCamera = new Camera3D
            {
                Fov = CandidateFov,
                KeepAspect = baselineCamera.KeepAspect,
                Projection = baselineCamera.Projection,
                Near = baselineCamera.Near,
                Far = baselineCamera.Far,
            };
            AddChild(candidateCamera);
            candidateCamera.GlobalPosition = baselineCamera.GlobalPosition;
            candidateCamera.RotationDegrees = CandidateRotationDegrees;

            var baselinePlayer = ReferenceProjectionEvaluator.EvaluatePoint(
                baselineCamera, dataset.SourceSize, sample.PlayerFeetPixel, gameRoot.Player.GlobalPosition);
            var baselineVp = ReferenceProjectionEvaluator.EvaluateWorldDirectionVanishingPoint(
                baselineCamera, dataset.SourceSize, referenceVp.Pixel, Vector3.Forward);

            var candidatePlayer = ReferenceProjectionEvaluator.EvaluatePoint(
                candidateCamera, dataset.SourceSize, sample.PlayerFeetPixel, gameRoot.Player.GlobalPosition);
            var candidateVp = ReferenceProjectionEvaluator.EvaluateWorldDirectionVanishingPoint(
                candidateCamera, dataset.SourceSize, referenceVp.Pixel, Vector3.Forward);

            GD.Print($"[M1.6.3 CANDIDATE] baseline player={baselinePlayer.ProjectedPixel} " +
                     $"delta={baselinePlayer.Delta} error={baselinePlayer.PixelError:0.00}px " +
                     $"vp_error={baselineVp.PixelError:0.00}px");
            GD.Print($"[M1.6.3 CANDIDATE] orientation-only yaw={CandidateRotationDegrees.Y:0.00} " +
                     $"pitch={CandidateRotationDegrees.X:0.00} fov={CandidateFov:0.00} " +
                     $"player={candidatePlayer.ProjectedPixel} delta={candidatePlayer.Delta} " +
                     $"player_error={candidatePlayer.PixelError:0.00}px vp_error={candidateVp.PixelError:0.00}px");
            GD.Print($"[M1.6.3 CANDIDATE] framing_regression=" +
                     $"{candidatePlayer.PixelError - baselinePlayer.PixelError:+0.00;-0.00;0.00}px " +
                     $"vp_improvement={baselineVp.PixelError - candidateVp.PixelError:0.00}px");

            Require(candidateVp.PixelError < 5.0f,
                "Fixed-FOV orientation candidate must reproduce the reference longitudinal VP within 5 px.");
            Require(candidateVp.PixelError < baselineVp.PixelError * 0.02f,
                "Orientation candidate must remove at least 98% of baseline VP error.");
            Require(float.IsFinite(candidatePlayer.PixelError),
                "Orientation-only Player framing regression must be measurable.");

            GD.Print("[M1.6.3] PASS: Start orientation candidate validated independently from framing compensation.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: Start orientation candidate: {exception}");
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
