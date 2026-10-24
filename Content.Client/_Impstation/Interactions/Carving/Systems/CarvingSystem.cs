using Content.Shared._Impstation.Interactions.Carving.Components;
using Content.Shared._Impstation.Interactions.Carving.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client._Impstation.Interactions.Carving.Systems;

public sealed partial class CarvingSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CarveSpriteEvent>(Personalise);
        SubscribeNetworkEvent<CustomisePersonalisationEvent>(Customise);
        SubscribeNetworkEvent<AddPersonalisationState>(AddState);
    }

    /// <summary>
    /// Adds a new layer to the <see cref="SpriteComponent"/> and then sets that layer to the
    /// sprite that is set in <see cref="CarvableComponent"/>
    /// </summary>
    /// <param name="args"></param>
    private void Personalise(CarveSpriteEvent args)
    {
        var ent = GetEntity(args.Entity);
        if (!TryComp<CarvableComponent>(ent, out var carve))
            return;

        if (args.Sprite != null)
            TryAddSprite((ent, carve), args.Sprite);

    }

    private bool TryAddSprite(Entity<CarvableComponent> ent, SpriteSpecifier sprite, CarvedVisuals layerMap = CarvedVisuals.Carved)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var comp))
            return false;

        if(!TryGetLayer((ent.Owner, comp), layerMap, out var layer))
        {
            layer = _spriteSystem.AddLayer((ent.Owner, comp), sprite);
            _spriteSystem.LayerMapAdd((ent.Owner, comp), layerMap, (int)layer);
        }
        else
        {
            _spriteSystem.LayerSetSprite((ent.Owner, comp), layerMap, sprite);
        }

        return true;
    }

    private bool EnsureLayer(Entity<SpriteComponent?> ent, Enum layer, SpriteSpecifier sprite, out int? layerOut)
    {
        layerOut = 0;

        if (ent.Comp == null ||
            !TryComp<SpriteComponent>(ent.Owner, out var comp))
            return false;

        if (_spriteSystem.LayerExists(ent, layer))
            layerOut = _spriteSystem.LayerMapGet(ent, layer);
        else
        {
            layerOut = _spriteSystem.AddLayer((ent.Owner, comp), sprite);
            _spriteSystem.LayerMapAdd((ent.Owner, comp), layer, (int)layerOut);
        }

        return layerOut == 0 ? false : true;
    }

    private bool TryGetLayer(Entity<SpriteComponent?> ent, Enum layer, out int? layerOut)
    {
        layerOut = 0;

        if (ent.Comp == null)
            return false;

        if(_spriteSystem.LayerExists(ent, layer))
            layerOut = _spriteSystem.LayerMapGet(ent, layer);

        return layerOut == 0 ? false : true;
    }
}
