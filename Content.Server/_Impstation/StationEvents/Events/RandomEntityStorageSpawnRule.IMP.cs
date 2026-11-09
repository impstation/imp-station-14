
using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class RandomEntityStorageSpawnRule
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    /// <summary>
    ///     Get a collection of entity prototypes to spawn in this event.
    /// </summary>
    private IEnumerable<EntProtoId> GetSpawns(Entity<RandomEntityStorageSpawnRuleComponent> rule)
    {
        // Fallback to the entity prototype ID if this is not defined.
        var table = rule.Comp.Table;
        if (table == null)
        {
            var spawns = new List<EntProtoId>() { rule.Comp.Prototype };
            return spawns;
        }

        // Otherwise, get a set of spawns from the entity table.
        return _entityTable.GetSpawns(table).ToList();

        // Did you know this natively supports conditionals like "round duration" or "current player count"?
        // I thought that was interesting. For more information google EntityTableCondition
    }

    /// <summary>
    ///     Spawn entities from the given prototype IDs.
    /// </summary>
    /// <param name="spawns">Entities to spawn.</param>
    /// <returns>A list of spawned entity IDs.</returns>
    private List<EntityUid> SpawnEntities(IEnumerable<EntProtoId> spawns)
    {
        var spawnEnts = new List<EntityUid>();
        foreach (var spawn in spawns)
        {
            var ent = Spawn(spawn, MapCoordinates.Nullspace);
            spawnEnts.Add(ent);
        }

        return spawnEnts;
    }

    /// <summary>
    ///     Delete all entities in a given list.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    private void DeleteEntities(List<EntityUid> entities)
    {
        foreach (var uid in entities)
            Del(uid);
    }

    /// <summary>
    ///     Get a random, valid locker that will fit all of the given entities on a certain station.
    /// </summary>
    /// <param name="spawnEnts">The entities to insert into the locker.</param>
    /// <param name="station">The station to get the locker from.</param>
    /// <returns>A valid locker, or null if there are no spawned entities, or one could not be found.</returns>
    private Entity<EntityStorageComponent>? GetValidLocker(List<EntityUid> spawnEnts, EntityUid station)
    {
        if (spawnEnts.Count == 0)
            return null;

        var validLockers = new List<(EntityUid, EntityStorageComponent)>();
        var query = EntityQueryEnumerator<EntityStorageComponent, TransformComponent>();

        while (query.MoveNext(out var ent, out var storage, out var xform))
        {
            if (StationSystem.GetOwningStation(ent, xform) != station)
                continue;

            if (!CanInsertAll((ent, storage), spawnEnts))
                continue;

            validLockers.Add((ent, storage));
        }

        if (validLockers.Count == 0)
            return null;

        return RobustRandom.Pick(validLockers);
    }

    /// <summary>
    ///     Check if a certain entity storage can fit all of the given entities.
    /// </summary>
    /// <param name="storage">The storage to use.</param>
    /// <param name="spawnEnts">Entities to check insertability.</param>
    private bool CanInsertAll(Entity<EntityStorageComponent> storage, List<EntityUid> spawnEnts)
    {
        // This upsets me. Please don't make entity table odds that return a lot of entities.
        foreach (var ent in spawnEnts)
        {
            if (!_entityStorage.CanInsert(ent, storage))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Insert all of the entities into the given storage, or drop the entity next to it if impossible.
    /// </summary>
    /// <param name="storage">The storage to use.</param>
    /// <param name="spawnEnts">Entities to insert into storage.</param>
    private void InsertAll(Entity<EntityStorageComponent> storage, List<EntityUid> spawnEnts)
    {
        foreach (var ent in spawnEnts)
        {
            if (!_entityStorage.Insert(ent, storage))
                _xform.DropNextTo(ent, storage.Owner);
        }
    }
}
