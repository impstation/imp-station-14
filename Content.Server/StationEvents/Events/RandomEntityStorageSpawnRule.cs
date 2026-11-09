using System.Linq; // Imp
using Content.Server.StationEvents.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

// Imp: Make partial
public sealed partial class RandomEntityStorageSpawnRule : StationEventSystem<RandomEntityStorageSpawnRuleComponent>
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    protected override void Started(EntityUid uid, RandomEntityStorageSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        // Imp Start: Refactor this to support multiple spawns.

        // Spawn entities for event.
        var spawns = GetSpawns((uid, comp));
        if (spawns.Count() == 0) // No valid entities to spawn.
            return;

        var spawnEnts = SpawnEntities(spawns);

        // Get a valid storage entity to insert these entity spawns into.
        var validLocker = GetValidLocker(spawnEnts, station.Value);
        if (validLocker == null)
        {
            // If there are no lockers to use, we just clean up.
            DeleteEntities(spawnEnts);
            return;
        }

        // Insert spawns into chosen locker.
        InsertAll(validLocker.Value, spawnEnts);

        // var validLockers = new List<(EntityUid, EntityStorageComponent)>();
        // var spawn = Spawn(comp.Prototype, MapCoordinates.Nullspace);

        // var query = EntityQueryEnumerator<EntityStorageComponent, TransformComponent>();
        // while (query.MoveNext(out var ent, out var storage, out var xform))
        // {
        //     if (StationSystem.GetOwningStation(ent, xform) != station)
        //         continue;

        //     if (!_entityStorage.CanInsert(spawn, ent, storage))
        //         continue;

        //     validLockers.Add((ent, storage));
        // }

        // if (validLockers.Count == 0)
        // {
        //     Del(spawn);
        //     return;
        // }

        // var (locker, storageComp) = RobustRandom.Pick(validLockers);
        // if (!_entityStorage.Insert(spawn, locker, storageComp))
        // {
        //     Del(spawn);
        // }
        // Imp End
    }
}
