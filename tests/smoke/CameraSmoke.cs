using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game.Camera;
using PhantomCamera;

namespace Lime.Tests.Smoke;

public partial class CameraSmoke : Node3D
{
    public override async void _Ready()
    {
        try
        {
            var cameraDirector = GetNode<CameraDirector>("%CameraSystem");
            var followTarget = GetNode<Node3D>("%FollowTarget");
            var perspective = cameraDirector
                .GetNode<Node3D>("PCams/ExplorePerspective")
                .AsPhantomCamera3D();
            var orthographic = cameraDirector
                .GetNode<Node3D>("PCams/ExploreOrthographic")
                .AsPhantomCamera3D();

            cameraDirector.BindExploreTarget(followTarget);
            Require(perspective.FollowTarget.GetInstanceId() == followTarget.GetInstanceId(),
                "Perspective PCam must follow the bound Explore target.");
            Require(orthographic.FollowTarget.GetInstanceId() == followTarget.GetInstanceId(),
                "Orthographic PCam must follow the bound Explore target.");

            cameraDirector.ActivateInstant(CameraId.ExplorePerspective);
            await WaitFramesAsync(3);

            Require(cameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
                "Perspective must be the active semantic camera after instant activation.");
            Require(perspective.IsActive,
                "Perspective PhantomCamera3D must become active.");
            Require((int)cameraDirector.RenderCamera.Projection == 0,
                "Perspective PCam must apply perspective projection to RenderCamera.");

            var positionBeforeMove = cameraDirector.RenderCamera.GlobalPosition;
            followTarget.GlobalPosition += new Vector3(2.0f, 0.0f, 0.0f);
            cameraDirector.SnapActiveToTarget();
            await WaitFramesAsync(2);

            Require(cameraDirector.RenderCamera.GlobalPosition.DistanceTo(positionBeforeMove) > 0.01f,
                "SnapActiveToTarget must move RenderCamera with the followed target.");

            cameraDirector.Activate(CameraId.ExploreOrthographic);
            Require(cameraDirector.ActiveCameraId == CameraId.ExploreOrthographic,
                "Activate must update the active semantic camera id.");
            Require(orthographic.Priority == 100 && perspective.Priority == 0,
                "Activate must apply the gameplay priority policy.");

            cameraDirector.ActivateInstant(CameraId.ExploreOrthographic);
            await WaitFramesAsync(3);

            Require(orthographic.IsActive,
                "Orthographic PhantomCamera3D must become active.");
            Require((int)cameraDirector.RenderCamera.Projection == 1,
                "Orthographic PCam must apply orthographic projection to RenderCamera.");

            cameraDirector.ActivateInstant(CameraId.ExplorePerspective);
            await WaitFramesAsync(3);
            Require(perspective.IsActive,
                "Perspective PhantomCamera3D must regain control after A/B switching.");
            Require((int)cameraDirector.RenderCamera.Projection == 0,
                "RenderCamera must return to perspective projection after switching back.");

            GD.Print("[M1.3] PASS: Camera runtime smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.3] FAIL: {exception}");
            GetTree().Quit(1);
        }
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
