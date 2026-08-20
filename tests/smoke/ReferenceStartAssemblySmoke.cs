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
                "Legacy UpperPlatform must be removed once StartPlatform owns the Start deck.");
            Require(level.GetNodeOrNull<Node3D>("Geometry/Stairs01") is null,
                "Legacy Stairs01 must be removed once StartPlatform owns the Start stair.");
            Require(level.GetNodeOrNull<CollisionShape3D>("Collision/WorldCollision/UpperPlatform") is null,
                "Legacy UpperPlatform collision must be removed with its visual.");
            Require(level.GetNodeOrNull<CollisionShape3D>("Collision/WorldCollision/StairRamp01") is null,
                "Legacy StairRamp01 collision must be removed with its stair visual.");

            var deck = startPlatform.GetNode<MeshInstance3D>("DeckMain");
            var deckMesh = deck.Mesh as BoxMesh
                ?? throw new InvalidOperationException("StartPlatform/DeckMain must use BoxMesh.");
            Require(deckMesh.Size.X > 10.0f,
                $"StartPlatform must own one continuous Start deck instead of wings around a legacy hole. Width={deckMesh.Size.X:0.000}.");
            Require(deckMesh.Material is not null &&
                    deckMesh.Material.ResourcePath.Contains("/materials/", StringComparison.Ordinal),
                "StartPlatform DeckMain must use an external material resource.");

            var deckCollision = startPlatform.GetNode<CollisionShape3D>("Collision/Deck");
            var deckShape = deckCollision.Shape as BoxShape3D
                ?? throw new InvalidOperationException("StartPlatform Collision/Deck must use BoxShape3D.");
            Require(deckShape.Size.DistanceTo(deckMesh.Size) < 0.01f,
                "StartPlatform must own collision matching the complete DeckMain mesh.");

            var stairVisual = startPlatform.GetNode<Node3D>("StairVisual");
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
                    ?? throw new InvalidOperationException($"{step.Name} must use BoxMesh.");
                Require(Mathf.Abs(box.Size.X - 4.2f) < 0.01f,
                    $"StartPlatform stair must keep the 4.2m replacement width. Actual={box.Size.X:0.000}.");
            }

            Require(stepCount == 12, $"StartPlatform must contain 12 visible treads. Actual={stepCount}.");
            Require(startPlatform.GetNodeOrNull<CollisionShape3D>("Collision/StairRamp") is not null,
                "StartPlatform must own the replacement stair collision.");
            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("StairScreenRightWall") is not null &&
                    startPlatform.GetNodeOrNull<MeshInstance3D>("StairScreenLeftWall") is not null,
                "StartPlatform must include both stair side walls.");

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

            GD.Print("[M1.6.4] PASS: StartPlatform completely replaces the legacy Start graybox and owns its collision.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.4] FAIL: complete Start replacement: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
