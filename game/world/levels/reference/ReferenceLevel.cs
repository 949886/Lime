using Godot;

namespace Lime.Game.World.Levels.Reference;

public partial class ReferenceLevel : Node3D
{
    private const float ReferenceStairWidth = 4.2f;

    public Marker3D PlayerStart { get; private set; } = null!;
    public Marker3D RouteEnd { get; private set; } = null!;
    public Marker3D CameraCheck01 { get; private set; } = null!;
    public Marker3D CameraCheck02 { get; private set; } = null!;
    public Marker3D CameraCheck03 { get; private set; } = null!;

    public override void _Ready()
    {
        ApplyReferenceWhiteboxCalibration();

        PlayerStart = GetNode<Marker3D>("%PlayerStart");
        RouteEnd = GetNode<Marker3D>("%RouteEnd");
        CameraCheck01 = GetNode<Marker3D>("%CameraCheck01");
        CameraCheck02 = GetNode<Marker3D>("%CameraCheck02");
        CameraCheck03 = GetNode<Marker3D>("%CameraCheck03");
    }

    private void ApplyReferenceWhiteboxCalibration()
    {
        // The original 3m stair width only covered ~41 px at Check01 while the captured
        // stair run is ~57-60 px wide.  4.2m matches that projected silhouette across
        // Check01/02 without moving the already-calibrated route centres.
        var firstStep = GetNode<MeshInstance3D>("Geometry/Stairs01/Step01");
        if (firstStep.Mesh is BoxMesh stepBox)
        {
            stepBox.Size = new Vector3(ReferenceStairWidth, stepBox.Size.Y, stepBox.Size.Z);
        }

        // All stair meshes share Box_step and all three ramps share Shape_ramp, but keep
        // the collision update explicit so the traversable whitebox stays visually honest.
        foreach (var rampPath in new[]
                 {
                     "Collision/WorldCollision/StairRamp01",
                     "Collision/WorldCollision/StairRamp02",
                     "Collision/WorldCollision/LowerFrontRamp",
                 })
        {
            var ramp = GetNode<CollisionShape3D>(rampPath);
            if (ramp.Shape is BoxShape3D rampShape)
            {
                rampShape.Size = new Vector3(ReferenceStairWidth, rampShape.Size.Y, rampShape.Size.Z);
            }
        }
    }
}
