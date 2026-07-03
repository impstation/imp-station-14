using System.Numerics;
using Content.Server.GameTicking.Rules;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Periodically toggles doors near random non-slasher players for a fixed number of cycles.
/// </summary>
public sealed class SlasherGameRuleDoorMaliceSystem : GameRuleSystem<SlasherGameRuleDoorMaliceComponent>
{
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Initializes cycle timing so the first door-malice pass runs on the next active tick.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleDoorMaliceComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        // Schedule the first sparse cycle to fire immediately on the next ActiveTick.
        component.NextCycleAt = Timing.CurTime;
        component.CyclesDone = 0;
        component.DoorsSelected = false;
        component.AffectedDoors.Clear();
    }

    /// <summary>
    /// Executes door toggling cycles until the configured count is reached, then ends the rule.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule runtime state/configuration.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="frameTime">Frame delta in seconds.</param>
    protected override void ActiveTick(EntityUid uid, SlasherGameRuleDoorMaliceComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        // End the rule once all cycles are done.
        if (component.CyclesDone >= component.CycleCount)
        {
            GameTicker.EndGameRule(uid, gameRule);
            return;
        }

        if (Timing.CurTime < component.NextCycleAt)
            return;

        if (!component.DoorsSelected)
            SelectAffectedDoors(component);

        foreach (var doorUid in component.AffectedDoors)
        {
            if (Deleted(doorUid) || !HasComp<DoorComponent>(doorUid))
                continue;

            _door.TryToggleDoor(doorUid);
        }

        component.CyclesDone++;
        component.NextCycleAt = Timing.CurTime + TimeSpan.FromSeconds(component.CycleIntervalSeconds);
    }

    /// <summary>
    /// Chooses a fixed set of doors near a small set of random crew and keeps them for the whole pulse.
    /// </summary>
    private void SelectAffectedDoors(SlasherGameRuleDoorMaliceComponent component)
    {
        component.DoorsSelected = true;

        var players = GetNonSlasherPlayers();
        if (players.Count == 0)
            return;

        RobustRandom.Shuffle(players);
        var selectedPlayerCount = Math.Clamp(component.TargetPlayerCount, 1, players.Count);
        var radiusSq = component.DoorRadius * component.DoorRadius;
        var doorPositions = new List<(EntityUid Uid, Vector2 Position)>();

        PopulateDoorPositions(doorPositions);

        for (var i = 0; i < selectedPlayerCount; i++)
        {
            var player = players[i];
            var playerPos = _transform.GetWorldPosition(player);
            var nearbyDoors = GetNearbyDoors(playerPos, radiusSq, doorPositions);

            if (nearbyDoors.Count == 0)
                continue;

            RobustRandom.Shuffle(nearbyDoors);
            var selectedDoorCount = Math.Clamp(component.DoorsPerPlayer, 1, nearbyDoors.Count);

            for (var doorIndex = 0; doorIndex < selectedDoorCount; doorIndex++)
            {
                component.AffectedDoors.Add(nearbyDoors[doorIndex]);
            }
        }
    }

    /// <summary>
    /// Builds a list of currently attached non-slasher player entities.
    /// </summary>
    /// <returns>List of candidate player entity UIDs.</returns>
    private List<EntityUid> GetNonSlasherPlayers()
    {
        var players = new List<EntityUid>();
        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } playerEnt)
                continue;

            if (HasComp<SlasherRoleComponent>(playerEnt))
                continue;

            players.Add(playerEnt);
        }

        return players;
    }

    /// <summary>
    /// Caches world-space positions for all door entities.
    /// </summary>
    /// <param name="doorPositions">Destination list for door UID/position tuples.</param>
    private void PopulateDoorPositions(List<(EntityUid Uid, Vector2 Position)> doorPositions)
    {
        var doorQuery = AllEntityQuery<DoorComponent, TransformComponent>();
        while (doorQuery.MoveNext(out var doorUid, out _, out _))
        {
            doorPositions.Add((doorUid, _transform.GetWorldPosition(doorUid)));
        }
    }

    /// <summary>
    /// Filters cached doors to those within the squared radius around the supplied player position.
    /// </summary>
    /// <param name="playerPos">Player world position.</param>
    /// <param name="radiusSq">Squared search radius.</param>
    /// <param name="doorPositions">Cached door UID/position tuples.</param>
    /// <returns>Door UIDs within range.</returns>
    private static List<EntityUid> GetNearbyDoors(
        Vector2 playerPos,
        float radiusSq,
        List<(EntityUid Uid, Vector2 Position)> doorPositions)
    {
        var nearbyDoors = new List<EntityUid>();
        foreach (var (doorUid, doorPos) in doorPositions)
        {
            if ((doorPos - playerPos).LengthSquared() <= radiusSq)
                nearbyDoors.Add(doorUid);
        }

        return nearbyDoors;
    }

    /// <summary>
    /// Closes any tracked doors that remain open when the rule ends.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule runtime state/configuration.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule end event data.</param>
    protected override void Ended(EntityUid uid, SlasherGameRuleDoorMaliceComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        // Close any doors that were left open.
        foreach (var doorUid in component.AffectedDoors)
        {
            if (!TryComp<DoorComponent>(doorUid, out var door) || door.State != DoorState.Open)
                continue;

            _door.TryClose(doorUid, door);
        }
    }
}
