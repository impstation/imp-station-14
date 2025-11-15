using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;
public sealed partial class RitualAshAscendBehavior : RitualSacrificeBehavior
{
    private List<EntityUid> _usableUids = new();

    // check for burning corpses
    public override bool Execute(RitualData args, out string? outstr)
    {
        if (!base.Execute(args, out outstr))
            return false;

        for (int i = 0; i < Max; i++)
        {
            if (args.EntityManager.TryGetComponent<FlammableComponent>(Uids[i], out var flam))
                if (flam.OnFire)
                    _usableUids.Add(Uids[i]);
        }

        if (_usableUids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ash");
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
