using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;

namespace Content.Shared._Goobstation.Changeling;

public sealed partial class GoobAbsorbedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoobAbsorbedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<GoobAbsorbedComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    private void OnExamine(Entity<GoobAbsorbedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-absorb-onexamine", ("target", Identity.Entity(ent, EntityManager))));
    }

    private void OnMobStateChange(Entity<GoobAbsorbedComponent> ent, ref MobStateChangedEvent args)
    {
        // in case one somehow manages to dehusk someone
        if (args.NewMobState != MobState.Dead)
            RemComp<GoobAbsorbedComponent>(ent);
    }
}
