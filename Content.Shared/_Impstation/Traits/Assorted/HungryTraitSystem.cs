using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._Impstation.Traits.Assorted;

/// <summary>
/// This handles modifying hunger with the hungry trait component.
/// </summary>
public sealed class HungryTraitSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungryTraitComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<HungryTraitComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentStartup(Entity<HungryTraitComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<HungerComponent>(ent.Owner, out var hungerComp))
            return;
        var comp = ent.Comp;
        comp.StoredHunger = hungerComp.BaseDecayRate;
        hungerComp.BaseDecayRate = comp.HungryRate;
        _hunger.SetHunger(ent.Owner, comp.HungerLevel); //sets hunger & will run calculations and do hunger effects
        Dirty(ent);
        DirtyField(ent, hungerComp, nameof(HungerComponent.BaseDecayRate));
    }

    private void OnComponentShutdown(Entity<HungryTraitComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HungerComponent>(ent.Owner, out var hungerComp))
            return;
        hungerComp.BaseDecayRate = ent.Comp.StoredHunger; // returns hunger decay to stored amount or default
    }
}
