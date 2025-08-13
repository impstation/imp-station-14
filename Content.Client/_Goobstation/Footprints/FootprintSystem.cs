// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// imp cleanup on this whole thing to be more in line with wizden conventions -mq

using Content.Shared._Goobstation.Footprints;
using Robust.Client.GameObjects;

namespace Content.Client._Goobstation.Footprints;

public sealed class FootprintSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FootprintComponent, ComponentStartup>(OnComponentStartup);
        SubscribeNetworkEvent<FootprintChangedEvent>(OnFootprintChanged);
    }

    private void OnComponentStartup(Entity<FootprintComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent.Owner, ent.Comp);
    }

    private void OnFootprintChanged(FootprintChangedEvent args)
    {
        if (!TryGetEntity(args.Entity, out var ent))
            return;

        if (!TryComp<FootprintComponent>(ent, out var footprint))
            return;

        UpdateSprite(ent.Value, footprint);
    }

    private void UpdateSprite(Entity<SpriteComponent?> ent, FootprintComponent footprint)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        for (var i = 0; i < footprint.Footprints.Count; i++)
        {
            if (!_sprite.LayerExists(ent, i))
                _sprite.AddBlankLayer((ent, ent.Comp), i);

            _sprite.LayerSetOffset(ent, i, footprint.Footprints[i].Offset);
            _sprite.LayerSetRotation(ent, i, footprint.Footprints[i].Rotation);
            _sprite.LayerSetColor(ent, i, footprint.Footprints[i].Color);
            _sprite.LayerSetSprite(ent, i, footprint.Footprints[i].Sprite);
        }
    }
}
