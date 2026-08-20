using Godot;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Diagnostics.Reference.Solve;

public readonly record struct ReferenceProjectionError(
    ReferenceCalibrationLandmarkId LandmarkId,
    Vector2 ReferencePixel,
    Vector2 ProjectedPixel,
    Vector2 DeltaPixel,
    float PixelError,
    float Weight);
