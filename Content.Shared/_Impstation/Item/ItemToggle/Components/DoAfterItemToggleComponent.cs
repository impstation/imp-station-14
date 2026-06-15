using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Item.ItemToggle.Components;

/// <summary>
///     Generic component for toggling an item using a do-after.
///     Requires <see cref="ItemToggleComponent"/>.
/// </summary>
/// <remarks>
///     When using this component, you probably want the equivalent
///     "OnActivate" and "OnUse" in ItemToggleComponent set to false.
/// </remarks>
[RegisterComponent]
public sealed partial class Imp_DoAfterItemToggleComponent : Component
{
    /// <summary>
    ///     If true, this item can be toggled in world (E key, context menu verb).
    /// </summary>
    [DataField]
    public bool OnActivate = true;

    /// <summary>
    ///     If true, this item can be toggled in hand (Z key).
    /// </summary>
    [DataField]
    public bool OnUse = true;

    // TODO have a whole DoAfterArgs DataField without needing to have a user
    /// <inheritdoc cref="DoAfterArgs.Delay"/>
    [DataField]
    public float DoAfterTime = .75f;

    /// <inheritdoc cref="DoAfterArgs.NeedHand"/>
    [DataField]
    public bool NeedHand;

    /// <inheritdoc cref="DoAfterArgs.BreakOnMove"/>
    [DataField]
    public bool BreakOnMove;

    /// <inheritdoc cref="DoAfterArgs.BreakOnDamage"/>
    [DataField]
    public bool BreakOnDamage = true;
}

/// <summary>
///     Event which attempts to toggle <see cref="ItemToggleComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ItemToggleDoAfterEvent : SimpleDoAfterEvent
{
}
