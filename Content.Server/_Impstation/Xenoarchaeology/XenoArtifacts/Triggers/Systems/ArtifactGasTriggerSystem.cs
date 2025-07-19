using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactGasTriggerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactGasTriggerComponent, ArtifactNodeEnteredEvent>(OnRandomizeTrigger);
    }

    private void OnRandomizeTrigger(Entity<ArtifactGasTriggerComponent> ent, ref ArtifactNodeEnteredEvent args)
    {
        if (ent.Comp.ActivationGas != null)
            return;

        var gas = ent.Comp.PossibleGases[args.RandomSeed % ent.Comp.PossibleGases.Count];
        ent.Comp.ActivationGas = gas;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<Entity<ArtifactComponent>> toUpdate = new();
        var query = EntityQueryEnumerator<ArtifactGasTriggerComponent, ArtifactComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var artifact, out var transform))
        {
            if (trigger.ActivationGas == null)
                continue;

            var environment = _atmosphereSystem.GetTileMixture((uid, transform));
            if (environment == null)
                continue;

            // check if outside there is enough moles to activate artifact
            var moles = environment.GetMoles(trigger.ActivationGas.Value);
            if (moles < trigger.ActivationMoles)
                continue;

            toUpdate.Add((uid, artifact));
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a, null, a);
        }
    }
}