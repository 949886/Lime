using System.Collections.Generic;
using Godot;
using Lime.Diagnostics.Reference.V2;

namespace Lime.Game.World.Levels.Reference;

public partial class ReferenceLevel : Node3D
{
    public const int ReferenceStairStepCount = 12;

    private readonly Dictionary<ReferenceCalibrationLandmarkId, Node3D> _calibrationLandmarks = new();

    public Marker3D PlayerStart { get; private set; } = null!;
    public Marker3D RouteEnd { get; private set; } = null!;
    public Marker3D CameraCheck01 { get; private set; } = null!;
    public Marker3D CameraCheck02 { get; private set; } = null!;
    public Marker3D CameraCheck03 { get; private set; } = null!;
    public IReadOnlyDictionary<ReferenceCalibrationLandmarkId, Node3D> CalibrationLandmarks => _calibrationLandmarks;

    public override void _Ready()
    {
        PlayerStart = GetNode<Marker3D>("%PlayerStart");
        RouteEnd = GetNode<Marker3D>("%RouteEnd");
        CameraCheck01 = GetNode<Marker3D>("%CameraCheck01");
        CameraCheck02 = GetNode<Marker3D>("%CameraCheck02");
        CameraCheck03 = GetNode<Marker3D>("%CameraCheck03");

        RegisterCalibrationLandmark(
            ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft,
            "%StartForegroundStairTopLeft");
        RegisterCalibrationLandmark(
            ReferenceCalibrationLandmarkId.StartForegroundStairTopRight,
            "%StartForegroundStairTopRight");
    }

    private void RegisterCalibrationLandmark(
        ReferenceCalibrationLandmarkId id,
        NodePath path)
    {
        var marker = GetNodeOrNull<Node3D>(path);
        if (marker is not null)
        {
            _calibrationLandmarks[id] = marker;
        }
    }
}
