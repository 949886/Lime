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
        var assembly = new Node3D { Name = "StartReferenceAssembly" };
        geometry.AddChild(assembly);

        var deckMaterial = MakeMaterial(new Color(0.50f, 0.49f, 0.45f), 0.90f);
        var stepMaterial = MakeMaterial(new Color(0.60f, 0.58f, 0.53f), 0.90f);
        var concreteMaterial = MakeMaterial(new Color(0.36f, 0.35f, 0.33f), 0.95f);
        var recessMaterial = MakeMaterial(new Color(0.24f, 0.23f, 0.20f), 1.00f);
        var railMaterial = MakeMaterial(new Color(0.15f, 0.15f, 0.15f), 0.55f, 0.40f);
        var equipmentMaterial = MakeMaterial(new Color(0.25f, 0.27f, 0.27f), 0.65f, 0.25f);
        var gridMaterial = MakeMaterial(new Color(0.17f, 0.17f, 0.16f), 0.95f);

        // Broad upper deck mass. These wings intentionally extend well beyond the
        // legacy 5.6 m graybox so the Start composition reads like the reference.
        AddBox(assembly, "DeckScreenRightWing", new Vector3(3.4f, 0.3f, 5.0f),
            new Vector3(-1.2221975f, 1.85f, 2.9853277f), deckMaterial);
        AddBox(assembly, "DeckScreenLeftWing", new Vector3(1.3f, 0.3f, 5.0f),
            new Vector3(6.7278023f, 1.85f, 2.9853277f), deckMaterial);
        AddBox(assembly, "DeckScreenRightFascia", new Vector3(4.1f, 1.45f, 0.42f),
            new Vector3(-0.8721975f, 1.275f, 0.315f), concreteMaterial);

        // Rebuilt central stair: wider than the traversal proxy and visually dominant.
        var stairVisual = new Node3D { Name = "StairVisual" };
        assembly.AddChild(stairVisual);
        var stairCenters = new[]
        {
            new Vector3(3.2778025f, 1.9485f, 0.3401057f),
            new Vector3(3.2778025f, 1.8394f, 0.0496618f),
            new Vector3(3.2778025f, 1.7303f, -0.24078226f),
            new Vector3(3.2778025f, 1.6212f, -0.5312262f),
            new Vector3(3.2778025f, 1.5121f, -0.8216702f),
            new Vector3(3.2778025f, 1.4030f, -1.1121142f),
            new Vector3(3.2778025f, 1.2939f, -1.4025581f),
            new Vector3(3.2778025f, 1.1848f, -1.6930021f),
            new Vector3(3.2778025f, 1.0757f, -1.9834461f),
            new Vector3(3.2778025f, 0.9666f, -2.2738900f),
            new Vector3(3.2778025f, 0.8575f, -2.5643340f),
            new Vector3(3.2778025f, 0.7485f, -2.8547780f),
        };
        for (var index = 0; index < stairCenters.Length; index++)
        {
            AddBox(stairVisual, $"Step{index + 1:00}", new Vector3(4.2f, 0.105f, 0.302444f),
                stairCenters[index], stepMaterial);
        }

        AddBox(assembly, "StairScreenRightWall", new Vector3(0.42f, 0.78f, 3.75f),
            new Vector3(0.93f, 1.38f, -1.24f), concreteMaterial, new Vector3(-20.0f, 0.0f, 0.0f));
        AddBox(assembly, "StairScreenLeftWall", new Vector3(0.42f, 0.78f, 3.75f),
            new Vector3(5.625f, 1.38f, -1.24f), concreteMaterial, new Vector3(-20.0f, 0.0f, 0.0f));
        AddBox(assembly, "StairScreenRightRail", new Vector3(0.12f, 0.12f, 3.72f),
            new Vector3(0.93f, 2.02f, -1.24f), railMaterial, new Vector3(-20.0f, 0.0f, 0.0f));
        AddBox(assembly, "StairScreenLeftRail", new Vector3(0.12f, 0.12f, 3.72f),
            new Vector3(5.625f, 2.02f, -1.24f), railMaterial, new Vector3(-20.0f, 0.0f, 0.0f));

        AddBox(assembly, "FrontRailScreenRight", new Vector3(4.0f, 0.12f, 0.12f),
            new Vector3(-0.86f, 2.43f, 0.39f), railMaterial);
        AddBox(assembly, "OuterRailScreenRight", new Vector3(0.12f, 0.12f, 4.5f),
            new Vector3(-2.86f, 2.43f, 2.75f), railMaterial);
        AddBox(assembly, "FrontRailPost01", new Vector3(0.12f, 0.9f, 0.12f),
            new Vector3(-2.72f, 2.38f, 0.39f), railMaterial);
        AddBox(assembly, "FrontRailPost02", new Vector3(0.12f, 0.9f, 0.12f),
            new Vector3(-0.86f, 2.38f, 0.39f), railMaterial);
        AddBox(assembly, "FrontRailPost03", new Vector3(0.12f, 0.9f, 0.12f),
            new Vector3(0.96f, 2.38f, 0.39f), railMaterial);

        // Screen-left recessed lower area visible beside the upper platform.
        AddBox(assembly, "ScreenLeftRecessFloor", new Vector3(3.0f, 0.25f, 3.6f),
            new Vector3(7.9f, 0.42f, 2.8f), recessMaterial);
        AddBox(assembly, "ScreenLeftRecessBackWall", new Vector3(3.0f, 1.25f, 0.32f),
            new Vector3(7.9f, 1.16f, 4.55f), concreteMaterial);

        // Rear equipment silhouettes and the round masses establish the same depth layers
        // visible behind the Start deck in the video.
        var rearEquipment = new Node3D { Name = "RearEquipment" };
        assembly.AddChild(rearEquipment);
        AddBox(rearEquipment, "Crate", new Vector3(1.65f, 1.85f, 1.25f),
            new Vector3(1.15f, 2.925f, 5.35f), equipmentMaterial);
        AddBox(rearEquipment, "TallCabinet", new Vector3(0.72f, 2.45f, 0.78f),
            new Vector3(-0.15f, 3.225f, 5.45f), equipmentMaterial);
        AddCylinder(rearEquipment, "Pipe", 0.28f, 3.4f,
            new Vector3(4.85f, 2.85f, 5.7f), equipmentMaterial, new Vector3(90.0f, 0.0f, 0.0f));
        AddCylinder(assembly, "ScreenRightRoundBuilding", 2.15f, 4.7f,
            new Vector3(-2.3f, 4.35f, 7.15f), concreteMaterial);
        AddCylinder(assembly, "ScreenLeftPlanterMass", 1.52f, 2.8f,
            new Vector3(7.55f, 3.35f, 6.85f), concreteMaterial);

        BuildStartDeckGrid(assembly, gridMaterial);

        // Keep the old stair only as collision/calibration proxy during Pass A.
        var legacyStairs = GetNodeOrNull<Node3D>("Geometry/Stairs01");
        if (legacyStairs is not null)
        {
            legacyStairs.Visible = false;
        }
    }

    private static void BuildStartDeckGrid(Node3D assembly, StandardMaterial3D material)
    {
        var grid = new Node3D { Name = "DeckGrid" };
        assembly.AddChild(grid);

        for (var row = 0; row < 6; row++)
        {
            AddBox(grid, $"Row{row:00}", new Vector3(10.2f, 0.012f, 0.035f),
                new Vector3(2.25f, 2.006f, 0.55f + row), material);
        }

        for (var column = 0; column < 11; column++)
        {
            AddBox(grid, $"Column{column:00}", new Vector3(0.035f, 0.012f, 5.0f),
                new Vector3(-2.85f + column, 2.006f, 3.05f), material);
        }
    }

    private static StandardMaterial3D MakeMaterial(Color color, float roughness, float metallic = 0.0f)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = roughness,
            Metallic = metallic,
        };
    }

    private static MeshInstance3D AddBox(
        Node3D parent,
        string name,
        Vector3 size,
        Vector3 position,
        Material material,
        Vector3? rotationDegrees = null)
    {
        var mesh = new BoxMesh
        {
            Size = size,
            Material = material,
        };
        var instance = new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            Position = position,
        };
        if (rotationDegrees is { } rotation)
        {
            instance.RotationDegrees = rotation;
        }
        parent.AddChild(instance);
        return instance;
    }

    private static MeshInstance3D AddCylinder(
        Node3D parent,
        string name,
        float radius,
        float height,
        Vector3 position,
        Material material,
        Vector3? rotationDegrees = null)
    {
        var mesh = new CylinderMesh
        {
            TopRadius = radius,
            BottomRadius = radius,
            Height = height,
            RadialSegments = 24,
            Material = material,
        };
        var instance = new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            Position = position,
        };
        if (rotationDegrees is { } rotation)
        {
            instance.RotationDegrees = rotation;
        }
        parent.AddChild(instance);
        return instance;
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