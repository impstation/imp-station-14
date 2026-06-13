using Content.Shared.Cuffs.Components;
using Content.Shared.Heretic.Prototypes;


namespace Content.Server.Heretic.Ritual;
public sealed partial class RitualHuntAscendBehavior : RitualSacrificeBehavior
{
    private List<EntityUid> _usableUids = new();

    // check for cuffed corpses
    public override bool Execute(RitualData args, out string? outstr)
    {
        if (!base.Execute(args, out outstr))
            return false;

        for (int i = 0; i < Max; i++)
        {
            if (args.EntityManager.TryGetComponent<CuffableComponent>(Uids[i], out var cuff))
                if (!cuff.CanStillInteract)
                    _usableUids.Add(Uids[i]);
        }

        if (_usableUids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-hunt");
            return false;
        }

        outstr = null;
        return true;
    }
    public override void Finalize(RitualData args)
    {
        base.Finalize(args);

        // reset it because blehhh
        _usableUids = new List<EntityUid>();
        Uids = new List<EntityUid>();
    }
}
