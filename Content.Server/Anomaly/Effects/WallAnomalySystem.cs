using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;

namespace Content.Server.Anomaly.Effects;

public sealed class WallAnomalySystem : SharedWallAnomalySystem
{
    [Dependency] private readonly SharedAnomalySystem _anomaly = default!;

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

    }

    private void OnSupercritical(Entity<WallSpawnAnomalyComponent> component, ref AnomalySupercriticalEvent args)
    {

    }

    private void OnStabilityChanged(Entity<WallSpawnAnomalyComponent> component, ref AnomalyStabilityChangedEvent args)
    {

    }

    private void OnSeverityChanged(Entity<WallSpawnAnomalyComponent> component, ref AnomalySeverityChangedEvent args)
    {

    }

    private void OnShutdown(Entity<WallSpawnAnomalyComponent> component, ref AnomalyShutdownEvent args)
    {

    }

}