using Godot;

namespace Lime.Diagnostics.Reference.V2;

[GlobalClass]
public partial class ReferenceLandmarkObservation : Resource
{
    [Export] public ReferenceWorldAnchorId AnchorId { get; set; }
    [Export] public Vector2 Pixel { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float Confidence { get; set; } = 1.0f;
    [Export] public bool IsOccluded { get; set; }
}
