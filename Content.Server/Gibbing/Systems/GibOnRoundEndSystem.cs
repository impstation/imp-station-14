using Content.Shared.GameTicking;
using Content.Shared.Gibbing.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Server.Body.Systems;

namespace Content.Server.Gibbing.Systems;
public sealed class GibOnRoundEndSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        // this is raised after RoundEndTextAppendEvent, so they can successfully greentext before we gib them
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndMessageEvent args)
    {
        var gibQuery = EntityQueryEnumerator<GibOnRoundEndComponent>();

        // gib everyone with the component
        while (gibQuery.MoveNext(out var uid, out var gibComp))
        {
            var gib = false;
            // if they fulfill all objectives given in the component they are not gibbed
            if (_mind.TryGetMind(uid, out var mindId, out var mindComp))
            {
                foreach (var objectiveId in gibComp.PreventGibbingObjectives)
                {
                    if (!_mind.TryFindObjective((mindId, mindComp), objectiveId, out var objective)
                        || !_objectives.IsCompleted(objective.Value, (mindId, mindComp)))
                    {
                        gib = true;
                        break;
                    }
                }
                // imp addition: any clause
                if (!gib && gibComp.GibProofObjectives.Count != 0) //if not already failed (though why would you combine these two features anyway that sounds like hell)
                {
                    gib = true; //invert clause
                    foreach (var objectiveId in gibComp.GibProofObjectives)
                    {
                        if (_mind.TryFindObjective((mindId, mindComp), objectiveId, out var objective) //if they have the objective
                            && _objectives.IsCompleted(objective.Value, (mindId, mindComp))) //and they completed it
                        {
                            gib = false; // they're safe
                            break;
                        }
                    }
                }
                //end imp addition
            }
            else
                gib = true;

            if (!gib)
                continue;

            if (gibComp.SpawnProto != null)
                SpawnAtPosition(gibComp.SpawnProto, Transform(uid).Coordinates);

            _body.GibBody(uid, splatModifier: 5f);
        }
    }
}
