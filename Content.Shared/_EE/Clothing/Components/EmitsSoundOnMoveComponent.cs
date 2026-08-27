// SPDX-FileCopyrightText: 2024 FoxxoTrystan <45297731+FoxxoTrystan@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._EE.Clothing.Components;

/// <summary>
///   Indicates that the clothing entity emits sound when it moves.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitsSoundOnMoveComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier SoundCollection = default!;

    [DataField, AutoNetworkedField]
    public bool RequiresGravity = true;

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownTimer = TimeSpan.Zero;

    /// <summary>
    ///   Whether this item is equipped in a valid item slot, invalid itemslots are defined by the InvalidSlots datafield.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSlotValid = true;

    /// <summary>
    ///     If worn, how far the wearer has to walk in order to make a sound.
    /// </summary>
    [DataField]
    public TimeSpan SoundCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     What slots we don't want noises to be emitted from, pocket by default
    /// </summary>
    [DataField]
    public SlotFlags InvalidSlots = SlotFlags.POCKET;
}
