using Content.Shared.Chemistry.Reagent;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Fluids.Components;

/// <summary>
/// This is used for an entity to "extract" reagents from food.
/// </summary>
[RegisterComponent, Access(typeof(FoodReagentExtractorSystem))]
public sealed partial class FoodReagentExtractorComponent : Component
{
    /// <summary>
    /// The name of the solution to add to.
    /// </summary>
    [DataField("solution", required: true)]
    public string SolutionName = string.Empty;

    /// <summary>
    /// The reagent that food is converted into
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> ExtractedReagent = "Nutriment";

    /// <summary>
    /// List of reagents that determines how much is yielded.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> ExtractionReagents = new()
    {
        "Nutriment"
    };

    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/Effects/waterswirl.ogg");
}
