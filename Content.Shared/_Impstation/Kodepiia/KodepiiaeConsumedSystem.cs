using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared._Impstation.Kodepiia;

public sealed class KodepiiaeConsumedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Kodepiia.Components.KodepiiaeConsumedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<Kodepiia.Components.KodepiiaeConsumedComponent, MobStateChangedEvent>(OnMobStateChange);
    }
    private void OnExamine(Entity<Kodepiia.Components.KodepiiaeConsumedComponent> ent, ref ExaminedEvent args)
    {
        var locIndex = ent.Comp.TimesConsumed switch
        {
            >= 8 => 4,
            >= 4 => 3,
            >= 2 => 2,
            _ => 1,
        };
        args.PushMarkup(Loc.GetString($"kodepiiae-consumed-onexamine-{locIndex}", ("target", Identity.Entity(ent, EntityManager))));
    }
    private void OnMobStateChange(Entity<Kodepiia.Components.KodepiiaeConsumedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            RemComp<Kodepiia.Components.KodepiiaeConsumedComponent>(ent);
    }
}
