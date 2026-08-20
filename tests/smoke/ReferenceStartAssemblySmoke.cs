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
            var visuals = level.GetNodeOrNull<Node3D>("ReferenceLevelVisuals")
                ?? throw new InvalidOperationException("ReferenceLevelVisuals must be statically instanced by ReferenceLevelComposed.tscn.");
            var startPlatform = visuals.GetNodeOrNull<Node3D>("StartPlatform")
                ?? throw new InvalidOperationException("StartPlatform.tscn must be statically instanced in ReferenceLevelVisuals.");
            var grid = visuals.GetNodeOrNull<Node3D>("StartDeckGrid")
                ?? throw new InvalidOperationException("StartDeckGrid.tscn must be statically instanced in ReferenceLevelVisuals.");
            var background = visuals.GetNodeOrNull<Node3D>("StartBackgroundStructures")
                ?? throw new InvalidOperationException("StartBackgroundStructures.tscn must be statically instanced in ReferenceLevelVisuals.");

            var legacyStairs = level.GetNode<Node3D>("Geometry/Stairs01");
            Require(!legacyStairs.Visible,
                "ReferenceLevelComposed.tscn must own the legacy Stairs01 visibility override.");

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
                    $"StartPlatform stair must keep the Pass-A 4.2m width. Actual={box.Size.X:0.000}.");
                Require(box.Material is not null &&
                        box.Material.ResourcePath.Contains("/materials/", StringComparison.Ordinal),
                    $"{step.Name} material must be an external resource, not a runtime-created material.");
            }

            Require(stepCount == 12, $"StartPlatform must contain 12 visible treads. Actual={stepCount}.");
            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("DeckScreenRightWing") is not null,
                "StartPlatform must include the broad screen-right deck wing.");
            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("DeckScreenLeftWing") is not null,
                "StartPlatform must include the screen-left deck wing/recess boundary.");
            Require(startPlatform.GetNodeOrNull<MeshInstance3D>("StairScreenRightWall") is not null &&
                    startPlatform.GetNodeOrNull<MeshInstance3D>("StairScreenLeftWall") is not null,
                "StartPlatform must include both thick stair side walls.");

            Require(background.GetNodeOrNull<MeshInstance3D>("ScreenRightRoundBuilding") is not null,
                "StartBackgroundStructures must include the round-building mass.");
            Require(background.GetNodeOrNull<Node3D>("RearEquipment") is not null,
                "StartBackgroundStructures must include the rear equipment cluster.");

            var rowCount = 0;
            var columnCount = 0;
            foreach (var child in grid.GetChildren())
            {
                var name = child.Name.ToString();
                if (name.StartsWith("Row", StringComparison.Ordinal))
                {
                    rowCount++;
                }
                else if (name.StartsWith("Column", StringComparison.Ordinal))
                {
                    columnCount++;
                }
            }

            Require(rowCount == 6 && columnCount == 11,
                $"StartDeckGrid must preserve the Pass-A grid ruler. Rows={rowCount}, Columns={columnCount}.");

            GD.Print("[M1.6.4] PASS: Start reconstruction is scene-composed with external material resources.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.4] FAIL: scene-composed Start reconstruction: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
