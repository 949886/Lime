using System.Collections.Generic;
using Godot;

namespace Lime.Diagnostics.Reference.Solve;

/// <summary>
/// Camera-only line observations from the original 2548x1426 source frame 264
/// (8.80s, stable StartHold). These segments follow visible longitudinal seams
/// in the square-tile floor. They describe parallel world directions, not world
/// positions, so their common vanishing point is independent of whitebox scale
/// and translation.
/// </summary>
public static class ReferenceStartFloorGridMeasurements
{
    public const int SourceFrame = 264;

    public static IReadOnlyList<ReferenceLineObservation> LongitudinalLines { get; } =
        new ReferenceLineObservation[]
        {
            new(new Vector2(872.0f, 1250.0f), new Vector2(974.0f, 802.0f), 0.90f),
            new(new Vector2(746.0f, 1282.0f), new Vector2(796.0f, 1128.0f), 0.70f),
            new(new Vector2(1453.0f, 1100.0f), new Vector2(1472.0f, 1256.0f), 0.75f),
            new(new Vector2(1851.0f, 803.0f), new Vector2(2067.0f, 1227.0f), 0.90f),
        };
}
