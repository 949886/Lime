using Godot;
using Godot.Collections;

namespace Lime.Diagnostics.Reference.V2;

[GlobalClass]
public partial class ReferenceCameraTransitionSample : Resource
{
    [Export] public double TimestampSeconds { get; set; }
    [Export] public int SourceFrame { get; set; }
    [Export] public Vector2 PlayerFeetPixel { get; set; } = new(-1.0f, -1.0f);
    [Export] public float PlayerPixelHeight { get; set; } = -1.0f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float PlayerConfidence { get; set; }
    [Export] public Array<ReferenceLandmarkObservation> Landmarks { get; set; } = new();

    public bool HasMeasuredPlayer =>
        PlayerFeetPixel.X >= 0.0f && PlayerFeetPixel.Y >= 0.0f && PlayerPixelHeight > 0.0f;
}
