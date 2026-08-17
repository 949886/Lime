using Godot;

namespace Lime.Diagnostics.Reference;

[GlobalClass]
public partial class ReferenceFrame : Resource
{
    [Export] public double TimestampSeconds { get; set; }
    [Export] public int SourceFrame { get; set; }
    [Export] public Vector2I SourceSize { get; set; } = new(2548, 1426);
    [Export] public Texture2D Image { get; set; } = null!;
}
