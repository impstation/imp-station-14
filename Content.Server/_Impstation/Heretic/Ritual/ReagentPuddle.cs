using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class ReagentPuddle : EntitySystem, IRitualBehavior
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [DataField] public List<ProtoId<ReagentPrototype>>? Reagents;

    private List<EntityUid> _uids = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        if (Reagents == null)
        {
            //should only happen if someone fucked up their ritual yaml
            _popup.PopupEntity(Loc.GetString("heretic-ritual-unknown"), platform, performer);
            return false;
        }
        string reagStrings = "";

        foreach (var reagent in Reagents)
        {
            reagStrings += reagent.Id + ", ";

            var lookup = _lookup.GetEntitiesInRange(platform, .75f);

            foreach (var ent in lookup)
            {
                if (!TryComp<PuddleComponent>(ent, out var puddle))
                    continue;

                if (puddle.Solution == null)
                    continue;

                var soln = puddle.Solution.Value;

                if (!soln.Comp.Solution.ContainsPrototype(reagent))
                    continue;

                _uids.Add(ent);
            }

            if (_uids.Count == 0)
            {
                continue;
            }

            return true;
        }

        //take off the comma + space on the end of the reagStrings
        reagStrings = reagStrings.Substring(0, reagStrings.Length - 2);
        _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-reagentpuddle", ("reagentname", reagStrings)), platform, performer);
        return false;
    }

    public void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        foreach (var uid in _uids)
            QueueDel(uid);

        _uids = [];
    }
}
