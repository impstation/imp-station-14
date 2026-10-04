using Content.Shared._Impstation.Genetics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.Effects.Neutral;

/// <summary>
/// Causes the entity that the Gene is attached to to glow.
/// </summary>
public sealed partial class GlowEffect : BaseGeneEffect
{
    private SharedPointLightSystem _lightSystem;

    [DataField("lightShader")]
    public string ShaderProto = "GeneticsGlowOutline";

    /// <summary>
    /// The settings for the light that gets added to the entity
    /// </summary>
    [DataField("lightProfile")]
    public LightProfile Light;

    /// <summary>
    /// If there was already a point light attached, this stores the old settings
    /// Good for if you have a species that glows that you don't want to lose the glow when they
    /// lose the associated genes
    /// </summary>
    public LightProfile Previous;

    /// <summary>
    /// Applies the light to the given entity
    /// If there are already Genes adding lights to an entity, this will instead modify existing lights
    /// to blend the effects
    /// </summary>
    /// <param name="entity">The entity the Gene is attached to</param>
    /// <param name="chromosomes">The Chromosomes the Gene possesses</param>
    public override void ApplyGeneEffect(Entity<SharedGeneHostComponent> entity, Dictionary<Chromosome, bool> chromosomes)
    {
        base.ApplyGeneEffect(entity, chromosomes);
        _lightSystem ??= _entityManager.System<SharedPointLightSystem>();

        var light = _lightSystem.EnsureLight(entity.Owner);

        Previous = new LightProfile
        (
            light.Energy,
            light.Color,
            light.Radius
        );

        _lightSystem.SetEnergy(entity.Owner, Light.LightEnergy);
        _lightSystem.SetColor(entity.Owner, Light.LightColor);
        _lightSystem.SetRadius(entity.Owner, Light.LightEnergy);
    }
}

[DataDefinition, NetSerializable, Serializable]
public partial struct LightProfile
{
    [DataField]
    public float LightEnergy = 1;
    [DataField]
    public Color LightColor = Color.White;
    [DataField]
    public float Radius = 5;

    public LightProfile(float energy, Color color, float radius)
    {
        LightEnergy = energy;
        LightColor = color;
        Radius = radius;
    }
}
