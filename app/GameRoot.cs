using Godot;
using Lime.World;

namespace Lime.App;

public partial class GameRoot : Node
{
    public FlowController FlowController { get; private set; } = null!;
    public Node3D WorldRoot { get; private set; } = null!;
    public Node3D LevelRoot { get; private set; } = null!;
    public Node3D ActorRoot { get; private set; } = null!;
    public Node3D CameraRoot { get; private set; } = null!;
    public CanvasLayer UIRoot { get; private set; } = null!;
    public Node AudioRoot { get; private set; } = null!;

    public override void _Ready()
    {
        ResolveOwnedNodes();
        ValidateProjectContracts();
        FlowController.Start();
    }

    private void ResolveOwnedNodes()
    {
        FlowController = GetNode<FlowController>("%FlowController");
        WorldRoot = GetNode<Node3D>("%WorldRoot");
        LevelRoot = GetNode<Node3D>("%LevelRoot");
        ActorRoot = GetNode<Node3D>("%ActorRoot");
        CameraRoot = GetNode<Node3D>("%CameraRoot");
        UIRoot = GetNode<CanvasLayer>("%UIRoot");
        AudioRoot = GetNode<Node>("%AudioRoot");
    }

    private static void ValidateProjectContracts()
    {
        InputActions.ValidateConfigured();
        CollisionLayers.ValidateConfigured();
    }
}
