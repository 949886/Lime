using Godot;
using Godot.Collections;

namespace Lime.Diagnostics.Reference.V2;

[GlobalClass]
public partial class ReferenceDatasetV2 : Resource
{
    [Export] public Vector2I SourceSize { get; set; } = new(2548, 1426);
    [Export] public double SourceFps { get; set; } = 30.0;
    [Export] public double DurationSeconds { get; set; } = 64.55;
    [Export] public Array<ReferenceExploreSegment> ExploreSegments { get; set; } = new();
    [Export] public Array<ReferenceShot> Shots { get; set; } = new();
    [Export] public Array<ReferenceTrajectorySample> Trajectory { get; set; } = new();
}
