using Robust.Shared.Map.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared._Impstation.Anomaly.Effects;
using Content.Shared._Impstation.Anomaly.Effects.Components;
using Content.Shared.Tag;

namespace Content.Server._Impstation.Anomaly.Effects;

// this is all very hacky, yes.
public sealed class WallAnomalySystem : SharedWallAnomalySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<WallSpawnAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<WallSpawnAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<WallSpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
        SubscribeLocalEvent<WallSpawnAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<WallSpawnAnomalyComponent, AnomalyShutdownEvent>(OnShutdown);
    }

    private void OnPulse(Entity<WallSpawnAnomalyComponent> component, ref AnomalyPulseEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnPulse)
                continue;

            ConvertWalls(component, entry, args.Stability, args.Severity, args.PowerModifier);
        }
    }

    private void OnSupercritical(Entity<WallSpawnAnomalyComponent> component, ref AnomalySupercriticalEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnSuperCritical)
                continue;

            ConvertWalls(component, entry, 1, 1, args.PowerModifier);
        }
    }

    private void OnStabilityChanged(Entity<WallSpawnAnomalyComponent> component, ref AnomalyStabilityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnStabilityChanged)
                continue;

            ConvertWalls(component, entry, args.Stability, args.Severity, 1);
        }
    }

    private void OnSeverityChanged(Entity<WallSpawnAnomalyComponent> component, ref AnomalySeverityChangedEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnSeverityChanged)
                continue;

            ConvertWalls(component, entry, args.Stability, args.Severity, 1);
        }
    }

    private void OnShutdown(Entity<WallSpawnAnomalyComponent> component, ref AnomalyShutdownEvent args)
    {
        foreach (var entry in component.Comp.Entries)
        {
            if (!entry.Settings.SpawnOnShutdown || args.Supercritical)
                continue;

            ConvertWalls(component, entry, 1, 1, 1);
        }
    }

    // VERY watered down cosmiccult code. because cosmiccorrupting had a bunch of stuff hardcoded in and we just want walls
    private void ConvertWalls(Entity<WallSpawnAnomalyComponent> uid, WallSpawnSettingsEntry entry, float stability, float severity, float powerMod)
    {
        var tgtPos = Transform(uid);
        if (tgtPos.GridUid is not { } gridUid || !TryComp(gridUid, out MapGridComponent? mapGrid))
            return;

        var amountSpawned = (int)(MathHelper.Lerp(entry.Settings.MinAmount, entry.Settings.MaxAmount, severity * stability * powerMod) + 0.5f);
        var radius = entry.Settings.MaxRange;
        var entityHash = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, radius);
        int spawnedCount = 0;
        foreach (var entity in entityHash)
        {
            if (spawnedCount >= amountSpawned)
                break;

            if (TryComp<TagComponent>(entity, out var tag))
            {
                var tags = tag.Tags;
                if (tags.Contains("Wall") && Prototype(entity) != null && Prototype(entity)!.ID != entry.Wall)
                {
                    Spawn(entry.Wall, Transform(entity).Coordinates);
                    QueueDel(entity);
                    spawnedCount++;
                }
            }
        }
    }
}
