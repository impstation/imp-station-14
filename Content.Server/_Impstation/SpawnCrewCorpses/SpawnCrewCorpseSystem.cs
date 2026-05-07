using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Cloning;
using Content.Server.Humanoid;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.SpawnCrewCorpses;

/// <summary>
/// Generic _Impstation helper for spawning fake crew corpses.
/// Optionally copies appearance from connected crew entities.
/// Requires a <see cref="SpawnCrewCorpseComponent"/> for configuration.
/// </summary>
public sealed class SpawnCrewCorpseSystem : EntitySystem
{
    private static readonly ProtoId<CloningSettingsPrototype> SpeciesCloneSettings = "ChangelingCloningSettings";

    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

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

        List<EntityUid>? crewSources = null;
        if (settings.CloneAppearance)
        {
            crewSources = BuildCrewSourcePool();
            if (settings.DistinctCrewPerBatch)
                _random.Shuffle(crewSources);
        }

        for (var i = 0; i < count; i++)
        {
            var corpse = Spawn(settings.CorpsePrototype, coords[i]);
            _metaData.SetEntityName(corpse, settings.CorpseName);

            if (settings.CloneAppearance && crewSources is { Count: > 0 })
            {
                var source = settings.DistinctCrewPerBatch
                    ? crewSources[i % crewSources.Count]
                    : _random.Pick(crewSources);

                _humanoid.CloneAppearance(source, corpse);
                // Clone species-dependent body components to keep visual/examine state in sync.
                _cloning.CloneComponents(source, corpse, SpeciesCloneSettings);
                CopyBloodReferenceSolution(source, corpse);
                // CloneAppearance updates humanoid visuals, but damage overlays cache thresholds/layers.
                // Force one recompute so species-specific damage sprites match the copied appearance.
                _appearance.SetData(corpse, DamageVisualizerKeys.ForceUpdate, true);
                // Re-apply name after CloneAppearance since it may copy the source name
                _metaData.SetEntityName(corpse, settings.CorpseName);
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

    private void CopyBloodReferenceSolution(EntityUid source, EntityUid corpse)
    {
        if (!HasComp<BloodstreamComponent>(source)
            || !HasComp<BloodstreamComponent>(corpse))
            return;

        Entity<SolutionComponent>? sourceBlood = null;
        if (!_solutionContainer.ResolveSolution(source, BloodstreamComponent.DefaultBloodSolutionName, ref sourceBlood, out var sourceBloodSolution))
            return;

        _bloodstream.ChangeBloodReagents(corpse, sourceBloodSolution.Clone());
    }
}
