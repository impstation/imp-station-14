using Content.Server._Impstation.Genetics.Components;
using Content.Server.Database.Migrations.Postgres;
using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Prototypes;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
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
    //[Dependency] private readonly ISawmill _sawmill = default!;

    private int _geneTiers = 5;

    /// <summary>
    /// The table holding all of the possible Genes
    ///     The Tier 
    /// </summary>
    WeightedRandomPrototype _geneTierTable = default!;
    private Dictionary<string, WeightedRandomEntityPrototype> _geneTable = new();

    private Dictionary<string, ComponentRegistry> _registeredGenes = new();

    private Dictionary<string, BaseGeneEntitySystem> _geneSystems = new();

    public override void Initialize()
    {
        base.Initialize();
        LoadGeneRegistry();

        var systems = _entityManager.EntitySysManager.GetEntitySystemTypes().GetEnumerator();
        while(systems.MoveNext())
        {
            if(systems.Current.BaseType == typeof(BaseGeneEntitySystem))
            {
                var system = (BaseGeneEntitySystem)_entityManager.EntitySysManager.GetEntitySystem(systems.Current);
                _geneSystems.Add(system.GetType().Name, system);
            }
        }
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
            foreach(var (entryName, entry) in proto.Value._geneComponent)
            {
                var newGene = _componentFactory.GetRegistration(entryName);

                var comp = _componentFactory.GetComponent(newGene);
                _serializationManager.CopyTo(entry.Component, ref comp, notNullableOverride: true);
            }
        }
    }

    public void AddGene(EntityUid entity, string gene)
    {
        if (!_entityManager.TryGetComponent<GeneHostComponent>(entity, out var geneComp))
            return;

        if (!_registeredGenes.TryGetValue(gene, out var geneEntry))
            return;

        var newGene = _componentFactory.GetRegistration(geneEntry.Keys.ElementAt(0));

        var comp = _componentFactory.GetComponent(newGene);
        _serializationManager.CopyTo(geneEntry.Values.ElementAt(0).Component, ref comp, notNullableOverride: true);
        _entityManager.AddComponent(entity, comp);

        geneComp._genes.Add(gene, comp);

        var baseGene = (BaseGeneComponent)comp;

        geneComp._geneScaleValue += baseGene._geneStabilityValue;

        if(baseGene._linkedSystem != null)
            _geneSystems[baseGene._linkedSystem].OnGeneAdded((entity, baseGene));
    }

    public void AddGeneRandom(EntityUid entity)
    {

    }

    public struct GeneRegistration
    {
        public WeightedRandomEntityPrototype GeneTable;
    };
}
