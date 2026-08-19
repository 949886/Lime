using Godot;

namespace Lime.Diagnostics.Reference.Solve;

public readonly record struct ReferencePointProjection(
    Vector2 ReferencePixel,
    Vector2 ProjectedPixel,
    Vector2 Delta,
    float PixelError);
