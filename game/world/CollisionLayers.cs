using System;
using Godot;

namespace Lime.Game.World;

public static class CollisionLayers
{
    public const int WorldLayer = 1;
    public const int PlayerLayer = 2;
    public const int PartyLayer = 3;
    public const int NpcLayer = 4;
    public const int InteractableLayer = 5;
    public const int TriggerLayer = 6;

    public const uint WorldMask = 1u << (WorldLayer - 1);
    public const uint PlayerMask = 1u << (PlayerLayer - 1);
    public const uint PartyMask = 1u << (PartyLayer - 1);
    public const uint NpcMask = 1u << (NpcLayer - 1);
    public const uint InteractableMask = 1u << (InteractableLayer - 1);
    public const uint TriggerMask = 1u << (TriggerLayer - 1);

    private static readonly (int Layer, string Name)[] RequiredLayers =
    [
        (WorldLayer, "World"),
        (PlayerLayer, "Player"),
        (PartyLayer, "Party"),
        (NpcLayer, "NPC"),
        (InteractableLayer, "Interactable"),
        (TriggerLayer, "Trigger"),
    ];

    public static void ValidateConfigured()
    {
        foreach (var (layer, expectedName) in RequiredLayers)
        {
            var key = $"layer_names/3d_physics/layer_{layer}";
            var actualName = ProjectSettings.GetSetting(key).AsString();

            if (actualName != expectedName)
            {
                throw new InvalidOperationException(
                    $"Expected 3D physics layer {layer} to be '{expectedName}', got '{actualName}'.");
            }
        }
    }
}
