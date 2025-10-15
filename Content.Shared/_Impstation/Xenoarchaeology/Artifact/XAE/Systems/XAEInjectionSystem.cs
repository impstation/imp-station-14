using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Systems;

public sealed class XAEInjectionSystem : BaseXAESystem<XAEInjectionComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<InjectableSolutionComponent> _injectableQuery;
    private readonly HashSet<EntityUid> _entitiesInRange = new();

    protected override void OnActivated(Entity<XAEInjectionComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _entitiesInRange.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Range, _entitiesInRange);

        foreach (var chem in ent.Comp.Entries)
        {
            ent.Comp.ChemicalSolution.AddReagent(chem.Chemical, chem.Amount);
        }

        foreach (var entityInRange in _entitiesInRange)
        {
            if (!HasComp<InjectableSolutionComponent>(entityInRange))
                continue;

            if (!_solutionContainer.TryGetInjectableSolution(entityInRange, out var injectable, out _))
                continue;

            //inject
            _solutionContainer.AddSolution(injectable.Value, ent.Comp.ChemicalSolution);

            //Spawn Effect
            if (ent.Comp.ShowEffect)
            {
                var uidXform = Transform(entityInRange);
                Spawn(ent.Comp.VisualEffectPrototype, uidXform.Coordinates);
            }
        }
    }
}
