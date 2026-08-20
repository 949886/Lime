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
            var background = visuals.GetNode<Node3D>("StartBackgroundStructures");

            Require(level.GetNodeOrNull<Node3D>("Geometry/UpperPlatform") is null,
                "Legacy UpperPlatform must stay removed after StartPlatform takes ownership.");
            Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is null,
                "Legacy Stairs01 must stay removed after StartPlatform takes ownership.");

            var deck = startPlatform.GetNode<MeshInstance3D>("DeckMain");
            var deckMesh = deck.Mesh as BoxMesh
                ?? throw new InvalidOperationException("StartPlatform/DeckMain must use BoxMesh.");
            Require(deckMesh.Size.X >= 16.0f,
                $"Supplemental Start reference requires a broad continuous upper plaza. Width={deckMesh.Size.X:0.000}.");
            Require(deckMesh.Material is not null &&
                    deckMesh.Material.ResourcePath.Contains("/materials/", StringComparison.Ordinal),
                "StartPlatform DeckMain must use an external material resource.");

            var deckCollision = startPlatform.GetNode<CollisionShape3D>("Collision/Deck");
            var deckShape = deckCollision.Shape as BoxShape3D
                ?? throw new InvalidOperationException("StartPlatform Collision/Deck must use BoxShape3D.");
            Require(deckShape.Size.DistanceTo(deckMesh.Size) < 0.01f,
                "StartPlatform collision must match the broad continuous deck.");

            ValidateStair(startPlatform.GetNode<Node3D>("RightStair"), "RightStair");
            ValidateStair(startPlatform.GetNode<Node3D>("LeftStair"), "LeftStair");

            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("FrontRetainingWall") is not null,
                "StartPlatform must include the retaining-wall mass between the two stair openings.");
            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("FrontRail") is not null,
                "StartPlatform must include the front rail above the central retaining wall.");

            Require(background.GetNodeOrNull<MeshInstance3D>("ScreenRightRoundBuilding") is not null,
                "StartBackgroundStructures must include the round-building mass.");
            Require(background.GetNodeOrNull<Node3D>("RearEquipment") is not null,
                "StartBackgroundStructures must include the rear equipment cluster.");

            var rowCount = 0;
            var columnCount = 0;
            foreach (var child in grid.GetChildren())
            {
                var name = child.Name.ToString();
                if (name.StartsWith("Row", StringComparison.Ordinal)) rowCount++;
                else if (name.StartsWith("Column", StringComparison.Ordinal)) columnCount++;
            }
            Require(rowCount == 6 && columnCount == 11,
                $"StartDeckGrid must preserve the square-grid ruler. Rows={rowCount}, Columns={columnCount}.");

            GD.Print("[M1.6.4] PASS: supplemental Start reference topology (broad deck + dual stairs + retaining wall) is scene-authored.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.4] FAIL: supplemental Start topology: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateStair(Node3D stair, string name)
    {
        var stairVisual = stair.GetNode<Node3D>("StairVisual");
        var stepCount = 0;
        foreach (var child in stairVisual.GetChildren())
        {
            if (child is not MeshInstance3D step ||
                !step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
            {
                continue;
            }

            stepCount++;
            var box = step.Mesh as BoxMesh
                ?? throw new InvalidOperationException($"{name}/{step.Name} must use BoxMesh.");
            Require(Mathf.Abs(box.Size.X - 3.4f) < 0.01f,
                $"{name} reference stair width must be 3.4m in this topology pass. Actual={box.Size.X:0.000}.");
            Require(box.Material is not null &&
                    box.Material.ResourcePath.Contains("/materials/", StringComparison.Ordinal),
                $"{name}/{step.Name} must use the external Start step material.");
        }

        Require(stepCount == 12, $"{name} must contain 12 visible treads. Actual={stepCount}.");
        Require(stair.GetNodeOrNull<CollisionShape3D>("Collision/Ramp") is not null,
            $"{name} must own its traversal ramp collision.");
        Require(stair.GetNodeOrNull<MeshInstance3D>("OuterSideWall") is not null &&
                stair.GetNodeOrNull<MeshInstance3D>("InnerSideWall") is not null,
            $"{name} must include both thick side walls.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
