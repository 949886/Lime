using Godot;

namespace Lime.Diagnostics.Reference.Solve;

public readonly record struct ReferenceLineObservation(
    Vector2 StartPixel,
    Vector2 EndPixel,
    float Confidence = 1.0f)
{
    public Vector2 Direction => EndPixel - StartPixel;
}
