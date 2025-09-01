using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Atmos.Rotting;

namespace Content.Shared._Goobstation.Changeling;

public sealed partial class SharedGoobAbsorbableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoobAbsorbableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<GoobAbsorbableComponent> ent, ref ExaminedEvent args)
    {
        var reducedBiomass = false;
        if (!HasComp<RottingComponent>(ent.Owner) && TryComp<GoobAbsorbableComponent>(ent.Owner, out var comp) && comp.BiomassRestored < 1)
            reducedBiomass = true;

        if (HasComp<GoobChangelingComponent>(args.Examiner) && !HasComp<GoobAbsorbedComponent>(ent.Owner) && reducedBiomass)
        {
            args.PushMarkup(Loc.GetString("changeling-examine-reduced-biomass", ("target", Identity.Entity(ent.Owner, EntityManager))));
        }
    }
}
