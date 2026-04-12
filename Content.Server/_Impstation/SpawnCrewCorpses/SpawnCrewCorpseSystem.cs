using System.Linq;
using Content.Server.Cloning;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Impstation.SpawnCrewCorpses;

/// <summary>
/// Generic _Impstation helper for spawning fake crew corpses.
/// Optionally copies appearance and jumpsuit from connected crew entities.
/// Requires a <see cref="SpawnCrewCorpseComponent"/> for configuration.
/// </summary>
public sealed class SpawnCrewCorpseSystem : EntitySystem
{
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <summary>
    /// Spawns corpses at random positions from <paramref name="candidateCoordinates"/>,
    /// using settings from <paramref name="settings"/>.
    /// Count is randomized between <see cref="SpawnCrewCorpseComponent.MinSpawnCount"/> and
    /// <see cref="SpawnCrewCorpseComponent.MaxSpawnCount"/>, clamped to available coordinate count.
    /// </summary>
    public List<EntityUid> SpawnCrewCorpses(IReadOnlyList<EntityCoordinates> candidateCoordinates, SpawnCrewCorpseComponent settings)
    {
        var spawned = new List<EntityUid>();
        if (candidateCoordinates.Count == 0 || settings.MaxSpawnCount <= 0)
            return spawned;

        var coords = candidateCoordinates.ToList();
        _random.Shuffle(coords);

        var count = _random.Next(settings.MinSpawnCount, settings.MaxSpawnCount + 1);
        count = Math.Clamp(count, 1, coords.Count);

        var crewSources = BuildCrewSourcePool();
        if (settings.DistinctCrewPerBatch)
            _random.Shuffle(crewSources);

        for (var i = 0; i < count; i++)
        {
            var corpse = Spawn(settings.CorpsePrototype, coords[i]);
            _metaData.SetEntityName(corpse, settings.CorpseName);

            if (settings.CloneAppearance && crewSources.Count > 0)
            {
                var source = settings.DistinctCrewPerBatch
                    ? crewSources[i % crewSources.Count]
                    : _random.Pick(crewSources);

                _humanoid.CloneAppearance(source, corpse);
                // Re-apply name after CloneAppearance since it may copy the source name
                _metaData.SetEntityName(corpse, settings.CorpseName);

                if (settings.CopyJumpsuitOnly)
                    CopyJumpsuit(source, corpse);
            }

            spawned.Add(corpse);
        }

        return spawned;
    }

    private List<EntityUid> BuildCrewSourcePool()
    {
        var sources = new List<EntityUid>();
        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;

            if (session.AttachedEntity is not { Valid: true } attached)
                continue;

            if (!HasComp<HumanoidAppearanceComponent>(attached))
                continue;

            sources.Add(attached);
        }

        return sources;
    }

    private void CopyJumpsuit(EntityUid source, EntityUid corpse)
    {
        if (!TryComp<InventoryComponent>(source, out var sourceInv) || !TryComp<InventoryComponent>(corpse, out var corpseInv))
            return;

        _cloning.CopyEquipment((source, sourceInv), (corpse, corpseInv), SlotFlags.INNERCLOTHING);
    }
}
