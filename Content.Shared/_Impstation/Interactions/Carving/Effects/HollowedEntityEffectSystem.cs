using Content.Shared._Impstation.Interactions.Carving.Components;
using Content.Shared._Impstation.Interactions.Carving.Systems;
using Content.Shared.EntityEffects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Interactions.Carving.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HollowedEntityEffectSystem : EntityEffectSystem<CarvableComponent, Hollow>
{
    [Dependency] private readonly CarvingSystem _carvingSystem = default!;

    protected override void Effect(Entity<CarvableComponent> entity, ref EntityEffectEvent<Hollow> args)
    {
        if (!TryComp<HollowedComponent>(entity, out var hollow))
            AddComp<HollowedComponent>(entity);

        if(args.Effect.InheritLight)
        {
            _carvingSystem.AddLayerToSprite(entity, args.Effect.LitStateExtension, CarvedVisuals.Glowing, false);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Hollow : EntityEffectBase<Hollow>
{
    /// <summary>
    /// Allows the hollowed entity to adopt the light of any
    /// entity placed inside with a <see cref="PointLightComponent"/>
    /// </summary>
    [DataField("inheritLight")]
    public bool InheritLight = false;

    /// <summary>
    /// The extension for the lit sprite, usually this will just add onto the base state
    /// Example: pumpkin > pumpking_glowing
    /// </summary>
    [DataField("litStateExt")]
    public string LitStateExtension = "_glowing";
}
