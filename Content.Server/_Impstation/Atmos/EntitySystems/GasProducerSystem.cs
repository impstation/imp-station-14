using System.Diagnostics.CodeAnalysis;
using Content.Server._Impstation.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.Atmos.EntitySystems;

public sealed class GasProducerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasProducerComponent, AtmosDeviceUpdateEvent>(OnProducerUpdated);
    }

    private void OnProducerUpdated(Entity<GasProducerComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        float toSpawn;
        var producer = ent.Comp;
        if (!producer.Enabled)
            return;

        if (!GetValidEnvironment(ent, out var environment)) // imp minewhenunanchored
            return;

        if ((toSpawn = CapSpawnAmount(ent, producer.SpawnAmount * args.dt, environment)) == 0)
            return;
        var merger = new GasMixture(1) { Temperature = producer.SpawnTemperature };
        merger.SetMoles(producer.SpawnGas, toSpawn);
        _atmosphereSystem.Merge(environment, merger);
    }
    private bool GetValidEnvironment(Entity<GasProducerComponent> ent, [NotNullWhen(true)] out GasMixture? environment)
    {
        var (uid, miner) = ent;
        var transform = Transform(uid);
        var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);

        // Treat space as an invalid environment
        if (_atmosphereSystem.IsTileSpace(transform.GridUid, transform.MapUid, position))
        {
            environment = null;
            return false;
        }

        environment = _atmosphereSystem.GetContainingMixture((uid, transform), true, true);
        return environment != null;
    }

    private float CapSpawnAmount(Entity<GasProducerComponent> ent, float toSpawnTarget, GasMixture environment)
    {
        var (uid, producer) = ent;

        // How many moles could we theoretically spawn. Cap by pressure and amount.
        var allowableMoles = Math.Min(
            (producer.MaxExternalPressure - environment.Pressure) * environment.Volume / (producer.SpawnTemperature * Atmospherics.R),
            producer.MaxExternalAmount - environment.TotalMoles);

        var toSpawnReal = Math.Clamp(allowableMoles, 0f, toSpawnTarget);

        if (toSpawnReal < Atmospherics.GasMinMoles) {
            return 0f;
        }

        return toSpawnReal;
    }
}
