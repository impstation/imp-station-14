using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.AnimalHusbandry.Prototypes;

/// <summary>
/// Stores the info for a mobs breeding information
/// including compatible partners, offspring, food settings and more
/// </summary>
[Prototype]
public sealed partial class BreedSettingsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The compatible mobs that this can breed with
    /// </summary>
    [DataField("compatibleBreeds")]
    public List<EntProtoId> CompatibleBreeds = default!;

    /// <summary>
    /// Format the chosen animals like this within your YAML.
    /// This variable allows animals to have multiple offspring
    ///     possibleInfants: !type:GroupSelector
    ///children:
    ///  - id: Example
    ///    weight: 10
    /// </summary>
    [DataField("possibleInfants")]
    public EntityTableSelector? PossibleInfants = default!;
}
