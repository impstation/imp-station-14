using Content.Shared.Body.Events;

namespace Content.Shared._Impstation.Traits.Assorted;

public sealed class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HemorrhageComponent, BleedModifierEvent>(OnBleedModifier);
    }

    private void OnBleedModifier(Entity<HemorrhageComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedAmount *= ent.Comp.BleedAmountCoefficient;
    }
}
