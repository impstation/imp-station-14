using Content.Shared._Impstation.StatusEffects;
using Content.Shared.StatusEffectNew;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;

namespace Content.Shared._Impstation.StatusEffects;

public sealed class MetabolicStasisSystem : EntitySystem
{
    [Dependency] private readonly SharedMetabolizerSystem _metabolizer = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolicStasisStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<MetabolicStasisStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<MetabolicStasisStatusEffectComponent, StatusEffectRelayedEvent<GetMetabolicMultiplierEvent>>(OnGetMetabolicMultiplier);
    }

    private void OnEffectApplied(Entity<MetabolicStasisStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _metabolizer.UpdateMetabolicMultiplier(args.Target);
    }

    private void OnEffectRemoved(Entity<MetabolicStasisStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _metabolizer.UpdateMetabolicMultiplier(args.Target);
    }

    private void OnGetMetabolicMultiplier(Entity<MetabolicStasisStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetMetabolicMultiplierEvent> args)
    {
        // multiply the metabolic rate by the stasis coefficient when it is updated.
        args.Args = args.Args with { Multiplier = args.Args.Multiplier * ent.Comp.StasisCoefficient };
    }
}
