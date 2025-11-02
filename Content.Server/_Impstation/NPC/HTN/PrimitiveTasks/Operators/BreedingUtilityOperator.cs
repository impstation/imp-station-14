using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Queries;
using Content.Server.NPC.Queries.Queries;
using Content.Server.NPC.Systems;
using Content.Shared._Impstation.Nutrition.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;
public sealed partial class BreedingUtilityOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("key")] public string Key = "Target";

    [DataField("keyCoordinates")] public string KeyCoordinates = "TargetCoordinates";

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<UtilityQueryPrototype>))]
    public string Prototype = string.Empty;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard, CancellationToken cancelToken)
    {
        blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager);

        // Let's make sure it's something that can even reproduce first
        if (!_entManager.TryGetComponent<ImpReproductiveComponent>(owner, out var reproComp))
            return (false, null);

        var result = _entManager.System<NPCUtilitySystem>().GetEntities(blackboard, Prototype);
        var score = -float.MaxValue;
        EntityUid? target = null;

        //for every target
        foreach (var potentialTarget in result.Entities.Keys)
        {
            //if it doesn't exist, doesn't have a repro comp or isn't in our repro group, continue
            if (potentialTarget == EntityUid.Invalid ||
                !_entManager.TryGetComponent<ImpReproductiveComponent>(potentialTarget, out var comp) ||
                comp.ReproductiveGroup != reproComp.ReproductiveGroup)
                continue;

            //else, if it's score is less than our current score, continue
            var targScore = result.Entities[potentialTarget];
            if (targScore < score)
                continue;

            //finally, update our score and set the new target
            score = targScore;
            target = potentialTarget;
        }

        //we didn't find a valid target, so don't plan anything
        if (target == null)
            return (false, null);

        //we found a valid target, so return the plan
        return (true, new Dictionary<string, object>()
        {
            {Key, target.Value!},
            {KeyCoordinates, new EntityCoordinates(target.Value!, Vector2.Zero)}
        });
    }
}
