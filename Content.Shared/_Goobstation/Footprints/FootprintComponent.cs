﻿// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Goobstation.Footprints;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FootprintComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public List<Footprint> Footprints = [];
}

[Serializable, NetSerializable]
public readonly record struct Footprint(
    Vector2 Offset,
    Angle Rotation,
    Color Color,
    SpriteSpecifier Sprite);
