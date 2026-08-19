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
/// without mutating Phantom Camera or production Camera resources. After measuring
/// orientation-only framing regression, it searches translation only in the camera
/// image plane (local Right/Up). Forward distance and FOV remain locked so apparent
/// scale is not silently changed before a semantic Player-height constraint exists.
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

            var compensation = SolveImagePlaneCompensation(
                candidateCamera,
                baselineCamera.GlobalPosition,
                dataset.SourceSize,
                sample.PlayerFeetPixel,
                gameRoot.Player.GlobalPosition);
            candidateCamera.GlobalPosition = compensation.CameraPosition;
            var compensatedPlayer = ReferenceProjectionEvaluator.EvaluatePoint(
                candidateCamera, dataset.SourceSize, sample.PlayerFeetPixel, gameRoot.Player.GlobalPosition);
            var compensatedVp = ReferenceProjectionEvaluator.EvaluateWorldDirectionVanishingPoint(
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
            GD.Print($"[M1.6.3 COMPENSATION] image_plane right={compensation.RightOffset:+0.000;-0.000;0.000} " +
                     $"up={compensation.UpOffset:+0.000;-0.000;0.000} world_units " +
                     $"player={compensatedPlayer.ProjectedPixel} delta={compensatedPlayer.Delta} " +
                     $"error={compensatedPlayer.PixelError:0.00}px vp_error={compensatedVp.PixelError:0.00}px");

            Require(candidateVp.PixelError < 5.0f,
                "Fixed-FOV orientation candidate must reproduce the reference longitudinal VP within 5 px.");
            Require(candidateVp.PixelError < baselineVp.PixelError * 0.02f,
                "Orientation candidate must remove at least 98% of baseline VP error.");
            Require(float.IsFinite(candidatePlayer.PixelError),
                "Orientation-only Player framing regression must be measurable.");
            Require(compensatedPlayer.PixelError < 3.0f,
                "Image-plane compensation must restore Player feet within 3 px without changing FOV/distance.");
            Require(Mathf.Abs(compensation.RightOffset) < 2.45f && Mathf.Abs(compensation.UpOffset) < 2.45f,
                "Image-plane compensation must not pin to the search boundary.");
            Require(Mathf.Abs(compensatedVp.PixelError - candidateVp.PixelError) < 0.25f,
                "Image-plane translation must leave the orientation vanishing-point objective invariant.");

            GD.Print("[M1.6.3] PASS: Start orientation + image-plane framing candidate solved without production changes.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: Start orientation candidate: {exception}");
            GetTree().Quit(1);
        }
    }

    private static CompensationResult SolveImagePlaneCompensation(
        Camera3D camera,
        Vector3 baselinePosition,
        Vector2I sourceSize,
        Vector2 playerReferencePixel,
        Vector3 playerWorldPosition)
    {
        var right = camera.GlobalBasis.X.Normalized();
        var up = camera.GlobalBasis.Y.Normalized();
        var best = EvaluateCompensation(camera, baselinePosition, right, up, sourceSize,
            playerReferencePixel, playerWorldPosition, 0.0f, 0.0f);

        for (var rightOffset = -2.5f; rightOffset <= 2.5001f; rightOffset += 0.10f)
        {
            for (var upOffset = -2.5f; upOffset <= 2.5001f; upOffset += 0.10f)
            {
                var candidate = EvaluateCompensation(camera, baselinePosition, right, up, sourceSize,
                    playerReferencePixel, playerWorldPosition, rightOffset, upOffset);
                if (candidate.PlayerProjection.PixelError < best.PlayerProjection.PixelError)
                {
                    best = candidate;
                }
            }
        }

        var coarse = best;
        for (var rightOffset = coarse.RightOffset - 0.12f;
             rightOffset <= coarse.RightOffset + 0.1201f;
             rightOffset += 0.01f)
        {
            for (var upOffset = coarse.UpOffset - 0.12f;
                 upOffset <= coarse.UpOffset + 0.1201f;
                 upOffset += 0.01f)
            {
                var candidate = EvaluateCompensation(camera, baselinePosition, right, up, sourceSize,
                    playerReferencePixel, playerWorldPosition, rightOffset, upOffset);
                if (candidate.PlayerProjection.PixelError < best.PlayerProjection.PixelError)
                {
                    best = candidate;
                }
            }
        }

        return best;
    }

    private static CompensationResult EvaluateCompensation(
        Camera3D camera,
        Vector3 baselinePosition,
        Vector3 right,
        Vector3 up,
        Vector2I sourceSize,
        Vector2 playerReferencePixel,
        Vector3 playerWorldPosition,
        float rightOffset,
        float upOffset)
    {
        camera.GlobalPosition = baselinePosition + right * rightOffset + up * upOffset;
        var projection = ReferenceProjectionEvaluator.EvaluatePoint(
            camera, sourceSize, playerReferencePixel, playerWorldPosition);
        return new CompensationResult(rightOffset, upOffset, camera.GlobalPosition, projection);
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

    private readonly record struct CompensationResult(
        float RightOffset,
        float UpOffset,
        Vector3 CameraPosition,
        ReferencePointProjection PlayerProjection);
}
