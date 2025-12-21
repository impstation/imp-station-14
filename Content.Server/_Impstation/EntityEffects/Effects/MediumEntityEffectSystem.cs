using Content.Shared.EntityEffects;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared._Impstation.Ghost;

namespace Content.Server._Impstation.EntityEffects.Effects;

// TODO IMP: I don't think this works currently but it should probably be a status effect
public sealed partial class MediumEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Medium>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Medium> args)
    {
        EnsureComp<MediumComponent>(entity);
    }
}
