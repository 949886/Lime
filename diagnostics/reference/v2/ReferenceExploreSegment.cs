using Godot;

namespace Lime.Diagnostics.Reference.V2;

[GlobalClass]
public partial class ReferenceExploreSegment : Resource
{
    [Export] public ReferenceExploreSegmentId Id { get; set; }
    [Export] public double StartSeconds { get; set; }
    [Export] public double EndSeconds { get; set; }

    public bool Contains(double timestampSeconds) =>
        timestampSeconds >= StartSeconds && timestampSeconds <= EndSeconds;
}
