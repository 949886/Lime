using System;
using Godot;
using Lime.Game;
using Lime.Game.Actors.Player;
using Lime.Game.World;

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

            var scene = GD.Load<PackedScene>("res://game/GameRoot.tscn")
                ?? throw new InvalidOperationException("GameRoot.tscn could not be loaded.");

            var gameRoot = scene.Instantiate<GameRoot>();
            AddChild(gameRoot);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            ValidateCompositionRoot(gameRoot);
            ValidateFlowState(gameRoot);

            GD.Print("[M1.1/M1.2] PASS: Project skeleton runtime smoke completed successfully.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[M1.1/M1.2] FAIL: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ValidateProjectSettings()
    {
        Require(
            ProjectSettings.GetSetting("application/run/main_scene").AsString() ==
            "res://game/GameRoot.tscn",
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
        Require(gameRoot.Player.Name == "Player", "Player wiring is invalid.");
        Require(gameRoot.CameraRoot.Name == "CameraRoot", "CameraRoot wiring is invalid.");
        Require(gameRoot.UIRoot.Name == "UIRoot", "UIRoot wiring is invalid.");
        Require(gameRoot.AudioRoot.Name == "AudioRoot", "AudioRoot wiring is invalid.");
    }

    private static void ValidateFlowState(GameRoot gameRoot)
    {
        var flow = gameRoot.FlowController;
        var playerInput = gameRoot.Player.GetNode<PlayerInput>("%PlayerInput");

        Require(flow.CurrentGameState == GameState.Explore,
            "M1 bootstrap must start in Explore game state.");
        Require(flow.CurrentUiState == UiState.None,
            "M1 bootstrap must start with no blocking UI state.");
        Require(flow.GameplayInputEnabled,
            "Gameplay input must be enabled in Explore with no blocking UI.");
        Require(playerInput.Enabled,
            "GameRoot must enable PlayerInput while gameplay input is enabled.");

        flow.SetUiState(UiState.Dialogue);
        Require(!flow.GameplayInputEnabled,
            "Dialogue UI must gate gameplay input without changing GameState.");
        Require(flow.CurrentGameState == GameState.Explore,
            "Dialogue UI must not replace Explore GameState.");
        Require(!playerInput.Enabled,
            "Dialogue UI must disable PlayerInput through GameRoot flow wiring.");

        flow.SetUiState(UiState.None);
        Require(flow.GameplayInputEnabled,
            "Gameplay input must resume when blocking UI closes.");
        Require(playerInput.Enabled,
            "PlayerInput must resume when blocking UI closes.");

        flow.SetGameState(GameState.Cutscene);
        Require(!flow.GameplayInputEnabled,
            "Cutscene game state must gate gameplay input.");
        Require(flow.CurrentUiState == UiState.None,
            "Changing GameState must not implicitly mutate UiState.");
        Require(!playerInput.Enabled,
            "Cutscene game state must disable PlayerInput through GameRoot flow wiring.");

        flow.SetGameState(GameState.Explore);
        Require(flow.GameplayInputEnabled,
            "Gameplay input must resume after returning to Explore.");
        Require(playerInput.Enabled,
            "PlayerInput must resume after returning to Explore.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
