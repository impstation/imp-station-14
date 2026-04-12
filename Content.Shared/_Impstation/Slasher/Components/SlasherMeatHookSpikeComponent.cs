using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Impstation variant of kitchen spike behavior used by Slasher meathooks.
/// Adapted from upstream <c>KitchenSpikeComponent</c> by Space Wizards contributors.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedSlasherMeatHookSpikeSystem))]
public sealed partial class SlasherMeatHookSpikeComponent : Component
{
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpike = new("Spike");

    /// <summary>
    /// ID of the container where the hooked victim will be stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = "body";

    /// <summary>
    /// Container slot where the hooked victim is stored.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// Sound to play when a victim is hooked or unhooked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier SpikeSound = new SoundCollectionSpecifier(DefaultSpike);

    /// <summary>
    /// Damage applied when a victim is hooked or unhooked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier SpikeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Piercing", 10 },
        },
    };

    /// <summary>
    /// Time required to hook a victim.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HookDelay = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Base time required to unhook a victim.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UnhookDelay = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Marker for entities currently immobilized on a Slasher meathook.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSlasherMeatHookSpikeSystem))]
public sealed partial class SlasherMeatHookedComponent : Component;

/// <summary>
/// Appearance visual keys for the Slasher meathook sprite.
/// These live with the component because the hook system and prototype visualizer both reference them.
/// </summary>
[Serializable, NetSerializable]
public enum SlasherMeatHookVisuals : byte
{
    Status,
}

/// <summary>
/// Visual states broadcast to clients for the meathook's current harvest state.
/// </summary>
[Serializable, NetSerializable]
public enum SlasherMeatHookStatus : byte
{
    Empty,
    PendingHarvest,
    Harvested,
}
