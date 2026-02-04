using Content.Shared._Impstation.Genetics.Systems;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

/// <summary>
/// The Base of all Genes for the Genetics system
/// </summary>
[ImplicitDataDefinitionForInheritors, RegisterComponent, Access(typeof(SharedGeneSystem))]
[Virtual]
public partial class BaseGeneComponent : Component
{

    [DataField("compName")]
    public string _compName;

    /// <summary>
    /// The value added to a Mobs Gene Stability
    /// </summary>
    [DataField("stability")]
    public int _geneStabilityValue = 0;

    /// <summary>
    /// Used for determining how much of a Gene's strain should be hidden at first
    /// </summary>
    [DataField("complexity")]
    public Vector2i _geneComplexity = (0, 5);

    /// <summary>
    /// Used for determining how complex a Gene should be
    /// Tier 1 being the simplest and Tier 6 being the max
    /// Tier Special is to be reserved for Genes that can only be obtained in very special circumstances
    /// and thus should be the most complex to reflect that
    /// </summary>
    [DataField("tier")]
    public GeneTier _geneTier = GeneTier.Tier1;

    /// <summary>
    /// The group this specific Gene belongs to.
    /// </summary>
    [DataField("group")]
    public GeneGroup _geneGroup = GeneGroup.None;

    /// <summary>
    /// A list of Gene Groups that this Gene cannot be added alongside
    /// </summary>
    [DataField("groupBlacklist")]
    public List<GeneGroup> _geneGroupBlacklist = new List<GeneGroup>();

    /// <summary>
    /// If this Gene has been discovered by the Geneticists
    /// </summary>
    public static bool _discovered = false;

    /// <summary>
    /// The correct strain of the Gene & the Scrambled strain of the Gene
    /// </summary>
    public List<GeneData> _geneStrain = new List<GeneData>();
    public List<GeneData> _geneStrainScrambled = new List<GeneData>();

    /// <summary>
    /// The Entity System associated with this Gene
    /// If one is set then it will be used to handle the functions that happen when
    /// a gene is applied and removed
    /// </summary>
    [DataField("system"), ViewVariables(VVAccess.ReadOnly)]
    public string _linkedSystem;

    /// <summary>
    /// Our active Chromosomes
    /// These don't really need to be more advanced than this. It's up to the Genetek console to
    /// toggle them on and off
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<Chromosome, bool> _activeChromosomes = new Dictionary<Chromosome, bool>
    {
        { Chromosome.Camouflager, false },
        { Chromosome.Reinforcer, false },
        { Chromosome.Synchronizer, false },
        { Chromosome.EnergyBooster, false },
        { Chromosome.PowerBooster, false }
    };

    /// <summary>
    /// How common the Gene is in its respective Tier
    /// </summary>
    [DataField("weight")]
    public float _weight = 1;

    /// <summary>
    /// The person this Gene is currently stuck to
    /// Mostly used by Genes that use events or need to constantly access their master
    /// </summary>
    protected EntityUid _host;

    /// <summary>
    /// Called when the Gene System initialises
    /// </summary>
    public virtual void OnGeneInitialise()
    {

    }

    /// <summary>
    /// Called when a Gene is added to a Mob
    /// </summary>
    public virtual void OnGeneAdded(EntityUid host)
    {
        _host = host;
    }

    /// <summary>
    /// Called when a Gene is removed from a Mob
    /// </summary>
    public virtual void OnGeneRemoved()
    {

    }

    /// <summary>
    /// Detects if a Gene is on an Entity
    /// </summary>
    /// <returns></returns>
    public virtual bool DoesEntityHaveGene()
    {
        return false;
    }

    /// <summary>
    /// Checks if two specific bits of Gene strain data match
    /// </summary>
    /// <param name="gene"></param>
    /// <returns></returns>
    public virtual bool DoesGeneDataMatch(int gene)
    {
        if (_geneStrain.Count <= gene || _geneStrainScrambled.Count <= gene)
            return true;

        return _geneStrain[gene] == _geneStrainScrambled[gene];
    }

    /// <summary>
    /// Check if the Gene Strain and Scrambled Gene Strain match
    /// </summary>
    /// <returns></returns>
    public virtual bool DoesGeneStrainMatch()
    {
        return false;
    }

    public virtual bool IsGeneDiscovered()
    {
        return _discovered;
    }

    public virtual void SetGeneDiscovered(bool discovered = true)
    {
        _discovered = discovered;
    }

    public enum GeneData
    {

    }

    public enum GeneTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
        TierSpecial = 6,
        TierAdmin = 7,
    }

    public enum GeneGroup
    {
        None,
        Language,
        Thermal
    }
}

public enum Chromosome
{
    Synchronizer,
    Reinforcer,
    PowerBooster,
    EnergyBooster,
    Camouflager
}
