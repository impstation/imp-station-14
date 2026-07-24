using Robust.Shared.Prototypes;
using Content.Shared._Impstation.Slasher.Components;

namespace Content.Shared._Impstation.Slasher.Prototypes;

/// <summary>
/// Defines the weighted probability table for Slasher effigy pulse effects.
/// Multiple variants can be created for different difficulty levels or custom configurations.
/// </summary>
[Prototype]
public sealed partial class PulseWeightsPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// List of available pulse effects and their relative weights for random selection.
    /// Each entry defines a pulse game rule proto ID and how often it should be chosen.
    /// Set weight to 0 to disable a pulse effect.
    /// </summary>
    [DataField(required: true)]
    public List<PulseWeightEntry> Weights { get; private set; } = new();
}
