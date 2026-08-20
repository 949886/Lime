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

        BuildStartReferenceAssembly();
        BuildStartCalibrationLandmarks();
    }

    private void BuildStartReferenceAssembly()
    {
        var geometry = GetNode<Node3D>("Geometry");
        var scene = GD.Load<PackedScene>("res://game/world/levels/reference/StartReferenceAssembly.tscn");
        if (scene is null)
        {
            GD.PushWarning("M1.6.4 StartReferenceAssembly.tscn could not be loaded.");
            return;
        }

        var assembly = scene.Instantiate<Node3D>();
        assembly.Name = "StartReferenceAssembly";
        geometry.AddChild(assembly);

        var gridScene = GD.Load<PackedScene>("res://game/world/levels/reference/StartDeckGrid.tscn");
        if (gridScene is not null)
        {
            var grid = gridScene.Instantiate<Node3D>();
            grid.Name = "DeckGrid";
            assembly.AddChild(grid);
        }
        else
        {
            GD.PushWarning("M1.6.4 StartDeckGrid.tscn could not be loaded.");
        }

        // The legacy Stairs01 meshes remain as traversal/calibration geometry for this pass,
        // but are hidden visually so the rebuilt reference stair is the only rendered stair.
        // Collision stays unchanged until the visual solve is frozen and then follows it.
        var legacyStairs = GetNodeOrNull<Node3D>("Geometry/Stairs01");
        if (legacyStairs is not null)
        {
            legacyStairs.Visible = false;
        }
    }

    private void BuildStartCalibrationLandmarks()
    {
        // M1.6.3 calibration remains tied to the legacy traversal geometry while M1.6.4
        // rebuilds the visible Start assembly. Once the Start structure is frozen, these
        // markers will be promoted onto the reconstructed semantic edges.
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
            new Vector3(half.X, half.Y, -half.Z));
        AddCalibrationMarker(
            step,
            ReferenceCalibrationLandmarkId.StartForegroundStairTopRight,
            new Vector3(-half.X, half.Y, -half.Z));
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
