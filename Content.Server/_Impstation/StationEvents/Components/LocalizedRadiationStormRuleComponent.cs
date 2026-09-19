using Content.Server._Impstation.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Map;

namespace Content.Server._Impstation.StationEvents.Components;

[RegisterComponent, Access(typeof(LocalizedRadiationStormRule))]
public sealed partial class LocalizedRadiationStormRuleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public MinMax StormRadius = new(10, 15);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public LocId Announcement = "station-event-localized-radiation-storm-announcement";

    [DataField]
    public MinMax TimeBetweenPulse = new(0, 2);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilNextPulse;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<TileRef> AvailableTiles = new();
}
