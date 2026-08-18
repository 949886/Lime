using System;
using Godot;

namespace Lime.Diagnostics.Reference.V2;

public static class ReferenceDatasetV2Catalog
{
    public static ReferenceDatasetV2 CreateSeed()
    {
        var dataset = new ReferenceDatasetV2();

        dataset.ExploreSegments.Add(Segment(ReferenceExploreSegmentId.ExploreA, 6.20, 22.50));
        dataset.ExploreSegments.Add(Segment(ReferenceExploreSegmentId.ExploreB, 43.30, 51.90));
        dataset.ExploreSegments.Add(Segment(ReferenceExploreSegmentId.ExploreC, 56.50, 60.00));

        AddShot(dataset, "start_entry", ReferenceExploreSegmentId.ExploreA, 6.50, ReferenceCameraPhase.StartHold);
        AddShot(dataset, "start_hold", ReferenceExploreSegmentId.ExploreA, 8.50, ReferenceCameraPhase.StartHold);
        AddShot(dataset, "pullback_begin", ReferenceExploreSegmentId.ExploreA, 9.00, ReferenceCameraPhase.Pullback);
        AddShot(dataset, "pullback_mid", ReferenceExploreSegmentId.ExploreA, 9.35, ReferenceCameraPhase.Pullback);
        AddShot(dataset, "pullback_end", ReferenceExploreSegmentId.ExploreA, 9.75, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "stairs_a", ReferenceExploreSegmentId.ExploreA, 10.50, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "mid_entry", ReferenceExploreSegmentId.ExploreA, 11.70, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "mid_terrace", ReferenceExploreSegmentId.ExploreA, 12.50, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "stairs_b_approach", ReferenceExploreSegmentId.ExploreA, 14.20, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "stairs_b_bottom", ReferenceExploreSegmentId.ExploreA, 16.20, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "tower_pass", ReferenceExploreSegmentId.ExploreA, 17.50, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "lower_corridor", ReferenceExploreSegmentId.ExploreA, 19.00, ReferenceCameraPhase.ExploreFollow);
        AddShot(dataset, "explore_a_end", ReferenceExploreSegmentId.ExploreA, 21.80, ReferenceCameraPhase.ExploreFollow);

        AddShot(dataset, "explore_b_resume", ReferenceExploreSegmentId.ExploreB, 43.50, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "tree_bend", ReferenceExploreSegmentId.ExploreB, 45.00, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "pool_entry", ReferenceExploreSegmentId.ExploreB, 46.50, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "pool_walkway", ReferenceExploreSegmentId.ExploreB, 48.00, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "worker_plaza_entry", ReferenceExploreSegmentId.ExploreB, 49.50, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "worker_approach", ReferenceExploreSegmentId.ExploreB, 51.00, ReferenceCameraPhase.PostDialogueExplore);

        AddShot(dataset, "explore_c_resume", ReferenceExploreSegmentId.ExploreC, 56.70, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "final_route", ReferenceExploreSegmentId.ExploreC, 58.00, ReferenceCameraPhase.PostDialogueExplore);
        AddShot(dataset, "final_explore", ReferenceExploreSegmentId.ExploreC, 59.50, ReferenceCameraPhase.PostDialogueExplore);

        AddTrajectoryRange(dataset, ReferenceExploreSegmentId.ExploreA, 6.50, 22.00, 0.50);
        AddTrajectoryRange(dataset, ReferenceExploreSegmentId.ExploreB, 43.50, 51.50, 0.50);
        AddTrajectoryRange(dataset, ReferenceExploreSegmentId.ExploreC, 56.50, 60.00, 0.50);

        AddTrajectorySample(dataset, ReferenceExploreSegmentId.ExploreA, 9.00);
        AddTrajectorySample(dataset, ReferenceExploreSegmentId.ExploreA, 9.25);
        AddTrajectorySample(dataset, ReferenceExploreSegmentId.ExploreA, 9.50);
        AddTrajectorySample(dataset, ReferenceExploreSegmentId.ExploreA, 9.75);

        return dataset;
    }

    private static ReferenceExploreSegment Segment(
        ReferenceExploreSegmentId id,
        double startSeconds,
        double endSeconds) => new()
    {
        Id = id,
        StartSeconds = startSeconds,
        EndSeconds = endSeconds,
    };

    private static void AddShot(
        ReferenceDatasetV2 dataset,
        string id,
        ReferenceExploreSegmentId segmentId,
        double timestampSeconds,
        ReferenceCameraPhase cameraPhase)
    {
        dataset.Shots.Add(new ReferenceShot
        {
            Id = id,
            SegmentId = segmentId,
            TimestampSeconds = timestampSeconds,
            SourceFrame = ToFrame(timestampSeconds),
            CameraPhase = cameraPhase,
        });
    }

    private static void AddTrajectoryRange(
        ReferenceDatasetV2 dataset,
        ReferenceExploreSegmentId segmentId,
        double startSeconds,
        double endSeconds,
        double intervalSeconds)
    {
        for (var timestamp = startSeconds; timestamp <= endSeconds + 0.0001; timestamp += intervalSeconds)
        {
            AddTrajectorySample(dataset, segmentId, timestamp);
        }
    }

    private static void AddTrajectorySample(
        ReferenceDatasetV2 dataset,
        ReferenceExploreSegmentId segmentId,
        double timestampSeconds)
    {
        var frame = ToFrame(timestampSeconds);
        foreach (var existing in dataset.Trajectory)
        {
            if (existing.SourceFrame == frame)
            {
                return;
            }
        }

        dataset.Trajectory.Add(new ReferenceTrajectorySample
        {
            SegmentId = segmentId,
            TimestampSeconds = timestampSeconds,
            SourceFrame = frame,
        });
    }

    private static int ToFrame(double timestampSeconds) =>
        (int)Math.Round(timestampSeconds * 30.0, MidpointRounding.AwayFromZero);
}
