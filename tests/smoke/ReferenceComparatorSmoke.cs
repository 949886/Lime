using System;
using Godot;
using Lime.Diagnostics.Reference;
using Lime.Game;
using Lime.Game.Camera;

namespace Lime.Tests.Smoke;

public partial class ReferenceComparatorSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");

            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var comparator = gameRoot.GetNode<ReferenceComparator>("UIRoot/ReferenceComparator");

            ValidateCheckpointCatalog(comparator);
            ValidateReviewUx(gameRoot, comparator);
            ValidateComparisonModes(comparator);
            ValidateCheckpointTeleport(gameRoot, comparator);
            ValidateCameraAb(gameRoot, comparator);
            await ValidateViewportCapture(comparator);

            GD.Print("[M1.5] PASS: Reference Comparator runtime smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.5] FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateCheckpointCatalog(ReferenceComparator comparator)
    {
        Require(!comparator.Visible, "Reference Comparator must be hidden by default.");
        Require(comparator.CheckpointCount == 5, "M1.5 requires five fixed reference checkpoints.");

        ValidateFrame(comparator.GetCheckpoint(0), ReferenceCheckpointId.Start, 8.50, 255);
        ValidateFrame(comparator.GetCheckpoint(1), ReferenceCheckpointId.Check01, 11.70, 351);
        ValidateFrame(comparator.GetCheckpoint(2), ReferenceCheckpointId.Check02, 14.20, 426);
        ValidateFrame(comparator.GetCheckpoint(3), ReferenceCheckpointId.Check03, 16.20, 486);
        ValidateFrame(comparator.GetCheckpoint(4), ReferenceCheckpointId.RouteEnd, 20.80, 624);
    }

    private static void ValidateFrame(
        ReferenceCheckpoint checkpoint,
        ReferenceCheckpointId expectedId,
        double expectedSeconds,
        int expectedFrame)
    {
        Require(checkpoint.Id == expectedId, $"Expected checkpoint {expectedId}, got {checkpoint.Id}.");
        Require(Math.Abs(checkpoint.Frame.TimestampSeconds - expectedSeconds) < 0.001,
            $"{expectedId} timestamp is not locked to {expectedSeconds:0.00}s.");
        Require(checkpoint.Frame.SourceFrame == expectedFrame,
            $"{expectedId} source frame is not locked to {expectedFrame}.");
        Require(checkpoint.Frame.SourceSize == new Vector2I(2548, 1426),
            $"{expectedId} source size must preserve the 2548 × 1426 reference capture metadata.");

        var image = checkpoint.Frame.Image
            ?? throw new InvalidOperationException($"{expectedId} reference image is missing.");
        Require(image.GetWidth() == 320 && image.GetHeight() == 179,
            $"{expectedId} diagnostic image must import at 320 × 179.");
    }

    private static void ValidateReviewUx(GameRoot gameRoot, ReferenceComparator comparator)
    {
        var margin = comparator.GetNode<MarginContainer>("Panel/Margin");
        var title = comparator.GetNode<Label>("Panel/Margin/VBox/Title");

        Require(margin.GetThemeConstant("margin_left") == 12,
            "Comparator panel must keep 12 px left padding.");
        Require(margin.GetThemeConstant("margin_right") == 12,
            "Comparator panel must keep 12 px right padding.");
        Require(margin.GetThemeConstant("margin_top") == 10,
            "Comparator panel must keep 10 px top padding.");
        Require(margin.GetThemeConstant("margin_bottom") == 10,
            "Comparator panel must keep 10 px bottom padding.");
        Require(title.Text.Contains("Ctrl+Shift+F9", StringComparison.Ordinal),
            "Comparator title must advertise the non-conflicting Ctrl+Shift+F9 toggle.");

        comparator.SelectCheckpoint(0);
        comparator.SelectNextCheckpoint();
        Require(comparator.CurrentCheckpoint.Id == ReferenceCheckpointId.Check01,
            "Next must select Check01 from Start.");
        Require(
            gameRoot.Player.GlobalPosition.DistanceTo(gameRoot.ReferenceLevel.CameraCheck01.GlobalPosition) < 0.01f,
            "Next must automatically teleport the real Player to CameraCheck01.");
        Require(gameRoot.Player.Velocity.Length() < 0.001f,
            "Next auto-teleport must clear Player velocity.");

        comparator.SelectPreviousCheckpoint();
        Require(comparator.CurrentCheckpoint.Id == ReferenceCheckpointId.Start,
            "Previous must return from Check01 to Start.");
        Require(
            gameRoot.Player.GlobalPosition.DistanceTo(gameRoot.ReferenceLevel.PlayerStart.GlobalPosition) < 0.01f,
            "Previous must automatically teleport the real Player to PlayerStart.");
        Require(gameRoot.Player.Velocity.Length() < 0.001f,
            "Previous auto-teleport must clear Player velocity.");
    }

    private static void ValidateComparisonModes(ReferenceComparator comparator)
    {
        comparator.Visible = true;

        var referenceLayer = comparator.GetNode<TextureRect>("%ReferenceLayer");
        var differenceLayer = comparator.GetNode<ColorRect>("%DifferenceLayer");

        comparator.SetComparisonMode(ReferenceComparisonMode.Reference);
        Require(referenceLayer.Visible, "Reference mode must display the reference layer.");
        Require(!differenceLayer.Visible, "Reference mode must hide the difference layer.");
        Require(Mathf.IsEqualApprox(referenceLayer.Modulate.A, 1f),
            "Reference mode must use full reference opacity.");

        comparator.SetComparisonMode(ReferenceComparisonMode.Overlay);
        Require(referenceLayer.Visible, "Overlay mode must display the reference layer.");
        Require(Mathf.IsEqualApprox(referenceLayer.Modulate.A, 0.5f),
            "Overlay mode must use the frozen 50% alpha baseline.");

        comparator.SetComparisonMode(ReferenceComparisonMode.Difference);
        Require(!referenceLayer.Visible, "Difference mode must hide the reference layer.");
        Require(differenceLayer.Visible, "Difference mode must display the live difference layer.");
        Require(differenceLayer.Material is ShaderMaterial,
            "Difference mode must use the reference difference ShaderMaterial.");

        comparator.SetComparisonMode(ReferenceComparisonMode.Live);
        Require(!referenceLayer.Visible && !differenceLayer.Visible,
            "Live mode must show the unobstructed production viewport.");
    }

    private static void ValidateCheckpointTeleport(GameRoot gameRoot, ReferenceComparator comparator)
    {
        comparator.SelectCheckpoint(3);
        comparator.TeleportToCurrentCheckpoint();

        Require(comparator.CurrentCheckpoint.Id == ReferenceCheckpointId.Check03,
            "Checkpoint selection did not advance to Check03.");
        Require(
            gameRoot.Player.GlobalPosition.DistanceTo(gameRoot.ReferenceLevel.CameraCheck03.GlobalPosition) < 0.01f,
            "Checkpoint teleport must place the real Player at ReferenceLevel.CameraCheck03.");
        Require(gameRoot.Player.Velocity.Length() < 0.001f,
            "Checkpoint teleport must clear Player velocity.");
    }

    private static void ValidateCameraAb(GameRoot gameRoot, ReferenceComparator comparator)
    {
        comparator.ActivateCamera(CameraId.ExploreOrthographic);
        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExploreOrthographic,
            "Comparator must switch to ExploreOrthographic through CameraDirector.");

        comparator.ActivateCamera(CameraId.ExplorePerspective);
        Require(gameRoot.CameraDirector.ActiveCameraId == CameraId.ExplorePerspective,
            "Comparator must switch back to ExplorePerspective through CameraDirector.");
    }

    private static async System.Threading.Tasks.Task ValidateViewportCapture(ReferenceComparator comparator)
    {
        comparator.Visible = true;

        if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
        {
            var rejectedHeadlessCapture = false;

            try
            {
                await comparator.CaptureCurrentAsync();
            }
            catch (InvalidOperationException exception)
            {
                rejectedHeadlessCapture = exception.Message.Contains(
                    "headless", StringComparison.OrdinalIgnoreCase);
            }

            Require(rejectedHeadlessCapture,
                "Headless CI must reject viewport capture immediately instead of waiting for FramePostDraw.");
            Require(comparator.Visible,
                "Rejected headless capture must preserve the comparator visibility state.");
            Require(comparator.CurrentCapture is null,
                "Rejected headless capture must not publish a fake CurrentCapture.");
            return;
        }

        var capture = await comparator.CaptureCurrentAsync();
        Require(comparator.Visible,
            "Viewport capture must restore the comparator visibility state after rendering completes.");
        Require(comparator.CurrentCapture == capture,
            "Viewport capture must publish the latest ImageTexture through CurrentCapture.");
        Require(capture.GetWidth() > 0 && capture.GetHeight() > 0,
            "Viewport capture must contain a non-empty rendered image.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
