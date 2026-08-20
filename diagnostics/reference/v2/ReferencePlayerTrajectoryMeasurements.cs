using Godot;

namespace Lime.Diagnostics.Reference.V2;

/// <summary>
/// Manual player-image measurements taken from the original 2548x1426 / 30 FPS capture.
/// FeetPixel is the ground-contact/root estimate. PixelHeight is measured from the top of
/// Angelina's head/ear silhouette to the feet anchor, excluding weapons and long trailing cloth.
/// Values are intentionally quantized to roughly 5-10 px; confidence records occlusion/cursor/UI
/// interference. Frames with no defensible observation are omitted rather than guessed.
/// </summary>
public static class ReferencePlayerTrajectoryMeasurements
{
    public readonly record struct Measurement(Vector2 FeetPixel, float PixelHeight, float Confidence);

    public static bool TryGet(int sourceFrame, out Measurement measurement)
    {
        measurement = sourceFrame switch
        {
            195 => new(new Vector2(1250.0f, 1040.0f), 515.0f, 0.95f), // 6.50s
            210 => new(new Vector2(1255.0f, 1040.0f), 525.0f, 0.95f), // 7.00s
            225 => new(new Vector2(1250.0f, 1040.0f), 530.0f, 0.95f), // 7.50s
            240 => new(new Vector2(1255.0f, 1035.0f), 535.0f, 0.95f), // 8.00s
            255 => new(new Vector2(1250.0f, 1040.0f), 525.0f, 0.95f), // 8.50s
            270 => new(new Vector2(1280.0f, 1045.0f), 530.0f, 0.95f), // 9.00s
            278 => new(new Vector2(1240.0f, 1090.0f), 470.0f, 0.90f), // 9.25s
            285 => new(new Vector2(1280.0f, 1040.0f), 340.0f, 0.90f), // 9.50s
            293 => new(new Vector2(1260.0f, 895.0f), 245.0f, 0.90f), // 9.75s
            300 => new(new Vector2(1270.0f, 865.0f), 230.0f, 0.90f), // 10.00s
            315 => new(new Vector2(1260.0f, 830.0f), 185.0f, 0.90f), // 10.50s
            330 => new(new Vector2(1270.0f, 815.0f), 170.0f, 0.90f), // 11.00s
            345 => new(new Vector2(1280.0f, 885.0f), 195.0f, 0.90f), // 11.50s
            360 => new(new Vector2(1370.0f, 870.0f), 190.0f, 0.90f), // 12.00s
            375 => new(new Vector2(1300.0f, 845.0f), 170.0f, 0.90f), // 12.50s
            390 => new(new Vector2(1320.0f, 835.0f), 165.0f, 0.90f), // 13.00s
            405 => new(new Vector2(1290.0f, 835.0f), 185.0f, 0.90f), // 13.50s
            420 => new(new Vector2(1360.0f, 830.0f), 180.0f, 0.90f), // 14.00s
            435 => new(new Vector2(1290.0f, 815.0f), 170.0f, 0.90f), // 14.50s
            450 => new(new Vector2(1360.0f, 825.0f), 175.0f, 0.90f), // 15.00s
            465 => new(new Vector2(1320.0f, 895.0f), 195.0f, 0.85f), // 15.50s
            510 => new(new Vector2(1410.0f, 860.0f), 170.0f, 0.75f), // 17.00s
            525 => new(new Vector2(1370.0f, 850.0f), 190.0f, 0.90f), // 17.50s
            540 => new(new Vector2(1330.0f, 845.0f), 195.0f, 0.90f), // 18.00s
            555 => new(new Vector2(1270.0f, 825.0f), 180.0f, 0.90f), // 18.50s
            570 => new(new Vector2(1430.0f, 800.0f), 200.0f, 0.90f), // 19.00s
            585 => new(new Vector2(1450.0f, 790.0f), 150.0f, 0.55f), // 19.50s
            600 => new(new Vector2(1450.0f, 810.0f), 190.0f, 0.90f), // 20.00s
            615 => new(new Vector2(1380.0f, 805.0f), 175.0f, 0.75f), // 20.50s
            630 => new(new Vector2(1360.0f, 800.0f), 190.0f, 0.90f), // 21.00s
            645 => new(new Vector2(1310.0f, 780.0f), 190.0f, 0.90f), // 21.50s
            660 => new(new Vector2(1330.0f, 780.0f), 180.0f, 0.90f), // 22.00s
            1305 => new(new Vector2(1300.0f, 800.0f), 180.0f, 0.60f), // 43.50s
            1320 => new(new Vector2(1300.0f, 800.0f), 180.0f, 0.90f), // 44.00s
            1335 => new(new Vector2(1290.0f, 820.0f), 200.0f, 0.65f), // 44.50s
            1350 => new(new Vector2(1290.0f, 820.0f), 200.0f, 0.65f), // 45.00s
            1365 => new(new Vector2(1290.0f, 820.0f), 200.0f, 0.65f), // 45.50s
            1380 => new(new Vector2(1290.0f, 820.0f), 200.0f, 0.65f), // 46.00s
            1395 => new(new Vector2(1140.0f, 810.0f), 200.0f, 0.75f), // 46.50s
            1410 => new(new Vector2(1090.0f, 800.0f), 180.0f, 0.55f), // 47.00s
            1425 => new(new Vector2(1080.0f, 790.0f), 180.0f, 0.55f), // 47.50s
            1440 => new(new Vector2(1300.0f, 790.0f), 180.0f, 0.60f), // 48.00s
            1455 => new(new Vector2(1320.0f, 820.0f), 170.0f, 0.70f), // 48.50s
            1470 => new(new Vector2(1390.0f, 850.0f), 160.0f, 0.85f), // 49.00s
            1485 => new(new Vector2(1340.0f, 820.0f), 170.0f, 0.65f), // 49.50s
            1500 => new(new Vector2(1420.0f, 840.0f), 190.0f, 0.65f), // 50.00s
            1515 => new(new Vector2(1330.0f, 850.0f), 180.0f, 0.65f), // 50.50s
            1530 => new(new Vector2(1370.0f, 850.0f), 160.0f, 0.65f), // 51.00s
            1545 => new(new Vector2(1300.0f, 820.0f), 150.0f, 0.65f), // 51.50s
            1710 => new(new Vector2(1320.0f, 830.0f), 180.0f, 0.60f), // 57.00s
            1725 => new(new Vector2(1320.0f, 830.0f), 180.0f, 0.60f), // 57.50s
            1740 => new(new Vector2(1320.0f, 820.0f), 160.0f, 0.60f), // 58.00s
            1755 => new(new Vector2(1410.0f, 820.0f), 170.0f, 0.90f), // 58.50s
            1770 => new(new Vector2(1300.0f, 840.0f), 190.0f, 0.80f), // 59.00s
            1785 => new(new Vector2(1340.0f, 840.0f), 190.0f, 0.80f), // 59.50s
            1800 => new(new Vector2(1310.0f, 840.0f), 190.0f, 0.80f), // 60.00s
            _ => default,
        };

        return measurement.PixelHeight > 0.0f;
    }
}
