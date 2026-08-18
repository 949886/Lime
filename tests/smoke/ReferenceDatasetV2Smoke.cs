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
            ValidateLegacyComparatorAssets();

            GD.Print($"[M1.6.2] PASS: {dataset.Shots.Count} structure shots / " +
                     $"{dataset.Trajectory.Count} trajectory samples cover all Explore segments.");
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
        Require(dataset.DurationSeconds >= 64.0,
            "Reference Dataset v2 must describe the complete source clip duration.");
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

        foreach (var shot in dataset.Shots)
        {
            Require(!string.IsNullOrWhiteSpace(shot.Id), "Every structure shot needs a stable id.");
            Require(ids.Add(shot.Id), $"Duplicate structure shot id: {shot.Id}.");
            Require(shot.SourceFrame >= 0, $"Shot {shot.Id} must have a source frame.");
            Require(IsInsideSegment(dataset, shot.SegmentId, shot.TimestampSeconds),
                $"Shot {shot.Id} is outside its Explore segment.");
            observedSegments.Add(shot.SegmentId);

            foreach (var landmark in shot.Landmarks)
            {
                Require(landmark.Pixel.X >= 0.0f && landmark.Pixel.X < dataset.SourceSize.X &&
                        landmark.Pixel.Y >= 0.0f && landmark.Pixel.Y < dataset.SourceSize.Y,
                    $"Shot {shot.Id} landmark {landmark.AnchorId} is outside the source frame.");
            }
        }

        Require(observedSegments.Count == 3,
            "Structure shots must cover Explore A / B / C, not only the first segment.");
    }

    private static void ValidateTrajectory(ReferenceDatasetV2 dataset)
    {
        var frames = new HashSet<int>();
        foreach (var sample in dataset.Trajectory)
        {
            Require(frames.Add(sample.SourceFrame),
                $"Duplicate trajectory source frame: {sample.SourceFrame}.");
            Require(IsInsideSegment(dataset, sample.SegmentId, sample.TimestampSeconds),
                $"Trajectory frame {sample.SourceFrame} is outside its Explore segment.");

            if (sample.HasMeasuredPlayerFeet)
            {
                Require(sample.PlayerFeetPixel.X < dataset.SourceSize.X &&
                        sample.PlayerFeetPixel.Y < dataset.SourceSize.Y,
                    $"Measured trajectory frame {sample.SourceFrame} is outside the source frame.");
            }
        }

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
        }
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
