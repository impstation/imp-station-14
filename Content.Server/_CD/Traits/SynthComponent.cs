using Content.Shared.Chemistry.Components;
namespace Content.Server._CD.Traits;

/// <summary>
/// Set players' blood to coolant, and is used to notify them of ion storms
/// </summary>
[RegisterComponent, Access(typeof(SynthSystem))]
public sealed partial class SynthComponent : Component
{
    /// <summary>
    /// The chance that the synth is alerted of an ion storm
    /// </summary>
    [DataField]
    public float AlertChance = 0.3f;

    /// <summary>
    /// VDS - The reagent that replaces the synth's blood
    /// </summary>
    [DataField]
    public Solution SynthBloodReagent = new([new("SynthBlood", 300)]);
}
