using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared._Impstation.Kodepiia;

public sealed class KodepiiaConsumedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Consume.Components.ConsumedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<Consume.Components.ConsumedComponent, MobStateChangedEvent>(OnMobStateChange);
    }
    private void OnExamine(Entity<Consume.Components.ConsumedComponent> ent, ref ExaminedEvent args)
    {
        var locIndex = ent.Comp.TimesConsumed switch
        {
            >= 8 => 4,
            >= 4 => 3,
            >= 2 => 2,
            _ => 1,
        };
        args.PushMarkup(Loc.GetString($"kodepiia-consumed-onexamine-{locIndex}", ("target", Identity.Entity(ent, EntityManager))));
    }
    private void OnMobStateChange(Entity<Consume.Components.ConsumedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            RemComp<Consume.Components.ConsumedComponent>(ent);
    }
}
