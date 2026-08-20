using Godot;

namespace Lime.Diagnostics.Reference.Solve;

public readonly record struct ReferenceVanishingPointResult(
    Vector2 Pixel,
    float WeightedRmsLineDistance,
    int LineCount);
