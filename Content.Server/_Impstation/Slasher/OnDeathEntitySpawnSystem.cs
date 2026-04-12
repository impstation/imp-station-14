using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Content.Shared._Impstation.EntityEffects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles <see cref="OnDeathEntitySpawnComponent"/> by spawning entities in a tile area when the owner dies.
/// </summary>
public sealed class OnDeathEntitySpawnSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OnDeathEntitySpawnComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    /// Type definition for OnMobStateChanged.
    /// </summary>
    /// <param name="ent">Entity tuple containing UID and component data.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnMobStateChanged(Entity<OnDeathEntitySpawnComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead)
            return;

        var xform = Transform(ent.Owner);
        if (xform.GridUid == null || xform.MapUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var center = _map.WorldToTile(xform.GridUid.Value, grid, _xform.GetMapCoordinates(ent.Owner, xform).Position);
        var radius = Math.Max(0, ent.Comp.Radius);

        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                var tile = center + new Vector2i(dx, dy);
                if (!_map.TryGetTileRef(xform.GridUid.Value, grid, tile, out var tileRef)
                    || tileRef.Tile.IsEmpty
                    || _turf.IsSpace(tileRef)
                    || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                {
                    continue;
                }

                var spawnCoords = _map.GridTileToLocal(xform.GridUid.Value, grid, tile).SnapToGrid();
                Spawn(ent.Comp.EntityPrototype, spawnCoords);
            }
        }
    }
}
