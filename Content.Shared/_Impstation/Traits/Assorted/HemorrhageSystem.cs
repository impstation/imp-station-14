using Content.Shared.Body.Events;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Impstation.Traits.Assorted;

public sealed class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HemorrhageComponent, StatusEffectRelayedEvent<BleedModifierEvent>>(OnBleedModifier);
    }

    private void OnBleedModifier(Entity<HemorrhageComponent> ent, ref StatusEffectRelayedEvent<BleedModifierEvent> args)
    {
        var ev = args.Args;
        ev.BleedAmount *= ent.Comp.BleedAmountCoefficient;
        args.Args = ev;
    }
}
