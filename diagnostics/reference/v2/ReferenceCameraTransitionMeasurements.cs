using Godot;

namespace Lime.Diagnostics.Reference.V2;

/// <summary>
/// Dense 0.1 s measurements through the Start -> Explore pullback. Static gate
/// landmarks were carried frame-to-frame with optical flow and manually reviewed.
/// Player measurements are deliberately quantized and confidence-weighted. The
/// near stair pair is a second-depth baseline used to distinguish pure zoom from
/// camera translation / pitch / target motion.
/// </summary>
public static class ReferenceCameraTransitionMeasurements
{
    public static void Populate(ReferenceDatasetV2 dataset)
    {
        Add(dataset, 264, 8.80, 1250, 1040, 530, 0.90f,
            1580,366,1755,366,1578,446, 917,1237,2136,1233);
        Add(dataset, 267, 8.90, 1250, 1040, 530, 0.90f,
            1580,366,1755,366,1578,446, 917,1237,2136,1233);
        Add(dataset, 270, 9.00, 1280, 1045, 530, 0.95f,
            1579,366,1754,366,1577,446, 919,1233,2131,1229);
        Add(dataset, 273, 9.10, 1260, 1060, 525, 0.85f,
            1569,371,1738,371,1567,448, 940,1195,2091,1194);
        Add(dataset, 276, 9.20, 1250, 1080, 505, 0.85f,
            1549,378,1707,378,1547,450, 977,1118,2019,1118);
        Add(dataset, 279, 9.30, 1250, 1090, 475, 0.85f,
            1524,383,1669,383,1523,449, 1018,1026,1938,1027);
        Add(dataset, 282, 9.40, 1260, 1080, 430, 0.80f,
            1500,377,1632,378,1500,437, 1054,925,1860,926);
        Add(dataset, 285, 9.50, 1280, 1040, 340, 0.90f,
            1477,361,1596,361,1477,415, 1084,833,1789,833);
        Add(dataset, 288, 9.60, 1270, 990, 300, 0.85f,
            1461,351,1571,351,1461,400, 1104,766,1727,766);
        Add(dataset, 291, 9.70, 1260, 940, 260, 0.85f,
            1447,348,1550,348,1447,393, 1120,722,1675,722);
        Add(dataset, 294, 9.80, 1260, 900, 235, 0.80f,
            1436,353,1533,353,1437,396, 1131,699,1637,699);
        Add(dataset, 297, 9.90, 1260, 875, 220, 0.80f,
            1429,361,1521,362,1429,402, 1138,688,1609,688);
        Add(dataset, 300, 10.00, 1270, 865, 230, 0.90f,
            1421,373,1509,374,1422,412, 1142,682,1571,682);
        Add(dataset, 303, 10.10, 1270, 855, 205, 0.80f,
            1415,385,1499,385,1416,422, 1150,680,1556,680);
        Add(dataset, 306, 10.20, 1265, 850, 195, 0.80f,
            1410,394,1491,395,1411,430, 1155,679,1544,679);
        Add(dataset, 309, 10.30, 1265, 845, 190, 0.80f,
            1407,401,1486,402,1408,436, 1159,679,1536,679);
        Add(dataset, 312, 10.40, 1260, 840, 185, 0.85f,
            1405,405,1483,405,1406,440, 1161,679,1532,679);
        Add(dataset, 315, 10.50, 1260, 830, 185, 0.90f,
            1404,406,1483,406,1406,440, 1162,678,1531,678);
    }

    private static void Add(
        ReferenceDatasetV2 dataset,
        int frame,
        double timestamp,
        float playerX,
        float playerY,
        float playerHeight,
        float playerConfidence,
        float gateUlX,
        float gateUlY,
        float gateUrX,
        float gateUrY,
        float gateLlX,
        float gateLlY,
        float stairLeftX,
        float stairLeftY,
        float stairRightX,
        float stairRightY)
    {
        var sample = new ReferenceCameraTransitionSample
        {
            TimestampSeconds = timestamp,
            SourceFrame = frame,
            PlayerFeetPixel = new Vector2(playerX, playerY),
            PlayerPixelHeight = playerHeight,
            PlayerConfidence = playerConfidence,
        };

        AddLandmark(sample, ReferenceCalibrationLandmarkId.StartGateGridUpperLeft, gateUlX, gateUlY, 0.90f);
        AddLandmark(sample, ReferenceCalibrationLandmarkId.StartGateGridUpperRight, gateUrX, gateUrY, 0.90f);
        AddLandmark(sample, ReferenceCalibrationLandmarkId.StartGateGridLowerLeft, gateLlX, gateLlY, 0.90f);
        AddLandmark(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft, stairLeftX, stairLeftY, 0.80f);
        AddLandmark(sample, ReferenceCalibrationLandmarkId.StartForegroundStairTopRight, stairRightX, stairRightY, 0.80f);

        dataset.CameraTransitionSamples.Add(sample);
    }

    private static void AddLandmark(
        ReferenceCameraTransitionSample sample,
        ReferenceCalibrationLandmarkId id,
        float x,
        float y,
        float confidence)
    {
        sample.Landmarks.Add(new ReferenceLandmarkObservation
        {
            LandmarkId = id,
            Pixel = new Vector2(x, y),
            Confidence = confidence,
        });
    }
}
