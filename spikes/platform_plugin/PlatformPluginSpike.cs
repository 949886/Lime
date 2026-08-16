using System;
using System.Threading.Tasks;
using Godot;
using PhantomCamera;

namespace Lime.Spikes.PlatformPlugin;

public partial class PlatformPluginSpike : Node3D
{
    private const int InactivePriority = 0;
    private const int GameplayPriority = 100;
    private const int TestPriority = 200;

    private Camera3D _renderCamera = null!;
    private Node3D _followTarget = null!;
    private PhantomCamera3D _followCamera = null!;
    private PhantomCamera3D _staticCamera = null!;

    public override async void _Ready()
    {
        var automated = HasUserArgument("--m1-spike");

        try
        {
            ResolveNodes();
            await RunSmokeAsync();

            GD.Print("[M1.0] PASS: Phantom Camera C# runtime spike completed successfully.");

            if (automated)
            {
                GetTree().Quit(0);
            }
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.0] FAIL: {exception}");

            if (automated)
            {
                GetTree().Quit(1);
            }
        }
    }

    private void ResolveNodes()
    {
        _renderCamera = GetNode<Camera3D>("RenderCamera");
        _followTarget = GetNode<Node3D>("FollowTarget");
        _followCamera = GetNode<Node3D>("PCams/FollowCamera").AsPhantomCamera3D();
        _staticCamera = GetNode<Node3D>("PCams/StaticCamera").AsPhantomCamera3D();
    }

    private async Task RunSmokeAsync()
    {
        var desktopRenderer = ProjectSettings
            .GetSetting("rendering/renderer/rendering_method")
            .AsString();
        var mobileRenderer = ProjectSettings
            .GetSetting("rendering/renderer/rendering_method.mobile")
            .AsString();

        Require(desktopRenderer == "forward_plus",
            $"Expected desktop renderer 'forward_plus', got '{desktopRenderer}'.");
        Require(mobileRenderer == "mobile",
            $"Expected mobile renderer override 'mobile', got '{mobileRenderer}'.");

        GD.Print($"[M1.0] Godot: {Engine.GetVersionInfo()["string"]}");
        GD.Print($"[M1.0] Platform: {OS.GetName()}");
        GD.Print($"[M1.0] Configured renderer: desktop={desktopRenderer}, mobile={mobileRenderer}");
        GD.Print($"[M1.0] Runtime renderer: {RenderingServer.GetCurrentRenderingMethod()}");

        await WaitFramesAsync(2);

        // Typed C# wrapper: follow target.
        _followCamera.FollowTarget = _followTarget;
        Require(
            _followCamera.FollowTarget.GetInstanceId() == _followTarget.GetInstanceId(),
            "Typed FollowTarget assignment did not round-trip correctly.");

        // Typed C# wrapper: priority and activation.
        _staticCamera.Priority = InactivePriority;
        _followCamera.Priority = GameplayPriority;
        _followCamera.TeleportPosition();
        await WaitFramesAsync(2);

        Require(_followCamera.Priority == GameplayPriority,
            "Typed Priority assignment did not round-trip correctly.");
        Require(_followCamera.IsActive,
            "Follow camera did not become active at gameplay priority.");

        // Typed C# wrapper: teleport/snap should bypass damping after the target moves.
        var cameraPositionBeforeMove = _renderCamera.GlobalPosition;
        _followTarget.GlobalPosition += new Vector3(2.0f, 0.0f, 0.0f);
        _followCamera.TeleportPosition();
        await WaitFramesAsync(2);

        Require(
            _renderCamera.GlobalPosition.DistanceTo(cameraPositionBeforeMove) > 0.01f,
            "Render Camera3D did not respond to the followed target after TeleportPosition().");

        // A higher-priority PCam must take control of the same RenderCamera.
        _staticCamera.Priority = TestPriority;
        await WaitFramesAsync(2);
        Require(_staticCamera.IsActive,
            "Static camera did not become active after receiving the higher priority.");

        // Switching back validates a second priority transition and instant snap.
        _followCamera.Priority = TestPriority + GameplayPriority;
        _followCamera.TeleportPosition();
        await WaitFramesAsync(2);
        Require(_followCamera.IsActive,
            "Follow camera did not regain control after its priority was raised.");
    }

    private async Task WaitFramesAsync(int frameCount)
    {
        for (var i = 0; i < frameCount; i++)
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

    private static bool HasUserArgument(string expected)
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument == expected)
            {
                return true;
            }
        }

        return false;
    }
}
