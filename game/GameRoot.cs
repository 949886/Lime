using Godot;
using Lime.Game.Actors.Player;
using Lime.Game.Camera;
using Lime.Game.World;
using Lime.Game.World.Levels.Reference;

namespace Lime.Game;

public partial class GameRoot : Node
{
    private const float PullbackMovementThreshold = 0.05f;

    public FlowController FlowController { get; private set; } = null!;
    public Node3D WorldRoot { get; private set; } = null!;
    public Node3D LevelRoot { get; private set; } = null!;
    public ReferenceLevel ReferenceLevel { get; private set; } = null!;
    public Node3D ActorRoot { get; private set; } = null!;
    public PlayerController Player { get; private set; } = null!;
    public Node3D CameraRoot { get; private set; } = null!;
    public CameraDirector CameraDirector { get; private set; } = null!;
    public CanvasLayer UIRoot { get; private set; } = null!;
    public Node AudioRoot { get; private set; } = null!;

    public override void _Ready()
    {
        ResolveOwnedNodes();
        ValidateProjectContracts();
        PlacePlayerAtReferenceStart();
        WireCamera();

        FlowController.StateChanged += SyncGameplayInput;
        FlowController.Start();
        SyncGameplayInput();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (CameraDirector.ActiveCameraId != CameraId.StartPerspective)
        {
            return;
        }

        var horizontalVelocity = new Vector2(Player.Velocity.X, Player.Velocity.Z);
        if (horizontalVelocity.Length() > PullbackMovementThreshold)
        {
            // The reference video holds a close composition through ~9.0s,
            // then pulls back as the player starts moving. ExplorePerspective's
            // 1.5s Phantom Camera tween reproduces that transition without ever
            // changing CharacterVisual scale.
            CameraDirector.Activate(CameraId.ExplorePerspective);
        }
    }

    public override void _ExitTree()
    {
        if (FlowController is not null)
        {
            FlowController.StateChanged -= SyncGameplayInput;
        }
    }

    private void ResolveOwnedNodes()
    {
        FlowController = GetNode<FlowController>("%FlowController");
        WorldRoot = GetNode<Node3D>("%WorldRoot");
        LevelRoot = GetNode<Node3D>("%LevelRoot");
        ReferenceLevel = GetNode<ReferenceLevel>("%ReferenceLevel");
        ActorRoot = GetNode<Node3D>("%ActorRoot");
        Player = GetNode<PlayerController>("%Player");
        CameraRoot = GetNode<Node3D>("%CameraRoot");
        CameraDirector = GetNode<CameraDirector>("%CameraSystem");
        UIRoot = GetNode<CanvasLayer>("%UIRoot");
        AudioRoot = GetNode<Node>("%AudioRoot");
    }

    private static void ValidateProjectContracts()
    {
        InputActions.ValidateConfigured();
        CollisionLayers.ValidateConfigured();
    }

    private void PlacePlayerAtReferenceStart()
    {
        Player.GlobalPosition = ReferenceLevel.PlayerStart.GlobalPosition;
        Player.Velocity = Vector3.Zero;
    }

    private void WireCamera()
    {
        CameraDirector.BindExploreTarget(Player);
        Player.SetMovementReference(CameraDirector.RenderCamera);
        CameraDirector.ActivateInstant(CameraId.StartPerspective);
    }

    private void SyncGameplayInput()
    {
        Player.SetInputEnabled(FlowController.GameplayInputEnabled);
    }
}
