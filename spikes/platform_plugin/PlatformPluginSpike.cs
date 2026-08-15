using System;
using Godot;
using PhantomCamera;

namespace Lime.Spikes.PlatformPlugin;

public partial class PlatformPluginSpike : Node3D
{
    private const int ExpectedPriority = 100;
    private const int CiValidationFrames = 12;

    private PhantomCamera3D _followCamera = null!;
    private Node3D _target = null!;
    private Vector3 _targetStart;
    private bool _ciMode;
    private int _ciFrames;

    public override void _Ready()
    {
        _target = GetNode<Node3D>("%Target");
        _followCamera = GetNode<Node3D>("%FollowCamera").AsPhantomCamera3D();

        _followCamera.FollowTarget = _target;
        _followCamera.Priority = ExpectedPriority;
        _followCamera.TweenOnLoad = false;
        _followCamera.TeleportPosition();

        _targetStart = _target.GlobalPosition;
        _ciMode = Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--m1-spike-ci");

        if (_followCamera.Priority != ExpectedPriority)
        {
            Fail("Phantom Camera Priority round-trip failed.");
            return;
        }

        if (_followCamera.FollowTarget.GetInstanceId() != _target.GetInstanceId())
        {
            Fail("Phantom Camera FollowTarget round-trip failed.");
            return;
        }

        GD.Print("[M1.0] Phantom Camera C# wrapper initialized successfully.");
    }

    public override void _Process(double delta)
    {
        float time = Time.GetTicksMsec() / 1000.0f;
        _target.GlobalPosition = _targetStart + new Vector3(
            Mathf.Sin(time) * 2.0f,
            0.0f,
            Mathf.Cos(time) * 2.0f);

        if (!_ciMode)
        {
            return;
        }

        _ciFrames++;
        if (_ciFrames < CiValidationFrames)
        {
            return;
        }

        if (!_followCamera.IsActive)
        {
            Fail("Phantom Camera did not become active through PhantomCameraHost.");
            return;
        }

        GD.Print("[M1.0] PASS: Godot runtime + Phantom Camera C# wrapper smoke test.");
        GetTree().Quit(0);
    }

    private void Fail(string message)
    {
        string fullMessage = $"[M1.0] FAIL: {message}";
        GD.PushError(fullMessage);

        if (_ciMode)
        {
            GetTree().Quit(1);
            return;
        }

        throw new InvalidOperationException(fullMessage);
    }
}
