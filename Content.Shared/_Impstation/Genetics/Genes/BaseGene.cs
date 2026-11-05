using Content.Shared._Impstation.Genetics.Genes.Effects;
using Content.Shared._Impstation.Genetics.Systems;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

/// <summary>
/// The Base of all Genes for the Genetics system
/// </summary>
[ImplicitDataDefinitionForInheritors, RegisterComponent]
[Virtual]
public abstract partial class BaseGeneComponent : Component
{
    /// <summary>
    /// The value added to a Mobs Gene Stability
    /// </summary>
    [DataField("stability")]
    public int GeneStabilityValue = 0;

    /// <summary>
    /// Used for determining how much of a Gene's strain should be hidden at first
    /// </summary>
    [DataField("complexity")]
    public Vector2i GeneComplexity = (0, 5);

    /// <summary>
    /// Used for determining how complex a Gene should be
    /// Tier 1 being the simplest and Tier 6 being the max
    /// Tier Special is to be reserved for Genes that can only be obtained in very special circumstances
    /// and thus should be the most complex to reflect that
    /// </summary>
    [DataField("tier")]
    public GeneTiers GeneTier = GeneTiers.Tier1;

    /// <summary>
    /// The group this specific Gene belongs to.
    /// </summary>
    [DataField("group")]
    public GeneGroups GeneGroup = GeneGroups.None;

    /// <summary>
    /// A list of Gene Groups that this Gene cannot be added alongside
    /// </summary>
    [DataField("groupBlacklist")]
    public List<GeneGroups> GeneGroupBlacklist = new List<GeneGroups>();

    /// <summary>
    /// The Positive effects that the Gene will add
    /// </summary>
    [DataField("positiveEffects")]
    public HashSet<BaseGeneEffect> PositiveEffects = new HashSet<BaseGeneEffect>();

    /// <summary>
    /// The Neutral effects that the Gene will add
    /// </summary>
    [DataField("neutralEffects")]
    public HashSet<BaseGeneEffect> NeutralEffects = new HashSet<BaseGeneEffect>();

    /// <summary>
    /// The negative effect this Gene will apply on use
    /// </summary>
    [DataField("negativeEffect")]
    public BaseGeneEffect? NegativeEffect;

    /// <summary>
    /// If this Gene is active and applying its affect
    /// </summary>
    public static bool Active = false;

    /// <summary>
    /// The correct strain of the Gene & the Scrambled strain of the Gene
    /// </summary>
    public List<GeneData> GeneStrain = new List<GeneData>();
    public List<GeneData> GeneStrainScrambled = new List<GeneData>();

    /// <summary>
    /// Our active Chromosomes
    /// These don't really need to be more advanced than this. It's up to the Genetek console to
    /// toggle them on and off
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<Chromosome, bool> ActiveChromosomes = new Dictionary<Chromosome, bool>
    {
        { Chromosome.Camouflager, false },
        { Chromosome.Reinforcer, false },
        { Chromosome.Synchronizer, false },
        { Chromosome.EnergyBooster, false },
        { Chromosome.PowerBooster, false }
    };

    /// <summary>
    /// The person this Gene is currently stuck to
    /// Mostly used by Genes that use events or need to constantly access their master
    /// </summary>
    protected EntityUid Host;

    /// <summary>
    /// The name of our Gene. Gets set in GeneSystem.cs
    /// </summary>
    public string GeneName = ""; 

    public enum GeneData
    {

    }

    public enum GeneTiers
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
        TierSpecial = 6,
        TierAdmin = 7,
    }

    public enum GeneGroups
    {
        None,
        Language,
        Thermal
    }
}

public enum Chromosome
{
    // Removes a Genes negative effect
    Synchronizer,
    // Makes a Gene immune to Mutadone
    Reinforcer,
    // Strengthens both a Genes POSITIVE and NEGATIVE effects
    PowerBooster,
    // Reduces cooldown on Action Genes
    EnergyBooster,
    // Hides the Gene addition message
    Camouflager
}
