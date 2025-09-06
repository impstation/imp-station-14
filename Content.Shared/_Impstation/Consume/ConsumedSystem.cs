using Content.Shared._Impstation.Consume.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared._Impstation.Consume;

public sealed class ConsumedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConsumedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ConsumedComponent, MobStateChangedEvent>(OnMobStateChange);
    }
    private void OnExamine(Entity<ConsumedComponent> ent, ref ExaminedEvent args)
    {
        var locIndex = ent.Comp.TimesConsumed switch
        {
            >= 8 => 4,
            >= 4 => 3,
            >= 2 => 2,
            _ => 1,
        };
        args.PushMarkup(Loc.GetString($"consumed-onexamine-{locIndex}", ("target", Identity.Entity(ent, EntityManager))));
    }
    private void OnMobStateChange(Entity<ConsumedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            RemComp<ConsumedComponent>(ent);
    }
}
