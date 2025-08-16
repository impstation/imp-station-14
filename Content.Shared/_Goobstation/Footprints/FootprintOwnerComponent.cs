// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Shared._Goobstation.Footprints;

[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component
{
    /// <summary>
    ///     The maximum amount of reagents that one footprint can contain.
    /// </summary>
    [DataField]
    public float MaxFootVolume = 10;

    /// <summary>
    ///     The maximum amount of reagents that can be transferred to one bodyprint.
    /// </summary>
    [DataField]
    public float MaxBodyVolume = 20;

    /// <summary>
    ///     The minimum amount of reagents that can be transferred to one footprint.
    /// </summary>
    [DataField]
    public float MinFootprintVolume = 0.5f;

    [DataField]
    public float MaxFootprintVolume = 1;

    [DataField]
    public float MinBodyprintVolume = 2;

    [DataField]
    public float MaxBodyprintVolume = 5;

    /// <summary>
    ///     The distance this entity needs to travel before a new footprint is placed
    /// </summary>
    [DataField]
    public float FootDistance = 0.5f;

    /// <summary>
    ///     The distance this entity needs to travel before a new bodyprint is placed
    /// </summary>

    [DataField]
    public float BodyDistance = 1;

    /// <summary>
    ///     Current distance travelled since the last print was placed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Distance;

    /// <summary>
    ///     Offset breadth between two footprints (think left foot and right foot)
    /// </summary>
    [DataField]
    public float NextFootOffset = 0.0625f;

    /// <summary>
    ///     Sprite to be used for footprints
    /// </summary>
    [DataField] // imp add
    public SpriteSpecifier FootSprite = new SpriteSpecifier.Rsi(new ResPath("_Corvax/Effects/footprint.rsi"), "foot");

    /// <summary>
    ///     Sprite to be used for bodyprints
    /// </summary>
    [DataField] // imp add
    public SpriteSpecifier BodySprite = new SpriteSpecifier.Rsi(new ResPath("_Corvax/Effects/footprint.rsi"), "body");
}
