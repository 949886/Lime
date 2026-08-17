using Godot;

namespace Lime.Diagnostics.Reference;

[GlobalClass]
public partial class ReferenceCheckpoint : Resource
{
    [Export] public ReferenceCheckpointId Id { get; set; }
    [Export] public ReferenceFrame Frame { get; set; } = null!;
    [Export] public NodePath MarkerPath { get; set; } = new();
}
