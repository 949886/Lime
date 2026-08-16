using System;
using System.Collections.Generic;
using Godot;

namespace Lime.App;

public static class InputActions
{
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string MoveForward = "move_forward";
    public const string MoveBackward = "move_backward";
    public const string Sprint = "sprint";
    public const string Interact = "interact";
    public const string UiConfirm = "ui_confirm";
    public const string UiCancel = "ui_cancel";

    private static readonly string[] RequiredActions =
    [
        MoveLeft,
        MoveRight,
        MoveForward,
        MoveBackward,
        Sprint,
        Interact,
        UiConfirm,
        UiCancel,
    ];

    public static IReadOnlyList<string> Required => RequiredActions;

    public static void ValidateConfigured()
    {
        foreach (var action in RequiredActions)
        {
            if (!InputMap.HasAction(action))
            {
                throw new InvalidOperationException(
                    $"Required InputMap action '{action}' is not configured in project.godot.");
            }
        }
    }
}
