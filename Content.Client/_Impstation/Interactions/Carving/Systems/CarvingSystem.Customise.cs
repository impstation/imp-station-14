using Content.Shared._Impstation.Interactions.Carving.Components;
using Content.Shared._Impstation.Interactions.Carving.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._Impstation.Interactions.Carving.Systems;

public sealed partial class CarvingSystem : EntitySystem
{
    private void Customise(CustomisePersonalisationEvent args)
    {
        var ent = GetEntity(args.Entity);
        if (!TryComp<CarvableComponent>(ent, out var carve) ||
            !TryComp<SpriteComponent>(ent, out var comp))
            return;

        if (!EnsureLayer((ent, comp), args.Mapping, args.Sprite, out var layer))
            return;

        _spriteSystem.LayerSetSprite((ent, comp), args.Mapping, args.Sprite);
    }

    private void AddState(AddPersonalisationState args)
    {
        var ent = GetEntity(args.Entity);
        if (!TryComp<CarvableComponent>(ent, out var carve) ||
            !TryComp<SpriteComponent>(ent, out var comp))
            return;

        if (!_spriteSystem.LayerExists(ent, CarvedVisuals.Carved))
            return;

        var baseLayer = _spriteSystem.LayerMapGet(ent, CarvedVisuals.Carved);

        if (!_spriteSystem.TryGetLayer((ent, comp), baseLayer, out var rsiLayer, false) ||
            rsiLayer.RSI == null)
            return;

        var newSprite = new Rsi(rsiLayer.RSI.Path, rsiLayer.State + args.StateExtension);

        if (!TryGetLayer((ent, comp), args.Mapping, out var layer))
            EnsureLayer((ent, comp), args.Mapping, newSprite, out layer);
        else
            _spriteSystem.LayerSetSprite((ent, comp), args.Mapping, newSprite);

        // Genuinely this should not be possible but the next line screams at me if this isn't included
        if (layer == null)
            return;

        _spriteSystem.LayerSetVisible((ent, comp), (int)layer, args.AddVisible);
    }
}
