using System;
using Godot;
using PhantomCamera;

namespace Lime.Game.Camera;

public partial class CameraDirector : Node3D
{
    private const int InactivePriority = 0;
    private const int GameplayPriority = 100;

    private PhantomCamera3D _explorePerspective = null!;
    private PhantomCamera3D _exploreOrthographic = null!;

    public Camera3D RenderCamera { get; private set; } = null!;
    public CameraId ActiveCameraId { get; private set; } = CameraId.ExplorePerspective;

    public override void _Ready()
    {
        RenderCamera = GetNode<Camera3D>("%RenderCamera");
        _explorePerspective = GetNode<Node3D>("PCams/ExplorePerspective").AsPhantomCamera3D();
        _exploreOrthographic = GetNode<Node3D>("PCams/ExploreOrthographic").AsPhantomCamera3D();

        _explorePerspective.Priority = InactivePriority;
        _exploreOrthographic.Priority = InactivePriority;
    }

    public void BindExploreTarget(Node3D target)
    {
        ArgumentNullException.ThrowIfNull(target);

        _explorePerspective.FollowTarget = target;
        _exploreOrthographic.FollowTarget = target;
    }

    public void Activate(CameraId id)
    {
        var active = GetCamera(id);
        var inactive = GetOtherCamera(id);

        inactive.Priority = InactivePriority;
        active.Priority = GameplayPriority;
        ActiveCameraId = id;
    }

    public void ActivateInstant(CameraId id)
    {
        var active = GetCamera(id);
        var duration = active.TweenDuration;

        try
        {
            // Phantom Camera also tweens Camera3DResource properties such as
            // FOV / projection size. Temporarily use its official zero-duration
            // path so "Instant" means the complete camera state, not only the
            // followed transform. The configured tween duration remains the
            // single source of truth for normal Activate() transitions.
            active.TweenDuration = 0.0f;
            Activate(id);
            active.TeleportPosition();
        }
        finally
        {
            active.TweenDuration = duration;
        }
    }

    public void SnapActiveToTarget()
    {
        GetCamera(ActiveCameraId).TeleportPosition();
    }

    private PhantomCamera3D GetCamera(CameraId id)
    {
        return id switch
        {
            CameraId.ExplorePerspective => _explorePerspective,
            CameraId.ExploreOrthographic => _exploreOrthographic,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown camera id."),
        };
    }

    private PhantomCamera3D GetOtherCamera(CameraId id)
    {
        return id switch
        {
            CameraId.ExplorePerspective => _exploreOrthographic,
            CameraId.ExploreOrthographic => _explorePerspective,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown camera id."),
        };
    }
}
