using Godot;

namespace Lime.Game.World.Levels.Reference;

public partial class ReferenceLevel : Node3D
{
    private static readonly Vector3 Check01IntermediatePlatformPosition = new(0.5f, 0.65f, -3.65f);
    private static readonly Vector3 Check01IntermediatePlatformSize = new(8.0f, 0.3f, 4.0f);
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
        // Check01 is the strongest scale reference in the captured sequence.  Projecting the
        // original whitebox over check01.webp showed that the intermediate platform front edge
        // landed ~17 px too low and the stair run was ~30% too narrow.  Keep the source scene
        // topology intact and apply the measured whitebox dimensions here so mesh/collision stay
        // paired while M1.6 converges.
        var intermediateMesh = GetNode<MeshInstance3D>("Geometry/IntermediatePlatform");
        intermediateMesh.Position = Check01IntermediatePlatformPosition;
        if (intermediateMesh.Mesh is BoxMesh intermediateBox)
        {
            intermediateBox.Size = Check01IntermediatePlatformSize;
        }

        var intermediateCollision = GetNode<CollisionShape3D>("Collision/WorldCollision/IntermediatePlatform");
        intermediateCollision.Position = Check01IntermediatePlatformPosition;
        if (intermediateCollision.Shape is BoxShape3D intermediateShape)
        {
            intermediateShape.Size = Check01IntermediatePlatformSize;
        }

        var firstStep = GetNode<MeshInstance3D>("Geometry/Stairs01/Step01");
        if (firstStep.Mesh is BoxMesh stepBox)
        {
            stepBox.Size = new Vector3(ReferenceStairWidth, stepBox.Size.Y, stepBox.Size.Z);
        }

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

        // Keep the existing left-side guide rail attached to the corrected platform footprint.
        var intermediateRail = GetNode<MeshInstance3D>("Geometry/Rails/IntermediateLeft");
        intermediateRail.Position = new Vector3(-3.5f, 1.2f, -3.65f);
        intermediateRail.Scale = new Vector3(0.8f, 1.0f, 1.0f);
    }
}
