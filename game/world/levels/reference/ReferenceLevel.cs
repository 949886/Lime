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

        BuildStartCalibrationLandmarks();
    }

    private void BuildStartCalibrationLandmarks()
    {
        // M1.6.3 starts from geometry points that have an unambiguous whitebox
        // correspondence. The foreground stair pair is the top/front edge of
        // Stairs01/Step01. The far gate landmarks remain intentionally unmapped
        // until M1.6.4 reconstructs a real matching gate structure.
        var step = GetNode<MeshInstance3D>("Geometry/Stairs01/Step01");
        var mesh = step.Mesh as BoxMesh;
        if (mesh is null)
        {
            return;
        }

        var half = mesh.Size * 0.5f;
        AddCalibrationMarker(
            step,
            ReferenceCalibrationLandmarkId.StartForegroundStairTopLeft,
            new Vector3(-half.X, half.Y, -half.Z));
        AddCalibrationMarker(
            step,
            ReferenceCalibrationLandmarkId.StartForegroundStairTopRight,
            new Vector3(half.X, half.Y, -half.Z));
    }

    private void AddCalibrationMarker(
        Node3D parent,
        ReferenceCalibrationLandmarkId id,
        Vector3 localPosition)
    {
        var marker = new Marker3D
        {
            Name = id.ToString(),
            Position = localPosition,
        };
        parent.AddChild(marker);
        _calibrationLandmarks[id] = marker;
    }
}
