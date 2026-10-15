using Content.Shared._Impstation.Interactions.Carving.Components;
using Robust.Client.GameObjects;
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

        SubscribeNetworkEvent<CarvedEvent>(SetCarvedSprite);
    }

    /// <summary>
    /// Adds a new layer to the <see cref="SpriteComponent"/> and then sets that layer to the
    /// sprite that is set in <see cref="CarvableComponent"/>
    /// </summary>
    /// <param name="args"></param>
    private void SetCarvedSprite(CarvedEvent args)
    {
        if (TryComp<SpriteComponent>(args.Carved, out var comp))
            return;

        _spriteSystem.AddLayer((args.Carved, comp), args.Sprite);
    }
}
