using Godot;
using Godot.Collections;

namespace Lime.Diagnostics.Reference.V2;

[GlobalClass]
public partial class ReferenceShot : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public ReferenceExploreSegmentId SegmentId { get; set; }
    [Export] public double TimestampSeconds { get; set; }
    [Export] public int SourceFrame { get; set; }
    [Export] public ReferenceCameraPhase CameraPhase { get; set; }
    [Export] public Array<ReferenceLandmarkObservation> Landmarks { get; set; } = new();
}
