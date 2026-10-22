using Content.Server._Impstation.Spawners.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Spawners.EntitySystems;

/// <summary>
/// A spawner that will spawn an entity once at a random spawner.
/// </summary>
public sealed class LinkedSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LinkedSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<LinkedSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (EntityManager.IsQueuedForDeletion(ent))
            return;

        var gridUid = Transform(ent).GridUid;
        var randomDict = new Dictionary<EntityUid, float>();
        var query = AllEntityQuery<LinkedSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var linkedSpawner, out var xform))
        {
            if (linkedSpawner.Prototype != ent.Comp.Prototype || xform.GridUid != gridUid)
                continue;

            randomDict.Add(uid, linkedSpawner.Weight);
            QueueDel(uid);
        }

        Spawn(ent.Comp.Prototype, Transform(_random.Pick(randomDict)).Coordinates);
    }
}
