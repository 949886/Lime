using System;
using System.Collections.Generic;
using Godot;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Tests.Smoke;

public partial class ReferenceDatasetV2Smoke : Node
{
    public override void _Ready()
    {
        try
        {
            var dataset = ReferenceDatasetV2Catalog.CreateSeed();
            ValidateSource(dataset);
            ValidateSegments(dataset);
            ValidateShots(dataset);
            ValidateTrajectory(dataset);
            ValidateCameraTransition(dataset);
            ValidateLegacyComparatorAssets();

            GD.Print($"[M1.6.2] PASS: {dataset.Shots.Count} structure shots / " +
                     $"{dataset.Trajectory.Count} trajectory samples / " +
                     $"{dataset.CameraTransitionSamples.Count} dense camera-transition samples.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.2] FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateSource(ReferenceDatasetV2 dataset)
    {
        Require(dataset.SourceSize == new Vector2I(2548, 1426),
            "Reference Dataset v2 must preserve the original capture size.");
        Require(Math.Abs(dataset.SourceFps - 30.0) < 0.001,
            "Reference Dataset v2 must preserve the 30 FPS capture timeline.");
        Require(Math.Abs(dataset.DurationSeconds - 64.40) < 0.001,
            "Reference Dataset v2 must preserve the measured 64.40 s source duration.");
        Require(dataset.SourceFrameCount == 1932,
            "Reference Dataset v2 must preserve the measured 1932-frame source count.");
    }

    private static void ValidateSegments(ReferenceDatasetV2 dataset)
    {
        Require(dataset.ExploreSegments.Count == 3,
            "Reference Dataset v2 must contain Explore A / B / C.");

        var ids = new HashSet<ReferenceExploreSegmentId>();
        foreach (var segment in dataset.ExploreSegments)
        {
            Require(segment.EndSeconds > segment.StartSeconds,
                $"Segment {segment.Id} must have a positive duration.");
            Require(ids.Add(segment.Id), $"Duplicate segment id: {segment.Id}.");
        }

        Require(ids.Contains(ReferenceExploreSegmentId.ExploreA) &&
                ids.Contains(ReferenceExploreSegmentId.ExploreB) &&
                ids.Contains(ReferenceExploreSegmentId.ExploreC),
            "All three Explore segment ids must be present.");
    }

    private static void ValidateShots(ReferenceDatasetV2 dataset)
    {
        Require(dataset.Shots.Count >= 15,
            "Reference Dataset v2 needs at least 15 structure shots.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var observedSegments = new HashSet<ReferenceExploreSegmentId>();
        var landmarkObservationCounts = new Dictionary<ReferenceCalibrationLandmarkId, int>();
        var totalLandmarkObservations = 0;
        var measuredShotCount = 0;

        foreach (var shot in dataset.Shots)
        {
            Require(!string.IsNullOrWhiteSpace(shot.Id), "Every structure shot needs a stable id.");
            Require(ids.Add(shot.Id), $"Duplicate structure shot id: {shot.Id}.");
            Require(shot.SourceFrame >= 0, $"Shot {shot.Id} must have a source frame.");
            Require(IsInsideSegment(dataset, shot.SegmentId, shot.TimestampSeconds),
                $"Shot {shot.Id} is outside its Explore segment.");
            observedSegments.Add(shot.SegmentId);

            if (shot.Landmarks.Count > 0)
            {
                measuredShotCount++;
            }

            var shotLandmarks = new HashSet<ReferenceCalibrationLandmarkId>();
            foreach (var landmark in shot.Landmarks)
            {
                ValidateLandmark(dataset, landmark, $"Shot {shot.Id}");
                Require(shotLandmarks.Add(landmark.LandmarkId),
                    $"Shot {shot.Id} repeats landmark {landmark.LandmarkId}.");

                totalLandmarkObservations++;
                landmarkObservationCounts[landmark.LandmarkId] =
                    landmarkObservationCounts.GetValueOrDefault(landmark.LandmarkId) + 1;
            }
        }

        Require(observedSegments.Count == 3,
            "Structure shots must cover Explore A / B / C, not only the first segment.");
        Require(measuredShotCount >= 17,
            "M1.6.2 Pass B must retain structural measurements on at least 17 structure shots.");
        Require(totalLandmarkObservations >= 60,
            "M1.6.2 Pass B must retain at least 60 source-frame structural observations.");
        Require(landmarkObservationCounts.Count >= 12,
            "M1.6.2 Pass B needs at least 12 distinct precise calibration landmarks.");

        foreach (var pair in landmarkObservationCounts)
        {
            Require(pair.Value >= 2,
                $"Calibration landmark {pair.Key} must be observed in multiple shots; found {pair.Value}.");
        }

        Require(landmarkObservationCounts.GetValueOrDefault(ReferenceCalibrationLandmarkId.BlueFormationPeak) >= 7,
            "The later blue formation must bridge Explore B and Explore C with repeated observations.");
        Require(landmarkObservationCounts.GetValueOrDefault(ReferenceCalibrationLandmarkId.WorkerUpperStairsTopLeft) >= 5,
            "Worker-area geometry must remain observable through the late route.");
    }

    private static void ValidateTrajectory(ReferenceDatasetV2 dataset)
    {
        var frames = new HashSet<int>();
        var measuredTotal = 0;
        var sampleCountBySegment = new Dictionary<ReferenceExploreSegmentId, int>();
        var measuredCountBySegment = new Dictionary<ReferenceExploreSegmentId, int>();

        foreach (var sample in dataset.Trajectory)
        {
            Require(frames.Add(sample.SourceFrame),
                $"Duplicate trajectory source frame: {sample.SourceFrame}.");
            Require(IsInsideSegment(dataset, sample.SegmentId, sample.TimestampSeconds),
                $"Trajectory frame {sample.SourceFrame} is outside its Explore segment.");

            sampleCountBySegment[sample.SegmentId] =
                sampleCountBySegment.GetValueOrDefault(sample.SegmentId) + 1;

            if (sample.HasMeasuredPlayerFeet || sample.HasMeasuredPlayerHeight)
            {
                Require(sample.HasMeasuredPlayerFeet && sample.HasMeasuredPlayerHeight,
                    $"Trajectory frame {sample.SourceFrame} must measure player feet and height together.");
                Require(sample.PlayerFeetPixel.X >= 0.0f && sample.PlayerFeetPixel.X < dataset.SourceSize.X &&
                        sample.PlayerFeetPixel.Y >= 0.0f && sample.PlayerFeetPixel.Y < dataset.SourceSize.Y,
                    $"Measured trajectory frame {sample.SourceFrame} is outside the source frame.");
                Require(sample.PlayerPixelHeight >= 100.0f && sample.PlayerPixelHeight <= 700.0f,
                    $"Measured trajectory frame {sample.SourceFrame} has implausible apparent height.");
                Require(sample.Confidence > 0.0f && sample.Confidence <= 1.0f,
                    $"Measured trajectory frame {sample.SourceFrame} needs confidence in (0, 1].");

                measuredTotal++;
                measuredCountBySegment[sample.SegmentId] =
                    measuredCountBySegment.GetValueOrDefault(sample.SegmentId) + 1;
            }
        }

        Require(dataset.Trajectory.Count == 59,
            "M1.6.2 Pass A currently expects 59 trajectory samples across all Explore segments.");
        Require(measuredTotal >= 56,
            "M1.6.2 Pass A must retain at least 56 source-frame player measurements.");

        foreach (var segment in dataset.ExploreSegments)
        {
            var first = double.PositiveInfinity;
            var last = double.NegativeInfinity;
            var count = 0;

            foreach (var sample in dataset.Trajectory)
            {
                if (sample.SegmentId != segment.Id)
                {
                    continue;
                }

                first = Math.Min(first, sample.TimestampSeconds);
                last = Math.Max(last, sample.TimestampSeconds);
                count++;
            }

            Require(count >= 5, $"Segment {segment.Id} needs trajectory coverage.");
            Require(first <= segment.StartSeconds + 0.50,
                $"Segment {segment.Id} trajectory starts too late.");
            Require(last >= segment.EndSeconds - 0.50,
                $"Segment {segment.Id} trajectory ends too early.");

            var measured = measuredCountBySegment.GetValueOrDefault(segment.Id);
            var total = sampleCountBySegment.GetValueOrDefault(segment.Id);
            Require(measured >= Math.Max(4, total - 2),
                $"Segment {segment.Id} must keep dense measured player coverage; measured {measured}/{total}.");
        }
    }

    private static void ValidateCameraTransition(ReferenceDatasetV2 dataset)
    {
        Require(dataset.CameraTransitionSamples.Count == 18,
            "M1.6.2 Pass C must retain the 8.8-10.5 s transition at 0.1 s cadence.");

        var previousFrame = -1;
        var previousTimestamp = double.NegativeInfinity;
        var firstGateWidth = -1.0f;
        var finalGateWidth = -1.0f;
        var firstNearWidth = -1.0f;
        var finalNearWidth = -1.0f;

        foreach (var sample in dataset.CameraTransitionSamples)
        {
            Require(sample.SourceFrame > previousFrame,
                "Camera transition source frames must be strictly increasing.");
            Require(sample.TimestampSeconds > previousTimestamp,
                "Camera transition timestamps must be strictly increasing.");
            Require(sample.HasMeasuredPlayer,
                $"Camera transition frame {sample.SourceFrame} needs measured Player framing.");
            Require(sample.PlayerFeetPixel.X >= 0.0f && sample.PlayerFeetPixel.X < dataset.SourceSize.X &&
                    sample.PlayerFeetPixel.Y >= 0.0f && sample.PlayerFeetPixel.Y < dataset.SourceSize.Y,
                $"Camera transition frame {sample.SourceFrame} Player feet are outside the source frame.");
            Require(sample.PlayerPixelHeight >= 150.0f && sample.PlayerPixelHeight <= 600.0f,
                $"Camera transition frame {sample.SourceFrame} Player height is implausible.");
            Require(sample.PlayerConfidence > 0.0f && sample.PlayerConfidence <= 1.0f,
                $"Camera transition frame {sample.SourceFrame} needs Player confidence in (0, 1].");
            Require(sample.Landmarks.Count == 5,
                $"Camera transition frame {sample.SourceFrame} must carry 3 far + 2 near landmarks.");

            var byId = new Dictionary<ReferenceCalibrationLandmarkId, Vector2>();
            foreach (var landmark in sample.Landmarks)
            {
                ValidateLandmark(dataset, landmark, $"Transition frame {sample.SourceFrame}");
                Require(byId.TryAdd(landmark.LandmarkId, landmark.Pixel),
                    $"Transition frame {sample.SourceFrame} repeats {landmark.LandmarkId}.");
            }

            Require(byId.ContainsKey(ReferenceCalibrationLandmarkId.StartGateGridUpperLeft) &&
                    byId.ContainsKey(ReferenceCalibrationLandmarkId.StartGateGridUpperRight) &&
                    byId.ContainsKey(ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft) &&
                    byId.ContainsKey(ReferenceCalibrationLandmarkId.StartForegroundStairTopRight),
                $"Transition frame {sample.SourceFrame} is missing depth-baseline landmarks.");

            var gateWidth = byId[ReferenceCalibrationLandmarkId.StartGateGridUpperLeft]
                .DistanceTo(byId[ReferenceCalibrationLandmarkId.StartGateGridUpperRight]);
            var nearWidth = byId[ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft]
                .DistanceTo(byId[ReferenceCalibrationLandmarkId.StartForegroundStairTopRight]);

            if (firstGateWidth < 0.0f)
            {
                firstGateWidth = gateWidth;
                firstNearWidth = nearWidth;
            }

            finalGateWidth = gateWidth;
            finalNearWidth = nearWidth;
            previousFrame = sample.SourceFrame;
            previousTimestamp = sample.TimestampSeconds;
        }

        Require(Math.Abs(dataset.CameraTransitionSamples[0].TimestampSeconds - 8.80) < 0.001 &&
                Math.Abs(dataset.CameraTransitionSamples[^1].TimestampSeconds - 10.50) < 0.001,
            "Camera transition measurements must cover the measured 8.8-10.5 s window.");
        Require(finalGateWidth < firstGateWidth * 0.55f,
            "Far gate span must show the measured pullback shrink.");
        Require(finalNearWidth < firstNearWidth * 0.40f,
            "Near stair span must show the stronger measured pullback shrink.");

        var farScale = finalGateWidth / firstGateWidth;
        var nearScale = finalNearWidth / firstNearWidth;
        Require(Math.Abs(farScale - nearScale) > 0.05f,
            "Depth baselines must preserve non-uniform scaling; the transition is not a pure image-space zoom.");
    }

    private static void ValidateLandmark(
        ReferenceDatasetV2 dataset,
        ReferenceLandmarkObservation landmark,
        string owner)
    {
        Require(landmark.LandmarkId != ReferenceCalibrationLandmarkId.Unknown,
            $"{owner} contains an unnamed calibration landmark.");
        Require(landmark.Pixel.X >= 0.0f && landmark.Pixel.X < dataset.SourceSize.X &&
                landmark.Pixel.Y >= 0.0f && landmark.Pixel.Y < dataset.SourceSize.Y,
            $"{owner} landmark {landmark.LandmarkId} is outside the source frame.");
        Require(landmark.Confidence > 0.0f && landmark.Confidence <= 1.0f,
            $"{owner} landmark {landmark.LandmarkId} needs confidence in (0, 1].");
    }

    private static void ValidateLegacyComparatorAssets()
    {
        var legacy = new[]
        {
            "res://diagnostics/reference/checkpoints/start.tres",
            "res://diagnostics/reference/checkpoints/check01.tres",
            "res://diagnostics/reference/checkpoints/check02.tres",
            "res://diagnostics/reference/checkpoints/check03.tres",
            "res://diagnostics/reference/checkpoints/route_end.tres",
        };

        foreach (var path in legacy)
        {
            Require(ResourceLoader.Exists(path), $"Legacy comparator checkpoint must remain loadable: {path}");
            Require(ResourceLoader.Load(path) is not null, $"Failed to load legacy comparator checkpoint: {path}");
        }
    }

    private static bool IsInsideSegment(
        ReferenceDatasetV2 dataset,
        ReferenceExploreSegmentId id,
        double timestampSeconds)
    {
        foreach (var segment in dataset.ExploreSegments)
        {
            if (segment.Id == id)
            {
                return segment.Contains(timestampSeconds);
            }
        }

        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
