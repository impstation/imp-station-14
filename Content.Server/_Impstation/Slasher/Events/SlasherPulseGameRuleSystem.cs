using Content.Server._Impstation.GameTicking.Rules;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared._Impstation.Slasher.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Base class for Slasher pulse game rules that should affect the Slasher's current station.
/// </summary>
public abstract class SlasherPulseGameRuleSystem<T> : GameRuleSystem<T> where T : IComponent
{
    [Dependency] private readonly SlasherRuleSystem _slasherRule = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// Resolves the station currently associated with the active Slasher encounter.
    /// Prefers the active effigy station, then falls back to any active Slasher role holder.
    /// </summary>
    /// <param name="station">Resolved station UID when successful.</param>
    /// <returns>True when a Slasher station is resolved; otherwise false.</returns>
    protected bool TryGetPulseStation(out EntityUid? station)
    {
        if (_slasherRule.TryGetActiveRule(out var rule)
            && rule.Comp.ActiveEffigy is { } effigy
            && Exists(effigy)
            && _station.GetOwningStation(effigy) is { } effigyStation)
        {
            station = effigyStation;
            return true;
        }

        var slashers = EntityQueryEnumerator<SlasherRoleComponent>();
        while (slashers.MoveNext(out var slasherUid, out _))
        {
            var slasherStation = _station.GetOwningStation(slasherUid);
            if (slasherStation == null)
                continue;

            station = slasherStation;
            return true;
        }

        station = null;
        return false;
    }

    /// <summary>
    /// Checks whether an entity is owned by the resolved pulse station.
    /// </summary>
    protected bool IsOnPulseStation(EntityUid entity, EntityUid? station)
    {
        if (station == null)
            return false;

        return _station.GetOwningStation(entity) == station;
    }
}
