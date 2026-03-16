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

    private int _geneTiers = 5;

    /// <summary>
    /// The table holding all of the possible Genes
    /// </summary>
    private Dictionary<string, ComponentRegistry> _registeredGenes = new();

    WeightedRandomPrototype _geneTierTable = default!;
    private Dictionary<string, WeightedRandomEntityPrototype> _geneTable = new();


    public override void Initialize()
    {
        base.Initialize();
        LoadGeneRegistry();

        SubscribeLocalEvent<GeneHostComponent, OnIrradiatedEvent>(Irradiated);
    }

    /// <summary>
    /// Load our Gene's from their YML's to the associated Tier
    /// </summary>
    public void LoadGeneRegistry()
    {
        var geneTiers = "GeneTiers";
        _geneTierTable = _prototypeManager.Index<WeightedRandomPrototype>(geneTiers);

        var name = "GeneTier";
        for (int i = 1; i < _geneTiers + 1; i++)
        {
            var tier = name + i;
            _geneTable.Add(tier, _prototypeManager.Index<WeightedRandomEntityPrototype>(tier));
        }

        var geneTierSpecial = "GeneTierSpecial";
        _geneTable.Add(geneTierSpecial, _prototypeManager.Index<WeightedRandomEntityPrototype>(geneTierSpecial));

        var protos = _prototypeManager.GetInstances<GenePrototype>();

        foreach (var proto in protos) {

            _registeredGenes.Add(proto.Key, proto.Value._geneComponent);
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

        // Oh boy Ok
        // What we're doing here is grabbing the Gene Component directly from the Component Factory
        // This is because this is how we go from a string, to a Component Type
        // We then add that Component onto the Entity and also store it in the GeneHostComponent
        // We mostly store it in there for ease of access but as i write this comment i question the point
        // I guess it can help admins a little
        var newGene = _componentFactory.GetRegistration(geneEntry.Keys.ElementAt(0));

        var comp = _componentFactory.GetComponent(newGene);
        _serializationManager.CopyTo(geneEntry.Values.ElementAt(0).Component, ref comp, notNullableOverride: true);
        _entityManager.AddComponent(entity, comp);

        geneComp._genes.Add(gene, comp);

        var baseGene = (BaseGeneComponent)comp;

        // Modify the entities Gene scale
        geneComp._geneScaleValue += baseGene._geneStabilityValue;

        // Throw our event for all systems to use
        // They will need this to apply their effects and set themselves up
        var performed = new GeneAddedEvent(entity);
        RaiseLocalEvent(entity, ref performed);
    }

    /// <summary>
    /// Every time an entity takes radiation damage, roll to see if they mutate based on the amount of damage
    /// and their innate mutation chance
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    public void Irradiated(Entity<GeneHostComponent> entity, ref OnIrradiatedEvent args)
    {
        if (!_entityManager.TryGetComponent<DamageableComponent>(entity, out var damage))
            return;

        var mutateOdds = entity.Comp._mutateChance * (damage.Damage.DamageDict["Radiation"].Value / 100);

        if (_random.NextFloat(0, 100) < mutateOdds)
            AddGeneRandom(entity);
    }

    /// <summary>
    /// Just picks a random Gene from all the Gene tables and adds it to an Entity
    /// </summary>
    /// <param name="entity"></param>
    public void AddGeneRandom(EntityUid entity)
    {
        var tier = _geneTierTable.Pick(_random);

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
