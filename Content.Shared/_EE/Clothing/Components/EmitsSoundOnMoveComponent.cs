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
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier SoundCollection = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("requiresGravity"), AutoNetworkedField]
    public bool RequiresGravity = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates LastPosition = EntityCoordinates.Invalid;

    /// <summary>
    ///   Whether this item is equipped in a inventory item slot.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSlotValid = true;

    /// <summary>
    ///     If worn, how far the wearer has to walk in order to make a sound.
    /// </summary>
    [DataField]
    public float DistanceNeeded = 1.5f;

    /// <summary>
    ///     Whether or not this item must be worn in order to make sounds.
    /// </summary>
    [DataField]
    public bool RequiresWorn;

    /// <summary>
    ///     What slots we don't want noises to be emmited from
    /// </summary>
    [DataField]
    public SlotFlags InvalidSlots = SlotFlags.NONE;
}
