using Content.Server._Impstation.Genetics.Components;
using Content.Server.Database.Migrations.Postgres;
using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Events;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Prototypes;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Radiation.Events;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Robust.Shared.Prototypes.EntityPrototype;

namespace Content.Server._Impstation.Genetics.Systems;

/// <summary>
/// The System that handles application, removal, oversight and more of the Genetics system
///     This functions by handing out Genes that are given to it as Prototype's by loading in YML.
///     The system will then hand out copies of these Genes to components for their own use
///     It will also handle the updating and tracking of Genes if necessary
/// </summary>
public sealed partial class GeneSystem : SharedGeneSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private int GeneTiers = 5;
    private string _tiersProtoName = "GeneTiers";

    /// <summary>
    /// The table holding all of the possible Genes
    /// </summary>
    private Dictionary<string, ComponentRegistryEntry> _registeredGenes = new();

    WeightedRandomPrototype GeneTierTable = default!;
    private Dictionary<string, WeightedRandomEntityPrototype> _geneTable = new();


    public override void Initialize()
    {
        base.Initialize();
        LoadGeneRegistry();

        SubscribeLocalEvent<GeneHostComponent, OnIrradiatedEvent>(Irradiated);
    }

    /// <summary>
    /// Load each of our Gene tier groups
    /// Each group contains the proto names of all of our genes, so each tier will
    /// be in charge of loading all the individual genes they contain.
    /// If a gene shares multiple tiers then it will just get skipped.
    /// </summary>
    public void LoadGeneRegistry()
    {
        GeneTierTable = _prototypeManager.Index<WeightedRandomPrototype>(_tiersProtoName);

        foreach(KeyValuePair<string, float> table in GeneTierTable.Weights)
        {
            var tier = _prototypeManager.Index<WeightedRandomEntityPrototype>(table.Key);
            _geneTable.Add(table.Key, tier);

            foreach(KeyValuePair<string, float> gene in tier.Weights)
            {
                if (_registeredGenes.ContainsKey(gene.Key))
                    continue;

                var geneProto = _prototypeManager.Index(gene.Key);

                if (geneProto == null)
                    throw new Exception("Gene could not be found. Is the name mispelled?");

                _registeredGenes.Add(gene.Key, geneProto.Components[gene.Key]);
            }
        }
    }

    /// <summary>
    ///     The base function for adding a Gene to an entity
    ///     This function will grab the Gene from a list of registered Genes and then apply it as
    ///     a component while also adding it to that Entity's GeneHostComponent
    /// </summary>
    /// <param name="entity">What we're applying the Gene to</param>
    /// <param name="gene">The name of the gene as stored in our _registeredGenes</param>
    /// <remark>
    ///     TODO: Make this actually good
    /// </remark>
    public void AddGene(EntityUid entity, string gene)
    {
        if (!_entityManager.TryGetComponent<GeneHostComponent>(entity, out var geneComp))
            return;

        if (!_registeredGenes.TryGetValue(gene, out var geneEntry))
            return;

        if (CheckForGene((entity, geneComp), gene))
            return;

        // Grab our Gene copy from ComponentFactory and then copy all the relevant data from our
        // genes entry
        var comp = _componentFactory.GetComponent(geneEntry);
        _serializationManager.CopyTo(geneEntry.Component, ref comp, notNullableOverride: true);
        _entityManager.AddComponent(entity, comp);

        // Adds to the GeneticsHostComponent for ease of tracking in game what genes someone has
        geneComp._genes.Add(gene, comp);

        var baseGene = (BaseGeneComponent)comp;

        baseGene.GeneName = gene;

        // Modify the entities Gene scale
        geneComp._geneScaleValue += baseGene.GeneStabilityValue;

        // Throw our event for all systems to use
        // They will need this to apply their effects and set themselves up
        var performed = new GeneAddedEvent(entity, gene);
        RaiseLocalEvent(entity, ref performed);
    }

    /// <summary>
    /// Just picks a random Gene from all the Gene tables and adds it to an Entity
    /// </summary>
    /// <param name="entity"></param>
    public void AddGeneRandom(EntityUid entity)
    {
        var tier = GeneTierTable.Pick(_random);

        var gene = _geneTable[tier].Pick(_random);

        AddGene(entity, gene);
    }

    /// <summary>
    /// Checks if an Entity has a particular Gene
    /// </summary>
    /// <param name="entity">The gene we're checking</param>
    /// <param name="gene">The name of the gene as stored in our _registeredGenes</param>
    /// <returns></returns>
    public bool CheckForGene(Entity<GeneHostComponent> entity, string gene)
    {
        return entity.Comp._genes.TryGetValue(gene, out var comp) ? true : false;
    }
}
