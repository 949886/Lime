using System;
using Godot;
using Lime.App;
using Lime.World;

namespace Lime.Tests.Smoke;

public partial class ProjectSkeletonSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            ValidateProjectSettings();
            InputActions.ValidateConfigured();
            CollisionLayers.ValidateConfigured();

            var scene = GD.Load<PackedScene>("res://app/GameRoot.tscn");
            Require(scene is not null, "GameRoot.tscn could not be loaded.");

            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            ValidateCompositionRoot(gameRoot);
            ValidateFlowState(gameRoot.FlowController);

            GD.Print("[M1.1] PASS: Project skeleton runtime smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.1] FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateProjectSettings()
    {
        Require(
            ProjectSettings.GetSetting("application/run/main_scene").AsString() ==
            "res://app/GameRoot.tscn",
            "GameRoot.tscn is not configured as the project main scene.");

        Require(
            ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32() == 2560,
            "Logical viewport width must be 2560 for the M1 bootstrap baseline.");
        Require(
            ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32() == 1440,
            "Logical viewport height must be 1440 for the M1 bootstrap baseline.");

        Require(
            ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString() ==
            "forward_plus",
            "Desktop renderer must remain Forward+.");
        Require(
            ProjectSettings.GetSetting("rendering/renderer/rendering_method.mobile").AsString() ==
            "mobile",
            "Mobile renderer override must remain Mobile.");

        Require(
            ProjectSettings.GetSetting("physics/common/physics_ticks_per_second").AsInt32() == 60,
            "Physics tick rate must remain 60 Hz.");
    }

    private static void ValidateCompositionRoot(GameRoot gameRoot)
    {
        Require(gameRoot.FlowController.IsStarted,
            "FlowController did not start when GameRoot became ready.");
        Require(gameRoot.WorldRoot.Name == "WorldRoot", "WorldRoot wiring is invalid.");
        Require(gameRoot.LevelRoot.Name == "LevelRoot", "LevelRoot wiring is invalid.");
        Require(gameRoot.ActorRoot.Name == "ActorRoot", "ActorRoot wiring is invalid.");
        Require(gameRoot.CameraRoot.Name == "CameraRoot", "CameraRoot wiring is invalid.");
        Require(gameRoot.UIRoot.Name == "UIRoot", "UIRoot wiring is invalid.");
        Require(gameRoot.AudioRoot.Name == "AudioRoot", "AudioRoot wiring is invalid.");
    }

    private static void ValidateFlowState(FlowController flow)
    {
        Require(flow.CurrentGameMode == GameMode.Explore,
            "M1 bootstrap must start in Explore mode.");
        Require(flow.CurrentUiMode == UiMode.None,
            "M1 bootstrap must start with no blocking UI mode.");
        Require(flow.GameplayInputEnabled,
            "Gameplay input must be enabled in Explore with no blocking UI.");

        flow.SetUiMode(UiMode.Dialogue);
        Require(!flow.GameplayInputEnabled,
            "Dialogue UI must gate gameplay input without changing GameMode.");
        Require(flow.CurrentGameMode == GameMode.Explore,
            "Dialogue UI must not replace Explore GameMode.");

        flow.SetUiMode(UiMode.None);
        Require(flow.GameplayInputEnabled,
            "Gameplay input must resume when blocking UI closes.");

        flow.SetGameMode(GameMode.Cutscene);
        Require(!flow.GameplayInputEnabled,
            "Cutscene mode must gate gameplay input.");
        Require(flow.CurrentUiMode == UiMode.None,
            "Changing GameMode must not implicitly mutate UiMode.");

        flow.SetGameMode(GameMode.Explore);
        Require(flow.GameplayInputEnabled,
            "Gameplay input must resume after returning to Explore.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
