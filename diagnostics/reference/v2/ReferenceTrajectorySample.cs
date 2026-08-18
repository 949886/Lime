using Godot;

namespace Lime.Diagnostics.Reference.V2;

[GlobalClass]
public partial class ReferenceTrajectorySample : Resource
{
    [Export] public ReferenceExploreSegmentId SegmentId { get; set; }
    [Export] public double TimestampSeconds { get; set; }
    [Export] public int SourceFrame { get; set; }
    [Export] public Vector2 PlayerFeetPixel { get; set; } = new(-1.0f, -1.0f);
    [Export] public float PlayerPixelHeight { get; set; } = -1.0f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Confidence { get; set; }

    public bool HasMeasuredPlayerFeet => PlayerFeetPixel.X >= 0.0f && PlayerFeetPixel.Y >= 0.0f;
    public bool HasMeasuredPlayerHeight => PlayerPixelHeight > 0.0f;
}
