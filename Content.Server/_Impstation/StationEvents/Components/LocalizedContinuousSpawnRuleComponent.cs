using Content.Server._Impstation.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Map;

namespace Content.Server._Impstation.StationEvents.Components;

[RegisterComponent, Access(typeof(LocalizedContinuousSpawnRule))]
public sealed partial class LocalizedContinuousSpawnRuleComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public MinMax Amount = new(1, 1);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public MinMax Radius = new(1, 1);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public LocId? NearestNavBeaconAnnouncement;

    [DataField]
    public MinMax TimeBetweenSpawn = new(1, 1);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public LocId? NearestNavBeaconEndAnnouncement;

    [ViewVariables(VVAccess.ReadOnly)]
    public string? NearestNavBeacon;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilNextSpawn;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<TileRef> AvailableTiles = new();
}
