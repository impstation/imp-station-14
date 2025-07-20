using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Atmos;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class GasArtifactSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
        SubscribeLocalEvent<GasArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnNodeEntered(Entity<GasArtifactComponent> ent, ref ArtifactNodeEnteredEvent args)
    {
        if (ent.Comp.SpawnGas == null && ent.Comp.PossibleGases.Count != 0)
        {
            var gas = ent.Comp.PossibleGases[args.RandomSeed % ent.Comp.PossibleGases.Count];
            ent.Comp.SpawnGas = gas;
        }

        if (ent.Comp.SpawnTemperature == null)
        {
            var temp = args.RandomSeed % ent.Comp.MaxRandomTemperature - ent.Comp.MinRandomTemperature +
                       ent.Comp.MinRandomTemperature;
            ent.Comp.SpawnTemperature = temp;
        }
    }

    private void OnActivate(Entity<GasArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        if (ent.Comp.SpawnGas == null || ent.Comp.SpawnTemperature == null)
            return;

        var environment = _atmosphereSystem.GetContainingMixture(ent.Owner, false, true);
        if (environment == null)
            return;

        if (environment.Pressure >= ent.Comp.MaxExternalPressure)
            return;

        var merger = new GasMixture(1) { Temperature = ent.Comp.SpawnTemperature.Value };
        merger.SetMoles(ent.Comp.SpawnGas.Value, ent.Comp.SpawnAmount);

        _atmosphereSystem.Merge(environment, merger);
    }
}