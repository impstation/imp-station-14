using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Queries;
using Content.Server.NPC.Systems;
using Content.Server.NPC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server._Impstation.NPC.Components;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Handles figuring out what our chase target is and setting the chasing timer.
/// </summary>
public sealed partial class ChaseOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    [DataField("key")] public string Key = "Target";

    /// <summary>
    /// The EntityCoordinates of the specified target.
    /// </summary>
    [DataField("keyCoordinates")]
    public string KeyCoordinates = "TargetCoordinates";

    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<UtilityQueryPrototype>))]
    public string Prototype = string.Empty;

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var result = _entManager.System<NPCUtilitySystem>().GetEntities(blackboard, Prototype);
        var target = result.GetHighest();

        // Are we an animal to begin with?
        if (!_entManager.TryGetComponent<AnimalNPCComponent>(owner, out var animalBrain))
            return (false, null);

        // Is our target real?
        if (!target.IsValid())
            return (false, null);

        // It's time to start the chase!
        if (animalBrain.CurrentMood != AnimalMood.Chasing && animalBrain.CurrentMood != AnimalMood.Tired)
        {
            animalBrain.CurrentMood = AnimalMood.Chasing;
            animalBrain.EndChase = _time.CurTime + animalBrain.MaxChaseTime;
        }

        var effects = new Dictionary<string, object>()
        {
            {Key, target},
            {KeyCoordinates, new EntityCoordinates(target, Vector2.Zero)}
        };

        return (true, effects);
    }
}

