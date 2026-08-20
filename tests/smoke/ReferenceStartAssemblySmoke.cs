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
            var assembly = level.GetNodeOrNull<Node3D>("Geometry/StartReferenceAssembly")
                ?? throw new InvalidOperationException("M1.6.4 StartReferenceAssembly was not instanced into production ReferenceLevel.");

            var legacyStairs = level.GetNode<Node3D>("Geometry/Stairs01");
            Require(!legacyStairs.Visible, "Legacy Stairs01 visual must be hidden during Start reconstruction Pass A.");

            var stairVisual = assembly.GetNode<Node3D>("StairVisual");
            var stepCount = 0;
            foreach (var child in stairVisual.GetChildren())
            {
                if (child is MeshInstance3D step && step.Name.ToString().StartsWith("Step", StringComparison.Ordinal))
                {
                    stepCount++;
                    var box = step.Mesh as BoxMesh
                        ?? throw new InvalidOperationException($"{step.Name} must use BoxMesh.");
                    Require(Mathf.Abs(box.Size.X - 4.2f) < 0.01f,
                        $"Rebuilt Start stair must keep the Pass-A 4.2m visual width. Actual={box.Size.X:0.000}.");
                }
            }

            Require(stepCount == 12, $"Rebuilt Start stair must contain 12 visible treads. Actual={stepCount}.");
            Require(assembly.GetNodeOrNull<MeshInstance3D>("DeckScreenRightWing") is not null,
                "Start assembly must include the broad screen-right deck wing.");
            Require(assembly.GetNodeOrNull<MeshInstance3D>("DeckScreenLeftWing") is not null,
                "Start assembly must include the screen-left deck wing/recess boundary.");
            Require(assembly.GetNodeOrNull<MeshInstance3D>("StairScreenRightWall") is not null &&
                    assembly.GetNodeOrNull<MeshInstance3D>("StairScreenLeftWall") is not null,
                "Start assembly must include both thick stair side walls.");
            Require(assembly.GetNodeOrNull<MeshInstance3D>("ScreenRightRoundBuilding") is not null,
                "Start assembly must include the rear round-building mass visible in the reference.");
            Require(assembly.GetNodeOrNull<Node3D>("RearEquipment") is not null,
                "Start assembly must include the rear equipment cluster.");

            GD.Print("[M1.6.4] PASS: visible Start reference assembly is instanced in production.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.6.4] FAIL: Start reference assembly: {exception}");
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
