using System;
using System.Collections.Generic;
using Godot;

namespace Lime.Game.World.Levels.Reference;

public partial class ReferenceLevel : Node3D
{
    public const int ReferenceStairStepCount = 12;

    private const float RampThickness = 0.16f;
    private const float Check03TopY = -0.40f;
    private const float Check03BodyY = -0.55f;
    private const float Check03BackZ = -10.80f;
    private const float Check03NeckFrontZ = -13.00f;
    private const float Check03ProjectionFrontZ = -16.00f;
    private const float Check03CenterX = -1.40f;
    private const float Check03NeckWidth = 6.00f;
    private const float Check03ProjectionWidth = 4.60f;
    private const float SideLandingTopY = -1.60f;
    private const float SideLandingBodyY = -1.75f;
    private const float SideLandingCenterX = 5.00f;
    private const float SideLandingWidth = 4.00f;
    private const float SideLandingDepth = 4.00f;
    private const float SideStairZ = -13.10f;

    public Marker3D PlayerStart { get; private set; } = null!;
    public Marker3D RouteEnd { get; private set; } = null!;
    public Marker3D CameraCheck01 { get; private set; } = null!;
    public Marker3D CameraCheck02 { get; private set; } = null!;
    public Marker3D CameraCheck03 { get; private set; } = null!;

    public override void _Ready()
    {
        PlayerStart = GetNode<Marker3D>("%PlayerStart");
        RouteEnd = GetNode<Marker3D>("%RouteEnd");
        CameraCheck01 = GetNode<Marker3D>("%CameraCheck01");
        CameraCheck02 = GetNode<Marker3D>("%CameraCheck02");
        CameraCheck03 = GetNode<Marker3D>("%CameraCheck03");

        // User-edited whitebox meshes are the geometry source of truth.  Collision
        // follows those meshes at runtime so an editor resize/translate cannot leave
        // gameplay on the previous calibration.
        ApplyCheck03ReferenceTopology();
        RebuildReferenceStairs();
        SynchronizePlatformCollisions();
    }

    private void ApplyCheck03ReferenceTopology()
    {
        var lowerCorridor = GetNode<MeshInstance3D>("Geometry/LowerCorridor");
        var groundMaterial = (lowerCorridor.Mesh as BoxMesh)?.Material;

        // Check03 in the reference is not one broad rectangle.  A wider rear neck
        // receives Stairs02, then a narrower mass projects toward the camera.  The
        // left side of that projecting mass is where the lateral descending stair
        // begins.
        EnsurePlatform(
            "Check03BackPlatform",
            new Vector3(Check03CenterX, Check03BodyY, (Check03BackZ + Check03NeckFrontZ) * 0.5f),
            new Vector3(Check03NeckWidth, 0.30f, Check03BackZ - Check03NeckFrontZ),
            groundMaterial);

        EnsurePlatform(
            "Check03Projection",
            new Vector3(Check03CenterX, Check03BodyY, (Check03NeckFrontZ + Check03ProjectionFrontZ) * 0.5f),
            new Vector3(Check03ProjectionWidth, 0.30f, Check03NeckFrontZ - Check03ProjectionFrontZ),
            groundMaterial);

        // Re-use the existing lower-front mass as the lower-left landing.  This keeps
        // the user's scene node contract intact while moving the mass to the topology
        // visible in the 16.2s frame.
        var sideLanding = GetNode<MeshInstance3D>("Geometry/LowerFrontPlatform");
        if (sideLanding.Mesh is not BoxMesh sideLandingBox)
        {
            throw new InvalidOperationException("LowerFrontPlatform must remain a BoxMesh whitebox mass.");
        }

        sideLandingBox.Size = new Vector3(SideLandingWidth, 0.30f, SideLandingDepth);
        sideLanding.GlobalPosition = new Vector3(SideLandingCenterX, SideLandingBodyY, SideStairZ);
    }

    private void RebuildReferenceStairs()
    {
        var upper = RequireBoxMesh("Geometry/UpperPlatform");
        var intermediate = RequireBoxMesh("Geometry/IntermediatePlatform");
        var check03Back = RequireBoxMesh("Geometry/Check03BackPlatform");
        var check03Projection = RequireBoxMesh("Geometry/Check03Projection");
        var sideLanding = RequireBoxMesh("Geometry/LowerFrontPlatform");

        var stairs01AxisX = GetNode<MeshInstance3D>("Geometry/Stairs01/Step01").GlobalPosition.X;
        var upperTopY = upper.Node.GlobalPosition.Y + upper.Box.Size.Y * 0.5f;
        var upperFrontZ = upper.Node.GlobalPosition.Z - upper.Box.Size.Z * 0.5f;
        var intermediateTopY = intermediate.Node.GlobalPosition.Y + intermediate.Box.Size.Y * 0.5f;
        var intermediateBackZ = intermediate.Node.GlobalPosition.Z + intermediate.Box.Size.Z * 0.5f;
        var intermediateFrontZ = intermediate.Node.GlobalPosition.Z - intermediate.Box.Size.Z * 0.5f;

        RebuildStairRun(
            "Geometry/Stairs01",
            "Collision/WorldCollision/StairRamp01",
            new Vector3(stairs01AxisX, upperTopY, upperFrontZ),
            new Vector3(stairs01AxisX, intermediateTopY, intermediateBackZ),
            3.0f);

        var stairs02AxisX = GetNode<MeshInstance3D>("Geometry/Stairs02/Step01").GlobalPosition.X;
        var check03BackTopY = check03Back.Node.GlobalPosition.Y + check03Back.Box.Size.Y * 0.5f;
        var check03BackEdgeZ = check03Back.Node.GlobalPosition.Z + check03Back.Box.Size.Z * 0.5f;

        RebuildStairRun(
            "Geometry/Stairs02",
            "Collision/WorldCollision/StairRamp02",
            new Vector3(stairs02AxisX, intermediateTopY, intermediateFrontZ),
            new Vector3(stairs02AxisX, check03BackTopY, check03BackEdgeZ),
            3.0f);

        // Screen-left is world +X in the Explore camera.  This lateral run therefore
        // descends from the projection's +X edge toward the lower-left landing.
        var projectionTopY = check03Projection.Node.GlobalPosition.Y + check03Projection.Box.Size.Y * 0.5f;
        var projectionLeftWorldX = check03Projection.Node.GlobalPosition.X + check03Projection.Box.Size.X * 0.5f;
        var landingTopY = sideLanding.Node.GlobalPosition.Y + sideLanding.Box.Size.Y * 0.5f;
        var landingRightWorldX = sideLanding.Node.GlobalPosition.X - sideLanding.Box.Size.X * 0.5f;

        RebuildStairRun(
            "Geometry/LowerFrontStairs",
            "Collision/WorldCollision/LowerFrontRamp",
            new Vector3(projectionLeftWorldX, projectionTopY, SideStairZ),
            new Vector3(landingRightWorldX, landingTopY, SideStairZ),
            3.0f);
    }

    private void RebuildStairRun(
        string stairsPath,
        string rampPath,
        Vector3 topEdge,
        Vector3 bottomEdge,
        float width)
    {
        var stairs = GetNode<Node3D>(stairsPath);
        var steps = GetStepNodes(stairs);
        if (steps.Count == 0)
        {
            throw new InvalidOperationException($"{stairsPath} must contain at least Step01 as the visual template.");
        }

        var prototype = steps[0];
        var material = (prototype.Mesh as BoxMesh)?.Material;

        while (steps.Count < ReferenceStairStepCount)
        {
            var step = new MeshInstance3D
            {
                Name = $"Step{steps.Count + 1:00}",
            };
            stairs.AddChild(step);
            steps.Add(step);
        }

        while (steps.Count > ReferenceStairStepCount)
        {
            var extra = steps[^1];
            steps.RemoveAt(steps.Count - 1);
            extra.QueueFree();
        }

        var horizontal = new Vector3(bottomEdge.X - topEdge.X, 0.0f, bottomEdge.Z - topEdge.Z);
        var horizontalLength = horizontal.Length();
        if (horizontalLength < 0.01f)
        {
            throw new InvalidOperationException($"{stairsPath} stair run needs a non-zero horizontal length.");
        }

        var direction = horizontal / horizontalLength;
        var treadDepth = horizontalLength / ReferenceStairStepCount;
        var stepHeight = Mathf.Max(
            0.06f,
            Mathf.Abs(topEdge.Y - bottomEdge.Y) / (ReferenceStairStepCount - 1));
        var yawDegrees = Mathf.RadToDeg(Mathf.Atan2(direction.X, direction.Z));

        var stairMesh = new BoxMesh
        {
            Material = material,
            Size = new Vector3(width, stepHeight, treadDepth + 0.012f),
        };

        for (var index = 0; index < ReferenceStairStepCount; index++)
        {
            var t = index / (float)(ReferenceStairStepCount - 1);
            var horizontalCenter = topEdge + direction * (treadDepth * (index + 0.5f));
            var surfaceY = Mathf.Lerp(topEdge.Y, bottomEdge.Y, t);
            var step = steps[index];
            step.Mesh = stairMesh;
            step.GlobalPosition = new Vector3(horizontalCenter.X, surfaceY - stepHeight * 0.5f, horizontalCenter.Z);
            step.GlobalRotationDegrees = new Vector3(0.0f, yawDegrees, 0.0f);
        }

        ConfigureRamp(rampPath, topEdge, bottomEdge, direction, width);
    }

    private void ConfigureRamp(
        string rampPath,
        Vector3 topEdge,
        Vector3 bottomEdge,
        Vector3 horizontalDirection,
        float width)
    {
        var ramp = GetNode<CollisionShape3D>(rampPath);
        var slope = bottomEdge - topEdge;
        var slopeLength = slope.Length();
        var zAxis = slope / slopeLength;
        var widthAxis = new Vector3(-horizontalDirection.Z, 0.0f, horizontalDirection.X).Normalized();
        var xAxis = -widthAxis;
        var yAxis = zAxis.Cross(xAxis).Normalized();

        if (yAxis.Y < 0.0f)
        {
            xAxis = -xAxis;
            yAxis = zAxis.Cross(xAxis).Normalized();
        }

        ramp.Shape = new BoxShape3D
        {
            Size = new Vector3(width, RampThickness, slopeLength),
        };

        var midpoint = (topEdge + bottomEdge) * 0.5f - yAxis * (RampThickness * 0.5f);
        ramp.GlobalTransform = new Transform3D(new Basis(xAxis, yAxis, zAxis), midpoint);
    }

    private void SynchronizePlatformCollisions()
    {
        SyncBoxCollision("Geometry/UpperPlatform", "Collision/WorldCollision/UpperPlatform");
        SyncBoxCollision("Geometry/IntermediatePlatform", "Collision/WorldCollision/IntermediatePlatform");
        SyncBoxCollision("Geometry/LowerCorridor", "Collision/WorldCollision/LowerCorridor");
        SyncBoxCollision("Geometry/PlazaExtension", "Collision/WorldCollision/PlazaExtension");
        SyncBoxCollision("Geometry/LowerFrontPlatform", "Collision/WorldCollision/LowerFrontPlatform");
        SyncBoxCollision("Geometry/Check03BackPlatform", "Collision/WorldCollision/Check03BackPlatform");
        SyncBoxCollision("Geometry/Check03Projection", "Collision/WorldCollision/Check03Projection");
    }

    private void SyncBoxCollision(string meshPath, string collisionPath)
    {
        var mesh = GetNode<MeshInstance3D>(meshPath);
        if (mesh.Mesh is not BoxMesh box)
        {
            throw new InvalidOperationException($"{meshPath} must use BoxMesh for whitebox collision sync.");
        }

        var collision = GetNodeOrNull<CollisionShape3D>(collisionPath);
        if (collision is null)
        {
            var parentPath = collisionPath[..collisionPath.LastIndexOf('/')];
            var nodeName = collisionPath[(collisionPath.LastIndexOf('/') + 1)..];
            var parent = GetNode<Node>(parentPath);
            collision = new CollisionShape3D { Name = nodeName };
            parent.AddChild(collision);
        }

        collision.Shape = new BoxShape3D { Size = box.Size };
        collision.GlobalTransform = mesh.GlobalTransform;
    }

    private void EnsurePlatform(string name, Vector3 position, Vector3 size, Material? material)
    {
        var geometry = GetNode<Node3D>("Geometry");
        var platform = geometry.GetNodeOrNull<MeshInstance3D>(name);
        if (platform is null)
        {
            platform = new MeshInstance3D { Name = name };
            geometry.AddChild(platform);
        }

        platform.Mesh = new BoxMesh
        {
            Material = material,
            Size = size,
        };
        platform.GlobalPosition = position;
    }

    private (MeshInstance3D Node, BoxMesh Box) RequireBoxMesh(string path)
    {
        var node = GetNode<MeshInstance3D>(path);
        if (node.Mesh is not BoxMesh box)
        {
            throw new InvalidOperationException($"{path} must use a BoxMesh.");
        }

        return (node, box);
    }

    private static List<MeshInstance3D> GetStepNodes(Node3D stairs)
    {
        var result = new List<MeshInstance3D>();
        foreach (var child in stairs.GetChildren())
        {
            if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
            {
                result.Add(step);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Name.ToString(), right.Name.ToString()));
        return result;
    }
}
