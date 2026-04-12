using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Spawns a random "Urist" near a random non-slasher player and gibs it after a short delay.
/// </summary>
public sealed class SlasherGameRuleUristGibSystem : GameRuleSystem<SlasherGameRuleUristGibComponent>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Picks a target player, spawns a random configured Urist prototype nearby, and schedules its gib.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleUristGibComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.UristPrototypes.Count == 0)
            return;

        var candidates = new List<EntityUid>();
        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is { } ent)
            {
                if (HasComp<SlasherRoleComponent>(ent))
                    continue;

                candidates.Add(ent);
            }
        }

        if (candidates.Count == 0)
            return;

        var target = candidates[RobustRandom.Next(candidates.Count)];
        var spawnCoords = ResolveSpawnNearTarget(target, component.SpawnRange);
        var uristProto = component.UristPrototypes[RobustRandom.Next(component.UristPrototypes.Count)];

        var urist = Spawn(uristProto, spawnCoords);
        _chat.TryEmoteWithChat(urist, "Scream");

        Timer.Spawn(component.GibDelay, () =>
        {
            if (Deleted(urist))
                return;

            _body.GibBody(urist, true);
        });
    }

    /// <summary>
    /// Finds a valid nearby tile around the target, falling back to target coordinates if none are suitable.
    /// </summary>
    /// <param name="target">Target entity UID.</param>
    /// <param name="range">Tile search radius.</param>
    /// <returns>Spawn coordinates near the target.</returns>
    private EntityCoordinates ResolveSpawnNearTarget(EntityUid target, int range)
    {
        var targetMap = _transform.GetMapCoordinates(target);
        if (!_mapManager.TryFindGridAt(targetMap, out var gridUid, out var gridComp))
            return Transform(target).Coordinates;

        var baseTile = _map.WorldToTile(gridUid, gridComp, targetMap.Position);
        var attempts = Math.Max(8, range * 8);

        for (var i = 0; i < attempts; i++)
        {
            var offsetX = RobustRandom.Next(-range, range + 1);
            var offsetY = RobustRandom.Next(-range, range + 1);
            var tile = new Vector2i(baseTile.X + offsetX, baseTile.Y + offsetY);

            if (!_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef)
                || tileRef.Tile.IsEmpty
                || _turf.IsSpace(tileRef)
                || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            return _map.GridTileToLocal(gridUid, gridComp, tile).SnapToGrid();
        }

        return Transform(target).Coordinates;
    }
}
