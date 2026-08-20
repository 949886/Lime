using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Lime.Game;
using Lime.Game.Camera;

namespace Lime.Tests.Smoke;

/// <summary>
/// Diagnostic capture of the production Phantom Camera Start -> Explore tween.
/// The Player is held at PlayerStart so this isolates the configured camera tween
/// from gameplay trajectory. Samples are taken at 0.1 s physics intervals to align
/// with the dense reference dataset cadence. This is a curve capture, not yet a
/// reference-score assertion because the current whitebox is structurally wrong.
/// </summary>
public partial class ReferenceRuntimeTransitionCaptureSmoke : Node
{
    private const int PhysicsFramesPerSample = 6;
    private const int SampleCount = 18;

    public override async void _Ready()
    {
        try
        {
            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");
            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await WaitProcessFramesAsync(4);

            gameRoot.Player.GlobalPosition = gameRoot.ReferenceLevel.PlayerStart.GlobalPosition;
            gameRoot.Player.Velocity = Vector3.Zero;
            gameRoot.CameraDirector.ActivateInstant(CameraId.StartPerspective);
            await WaitProcessFramesAsync(3);

            var camera = gameRoot.CameraDirector.RenderCamera;
            var samples = new List<RuntimeCameraSample>(SampleCount);
            samples.Add(Capture(camera, 0.0));

            gameRoot.CameraDirector.Activate(CameraId.ExplorePerspective);
            for (var index = 1; index < SampleCount; index++)
            {
                await WaitPhysicsFramesAsync(PhysicsFramesPerSample);
                samples.Add(Capture(camera, index * 0.1));
            }

            Require(samples.Count == SampleCount,
                $"Runtime transition capture must contain {SampleCount} samples.");
            Require(samples[0].Fov is > 44.9f and < 45.1f,
                "Start transition capture must begin at FOV 45.");
            Require(samples[^1].Fov is > 44.9f and < 45.1f,
                "Current Explore endpoint must remain FOV 45 until semantic height solve.");

            var start = samples[0];
            var end = samples[^1];
            Require(start.Position.DistanceTo(end.Position) > 1.0f,
                "Start -> Explore production tween must contain material camera translation.");
            Require(Mathf.Abs(start.RotationDegrees.X - end.RotationDegrees.X) > 1.0f,
                "Start -> Explore production tween must contain material pitch change.");

            foreach (var sample in samples)
            {
                GD.Print($"[M1.6.3 TRANSITION RUNTIME] t={sample.ElapsedSeconds:0.0}s " +
                         $"position={sample.Position} rotation={sample.RotationDegrees} fov={sample.Fov:0.00}");
            }

            GD.Print($"[M1.6.3 TRANSITION RUNTIME] translation=" +
                     $"{start.Position.DistanceTo(end.Position):0.000}wu " +
                     $"pitch_delta={end.RotationDegrees.X - start.RotationDegrees.X:+0.00;-0.00;0.00}deg");
            GD.Print("[M1.6.3] PASS: production Start -> Explore runtime curve captured.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.3] FAIL: runtime transition capture: {exception}");
            GetTree().Quit(1);
        }
    }

    private static RuntimeCameraSample Capture(Camera3D camera, double elapsedSeconds)
    {
        return new RuntimeCameraSample(
            elapsedSeconds,
            camera.GlobalPosition,
            camera.GlobalRotationDegrees,
            camera.Fov);
    }

    private async Task WaitPhysicsFramesAsync(int count)
    {
        for (var index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private async Task WaitProcessFramesAsync(int count)
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

    private readonly record struct RuntimeCameraSample(
        double ElapsedSeconds,
        Vector3 Position,
        Vector3 RotationDegrees,
        float Fov);
}
