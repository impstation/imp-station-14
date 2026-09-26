using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Tag;
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
    /// The compatible mobs that this can breed with defined through Tags
    /// </summary>
    [DataField]
    public HashSet<ProtoId<TagPrototype>> CompatibleBreeds = default!;

    /// <summary>
    /// Format the chosen animals like this within your YAML.
    /// This variable allows animals to have multiple offspring
    ///     possibleInfants: !type:GroupSelector
    ///children:
    ///  - id: Example
    ///    weight: 10
    /// </summary>
    [DataField]
    public EntityTableSelector? PossibleInfants = default!;
}
