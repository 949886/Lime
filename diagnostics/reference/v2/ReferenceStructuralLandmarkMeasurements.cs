using Godot;

namespace Lime.Diagnostics.Reference.V2;

/// <summary>
/// Manual structural observations measured from the original 2548x1426 capture.
/// Points name one concrete image-observable world feature and are deliberately
/// repeated across shots. Optical flow is used only to carry a manually selected
/// feature between adjacent source frames; every checked-in observation was
/// visually reviewed against the source frame before being accepted.
/// </summary>
public static class ReferenceStructuralLandmarkMeasurements
{
    public static void Populate(ReferenceShot shot)
    {
        switch (shot.SourceFrame)
        {
            case 195: // 6.50s start_entry
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft, 1580.0f, 366.0f, 0.95f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperRight, 1755.0f, 366.0f, 0.95f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridLowerLeft, 1578.0f, 446.0f, 0.95f);
                break;

            case 255: // 8.50s start_hold
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft, 1580.0f, 366.0f, 0.95f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperRight, 1755.0f, 366.0f, 0.95f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridLowerLeft, 1578.0f, 446.0f, 0.95f);
                break;

            case 270: // 9.00s pullback_begin
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft, 1579.0f, 366.0f, 0.95f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperRight, 1754.0f, 366.0f, 0.95f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridLowerLeft, 1577.0f, 446.0f, 0.95f);
                break;

            case 281: // 9.35s pullback_mid
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft, 1508.0f, 381.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperRight, 1643.0f, 382.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridLowerLeft, 1507.0f, 442.0f, 0.90f);
                break;

            case 293: // 9.75s pullback_end
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft, 1444.0f, 349.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridUpperRight, 1544.0f, 349.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StartGateGridLowerLeft, 1444.0f, 393.0f, 0.90f);
                break;

            case 315: // 10.50s stairs_a
                Add(shot, ReferenceCalibrationLandmarkId.StairsATopLeft, 1015.0f, 690.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsATopRight, 1540.0f, 690.0f, 0.90f);
                break;

            case 351: // 11.70s mid_entry
                Add(shot, ReferenceCalibrationLandmarkId.StairsATopLeft, 1012.0f, 573.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsATopRight, 1508.0f, 577.0f, 0.85f);
                break;

            case 375: // 12.50s mid_terrace
                Add(shot, ReferenceCalibrationLandmarkId.StairsATopLeft, 782.0f, 449.0f, 0.80f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsATopRight, 1225.0f, 454.0f, 0.80f);
                break;

            case 426: // 14.20s stairs_b_approach
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopLeft, 1375.0f, 840.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopRight, 1785.0f, 840.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBBottomLeft, 1370.0f, 1190.0f, 0.85f);
                break;

            case 486: // 16.20s stairs_b_bottom
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopLeft, 1152.0f, 480.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopRight, 1488.0f, 502.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBBottomLeft, 1126.0f, 751.0f, 0.80f);
                break;

            case 525: // 17.50s tower_pass
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopLeft, 981.0f, 384.0f, 0.80f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopRight, 1288.0f, 410.0f, 0.80f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBBottomLeft, 950.0f, 622.0f, 0.75f);
                break;

            case 570: // 19.00s lower_corridor
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopLeft, 885.0f, 377.0f, 0.75f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBTopRight, 1216.0f, 406.0f, 0.75f);
                Add(shot, ReferenceCalibrationLandmarkId.StairsBBottomLeft, 848.0f, 630.0f, 0.70f);
                break;

            case 1395: // 46.50s pool_entry
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 1320.0f, 300.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 1800.0f, 500.0f, 0.85f);
                break;

            case 1440: // 48.00s pool_walkway
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 1695.0f, 399.0f, 0.80f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 2226.0f, 636.0f, 0.75f);
                break;

            case 1485: // 49.50s worker_plaza_entry
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 966.0f, 221.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 1599.0f, 505.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopLeft, 1845.0f, 272.0f, 0.90f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopRight, 2075.0f, 272.0f, 0.90f);
                break;

            case 1530: // 51.00s worker_approach
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 369.0f, 185.0f, 0.80f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 985.0f, 429.0f, 0.80f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopLeft, 1302.0f, 242.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopRight, 1510.0f, 241.0f, 0.85f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerRoundDoorCenter, 1950.0f, 540.0f, 0.75f);
                break;

            case 1701: // 56.70s explore_c_resume
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 370.0f, 185.0f, 0.75f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 985.0f, 430.0f, 0.75f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopLeft, 1302.0f, 242.0f, 0.75f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopRight, 1510.0f, 241.0f, 0.75f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerRoundDoorCenter, 1950.0f, 540.0f, 0.70f);
                break;

            case 1740: // 58.00s final_route
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 380.0f, 200.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 1011.0f, 423.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopLeft, 1287.0f, 244.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopRight, 1512.0f, 237.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerRoundDoorCenter, 1950.0f, 545.0f, 0.70f);
                break;

            case 1785: // 59.50s final_explore
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationPeak, 170.0f, 161.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.BlueFormationRightUpper, 836.0f, 423.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopLeft, 1120.0f, 244.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerUpperStairsTopRight, 1342.0f, 235.0f, 0.70f);
                Add(shot, ReferenceCalibrationLandmarkId.WorkerRoundDoorCenter, 1757.0f, 545.0f, 0.70f);
                break;
        }
    }

    private static void Add(
        ReferenceShot shot,
        ReferenceCalibrationLandmarkId landmarkId,
        float x,
        float y,
        float confidence,
        bool isOccluded = false)
    {
        shot.Landmarks.Add(new ReferenceLandmarkObservation
        {
            LandmarkId = landmarkId,
            Pixel = new Vector2(x, y),
            Confidence = confidence,
            IsOccluded = isOccluded,
        });
    }
}
