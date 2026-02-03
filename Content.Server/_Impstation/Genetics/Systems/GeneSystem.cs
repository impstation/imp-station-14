using Content.Server._Impstation.Genetics.Components;
using Content.Server.Database.Migrations.Postgres;
using Content.Shared._Impstation.Genetics.Components;
using Content.Shared._Impstation.Genetics.Genes;
using Content.Shared._Impstation.Genetics.Systems;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
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
    //[Dependency] private readonly ISawmill _sawmill = default!;

    private int _geneTiers = 5;

    /// <summary>
    /// The table holding all of the possible Genes
    ///     The Tier 
    /// </summary>
    WeightedRandomPrototype _geneTierTable = default!;
    private Dictionary<string, WeightedRandomEntityPrototype> _geneTable = new();

    public override void Initialize()
    {
        base.Initialize();
        LoadGeneRegistry();
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
    }

    public void AddGene(EntityUid entity, string gene)
    {
        var geneProto = _prototypeManager.Index<BaseGenePrototype>(gene);

        if (geneProto == null)
            return;

        if (!_entityManager.TryGetComponent<GeneHostComponent>(entity, out var geneComp))
            return;

        geneProto.OnGeneAdded(entity);
        geneComp._geneScaleValue += geneProto._geneStabilityValue;
        geneComp._genes.Add(gene, geneProto);
    }

    public void AddGeneRandom(EntityUid entity)
    {

    }

    public struct GeneRegistration
    {
        public WeightedRandomEntityPrototype GeneTable;
    };
}
