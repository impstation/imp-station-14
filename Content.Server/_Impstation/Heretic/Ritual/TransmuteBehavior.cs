using Content.Server.Heretic.EntitySystems;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for rituals which spawn an entity on success.
/// </summary>
[Serializable]
public sealed partial class TransmuteBehavior : SharedRitualBehaviorSystem
{
    /// <summary>
    /// What entities will be spawned on success.
    /// </summary>
    [DataField] public Dictionary<EntProtoId, int> OutputItems = [];

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
        foreach (var ent in OutputItems.Keys)
        {
            for (var i = 0; i < OutputItems[ent]; i++)
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
