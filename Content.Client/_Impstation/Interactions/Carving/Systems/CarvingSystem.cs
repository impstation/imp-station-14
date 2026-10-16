using Content.Shared._Impstation.Interactions.Carving.Components;
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

        Subs.BuiEvents<CarvableComponent>(CarvingUiKey.Key, subs =>
        {
            subs.Event<CarvedEvent>(Personalise);
        });
    }

    /// <summary>
    /// Adds a new layer to the <see cref="SpriteComponent"/> and then sets that layer to the
    /// sprite that is set in <see cref="CarvableComponent"/>
    /// </summary>
    /// <param name="args"></param>
    private void Personalise(Entity<CarvableComponent> ent, ref CarvedEvent args)
    {
        if (args.Sprite != null)
            TryAddSprite(ent, args.Sprite);

    }

    private bool TryAddSprite(Entity<CarvableComponent> ent, SpriteSpecifier sprite)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var comp))
            return false;

        if(!TryGetLayer((ent.Owner, comp), CarvedVisuals.Carved, out var layer))
        {
            layer = _spriteSystem.AddLayer((ent.Owner, comp), sprite);
            _spriteSystem.LayerMapAdd((ent.Owner, comp), CarvedVisuals.Carved, (int)layer);
        }
        else
        {
            _spriteSystem.LayerSetSprite((ent.Owner, comp), CarvedVisuals.Carved, sprite);
        }

        return true;
    }

    private bool TryGetLayer(Entity<SpriteComponent?> ent, CarvedVisuals layer, out int? layerOut)
    {
        layerOut = 0;

        if (ent.Comp == null)
            return false;

        layerOut = _spriteSystem.LayerMapGet(ent, layer);

        return layerOut == 0 ? false : true;
    }
}
