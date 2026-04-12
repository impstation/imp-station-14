using Content.Server.Anomaly;
using Content.Server.Announcements.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Spawns flesh and shadow anomaly spawners as a Slasher pulse event after a station-wide warning.
/// </summary>
public sealed class SlasherGameRuleAnomalyBurstSystem : SlasherPulseGameRuleSystem<SlasherGameRuleAnomalyBurstComponent>
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// Announces the event, picks a station grid, and spawns configured anomaly spawners.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleAnomalyBurstComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var announcementId = _announcer.GetAnnouncementId("AnomalySpawn");
        _announcer.SendAnnouncement(announcementId, Filter.Broadcast(), _announcer.GetEventLocaleString(announcementId), colorOverride: Color.Gold);

        if (!TryGetPulseStation(out var chosenStation) || chosenStation is not { } stationUid)
            return;

        if (!TryComp<StationDataComponent>(stationUid, out var stationData))
            return;

        var grid = _station.GetLargestGrid((stationUid, stationData));
        if (grid == null)
            return;

        for (var i = 0; i < component.FleshCount; i++)
            _anomaly.SpawnOnRandomGridLocation(grid.Value, component.FleshSpawnerPrototype);

        for (var i = 0; i < component.ShadowCount; i++)
            _anomaly.SpawnOnRandomGridLocation(grid.Value, component.ShadowSpawnerPrototype);
    }
}
