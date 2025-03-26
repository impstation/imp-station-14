using Content.Shared._Impstation.Kodepiiae.Components;
using Content.Shared.Changeling;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared._Impstation.Kodepiiae;

public sealed class KodepiiaeConsumedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaeConsumedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<KodepiiaeConsumedComponent, MobStateChangedEvent>(OnMobStateChange);
    }
    private void OnExamine(Entity<KodepiiaeConsumedComponent> ent, ref ExaminedEvent args)
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
    private void OnMobStateChange(Entity<KodepiiaeConsumedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            RemComp<KodepiiaeConsumedComponent>(ent);
    }
}
