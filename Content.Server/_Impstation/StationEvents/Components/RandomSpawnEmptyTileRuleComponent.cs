using Content.Server._Impstation.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomSpawnEmptyTileRule))]
public sealed partial class RandomSpawnEmptyTileRuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId SpawnedEntity;

    [DataField]
    public EntProtoId? SpawnEffect = null;

    [DataField(required: true)]
    public MinMax MinMaxEntities;

    [DataField]
    public LocId? Announcement = null;
}
