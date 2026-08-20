using Content.Server.Heretic.EntitySystems;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for rituals which spawn an entity on success.
/// </summary>
public sealed partial class TransmuteBehavior : SharedRitualBehaviorSystem
{
    [Dependency] private readonly HereticRitualSystem _ritual = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // TryDoRitual() in HereticRitualSystem already checks for item requirements and such.
        return true;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Get whatever entities should be spawned on success.
        var output = _ritual.GetRitual(ritualId).OutputItems ?? new Dictionary<EntProtoId, int>();

        foreach (var ent in output.Keys)
        {
            for (var i = 0; i < output[ent]; i++)
            {
                var spawn = Spawn(ent, Transform(platform).Coordinates);

                // If it's a minion, make the performer the owner.
                if (TryComp<MinionComponent>(spawn, out var minion))
                {
                    minion.BoundOwner = performer;
                }
            }
        }
    }
}
