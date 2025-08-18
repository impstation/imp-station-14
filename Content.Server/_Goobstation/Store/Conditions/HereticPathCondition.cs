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

    [DataField] public int Stage = 0;

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
        if (Stage > hereticComp.PathStage)
            return false;

        if (Whitelist != null && hereticComp.MainPath != null)
        {
            foreach (var white in Whitelist)
            {
                if (hereticComp.MainPath == white)
                    return true;
            }
            return false;
        }

        if (Blacklist != null && hereticComp.MainPath != null)
        {
            foreach (var black in Blacklist)
            {
                if (hereticComp.MainPath == black)
                    return false;
            }
            return true;
        }

        // If the heretic has a main path
        if (hereticComp.MainPath == null || args.Listing.ProductHereticKnowledge == null)
            return true;

        var knowledgeProtoId = new ProtoId<HereticKnowledgePrototype>((ProtoId<HereticKnowledgePrototype>)args.Listing.ProductHereticKnowledge);
        knowledgeSys.GetKnowledgePath(knowledgeSys.GetKnowledge(knowledgeProtoId), out var knowledgePath);

        // and the knowledge isn't from the main path
        if (knowledgePath != null && hereticComp.MainPath == knowledgePath)
            return true;

        // then add a penalty
        return Stage <= hereticComp.PathStage - AlternatePathPenalty;
    }
}
