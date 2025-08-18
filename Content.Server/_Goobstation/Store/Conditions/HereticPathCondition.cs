using Content.Server.Heretic.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

public sealed partial class HereticPathCondition : ListingCondition
{
    public int AlternatePathPenalty = 1; //you can only buy alternate paths' abilities if they are this amount under your initial path's top ability level.
    [DataField] public HashSet<ProtoId<HereticPathPrototype>>? Whitelist = [];
    [DataField] public HashSet<ProtoId<HereticPathPrototype>>? Blacklist = [];

    [DataField] public int RequiredPower;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var knowledgeSys = ent.System<HereticKnowledgeSystem>();

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind))
            return false;

        if (!ent.TryGetComponent<HereticComponent>(mind.OwnedEntity, out var hereticComp))
            return false;

        //Stage is the level of the knowledge we're looking at
        //always check for level
        return hereticComp.Power >= RequiredPower;
    }
}
