using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;

namespace Content.Server._Offbrand.Wounds;

public sealed class MetabolicStasisSystem : EntitySystem
{
    [Dependency] private readonly SharedMetabolizerSystem _metabolizer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolicStasisComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MetabolicStasisComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<MetabolicStasisComponent, GetMetabolicMultiplierEvent>(OnGetMetabolicMultiplier);
    }

    private void OnComponentInit(Entity<MetabolicStasisComponent> ent, ref ComponentInit args)
    {
        // when this component is added to an entity, update that entity's metabolic multiplier
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnComponentRemoved(Entity<MetabolicStasisComponent> ent, ref ComponentRemove args)
    {
        // when this component is *removed* from an entity, set the stasis coefficient to 1
        ent.Comp.StasisCoefficient = 1.0f;
        // then update that entity's metabolic multiplier
        _metabolizer.UpdateMetabolicMultiplier(ent);
    }

    private void OnGetMetabolicMultiplier(Entity<MetabolicStasisComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        // multiply the metabolic rate by the stasis coefficient when it is updated.
        args.Multiplier *= ent.Comp.StasisCoefficient;
    }
}
