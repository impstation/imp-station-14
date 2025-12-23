using Content.Server.Heretic.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

public sealed partial class HereticPathCondition : ListingCondition
{
    [DataField] public int AlternatePathPenalty = 1;
    [DataField] public HashSet<ProtoId<HereticPathPrototype>>? Whitelist;
    [DataField] public HashSet<ProtoId<HereticPathPrototype>>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var knowledgeSys = ent.System<HereticKnowledgeSystem>();
        if (args.Listing.ProductHereticKnowledge == null)
            return false;

        var knowledgeProtoId = new ProtoId<HereticKnowledgePrototype>((ProtoId<HereticKnowledgePrototype>)args.Listing.ProductHereticKnowledge);
        var knowledge = knowledgeSys.GetKnowledge(knowledgeProtoId);
        // set effective power to required power
        var requiredPower = knowledge.RequiredPower;

        if (!ent.TryGetComponent<MindComponent>(args.Buyer, out var mind))
            return false;

        if (!ent.TryGetComponent<HereticComponent>(mind.OwnedEntity, out var hereticComp))
            return false;

        //Whitelist check
        if (Whitelist != null && hereticComp.MainPath != null && !Whitelist.Contains(hereticComp.MainPath.Value))
            return false;

        //Blacklist check
        if (Blacklist != null && hereticComp.MainPath != null && Blacklist.Contains(hereticComp.MainPath.Value))
            return false;

        // If the heretic's main path and the path the knowledge isn't the same
        if (knowledgeSys.GetKnowledgePath(knowledgeProtoId, out var path) && hereticComp.MainPath != null && hereticComp.MainPath.Value != path)
            requiredPower += AlternatePathPenalty;

        return hereticComp.Power >= requiredPower;
    }
}
