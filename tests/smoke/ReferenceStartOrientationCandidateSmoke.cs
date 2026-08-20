using System;
using System.Threading.Tasks;
using Godot;
using Lime.Diagnostics.Reference.Solve;
using Lime.Diagnostics.Reference.V2;
using Lime.Game;
using Lime.Game.Camera;

namespace Lime.Tests.Smoke;

/// <summary>
/// Guards the production StartPerspective calibration in the real GameRoot.
/// The solved orientation and image-plane framing compensation now live in
/// CameraSystem.tscn, so this smoke verifies the actual runtime camera rather
/// than a diagnostic-only candidate camera.
/// </summary>
public partial class ReferenceStartOrientationCandidateSmoke : Node
{
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
            var productionCamera = gameRoot.CameraDirector.RenderCamera;

            var player = ReferenceProjectionEvaluator.EvaluatePoint(
                productionCamera,
                dataset.SourceSize,
                sample.PlayerFeetPixel,
                gameRoot.Player.GlobalPosition);
            var vp = ReferenceProjectionEvaluator.EvaluateWorldDirectionVanishingPoint(
                productionCamera,
                dataset.SourceSize,
                referenceVp.Pixel,
                Vector3.Forward);

            GD.Print($"[M1.6.3 PRODUCTION] camera_position={productionCamera.GlobalPosition} " +
                     $"rotation={productionCamera.RotationDegrees} fov={productionCamera.Fov:0.00}");
            GD.Print($"[M1.6.3 PRODUCTION] player={player.ProjectedPixel} " +
                     $"delta={player.Delta} error={player.PixelError:0.00}px");
            GD.Print($"[M1.6.3 PRODUCTION] longitudinal_vp={vp.ProjectedPixel} " +
                     $"delta={vp.Delta} error={vp.PixelError:0.00}px");

            Require(Mathf.Abs(productionCamera.Fov - 45.0f) < 0.01f,
                "Start production solve currently requires the measured 45-degree FOV baseline.");
            Require(player.PixelError < 3.0f,
                $"Production Start Player feet must stay within 3 px, got {player.PixelError:0.00}px.");
            Require(vp.PixelError < 5.0f,
                $"Production Start longitudinal VP must stay within 5 px, got {vp.PixelError:0.00}px.");

            GD.Print("[M1.6.3] PASS: production Start camera calibration is active.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: production Start camera calibration: {exception}");
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
