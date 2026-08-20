using System;
using System.Threading.Tasks;
using Godot;
using Lime.Game;

namespace Lime.Tests.Smoke;

public partial class ReferenceStartAssemblySmoke : Node
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
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var level = gameRoot.ReferenceLevel;
            var visuals = level.GetNode<Node3D>("ReferenceLevelVisuals");
            var startPlatform = visuals.GetNode<Node3D>("StartPlatform");
            var grid = visuals.GetNode<Node3D>("StartDeckGrid");
            var props = visuals.GetNode<Node3D>("StartPlatformProps");

            Require(level.GetNodeOrNull<Node3D>("Geometry/UpperPlatform") is null,
                "Legacy UpperPlatform must stay removed.");
            Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is null,
                "Legacy Stairs01 must stay removed.");

            var deck = startPlatform.GetNode<Node3D>("Deck");
            Require(deck.GetNodeOrNull<MeshInstance3D>("RearDeck") is not null,
                "StartPlatform must contain the broad rear deck slab.");
            Require(deck.GetNodeOrNull<MeshInstance3D>("FrontRightDeck") is not null &&
                    deck.GetNodeOrNull<MeshInstance3D>("FrontCenterDeck") is not null &&
                    deck.GetNodeOrNull<MeshInstance3D>("FrontLeftDeck") is not null,
                "StartPlatform front edge must be segmented around both stair openings.");
            Require(deck.GetNodeOrNull<MeshInstance3D>("FrontLeftNub") is not null,
                "StartPlatform must preserve the screen-left front projection/nub.");

            var rearDeck = deck.GetNode<MeshInstance3D>("RearDeck");
            var rearMesh = (BoxMesh)rearDeck.Mesh;
            Require(rearMesh.Size.X >= 19.5f && rearMesh.Size.Z > 5.5f,
                $"Detailed Start rear plaza must span roughly 20m and remain deep. Size={rearMesh.Size}.");
            Require(startPlatform.GetNodeOrNull<CollisionShape3D>("Collision/RearDeck") is not null &&
                    startPlatform.GetNodeOrNull<CollisionShape3D>("Collision/FrontRightDeck") is not null &&
                    startPlatform.GetNodeOrNull<CollisionShape3D>("Collision/FrontCenterDeck") is not null &&
                    startPlatform.GetNodeOrNull<CollisionShape3D>("Collision/FrontLeftDeck") is not null,
                "Segmented Start deck must own matching segmented collision.");
            Require(level.PlayerStart.GlobalPosition.Z > rearDeck.GlobalPosition.Z - rearMesh.Size.Z * 0.5f,
                "PlayerStart must remain on the continuous rear plaza behind the stair cutouts.");

            var rightStair = startPlatform.GetNode<Node3D>("RightStair");
            var leftStair = startPlatform.GetNode<Node3D>("LeftStair");
            ValidateStair(rightStair, "RightStair");
            ValidateStair(leftStair, "LeftStair");
            Require(leftStair.Position.X - rightStair.Position.X > 8.0f,
                $"Dual Start stairs must have the wide reference separation. Separation={leftStair.Position.X - rightStair.Position.X:0.00}m.");

            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("FrontArchitecture/CenterRetainingWall") is not null,
                "StartPlatform must include the retaining wall between stair openings.");
            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("FrontArchitecture/ScreenRightRetainingWall") is not null &&
                    startPlatform.GetNodeOrNull<MeshInstance3D>("FrontArchitecture/ScreenLeftRetainingWall") is not null,
                "StartPlatform must preserve the segmented front retaining edge.");

            ValidateDetailedProps(props);

            Require(grid.GetNodeOrNull<Node3D>("RearRows") is not null &&
                    grid.GetNodeOrNull<Node3D>("FrontRows") is not null &&
                    grid.GetNodeOrNull<Node3D>("Columns") is not null,
                "Start deck grid must be segmented around the stair openings.");
            var rearOnlyColumn = grid.GetNode<MeshInstance3D>("Columns/Column07");
            var rearOnlyMesh = rearOnlyColumn.Mesh as BoxMesh
                ?? throw new InvalidOperationException("Column07 must use BoxMesh.");
            Require(Mathf.Abs(rearOnlyMesh.Size.Z - 5.85f) < 0.01f &&
                    Mathf.Abs(rearOnlyColumn.Position.Z - 3.525f) < 0.01f,
                "Grid columns inside a stair opening must begin at the rear-deck edge instead of floating over the stairs.");

            GD.Print("[M1.6.4] PASS: detailed Start platform and reusable world/props assets are scene-authored.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.4] FAIL: detailed Start platform: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateDetailedProps(Node3D props)
    {
        var bench = props.GetNode<Node3D>("Bench");
        var vehicle = props.GetNode<Node3D>("UtilityVehicle");
        var cafe = props.GetNode<Node3D>("CafeSet");
        var construction = props.GetNode<Node3D>("ConstructionCluster");
        var leftTree = props.GetNode<Node3D>("ScreenLeftTreePlanter");
        var rightTree = props.GetNode<Node3D>("ScreenRightTreePlanter");

        foreach (var prop in new[] { bench, vehicle, cafe, construction, leftTree, rightTree })
        {
            Require(prop.SceneFilePath.Contains("res://game/world/props/start/", StringComparison.Ordinal),
                $"{prop.Name} must be instanced from reusable game/world/props/start assets. Path={prop.SceneFilePath}.");
        }

        Require(bench.GetNodeOrNull<MeshInstance3D>("WoodSlats/Seat01") is not null &&
                bench.GetNodeOrNull<MeshInstance3D>("WoodSlats/Back03") is not null &&
                bench.GetNodeOrNull<MeshInstance3D>("MetalFrame/BackSupportLeft") is not null,
            "StartBench must preserve multi-slat woodwork and metal supports.");

        Require(vehicle.GetNodeOrNull<MeshInstance3D>("Cabin/Windshield") is not null &&
                vehicle.GetNodeOrNull<MeshInstance3D>("Cabin/SeatLeft") is not null &&
                vehicle.GetNodeOrNull<MeshInstance3D>("Wheels/HubFL") is not null &&
                vehicle.GetNodeOrNull<MeshInstance3D>("HeadLampLeft") is not null,
            "StartUtilityVehicle must preserve cabin, seat, wheel-hub and lamp detail.");

        Require(cafe.GetNodeOrNull<MeshInstance3D>("Table/Cup") is not null &&
                cafe.GetNodeOrNull<MeshInstance3D>("Table/Plate") is not null &&
                cafe.GetNodeOrNull<MeshInstance3D>("ChairA/LegFL") is not null,
            "StartCafeSet must preserve tabletop props and chair-leg detail.");

        Require(construction.GetNodeOrNull<MeshInstance3D>("Scaffold/BraceA") is not null &&
                construction.GetNodeOrNull<MeshInstance3D>("Scaffold/BraceB") is not null &&
                construction.GetNodeOrNull<MeshInstance3D>("Cabinet/DoorLeft") is not null &&
                construction.GetNodeOrNull<MeshInstance3D>("ConeB") is not null,
            "StartConstructionCluster must preserve scaffold bracing, cabinet doors and multiple cones.");

        Require(leftTree.GetNodeOrNull<MeshInstance3D>("Tree/BranchLeft") is not null &&
                leftTree.GetNodeOrNull<MeshInstance3D>("Flowers/Flower01") is not null &&
                rightTree.GetNodeOrNull<MeshInstance3D>("Tree/CanopyRight") is not null,
            "StartTreePlanter must preserve branched tree, canopy clusters and flower detail.");
    }

    private static void ValidateStair(Node3D stair, string label)
    {
        var visual = stair.GetNode<Node3D>("StairVisual");
        var count = 0;
        foreach (var child in visual.GetChildren())
        {
            if (child is not MeshInstance3D step ||
                !step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
            {
                continue;
            }

            count++;
            var mesh = step.Mesh as BoxMesh
                ?? throw new InvalidOperationException($"{label}/{step.Name} must use BoxMesh.");
            Require(Mathf.Abs(mesh.Size.X - 3.4f) < 0.01f,
                $"{label} width must remain 3.4m during this reference pass. Actual={mesh.Size.X:0.00}.");
            Require(mesh.Material is not null &&
                    mesh.Material.ResourcePath.Contains("/materials/", StringComparison.Ordinal),
                $"{label}/{step.Name} must use an external material resource.");
        }

        Require(count == 12, $"{label} must contain 12 visible treads. Actual={count}.");
        Require(stair.GetNodeOrNull<CollisionShape3D>("Collision/Ramp") is not null,
            $"{label} must own its ramp collision.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
