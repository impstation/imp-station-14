using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids.Components;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Chemistry.EntitySystems; // Impstation - Upmerge 2026 Chunk IV

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualReagentPuddleBehavior : RitualCustomBehavior
{
    private EntityLookupSystem _lookup = default!;
    private SharedSolutionContainerSystem _solutionContainerSystem = default!; // Impstation - Upmerge 2026 Chunk IV

    [DataField] public List<ProtoId<ReagentPrototype>>? Reagents;

    private List<EntityUid> _uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        if (Reagents == null)
        {
            //should only happen if someone fucked up their ritual yaml
            outstr = Loc.GetString("heretic-ritual-unknown");
            return false;
        }
        string reagStrings = "";

        _solutionContainerSystem = args.EntityManager.System<SharedSolutionContainerSystem>(); // Impstation - Upmerge 2026 Chunk IV

        foreach (var reagent in Reagents)
        {
            reagStrings += reagent.Id + ", ";

            outstr = null;
            _lookup = args.EntityManager.System<EntityLookupSystem>();

            var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);

            foreach (var ent in lookup)
            {
                if (!args.EntityManager.TryGetComponent<PuddleComponent>(ent, out var puddle))
                    continue;

                if(!args.EntityManager.TryGetComponent<SolutionContainerManagerComponent>(ent, out var container)) // Impstation - Upmerge 2026 Chunk IV
                    continue;

                if (puddle.Solution == null)
                    continue;

                var soln = puddle.Solution.Value;

                if(!_solutionContainerSystem.TryGetSolution((ent, container), reagent.Id, out var solution)) // Impstation - Upmerge 2026 Chunk IV
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
        outstr = Loc.GetString("heretic-ritual-fail-reagentpuddle", ("reagentname", reagStrings));
        return false;

    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in _uids)
            args.EntityManager.QueueDeleteEntity(uid);
        _uids = new();
    }
}
