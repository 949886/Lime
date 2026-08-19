namespace Lime.Diagnostics.Reference.V2;

/// <summary>
/// Stable, image-observable calibration points. These IDs intentionally do not
/// describe gameplay route semantics; ReferenceWorldAnchorId remains the route/
/// topology vocabulary. A calibration landmark names one specific world point
/// that can be projected into multiple reference shots.
/// </summary>
public enum ReferenceCalibrationLandmarkId
{
    Unknown = 0,

    StartGateGridUpperLeft = 100,
    StartGateGridUpperRight = 101,
    StartGateGridLowerLeft = 102,

    StairsATopLeft = 200,
    StairsATopRight = 201,

    StairsBTopLeft = 300,
    StairsBTopRight = 301,
    StairsBBottomLeft = 302,

    BlueFormationPeak = 400,
    BlueFormationRightUpper = 401,

    WorkerUpperStairsTopLeft = 500,
    WorkerUpperStairsTopRight = 501,
    WorkerRoundDoorCenter = 510,
}
