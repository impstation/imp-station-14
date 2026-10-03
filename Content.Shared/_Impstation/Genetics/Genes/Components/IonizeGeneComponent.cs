using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.Components;


[RegisterComponent, Virtual]
public partial class IonizeGeneComponent : BaseSetGeneComponent
{
    /// <summary>
    /// The settings for the light that gets added to the entity
    /// </summary>
    [DataField("lightProfile")]
    public LightProfile Light;

    /// <summary>
    /// If you are immune to radiation from this gene
    /// </summary>
    [DataField("selfImmune")]
    public bool RadImmune = true;

    /// <summary>
    /// How strong the radiation this gene emits is
    /// </summary>
    [DataField("radStrength")]
    public int RadStrength = 1;

    /// <summary>
    /// If there was already a point light attached, this stores the old settings
    /// </summary>
    public LightProfile Previous;

    /// <summary>
    /// If the entity was already immune to radiation before we got here
    /// </summary>
    public bool PrevImmune = false;
}

[DataDefinition]
public partial struct LightProfile
{
    public float LightEnergy = 1;
    public Color LightColor = Color.White;
    public float Radius = 5;

    public LightProfile(float energy, Color color, float radius)
    {
        LightEnergy = energy;
        LightColor = color;
        Radius = radius;
    }
}
