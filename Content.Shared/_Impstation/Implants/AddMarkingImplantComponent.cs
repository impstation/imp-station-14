using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Implants;

/// <summary>
/// Add markings to the recepient of this implant.
/// </summary>
[RegisterComponent]
public sealed partial class AddMarkingImplantComponent : Component
{
    /// <summary>
    /// The marking prototypes to be added.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<MarkingPrototype>> Markings;

    /// <summary>
    /// Whether to use the hair color for the added markings.
    /// </summary>
    [DataField]
    public bool UseHairColor;
}
