using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles <see cref="ChemicalPuddleArtifactComponent"/>
/// </summary>
public sealed class ChemicalPuddleArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    /// <summary>
    /// The key for the node data entry containing
    /// the chemicals that the puddle is made of.
    /// </summary>
    public const string NodeDataChemicalList = "nodeDataChemicalList";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChemicalPuddleArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(Entity<ChemicalPuddleArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        if (!TryComp<ArtifactComponent>(ent, out var artifact))
            return;

        if (!_artifact.TryGetNodeData(ent, NodeDataChemicalList, out List<string>? chemicalList, artifact))
        {
            chemicalList = new();
            for (var i = 0; i < ent.Comp.ChemAmount; i++)
            {
                var chemProto = _random.Pick(ent.Comp.PossibleChemicals);
                chemicalList.Add(chemProto);
            }

            _artifact.SetNodeData(ent, NodeDataChemicalList, chemicalList, artifact);
        }

        var amountPerChem = ent.Comp.ChemicalSolution.MaxVolume / ent.Comp.ChemAmount;
        foreach (var reagent in chemicalList)
        {
            ent.Comp.ChemicalSolution.AddReagent(reagent, amountPerChem);
        }

        _puddle.TrySpillAt(ent, ent.Comp.ChemicalSolution, out _);
    }
}